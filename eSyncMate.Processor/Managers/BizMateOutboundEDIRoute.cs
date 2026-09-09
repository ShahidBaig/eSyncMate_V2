using System.Text;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Connections;
using eSyncMate.Processor.Models;
using static eSyncMate.DB.Declarations;

namespace eSyncMate.Processor.Managers
{
    /// <summary>
    /// Route type 601 - BizMate's outbound documents out to the partner (W1-18; direction AD-03).
    ///
    /// The other half of the loop. Route 600 brings a partner's 850 in; this collects what BizMate
    /// stages in reply - the 855, 856 and 810 - renders each into the partner's format and puts it
    /// on the partner's transfer.
    ///
    /// Built as a route rather than a bare Hangfire job so it inherits what every other route in
    /// eSyncMate has: the execution lock, RouteLog, the Flow interface, test-run and per-partner
    /// scheduling. Nothing was calling BizMateOutboundPipeline before this.
    ///
    /// Direction of the connectors is the mirror of route 600:
    ///
    ///   SourceParty / SourceConnector            BizMate  (credentials come from ApplicationSettings,
    ///                                                     not from the connector row)
    ///   DestinationParty / DestinationConnector  the partner, whose Url is the folder we write into
    ///
    /// **A document type with no renderer is skipped, not fetched.** BizMate re-serves a Staged
    /// document on the next poll, so skipping costs nothing; fetching one we cannot render would
    /// mark it Fetched and push it onto the redelivery path for no reason. That is why the
    /// registry is consulted before ProcessAsync rather than inside the render delegate. The 856 and
    /// 865 renderers are built (W3-05, W3-11); the rest still report what is waiting and send
    /// nothing, which is the honest state of the outbound half.
    /// </summary>
    public class BizMateOutboundEDIRoute
    {
        /// <summary>Never take the whole outbox in one pass; the drain and the lock both have budgets.</summary>
        private const int MaxDocumentsPerRun = 100;

        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;

            try
            {
                route.SaveLog(LogTypeEnum.RouteInfo, $"[BizMateOutboundEDI] Started route [{route.Id}]", string.Empty, userNo);

                // Explicit, before anything consults the registry: what can be rendered decides
                // what gets fetched, so it must not depend on a static initialiser having run.
                BizMateRenderers.RegisterAll();

                ConnectorDataModel? l_Destination = ConnectorDataModel.Deserialize(route.DestinationConnectorObject.Data);

                if (l_Destination == null)
                {
                    route.SaveLog(LogTypeEnum.Error, "[BizMateOutboundEDI] The destination connector is not set up properly.", string.Empty, userNo);
                    return;
                }

                bool l_IsFolder = l_Destination.AuthType == ConnectorTypesEnum.File.ToString();
                bool l_IsSftp = l_Destination.AuthType == ConnectorTypesEnum.SFTP.ToString();

                if (!l_IsFolder && !l_IsSftp)
                {
                    route.SaveLog(LogTypeEnum.Error, $"[BizMateOutboundEDI] The destination connector is [{l_Destination.AuthType}]; this route writes File or SFTP.", string.Empty, userNo);
                    return;
                }

                // FileConnector writes into Url, falling back to BaseUrl. For a folder partner the
                // same connector serves both directions: BaseUrl is what route 600 reads, Url is
                // what this one writes.
                string l_Target = (l_IsFolder ? (l_Destination.Url ?? l_Destination.BaseUrl) : l_Destination.Host) ?? string.Empty;

                if (string.IsNullOrWhiteSpace(l_Target) || l_Target.TrimStart().StartsWith("<<"))
                {
                    route.SaveLog(LogTypeEnum.Error, "[BizMateOutboundEDI] The destination connector has no outbound folder or host set.", string.Empty, userNo);
                    return;
                }

                if (string.IsNullOrWhiteSpace(CommonUtils.BizMate_GatewayUrl)
                    || string.IsNullOrWhiteSpace(CommonUtils.BizMate_ClientId)
                    || string.IsNullOrWhiteSpace(CommonUtils.BizMate_ClientSecret)
                    || string.IsNullOrWhiteSpace(CommonUtils.BizMate_SigningSecret))
                {
                    route.SaveLog(LogTypeEnum.RouteInfo, "[BizMateOutboundEDI] Skipped: the BizMate gateway URL and credential trio are not set in ApplicationSettings.", string.Empty, userNo);
                    return;
                }

                // The partner. Same lookup as route 600 so the two agree about who this is.
                Customers l_Customer = new Customers();

                l_Customer.UseConnection(CommonUtils.ConnectionString);

                string l_PartnerKey = !string.IsNullOrWhiteSpace(l_Destination.Realm)
                    ? l_Destination.Realm.Trim()
                    : (route.CustomerName ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(l_PartnerKey) || !l_Customer.GetObject("ISACustomerID", l_PartnerKey).IsSuccess)
                {
                    route.SaveLog(LogTypeEnum.Error, $"[BizMateOutboundEDI] No customer carries ISACustomerID [{l_PartnerKey}].", string.Empty, userNo);
                    return;
                }

                string l_PartnerId = l_Customer.ERPCustomerID;

                var l_Connector = new BizMateConnector(
                    CommonUtils.BizMate_GatewayUrl,
                    CommonUtils.BizMate_ClientId,
                    CommonUtils.BizMate_ClientSecret,
                    CommonUtils.BizMate_SigningSecret);

                var l_Pipeline = new BizMateOutboundPipeline(l_Connector, CommonUtils.ConnectionString, userNo);
                string l_CorrelationId = BizMateInboundPipeline.NewCorrelationId();

                // ONE call, with redeliver=true.
                //
                // redeliver=true is a SUPERSET, not a separate set: the contract says it "ALSO
                // returns documents already Fetched but not yet Delivered or Acknowledged", so
                // asking for both and concatenating counts every Staged document twice - which is
                // exactly what the first live run did, reporting "147 collected of 71 staged".
                // One call gets the staged work and the crash-after-fetch recovery W1-11 wants.
                //
                // A re-served document records a Redelivered event on BizMate's timeline, which is
                // the audit trail W1-11 asked for. It stays quiet in practice because a document
                // with no renderer is never fetched, so it never enters the Fetched set.
                // Long-poll when configured (W1-09, E23). BizMate holds the request open and answers
                // the moment a document is staged, so an ASN's latency stops being "up to one poll
                // interval" - five minutes here - and becomes about as long as it takes them to
                // stage it. Zero keeps the old ask-and-be-told behaviour.
                //
                // The wait costs the route its execution lock for that long, which is why it is a
                // setting: the trade is latency against a route sitting idle holding a lock, and
                // that is an operational judgement rather than a code one.
                int? l_Wait = CommonUtils.BizMate_OutboundWaitSeconds > 0
                    ? Math.Min(CommonUtils.BizMate_OutboundWaitSeconds, 30)
                    : null;

                OutboundPage l_Page =
                    l_Pipeline.CollectRedeliveriesAsync(l_CorrelationId, l_PartnerId, l_Wait).GetAwaiter().GetResult();

                if (l_Page.Items.Count == 0)
                {
                    return;   // silence is the normal state of a polling route
                }

                // total is every row the call would serve regardless of limit, so it is the real
                // backlog for this partner - what X-07 reports to BizMate as QueueBacklog.
                route.SaveLog(LogTypeEnum.RouteInfo,
                    $"[BizMateOutboundEDI] {l_Page.Items.Count} document(s) on this page, {l_Page.Total} waiting in total for [{l_PartnerId}].",
                    string.Empty, userNo);

                int l_Sent = 0, l_Failed = 0, l_Pending = 0, l_Processed = 0;
                var l_SkippedByType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                foreach (OutboundDocument l_Document in l_Page.Items)
                {
                    // Ask BEFORE fetching. A document we cannot render stays Staged and BizMate
                    // serves it again next pass; fetching it first would mark it Fetched and put
                    // it on the redelivery path for nothing.
                    if (!BizMateDocumentRenderers.TryGet(l_Document.DocType, out var l_Renderer))
                    {
                        l_SkippedByType.TryGetValue(l_Document.DocType, out int l_Count);
                        l_SkippedByType[l_Document.DocType] = l_Count + 1;
                        continue;
                    }

                    // The budget counts documents actually WORKED, not documents looked at.
                    // Skipping is free, so a backlog of unrenderable types must not eat the run's
                    // allowance and starve the one type that can be sent.
                    if (l_Processed >= MaxDocumentsPerRun)
                    {
                        continue;
                    }

                    l_Processed++;

                    try
                    {
                        var l_Context = new OutboundRenderContext
                        {
                            Payload = l_Document.Payload,
                            DocumentType = l_Document.DocType,
                            PartnerId = l_Document.PartnerId,
                            PartnerControlNo = l_Document.PartnerControlNo,
                            Customer = l_Customer,
                            Family = l_Document.SourceFamily
                        };

                        EDILedger l_Ledger = l_Pipeline.ProcessAsync(
                            l_Document,
                            ledger =>
                            {
                                // The row exists by now, so the renderer can take its id as the
                                // interchange control number (EQ-01).
                                l_Context.Ledger = ledger;

                                return l_Renderer(l_Context);
                            },
                            (rendered, token) => TransmitAsync(rendered, l_Destination, l_Document, l_IsFolder))
                            .GetAwaiter().GetResult();

                        switch (l_Ledger.Outcome)
                        {
                            case "Translated":
                                l_Sent++;
                                route.SaveLog(LogTypeEnum.RouteInfo,
                                    $"[BizMateOutboundEDI] {l_Document.DocType} messageId {l_Document.MessageId} -> {l_Ledger.TransmissionReference}: delivered, control number {l_Ledger.InterchangeControlNo}.",
                                    string.Empty, userNo);
                                break;

                            case "Pending":
                                l_Pending++;
                                route.SaveLog(LogTypeEnum.RouteInfo,
                                    $"[BizMateOutboundEDI] {l_Document.DocType} messageId {l_Document.MessageId} -> {l_Ledger.TransmissionReference}: pending. {l_Ledger.ErrorDetail}",
                                    string.Empty, userNo);
                                break;

                            default:
                                l_Failed++;
                                route.SaveLog(LogTypeEnum.Error,
                                    $"[BizMateOutboundEDI] {l_Document.DocType} messageId {l_Document.MessageId} -> {l_Ledger.TransmissionReference}: {l_Ledger.Outcome}. {l_Ledger.ErrorDetail}",
                                    string.Empty, userNo);
                                break;
                        }
                    }
                    catch (Exception exDocument)
                    {
                        l_Failed++;
                        route.SaveLog(LogTypeEnum.Exception, $"[BizMateOutboundEDI] Error on messageId {l_Document.MessageId} ({l_Document.DocType})", exDocument.ToString(), userNo);
                    }
                }

                if (l_SkippedByType.Count > 0)
                {
                    string l_Waiting = string.Join(", ", l_SkippedByType.OrderBy(k => k.Key).Select(k => $"{k.Value} x {k.Key}"));
                    string l_Have = BizMateDocumentRenderers.Registered.Count == 0
                        ? "none are built yet"
                        : "built: " + string.Join(", ", BizMateDocumentRenderers.Registered.OrderBy(t => t));

                    // Once per run per type, not once per document: a backlog of 71 must not become
                    // 71 log rows saying the same thing.
                    //
                    // The message names no tracker items. It used to list four, and every one of
                    // them was wrong: W3-03 is line ordering, W3-07 the 856 Order-family map,
                    // W3-09 per-partner SSCC, and 860 is an INBOUND document that will never have
                    // an outbound renderer at all. Which types are covered is already answered
                    // truthfully by the registry, so let it answer.
                    route.SaveLog(LogTypeEnum.RouteInfo,
                        $"[BizMateOutboundEDI] Left staged in BizMate for want of a renderer - {l_Waiting}. Renderers {l_Have}. Nothing was fetched, so BizMate will serve them again.",
                        string.Empty, userNo);
                }

                route.SaveLog(LogTypeEnum.RouteInfo,
                    $"[BizMateOutboundEDI] Completed route [{route.Id}]: {l_Page.Items.Count} of {l_Page.Total} on this page, {l_Processed} worked ({l_Sent} delivered, {l_Pending} pending, {l_Failed} failed), {l_SkippedByType.Values.Sum()} awaiting a renderer.",
                    string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"[BizMateOutboundEDI] Error executing route [{route.Id}]", ex.ToString(), userNo);
            }
        }

        /// <summary>
        /// Puts the rendered document on the partner's transfer. Throwing means it did not arrive,
        /// which is what the pipeline needs to know: it records the failure and never marks the
        /// document delivered to BizMate.
        /// </summary>
        private static async Task TransmitAsync(
            RenderedDocument rendered, ConnectorDataModel destination, OutboundDocument document, bool isFolder)
        {
            string l_Name = rendered.FileName;

            if (string.IsNullOrWhiteSpace(l_Name))
            {
                string l_Reference = string.IsNullOrWhiteSpace(document.PartnerControlNo)
                    ? document.MessageId.ToString()
                    : document.PartnerControlNo!;

                l_Name = $"{document.DocType}_{document.PartnerId}_{l_Reference}.edi";
            }

            string l_Content = Encoding.UTF8.GetString(rendered.Content);

            if (isFolder)
            {
                // FileConnector writes through a temporary name and renames, so the partner never
                // reads a partial document out of the folder.
                await FileConnector.Execute(destination, false, l_Name, l_Content).ConfigureAwait(false);
                return;
            }

            // SftpConnector uploads into BaseUrl, so point a copy at the outbound folder rather
            // than mutating the connector the route is still holding.
            ConnectorDataModel l_Sftp = Newtonsoft.Json.JsonConvert.DeserializeObject<ConnectorDataModel>(
                Newtonsoft.Json.JsonConvert.SerializeObject(destination))!;

            l_Sftp.BaseUrl = string.IsNullOrWhiteSpace(destination.Url) ? destination.BaseUrl : destination.Url;

            await SftpConnector.Execute(l_Sftp, false, l_Name, l_Content).ConfigureAwait(false);
        }
    }
}
