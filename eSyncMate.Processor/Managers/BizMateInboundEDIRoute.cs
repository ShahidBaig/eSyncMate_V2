using System.Text;
using System.Text.Json;
using EdiEngine;
using EdiEngine.Runtime;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Connections;
using eSyncMate.Processor.Models;
using Newtonsoft.Json;
using static eSyncMate.DB.Declarations;

namespace eSyncMate.Processor.Managers
{
    /// <summary>
    /// Route type 600 - partner X12 in, BizMate canonical out (W1-01, W2-02, W7-11; direction AD-03).
    ///
    /// Reads the partner's inbound folder - an ordinary Windows folder that BizLink writes into,
    /// or SFTP where a partner needs it - and for each file runs eSyncMate's existing 850 intake
    /// WRAPPED in the BizMate ledger-first pipeline:
    ///
    ///   1. ledger row                                  (BizMateInboundPipeline)
    ///   2. InboundEDI + InboundEDIInfo, linked         (BizMateInboundPipeline, AD-02)
    ///   3. POST /raw to BizMate                        (BizMateInboundPipeline)
    ///   4. OrderManager.ParseOrder + SaveOrder         (here - the same call RepaintGetOrderRoute
    ///                                                   and process850 make, so Orders/OrderDetail/
    ///                                                   OrderData keep working through the parallel
    ///                                                   run) and the 997 back to the partner
    ///   5. POST /inbound/850 with the canonical JSON   (BizMateInboundPipeline)
    ///   6. Orders.Status -> SYNCED, ExternalId = BizMate messageId, mirroring what SyncOrder does
    ///      when the ERP is SPARS - here the ERP is BizMate and the post above IS the sync.
    ///
    /// The route matches the partner the way the existing intake does: the source connector's
    /// Realm is the ISA sender id it insists on, and that string is the Customers.ISACustomerID it
    /// looks the customer up by. Under the M1 contract that same string is BizMate's partnerId.
    ///
    /// Everything this route logs as a milestone is RouteInfo, because SaveLog drops Info (F-15).
    /// </summary>
    public class BizMateInboundEDIRoute
    {
        private const string TransformationMapType = "850 Transformation";
        private const string DbFieldsMapType = "850 DB Fields";

        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;

            try
            {
                route.SaveLog(LogTypeEnum.RouteInfo, $"[BizMateInboundEDI] Started route [{route.Id}]", string.Empty, userNo);

                ConnectorDataModel? l_Source = ConnectorDataModel.Deserialize(route.SourceConnectorObject.Data);

                if (l_Source == null)
                {
                    route.SaveLog(LogTypeEnum.Error, "[BizMateInboundEDI] The source connector is not set up properly.", string.Empty, userNo);
                    return;
                }

                // File is the default: BizLink hands eSyncMate its EDI as files in an ordinary
                // Windows folder. SFTP is supported for the partners that need it.
                bool l_IsFolder = l_Source.AuthType == ConnectorTypesEnum.File.ToString();
                bool l_IsSftp = l_Source.AuthType == ConnectorTypesEnum.SFTP.ToString();

                if (!l_IsFolder && !l_IsSftp)
                {
                    route.SaveLog(LogTypeEnum.Error, $"[BizMateInboundEDI] The source connector is [{l_Source.AuthType}]; this route reads File or SFTP.", string.Empty, userNo);
                    return;
                }

                string l_Location = (l_IsFolder ? l_Source.BaseUrl : l_Source.Host) ?? string.Empty;

                if (string.IsNullOrWhiteSpace(l_Location) || l_Location.TrimStart().StartsWith("<<"))
                {
                    route.SaveLog(LogTypeEnum.Error,
                        l_IsFolder
                            ? "[BizMateInboundEDI] The source connector has no inbound folder. Set BaseUrl on the connector to the folder BizLink writes into."
                            : "[BizMateInboundEDI] The source connector (partner SFTP) has no host.",
                        string.Empty, userNo);
                    return;
                }

                if (string.IsNullOrWhiteSpace(l_Source.Realm))
                {
                    route.SaveLog(LogTypeEnum.Error, "[BizMateInboundEDI] The source connector has no Realm (the ISA sender id to expect).", string.Empty, userNo);
                    return;
                }

                if (string.IsNullOrWhiteSpace(CommonUtils.BizMate_GatewayUrl)
                    || string.IsNullOrWhiteSpace(CommonUtils.BizMate_ClientId)
                    || string.IsNullOrWhiteSpace(CommonUtils.BizMate_ClientSecret)
                    || string.IsNullOrWhiteSpace(CommonUtils.BizMate_SigningSecret))
                {
                    route.SaveLog(LogTypeEnum.RouteInfo, "[BizMateInboundEDI] Skipped: the BizMate gateway URL and credential trio are not set in ApplicationSettings.", string.Empty, userNo);
                    return;
                }

                // The partner, found the way the existing intake finds it. GetObject by ISACustomerID
                // also loads the customer's maps.
                Customers l_Customer = new Customers();

                l_Customer.UseConnection(CommonUtils.ConnectionString);

                if (!l_Customer.GetObject("ISACustomerID", l_Source.Realm.Trim()).IsSuccess)
                {
                    route.SaveLog(LogTypeEnum.Error, $"[BizMateInboundEDI] No customer carries ISACustomerID [{l_Source.Realm.Trim()}]. Run the W7-11 seed (Tasks/00003/06).", string.Empty, userNo);
                    return;
                }

                string l_TransformationMap = l_Customer.Maps?.FirstOrDefault(p => p.MapTypeName == TransformationMapType)?.Map ?? string.Empty;
                string l_DbFieldsMap = l_Customer.Maps?.FirstOrDefault(p => p.MapTypeName == DbFieldsMapType)?.Map ?? string.Empty;

                if (string.IsNullOrEmpty(l_TransformationMap) || string.IsNullOrEmpty(l_DbFieldsMap))
                {
                    route.SaveLog(LogTypeEnum.Error, $"[BizMateInboundEDI] Required maps for 850 processing are missing for [{l_Customer.Name}] ({TransformationMapType}, {DbFieldsMapType}).", string.Empty, userNo);
                    return;
                }

                Dictionary<string, string> l_Files;

                try
                {
                    l_Files = l_IsFolder
                        ? FileConnector.Execute(l_Source).GetAwaiter().GetResult()
                        : SftpConnector.Execute(l_Source).GetAwaiter().GetResult();
                }
                catch (DirectoryNotFoundException ex)
                {
                    route.SaveLog(LogTypeEnum.Error, $"[BizMateInboundEDI] {ex.Message}", string.Empty, userNo);
                    return;
                }

                if (l_Files.Count == 0)
                {
                    // Silence is the normal state of a polling route; nothing to record. A file
                    // BizLink is still writing is not counted here either - FileConnector leaves it
                    // for the next pass rather than reading a fragment.
                    return;
                }

                var l_Connector = new BizMateConnector(
                    CommonUtils.BizMate_GatewayUrl,
                    CommonUtils.BizMate_ClientId,
                    CommonUtils.BizMate_ClientSecret,
                    CommonUtils.BizMate_SigningSecret);

                var l_Pipeline = new BizMateInboundPipeline(l_Connector, CommonUtils.ConnectionString, userNo);

                int l_Ok = 0, l_Pending = 0, l_Failed = 0;

                foreach (KeyValuePair<string, string> l_File in l_Files)
                {
                    try
                    {
                        // What the file says it is, before assuming. Everything BizLink drops in this
                        // folder used to be treated as an 850, which is right until the partner
                        // answers one - a 997 has no canonical form and nothing to post to /inbound,
                        // so it takes the correlation path instead (W2-10).
                        bool l_IsAck = X12Ack.TransactionSetId(l_File.Value) == X12Ack.DocumentType;

                        var l_Context = new InboundDocumentContext
                        {
                            PartnerId = l_Customer.ERPCustomerID,
                            DocumentType = l_IsAck ? X12Ack.DocumentType : "850",
                            Format = BizMateFormats.X12,
                            Channel = BizMateChannels.Edi,
                            Provenance = "Wire",
                            RawContent = Encoding.UTF8.GetBytes(l_File.Value),
                            ContentEncoding = "utf-8",
                            FileName = l_File.Key,
                            CustomerId = l_Customer.Id,
                            RouteId = route.Id,
                            MapName = l_IsAck ? "X12_004010.M_997" : TransformationMapType
                        };

                        var l_Pending997 = new PendingAcknowledgement();

                        EDILedger l_Ledger = l_IsAck
                            ? l_Pipeline.ProcessAcknowledgementAsync(l_Context).GetAwaiter().GetResult()
                            : l_Pipeline.ProcessAsync(
                                l_Context,
                                input => Translate(input, l_Customer, l_TransformationMap, l_DbFieldsMap, l_Source, l_File.Key, route, userNo, l_Pending997))
                                .GetAwaiter().GetResult();

                        // The document is BizMate's now, so the partner can be told we have it.
                        if (l_Ledger.Outcome == "Translated")
                        {
                            SendAcknowledgement(l_Pending997, l_Source, l_File.Key, route, userNo);
                        }

                        bool l_Handled = l_Ledger.Outcome == "Translated";

                        // The interchange is retained in InboundEDI and described on the ledger before
                        // this point, so the transfer's copy can go regardless of outcome - that is what
                        // ledger-first buys. A folder keeps it, filed under archive or error; SFTP has
                        // nowhere to file it, so it is deleted as the existing routes do.
                        if (l_IsFolder)
                        {
                            if (!FileConnector.ArchiveFile(l_File.Key, l_Source, l_Handled).GetAwaiter().GetResult())
                            {
                                route.SaveLog(LogTypeEnum.Error,
                                    $"[BizMateInboundEDI] Could not file [{l_File.Key}] into {(l_Handled ? FileConnector.ArchiveFolderName : FileConnector.ErrorFolderName)}. " +
                                    "If it is still in the inbound folder it will be read again next pass; BizMate answers a repeat with duplicate=true, so nothing is duplicated, but the folder needs attention.",
                                    string.Empty, userNo);
                            }
                        }
                        else
                        {
                            SftpConnector.DeleteFileFromSFTP(l_File.Key, l_Source).GetAwaiter().GetResult();
                        }

                        switch (l_Ledger.Outcome)
                        {
                            case "Translated":
                                l_Ok++;
                                MarkOrderSynced(l_Ledger);
                                route.SaveLog(LogTypeEnum.RouteInfo,
                                    $"[BizMateInboundEDI] {l_File.Key}{(l_IsAck ? " (997)" : string.Empty)} -> {l_Ledger.TransmissionReference}: BizMate messageId {l_Ledger.BizMateMessageId}" +
                                    (l_Ledger.BizMateDuplicate ? " (duplicate - already held)" : string.Empty) +
                                    (l_Ledger.OrderId.HasValue ? $", order {l_Ledger.OrderId}" : string.Empty),
                                    string.Empty, userNo);
                                break;

                            case "Pending":
                                l_Pending++;
                                route.SaveLog(LogTypeEnum.RouteInfo,
                                    $"[BizMateInboundEDI] {l_File.Key} -> {l_Ledger.TransmissionReference}: pending. {l_Ledger.ErrorDetail}",
                                    string.Empty, userNo);
                                break;

                            default:
                                l_Failed++;
                                route.SaveLog(LogTypeEnum.Error,
                                    $"[BizMateInboundEDI] {l_File.Key} -> {l_Ledger.TransmissionReference}: {l_Ledger.Outcome}. {l_Ledger.ErrorDetail}",
                                    string.Empty, userNo);
                                break;
                        }
                    }
                    catch (Exception exFile)
                    {
                        l_Failed++;
                        route.SaveLog(LogTypeEnum.Exception, $"[BizMateInboundEDI] Error processing file [{l_File.Key}]", exFile.ToString(), userNo);
                    }
                }

                route.SaveLog(LogTypeEnum.RouteInfo,
                    $"[BizMateInboundEDI] Completed route [{route.Id}]: {l_Files.Count} file(s), {l_Ok} handed to BizMate, {l_Pending} pending, {l_Failed} failed.",
                    string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"[BizMateInboundEDI] Error executing route [{route.Id}]", ex.ToString(), userNo);
            }
        }

        /// <summary>
        /// The translation step the pipeline calls between registering the raw file and posting the
        /// document. It is eSyncMate's existing 850 intake: ParseOrder with the customer's maps, then
        /// SaveOrder so the order tables keep being written through the parallel run, then the 997
        /// the partner expects. Anything thrown here becomes a Failed ledger row with the reason.
        /// </summary>
        private static InboundTranslationResult Translate(
            InboundTranslationInput input, Customers customer, string transformationMap, string dbFieldsMap,
            ConnectorDataModel source, string fileName, Routes route, int userNo, PendingAcknowledgement pending)
        {
            EdiBatch l_Batch = new EdiDataReader().FromString(input.Text);
            EdiTrans? l_Trans = null;
            int l_Count = 0;

            foreach (EdiInterchange i in l_Batch.Interchanges)
            {
                string l_Sender = i.ISA.Content[5].Val.ToString().Trim();

                // The existing intake silently skips an interchange from another sender. For BizMate
                // that would be a document that vanished, so it is a visible failure instead.
                if (l_Sender != source.Realm.Trim())
                {
                    throw new InvalidOperationException($"ISA sender [{l_Sender}] is not the expected [{source.Realm.Trim()}] for this route.");
                }

                foreach (EdiGroup g in i.Groups)
                {
                    if (g.GS.Content[0].ToString().Trim() != "PO")
                    {
                        continue;
                    }

                    foreach (EdiTrans t in g.Transactions)
                    {
                        l_Count++;
                        l_Trans ??= t;
                    }
                }
            }

            if (l_Trans == null)
            {
                throw new InvalidOperationException("The interchange carries no PO (850) transaction set.");
            }

            if (l_Count > 1)
            {
                // One ledger row is one document, and BizMate's /inbound takes one document per call.
                // Splitting a multi-set interchange into one ledger row per set is W2-05; until then
                // refuse the whole file rather than post the first set and lose the rest.
                throw new InvalidOperationException($"The interchange carries {l_Count} transaction sets; this route posts one document per file (W2-05). Split the file.");
            }

            OrderTransformationResponseModel l_Parsed = OrderManager.ParseOrder(customer, l_Trans, transformationMap, dbFieldsMap);

            if (l_Parsed.Code != (int)ResponseCodes.Success)
            {
                throw new InvalidOperationException("ParseOrder failed: " + (l_Parsed.Message ?? "no detail"));
            }

            l_Parsed.EDI = input.Text;
            l_Parsed.SystemUser = userNo;

            if (input.InboundEDI == null)
            {
                throw new InvalidOperationException("No InboundEDI row was linked to the ledger; the order cannot reference its interchange.");
            }

            // The intake rules that sit beyond the schema (W3-02), before the order is written, so a
            // refused document leaves no order, no lines and no half-state for somebody to unpick.
            // A rule names itself in the ledger's ErrorDetail: "shipTo needs an identifier" is
            // something an operator can act on, where BizMate refusing the document later is not.
            using (JsonDocument l_Canonical = JsonDocument.Parse(l_Parsed.JSON))
            {
                List<IntakeViolation> l_Broken = Canonical850Rules.Inspect(l_Canonical.RootElement);

                if (l_Broken.Count > 0)
                {
                    throw new IntakeRefusedException(
                        "The order breaks BizMate's 850 intake rules: "
                        + string.Join("; ", l_Broken.Take(10).Select(v => v.ToString()))
                        + (l_Broken.Count > 10 ? $" (+{l_Broken.Count - 10} more)" : string.Empty),
                        "Failed");
                }

                string? l_Duplicate = Canonical850Rules.FindDuplicateOrder(
                    CommonUtils.ConnectionString,
                    customer.Id,
                    Canonical850Rules.PoNumber(l_Canonical.RootElement),
                    input.Ledger?.PartnerControlNo,
                    input.Ledger?.Id ?? 0);

                if (l_Duplicate != null)
                {
                    // Rejected, not Failed: nothing is wrong with the document, and sending it again
                    // will not help. It is a decision not to create a second order for one PO.
                    throw new IntakeRefusedException(l_Duplicate, "Rejected");
                }
            }

            // Parallel run: Orders, OrderDetail and OrderData are written exactly as before (AD-02).
            OrderSaveResponseModel l_Saved = OrderManager.SaveOrder(input.InboundEDI, customer, l_Parsed);

            if (l_Saved.Code != (int)ResponseCodes.Success)
            {
                throw new InvalidOperationException("SaveOrder failed: " + (l_Saved.Message ?? "no detail"));
            }

            // The 997 is NOT sent here. It is held until the document is known to be good.
            //
            // A functional acknowledgment tells the partner we have their interchange and will act
            // on it. Sending it the moment an order row exists said that too early: the canonical
            // guard had not run, the intake rules had not run, and BizMate had not been given the
            // document - so an order eSyncMate went on to refuse, or one BizMate never received,
            // had already been acknowledged. The route sends it after the ledger says Translated,
            // which is the point at which the document has actually been handed over.
            //
            // Only on the EDI carrier (W2-11). A flat file, a DB map or a marketplace push has no
            // interchange to acknowledge and nobody waiting for one, so producing a 997 there would
            // invent a conversation that never happened. FirstInterchange is populated only for raw
            // EDI, so this already held by construction - it is written as its own condition so it
            // keeps holding when a carrier is added.
            //
            // An inbound 997 never reaches this method: the route reads ST01 first and sends it
            // down the correlation path, so eSyncMate does not acknowledge an acknowledgment.
            if (input.Ledger?.Mechanism == BizMateMechanisms.RawEdi
                && input.FirstInterchange != null
                && !string.IsNullOrWhiteSpace(source.Url))
            {
                pending.Interchange = input.FirstInterchange;
                pending.Saved = l_Saved;
                pending.Transaction = l_Trans;
            }

            using JsonDocument l_Doc = JsonDocument.Parse(l_Parsed.JSON);

            return new InboundTranslationResult
            {
                Payload = l_Doc.RootElement.Clone(),
                OrderId = l_Saved.OrderId,
                MapName = TransformationMapType
            };
        }

        /// <summary>
        /// What a 997 needs, held between translating a document and knowing it was accepted.
        ///
        /// Empty means no acknowledgment is owed - the document was not raw EDI, the partner has no
        /// return transfer configured, or translation never got far enough to know.
        /// </summary>
        private sealed class PendingAcknowledgement
        {
            public InboundEDIInfo? Interchange { get; set; }
            public OrderSaveResponseModel? Saved { get; set; }
            public EdiTrans? Transaction { get; set; }

            public bool IsOwed => Interchange != null && Saved != null && Transaction != null;
        }

        /// <summary>
        /// The 997 back to the partner, once the document has actually been handed to BizMate.
        ///
        /// Its failure is logged, not fatal: the order exists, BizMate has the document, and a
        /// missing acknowledgment is a smaller problem than pretending the document failed.
        /// </summary>
        private static void SendAcknowledgement(
            PendingAcknowledgement pending, ConnectorDataModel source, string fileName, Routes route, int userNo)
        {
            if (!pending.IsOwed)
            {
                return;
            }

            try
            {
                string l_997 = RepaintGetOrderRoute.Generate997(pending.Interchange!, pending.Saved!, pending.Transaction!);

                if (string.IsNullOrEmpty(l_997))
                {
                    return;
                }

                if (source.AuthType == ConnectorTypesEnum.File.ToString())
                {
                    // FileConnector writes into Url and renames into place, so BizLink never sees a
                    // partial acknowledgement.
                    FileConnector.Execute(source, false, $"{Path.GetFileNameWithoutExtension(fileName)}-997.edi", l_997)
                        .GetAwaiter().GetResult();
                }
                else
                {
                    ConnectorDataModel l_Outbound = JsonConvert.DeserializeObject<ConnectorDataModel>(JsonConvert.SerializeObject(source))!;
                    l_Outbound.BaseUrl = source.Url;

                    SftpConnector.Execute(l_Outbound, false, $"{Path.GetFileNameWithoutExtension(fileName)}-997", l_997).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex997)
            {
                route.SaveLog(LogTypeEnum.Error, $"[BizMateInboundEDI] 997 for [{fileName}] was not delivered to the partner", ex997.Message, userNo);
            }
        }

        /// <summary>
        /// What SyncOrder does for the SPARS ERP, done for BizMate: the order is synced, and the
        /// external id is BizMate's messageId. Best effort - the ledger already holds the truth.
        /// </summary>
        private static void MarkOrderSynced(EDILedger ledger)
        {
            if (!ledger.OrderId.HasValue)
            {
                return;
            }

            try
            {
                Orders l_Order = new Orders();

                l_Order.UseConnection(CommonUtils.ConnectionString);

                if (l_Order.GetObject(ledger.OrderId.Value).IsSuccess)
                {
                    l_Order.Status = "SYNCED";
                    l_Order.ExternalId = ledger.BizMateMessageId?.ToString() ?? l_Order.ExternalId;
                    l_Order.Modify();
                }
            }
            catch (Exception)
            {
                // Mirror only; the ledger is the record.
            }
        }
    }
}
