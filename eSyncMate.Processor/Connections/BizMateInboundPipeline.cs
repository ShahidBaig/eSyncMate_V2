using System.Text;
using System.Text.Json;
using EdiEngine;
using EdiEngine.Runtime;
using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;

namespace eSyncMate.Processor.Connections
{
    /// <summary>What a route hands the pipeline about a document that has arrived from a partner.</summary>
    public sealed class InboundDocumentContext
    {
        /// <summary>The trading partner as eSyncMate knows it. Stable per partner.</summary>
        public string PartnerId { get; set; } = string.Empty;

        /// <summary>
        /// RawEDI: interchange or group control number. FlatFile/DBMap: the batch identifier.
        /// May be left empty for X12 - the pipeline reads ISA13 from the envelope it links to.
        /// </summary>
        public string PartnerControlNo { get; set; } = string.Empty;

        /// <summary>850, 856, ... Document Catalog codes.</summary>
        public string DocumentType { get; set; } = string.Empty;

        /// <summary>What the artifact actually is. The mechanism derives from it unless declared.</summary>
        public string Format { get; set; } = BizMateFormats.X12;

        /// <summary>
        /// Only set this for marketplace traffic, as <c>PartnerAPI</c>. BizMate cannot infer that
        /// mechanism - the artifact is JSON, indistinguishable from any other API push - so it has
        /// to be declared (D-31). Leave it null everywhere else and let the format decide, which is
        /// what stops a document being labelled by how it travelled internally rather than by how
        /// the partner actually sent it.
        /// </summary>
        public string? DeclaredMechanism { get; set; }

        /// <summary>Order or Consignment. Required for 856 and 810 (E8), never inferred.</summary>
        public string? Family { get; set; }

        public string Channel { get; set; } = BizMateChannels.Edi;

        /// <summary>How the row reached us: Wire, PreProcessor, ExternalWriter, Marketplace, Doorway.</summary>
        public string Provenance { get; set; } = "Wire";

        /// <summary>The artifact exactly as it crossed the wire.</summary>
        public byte[] RawContent { get; set; } = Array.Empty<byte>();

        public string ContentEncoding { get; set; } = "utf-8";
        public string? FileName { get; set; }
        public long? CustomerNo { get; set; }
        public int? CustomerId { get; set; }
        public int? RouteId { get; set; }
        public string? MapName { get; set; }
        public string? MapVersion { get; set; }
    }

    /// <summary>What the pipeline hands the translation step, after the record and the raw row exist.</summary>
    public sealed class InboundTranslationInput
    {
        /// <summary>The artifact exactly as it crossed the wire.</summary>
        public byte[] RawContent { get; set; } = Array.Empty<byte>();

        /// <summary>The same bytes decoded as text, which is what the X12 reader and the maps take.</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>The InboundEDI row the ledger links to. Null for non-X12 carriers.</summary>
        public InboundEDI? InboundEDI { get; set; }

        /// <summary>The first interchange's ISA/GS identity, for the 997 and the order. Null if the envelope did not parse.</summary>
        public InboundEDIInfo? FirstInterchange { get; set; }

        /// <summary>The ledger row, already saved. Read it; the pipeline owns writing it.</summary>
        public EDILedger Ledger { get; set; } = null!;
    }

    /// <summary>What the translation step gives back: the canonical payload and what it learned on the way.</summary>
    public sealed class InboundTranslationResult
    {
        /// <summary>The canonical document, as BizMate's schema for the document type expects it.</summary>
        public JsonElement Payload { get; set; }

        /// <summary>The eSyncMate order the document became, when the translation created one.</summary>
        public int? OrderId { get; set; }

        public string? MapName { get; set; }
        public string? MapVersion { get; set; }
    }

    /// <summary>
    /// The inbound path, in the order the requirements demand (W2-02, W1-06, W1-07, W2-08).
    ///
    /// The ordering is the point. The ledger row and the registered artifact both exist before any
    /// translation runs, so a document that dies in translation still dies visibly - which is the
    /// whole difference between "we never received it" and "it arrived and broke, here is why"
    /// (E10, E12). Every exit from this method leaves a ledger row that says what happened.
    ///
    /// Where the artifact lives (AD-02, 2026-09-08). eSyncMate already keeps every X12 interchange
    /// it receives in <c>InboundEDI</c>, with the ISA and GS identity per interchange in
    /// <c>InboundEDIInfo</c>, and <c>Orders.InboundEDIId</c> points to that row. So for X12 this
    /// pipeline writes the same rows the intake routes write today and the ledger LINKS to them;
    /// <c>EDILedgerArtifact</c> is used only for the flat-file and DB-map carriers, which have no
    /// such table. F-3 - "a document that never becomes an order cannot be retained" - was true of
    /// the marketplace JSON side only, and is retracted for X12 (F-14).
    /// </summary>
    public sealed class BizMateInboundPipeline
    {
        private readonly BizMateConnector _connector;
        private readonly string _connectionString;
        private readonly int _userNo;

        public BizMateInboundPipeline(BizMateConnector connector, string connectionString, int userNo)
        {
            _connector = connector ?? throw new ArgumentNullException(nameof(connector));
            _connectionString = connectionString;
            _userNo = userNo;
        }

        /// <summary>
        /// Runs one document through: record it, keep the artifact, register it with BizMate,
        /// translate, deliver.
        ///
        /// <paramref name="translate"/> turns the raw artifact into the canonical payload. It is a
        /// delegate rather than a dependency because every carrier and partner translates
        /// differently, and the ordering guarantee here must hold whichever one runs. For X12 the
        /// implementation is <c>OrderManager.ParseOrder</c> with the customer's
        /// <c>850 Transformation</c> map, with <c>SaveOrder</c> retained for the parallel run (W2-02) -
        /// see <c>BizMateInboundEDIRoute</c>. It receives the linked InboundEDI row and the envelope
        /// identity so the order it creates can reference them, and gives back the order it created.
        /// </summary>
        public async Task<EDILedger> ProcessAsync(
            InboundDocumentContext context,
            Func<InboundTranslationInput, InboundTranslationResult> translate,
            CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (translate is null) throw new ArgumentNullException(nameof(translate));

            string l_CorrelationId = NewCorrelationId();
            string l_Text = DecodeForTransport(context);
            InboundEDI? l_InboundEDI = null;
            InboundEDIInfo? l_FirstInterchange = null;

            // ---- 1. The record exists first. Nothing below this line can lose the document. ----
            EDILedger l_Ledger = CreateLedgerRow(context, l_CorrelationId);

            // ---- 2. The artifact, exactly as it crossed the wire (AD-02). ----
            // X12 goes where eSyncMate already keeps it and the ledger links to that row. Anything
            // else has no existing home and is kept, hashed, in EDILedgerArtifact.
            if (IsRawEdi(context.Format))
            {
                (l_InboundEDI, l_FirstInterchange) = LinkInboundEDI(l_Ledger, context, l_Text);
            }
            else
            {
                long l_ArtifactId = SaveArtifact(l_Ledger.Id, context, BizMateRequestSigner.HashBody(context.RawContent));
                l_Ledger.RawArtifactRef = "artifact:" + l_ArtifactId;
            }

            l_Ledger.Modify();

            // ---- 2a. Test traffic must not reach a production credential (W3-04). ----
            //
            // BizMate enforces nothing here: `testIndicator` is read by none of their inbound
            // handlers, so a document marked T that arrives on a production credential becomes a
            // real order with real stock committed against it. ISA15 is where the partner states
            // intent, and this is the boundary that reads it.
            //
            // After the ledger row and the artifact, deliberately. A refused document is one we
            // received and declined, and that is a fact worth keeping - dropping it before it was
            // recorded would make it look like it never arrived.
            string? l_Refusal = UsageRefusal(l_FirstInterchange);

            if (l_Refusal is not null)
            {
                return Fail(l_Ledger, "Rejected", l_Refusal);
            }

            // ---- 3. Register it with BizMate before anything is interpreted. ----
            try
            {
                RegisterRawFileResponse l_Raw = await _connector.RegisterRawAsync(
                    new RegisterRawFileRequest
                    {
                        Content = l_Text,
                        Format = context.Format,
                        Direction = "In",
                        FileName = context.FileName,
                        PartnerId = context.PartnerId,
                        ReceivedAt = l_Ledger.ReceivedAt
                    },
                    l_CorrelationId,
                    cancellationToken).ConfigureAwait(false);

                // BizMate's reference, kept apart from ours. The trace contract's rawArtifactRef is
                // "your reference to the original artifact ... BizMate does not dereference it", so
                // storing theirs in it - as the first revision did - answered the wrong question (F-21).
                l_Ledger.BizMateRawFileRef = l_Raw.RawFileRef;
                l_Ledger.Modify();
            }
            catch (BizMateException ex)
            {
                // BizMate could not take the artifact. The document is still recorded here, which is
                // the point - it is visible on our side even when the far side is unreachable.
                return Fail(l_Ledger, "Rejected", "Could not register the artifact with BizMate: " + ex.Message);
            }

            // ---- 4. Translate. A failure here is a first-class record, not a dropped file (W2-08). ----
            JsonElement l_Payload;

            try
            {
                InboundTranslationResult l_Result = translate(new InboundTranslationInput
                {
                    RawContent = context.RawContent,
                    Text = l_Text,
                    InboundEDI = l_InboundEDI,
                    FirstInterchange = l_FirstInterchange,
                    Ledger = l_Ledger
                });

                l_Payload = l_Result.Payload;
                l_Ledger.TranslatedAt = DateTime.UtcNow;
                l_Ledger.OrderId ??= l_Result.OrderId;
                l_Ledger.MapName = l_Result.MapName ?? l_Ledger.MapName;
                l_Ledger.MapVersion = l_Result.MapVersion ?? l_Ledger.MapVersion;
                l_Ledger.Modify();
            }
            catch (IntakeRefusedException ex)
            {
                // The map did its job and the document is readable; it broke a rule about what
                // BizMate can accept (W3-02). Recorded as the refusal it is rather than dressed up
                // as a translation that fell over, because the two need different people.
                return Fail(l_Ledger, ex.Outcome, ex.Message);
            }
            catch (Exception ex)
            {
                return Fail(l_Ledger, "Failed", "Translation failed: " + ex.Message);
            }

            // ---- 4a. Normalise what is representation rather than meaning (W3-01). ----
            // An X12 element that is absent and one that is present but empty are the same thing on
            // the wire, so "" is what a map naturally produces for anything the partner did not
            // send - and the canonical contract forbids it. Dropping those properties here, once,
            // is what stops every map for every document type having to remember. Nothing that
            // carries meaning is touched: see CanonicalValues.EmptyIsMeaningful.
            l_Payload = CanonicalValues.PruneEmpty(l_Payload);

            // ---- 4b. Check the canonical conventions before BizMate ever sees it (X-02, X-03). ----
            // The maps are text in a database row with no schema and no compiled type behind them
            // (F-1), so nothing else would notice a map regressing to MM/dd/yyyy or to money as a
            // string. Catching it here makes it a visible failure instead of a document BizMate
            // accepts and misreads.
            List<CanonicalViolation> l_Violations = CanonicalGuard.Inspect(l_Payload);

            if (l_Violations.Count > 0)
            {
                return Fail(l_Ledger, "Failed",
                    "The translated document breaks the canonical conventions: " +
                    string.Join("; ", l_Violations.Take(10).Select(v => v.ToString())) +
                    (l_Violations.Count > 10 ? $" (+{l_Violations.Count - 10} more)" : string.Empty));
            }

            // ---- 5. Deliver the canonical document. ----
            try
            {
                InboundDocumentResponse l_Response = await _connector.PostInboundAsync(
                    context.DocumentType,
                    new InboundDocumentRequest
                    {
                        RawFileRef = l_Ledger.BizMateRawFileRef ?? 0,
                        PartnerId = context.PartnerId,
                        PartnerControlNo = l_Ledger.PartnerControlNo ?? string.Empty,
                        Format = context.Format,
                        Mechanism = l_Ledger.Mechanism,
                        SourceFamily = context.Family,
                        Channel = context.Channel,
                        CustomerNo = context.CustomerNo,
                        // Our ledger id, so BizMate can cross-reference back to this row (E17).
                        SourceReference = l_Ledger.TransmissionReference,
                        Payload = l_Payload
                    },
                    l_CorrelationId,
                    cancellationToken).ConfigureAwait(false);

                l_Ledger.BizMateMessageId = l_Response.MessageId;

                // duplicate: true is a SUCCESS - BizMate already holds this document, which is what
                // leaning on its idempotency instead of de-duplicating defensively looks like (W1-15).
                l_Ledger.BizMateDuplicate = l_Response.Duplicate;
                l_Ledger.HandedToBizMateAt = DateTime.UtcNow;
                l_Ledger.Outcome = "Translated";
                l_Ledger.Modify();

                // Keep the raw row's own status truthful for the screens that already read it.
                SetInboundEDIStatus(l_Ledger, "PROCESSED");

                return l_Ledger;
            }
            catch (BizMateRefusedException ex)
            {
                // A governance decision - not enabled, not granted, suspended. Recorded, never retried.
                return Fail(l_Ledger, "Rejected", "BizMate refused the document: " + ex.Message);
            }
            catch (BizMateRequestException ex)
            {
                return Fail(l_Ledger, "Failed", "BizMate rejected the document: " + ex.Message);
            }
            catch (BizMateException ex)
            {
                // Transport or rate limit. The document is fine and the attempt is retryable, so the
                // row stays Pending for the store-and-forward queue to drain (W1-14).
                l_Ledger.ErrorDetail = "Delivery attempt failed, will retry: " + ex.Message;
                l_Ledger.Modify();

                return l_Ledger;
            }
        }

        /// <summary>
        /// Runs one inbound 997 through: record it, keep the artifact, register it, resolve what it
        /// acknowledges, and tell BizMate (W2-10, W1-12, E11).
        ///
        /// A 997 is not a business document, so it does not take the ordinary path: there is no
        /// canonical form to translate it into and nothing to post to /inbound. It is a verdict on
        /// something we already sent, and the work is entirely correlation.
        ///
        /// The same ledger-first ordering applies. The acknowledgment gets its own row - it IS a
        /// document, and EDILedgerLink joins two ledger rows, so it needs one to link FROM - and the
        /// row exists before any of it is interpreted.
        /// </summary>
        public async Task<EDILedger> ProcessAcknowledgementAsync(
            InboundDocumentContext context,
            CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            string l_CorrelationId = NewCorrelationId();
            string l_Text = DecodeForTransport(context);

            // ---- 1. The record exists first. ----
            EDILedger l_Ledger = CreateLedgerRow(context, l_CorrelationId);

            // ---- 2. The artifact, where eSyncMate already keeps X12. ----
            LinkInboundEDI(l_Ledger, context, l_Text);

            l_Ledger.Modify();

            // ---- 3. Register it with BizMate before anything is interpreted. ----
            try
            {
                RegisterRawFileResponse l_Raw = await _connector.RegisterRawAsync(
                    new RegisterRawFileRequest
                    {
                        Content = l_Text,
                        Format = context.Format,
                        Direction = "In",
                        FileName = context.FileName,
                        PartnerId = context.PartnerId,
                        ReceivedAt = l_Ledger.ReceivedAt
                    },
                    l_CorrelationId,
                    cancellationToken).ConfigureAwait(false);

                l_Ledger.BizMateRawFileRef = l_Raw.RawFileRef;
                l_Ledger.Modify();
            }
            catch (BizMateException ex)
            {
                return Fail(l_Ledger, "Rejected", "Could not register the acknowledgment with BizMate: " + ex.Message);
            }

            // ---- 4. Read it. ----
            AcknowledgementReading l_Reading;

            try
            {
                l_Reading = X12Ack.Read(l_Text);

                l_Ledger.TranslatedAt = DateTime.UtcNow;
                l_Ledger.MapName = "X12_004010.M_997";
                l_Ledger.Modify();
            }
            catch (Exception ex)
            {
                return Fail(l_Ledger, "Failed", "The acknowledgment could not be read: " + ex.Message);
            }

            if (l_Reading.Groups.Count == 0)
            {
                return Fail(l_Ledger, "Failed",
                    "The acknowledgment carries no AK1 group, so nothing in it says which transmission it answers.");
            }

            // ---- 5. Correlate each group, then tell BizMate. ----
            var l_Unresolved = new List<string>();
            int l_Resolved = 0;

            foreach (AcknowledgedGroup l_Group in l_Reading.Groups)
            {
                if (string.IsNullOrWhiteSpace(l_Group.GroupControlNo))
                {
                    l_Unresolved.Add("a group with no AK102 control number");

                    continue;
                }

                var l_Acknowledged = new EDILedger();

                l_Acknowledged.UseConnection(_connectionString);

                // Resolved on OUR control number, which under EQ-01 is the ledger row id.
                if (!l_Acknowledged.GetOutboundByInterchangeControlNo(context.PartnerId, l_Group.GroupControlNo!).IsSuccess)
                {
                    l_Unresolved.Add($"control number {l_Group.GroupControlNo} matches nothing we sent");

                    continue;
                }

                string l_Result = X12Ack.Result(l_Group.Status);
                string l_Detail = Detail(l_Group);

                l_Acknowledged.AcknowledgedAt = DateTime.UtcNow;

                if (l_Result != "Accepted")
                {
                    l_Acknowledged.ErrorDetail = $"The partner acknowledged this as {l_Result}: {l_Detail}";
                }

                l_Acknowledged.ModifiedBy = _userNo;
                l_Acknowledged.Modify();

                SaveLink(l_Ledger.Id, l_Acknowledged.Id, l_Result, l_Detail);

                l_Resolved++;

                // BizMate identifies its outbound message by ITS control number, not ours: the ack
                // contract asks for "the control number of BizMate's outbound transmission". Ours
                // resolved the row; theirs is what the row has echoed since it was collected.
                try
                {
                    AcknowledgementResponse l_Response = await _connector.PostAckAsync(
                        new AcknowledgementRequest
                        {
                            PartnerId = context.PartnerId,
                            PartnerControlNo = l_Acknowledged.PartnerControlNo ?? string.Empty,
                            AckResult = l_Result,
                            ReceivedAt = l_Ledger.ReceivedAt,
                            Detail = X12Render.Text(l_Detail, 2000),
                            RawFileRef = l_Ledger.BizMateRawFileRef
                        },
                        l_CorrelationId,
                        cancellationToken).ConfigureAwait(false);

                    l_Ledger.BizMateMessageId ??= l_Response.MessageId;
                    l_Ledger.HandedToBizMateAt = DateTime.UtcNow;
                    l_Ledger.Modify();
                }
                catch (BizMateException ex)
                {
                    // The correlation stands - it is ours and it is written. Only telling BizMate
                    // failed, and that is retryable, so the row stays Pending rather than Failed and
                    // the call goes to the store-and-forward queue for the drain to send again.
                    l_Ledger.ErrorDetail = "Correlated, but BizMate was not told: " + ex.Message
                        + QueueForRetry(ex, "PostAck", context.PartnerId, l_Ledger.Id);

                    l_Ledger.Modify();

                    return l_Ledger;
                }
            }

            if (l_Resolved == 0)
            {
                return Fail(l_Ledger, "Failed",
                    "Nothing this acknowledgment answers could be found: " + string.Join("; ", l_Unresolved));
            }

            if (l_Unresolved.Count > 0)
            {
                l_Ledger.ErrorDetail = "Partly correlated. Unmatched: " + string.Join("; ", l_Unresolved);
            }

            l_Ledger.Outcome = "Translated";
            l_Ledger.Modify();

            SetInboundEDIStatus(l_Ledger, "PROCESSED");

            return l_Ledger;
        }

        /// <summary>What the partner said, as one line, for the link row and for BizMate.</summary>
        private static string Detail(AcknowledgedGroup group)
        {
            var l_Parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(group.FunctionalCode))
            {
                l_Parts.Add("group " + group.FunctionalCode);
            }

            l_Parts.AddRange(group.Notes);

            return l_Parts.Count == 0 ? "no detail given" : string.Join("; ", l_Parts);
        }

        /// <summary>
        /// The link from the acknowledgment to what it acknowledges.
        ///
        /// Rejects rather than Acknowledges when the partner refused it: both are acknowledgments in
        /// the everyday sense, and the ledger keeps the difference because only one of them means the
        /// document has to be sent again.
        /// </summary>
        private void SaveLink(long fromLedgerId, long toLedgerId, string result, string detail)
        {
            var l_Link = new EDILedgerLink();

            l_Link.UseConnection(_connectionString);

            l_Link.FromLedgerId = fromLedgerId;
            l_Link.ToLedgerId = toLedgerId;
            l_Link.LinkType = result == "Rejected" ? "Rejects" : "Acknowledges";
            l_Link.ResolvedBy = "Automatic";
            l_Link.ResolvedOn = "AK102 group control number";
            l_Link.Detail = detail;
            l_Link.CreatedDate = DateTime.UtcNow;
            l_Link.CreatedBy = _userNo;

            l_Link.SaveNew();
        }


        /// <summary>
        /// Hands a failed call to the store-and-forward queue so the drain sends it again (W1-14).
        ///
        /// Only calls that TELL BizMate something already true are queued - the delivery
        /// confirmation and the acknowledgment. Those have no result the caller needs, so replaying
        /// one later changes nothing except that BizMate finally hears it.
        ///
        /// Deliberately NOT queued, and each for its own reason:
        ///   /raw and /inbound  - the caller needs the rawFileRef and the messageId to finish the
        ///                        ledger row, so a queued call that succeeded later would leave the
        ///                        row saying Pending forever while the queue said Succeeded.
        ///   /outbound/fetched  - W1-11's redelivery already reconciles a document we hold but
        ///                        BizMate does not know we hold; a second mechanism racing it would
        ///                        make which one won a matter of timing.
        ///   /partner-state     - queueing a report that BizMate is unreachable helps nobody, and it
        ///                        would sit behind the very backlog it describes.
        ///
        /// Returns what to record on the ledger, so the row says whether the message is safe or
        /// merely sent into the dark. Never throws: a queue that cannot accept the call must not
        /// turn a delivered document into a failed one.
        /// </summary>
        private string QueueForRetry(BizMateException failure, string operation, string partnerId, long ledgerId)
        {
            if (!failure.IsRetryable || failure.Call is null)
            {
                return string.Empty;
            }

            try
            {
                var l_Queue = new BizMateOutboundQueue(_connector, _connectionString, _userNo);

                l_Queue.Enqueue(
                    partnerId,
                    operation,
                    failure.Call.Method,
                    failure.Call.PublicPath,
                    failure.Call.PayloadJson,
                    failure.Call.Scope,
                    failure.Call.CorrelationId,
                    ledgerId,
                    failure.Call.UrlPathWithQuery);

                return " Queued for retry.";
            }
            catch (BizMateQueueFullException ex)
            {
                // The ceiling exists so a partner that has been down for days cannot fill the table
                // unboundedly. Said out loud on the row rather than swallowed.
                return " NOT queued: " + ex.Message;
            }
            catch (Exception ex)
            {
                return " NOT queued: " + ex.Message;
            }
        }

        /// <summary>
        /// Refuses an interchange whose ISA15 does not match what this instance's credential is for
        /// (W3-04), or null when there is nothing to refuse.
        ///
        /// Silent when the instance is unclassified or the envelope did not parse - both are cases
        /// where refusing would be guessing, and a guard that fires on a guess is one somebody turns
        /// off. An unparseable envelope already fails visibly a few lines further down.
        /// </summary>
        private static string? UsageRefusal(InboundEDIInfo? interchange)
        {
            string l_Expected = (CommonUtils.BizMate_UsageIndicator ?? string.Empty).Trim();

            if (l_Expected.Length == 0 || interchange is null)
            {
                return null;
            }

            string l_Actual = (interchange.ISAUsageIndicator ?? string.Empty).Trim();

            if (l_Actual.Length == 0 || string.Equals(l_Actual, l_Expected, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return $"ISA15 says [{l_Actual}] and this instance's BizMate credential is for [{l_Expected}]. "
                 + "Refused at the boundary: BizMate does not enforce the usage indicator, so a test document "
                 + "accepted on a production credential becomes a real order (W3-04).";
        }

        private static bool IsRawEdi(string format)
        {
            return format is BizMateFormats.X12 or BizMateFormats.EDIFACT;
        }

        private EDILedger CreateLedgerRow(InboundDocumentContext context, string correlationId)
        {
            var l_Ledger = new EDILedger();

            l_Ledger.UseConnection(_connectionString);

            l_Ledger.TransmissionReference = NewTransmissionReference();
            l_Ledger.PartnerId = context.PartnerId;
            l_Ledger.PartnerControlNo = context.PartnerControlNo;
            l_Ledger.CustomerNo = context.CustomerNo?.ToString();
            l_Ledger.CorrelationId = correlationId;
            l_Ledger.BizMateDuplicate = false;

            // Direction is from BizMate's point of view: a partner document coming to us is 'In'.
            l_Ledger.Direction = "In";
            l_Ledger.DocumentType = context.DocumentType;
            l_Ledger.Family = context.Family;
            l_Ledger.Format = context.Format;
            l_Ledger.Mechanism = BizMateFormats.ResolveMechanism(context.Format, context.DeclaredMechanism);
            l_Ledger.Channel = context.Channel;
            l_Ledger.Provenance = context.Provenance;

            l_Ledger.MapName = context.MapName;
            l_Ledger.MapVersion = context.MapVersion;
            l_Ledger.Outcome = "Pending";

            l_Ledger.ReceivedAt = DateTime.UtcNow;
            l_Ledger.CustomerId = context.CustomerId;
            l_Ledger.RouteId = context.RouteId;

            l_Ledger.CreatedDate = DateTime.UtcNow;
            l_Ledger.CreatedBy = _userNo;

            l_Ledger.SaveNew();

            return l_Ledger;
        }

        /// <summary>
        /// Writes the <c>InboundEDI</c> row and its <c>InboundEDIInfo</c> identity rows exactly as
        /// <c>RepaintGetOrderRoute</c> and the <c>Download*FromFTP</c> routes do, and links the
        /// ledger to them (AD-02).
        ///
        /// The raw row is written BEFORE the envelope is parsed and is kept even if parsing fails:
        /// a malformed 850 is precisely the document E12 exists to make visible, and the translation
        /// step will report the failure on the ledger. The envelope identity also settles two ledger
        /// facts the caller may not know: the interchange control number that actually crossed the
        /// wire, and - when the route did not supply one - the partner control number.
        /// </summary>
        private (InboundEDI, InboundEDIInfo?) LinkInboundEDI(EDILedger ledger, InboundDocumentContext context, string text)
        {
            InboundEDIInfo? l_FirstInfo = null;
            var l_Edi = new InboundEDI();

            l_Edi.UseConnection(_connectionString);

            l_Edi.Type = context.DocumentType;
            l_Edi.Status = "NEW";
            l_Edi.Data = text;
            l_Edi.CreatedBy = _userNo;
            l_Edi.CreatedDate = DateTime.Now;   // the intake routes write local time here; kept identical

            l_Edi.SaveNew();

            ledger.InboundEDIId = l_Edi.Id;
            ledger.RawArtifactRef = "inbound-edi:" + l_Edi.Id;

            // EDIFACT envelope identity arrives with the commercial component in W3. Until then the
            // raw row is still retained and linked; only the InboundEDIInfo rows are absent.
            if (context.Format != BizMateFormats.X12)
            {
                return (l_Edi, null);
            }

            try
            {
                EdiBatch l_Batch = new EdiDataReader().FromString(text);
                bool l_First = true;

                foreach (EdiInterchange i in l_Batch.Interchanges)
                {
                    string l_Isa13 = i.ISA.Content[12].ToString().Trim();

                    if (l_First)
                    {
                        ledger.InterchangeControlNo = l_Isa13;

                        if (string.IsNullOrWhiteSpace(ledger.PartnerControlNo))
                        {
                            ledger.PartnerControlNo = l_Isa13;
                        }

                        l_First = false;
                    }

                    foreach (EdiGroup g in i.Groups)
                    {
                        var l_Info = new InboundEDIInfo();

                        l_Info.UseConnection(_connectionString);

                        l_Info.InboundEDIId = l_Edi.Id;
                        l_Info.ISASenderQual = i.ISA.Content[4].Val.ToString().Trim();
                        l_Info.ISASenderId = i.ISA.Content[5].Val.ToString().Trim();
                        l_Info.ISAReceiverQual = i.ISA.Content[6].Val.ToString().Trim();
                        l_Info.ISAReceiverId = i.ISA.Content[7].Val.ToString().Trim();
                        l_Info.ISAEdiVersion = i.ISA.Content[11].ToString().Trim();
                        l_Info.ISAUsageIndicator = i.ISA.Content[14].ToString().Trim();
                        l_Info.ISAControlNumber = l_Isa13;
                        l_Info.SegmentSeparator = i.SegmentSeparator;
                        l_Info.ElementSeparator = i.ElementSeparator;
                        l_Info.GSSenderId = g.GS.Content[1].ToString().Trim();
                        l_Info.GSReceiverId = g.GS.Content[2].ToString().Trim();
                        l_Info.GSControlNumber = g.GS.Content[5].ToString().Trim();
                        l_Info.GSEdiVersion = g.GS.Content[7].ToString().Trim();
                        l_Info.CreatedBy = _userNo;
                        l_Info.CreatedDate = DateTime.Now;

                        l_Info.SaveNew();

                        l_FirstInfo ??= l_Info;
                    }
                }
            }
            catch (Exception)
            {
                // Unparseable envelope: the raw row stands, the identity rows do not, and the
                // translation step below fails visibly with the reason. Nothing is lost.
            }

            return (l_Edi, l_FirstInfo);
        }

        /// <summary>Non-X12 carriers: the file itself, hashed, in EDILedgerArtifact. Returns the artifact id.</summary>
        private long SaveArtifact(long ledgerId, InboundDocumentContext context, string contentHash)
        {
            var l_Artifact = new EDILedgerArtifact();

            l_Artifact.UseConnection(_connectionString);

            l_Artifact.LedgerId = ledgerId;
            l_Artifact.Stage = "AsReceived";
            l_Artifact.FormatLabel = context.Format;
            l_Artifact.ContentEncoding = context.ContentEncoding;
            l_Artifact.ContentHash = contentHash;
            l_Artifact.SizeBytes = context.RawContent.LongLength;
            l_Artifact.Content = context.RawContent;
            l_Artifact.CreatedDate = DateTime.UtcNow;
            l_Artifact.CreatedBy = _userNo;

            l_Artifact.SaveNew();

            return l_Artifact.Id;
        }

        /// <summary>
        /// Mirrors the ledger outcome onto the linked InboundEDI row's own Status, using the
        /// vocabulary the intake routes already use (NEW, PROCESSED, ERROR), so the existing
        /// screens keep telling the truth. Never allowed to disturb the ledger outcome.
        /// </summary>
        private void SetInboundEDIStatus(EDILedger ledger, string status)
        {
            if (!ledger.InboundEDIId.HasValue)
            {
                return;
            }

            try
            {
                var l_Edi = new InboundEDI();

                l_Edi.UseConnection(_connectionString);

                if (l_Edi.GetObject(ledger.InboundEDIId.Value).IsSuccess)
                {
                    l_Edi.Status = status;
                    l_Edi.Modify();
                }
            }
            catch (Exception)
            {
                // The ledger is the record; the mirror is a courtesy to older screens.
            }
        }

        /// <summary>
        /// Records the failure on the ledger and returns. A document that broke is registered with
        /// its reason rather than dropped, which is the whole of E12.
        /// </summary>
        private EDILedger Fail(EDILedger ledger, string outcome, string detail)
        {
            ledger.Outcome = outcome;

            // CK_EDILedger_ErrorDetail refuses a non-Translated row with no reason, so a silent
            // failure cannot reach the table even if a future caller forgets to say why.
            ledger.ErrorDetail = string.IsNullOrWhiteSpace(detail) ? "No reason recorded." : detail;

            ledger.Modify();

            SetInboundEDIStatus(ledger, "ERROR");

            return ledger;
        }

        /// <summary>
        /// The artifact as text, for BizMate's /raw and for InboundEDI.Data. Decoding here rather
        /// than at the call site keeps the hash over the raw bytes reproducible where an
        /// EDILedgerArtifact row is written (W2-03).
        /// </summary>
        private static string DecodeForTransport(InboundDocumentContext context)
        {
            try
            {
                return Encoding.GetEncoding(context.ContentEncoding).GetString(context.RawContent);
            }
            catch (ArgumentException)
            {
                return Encoding.UTF8.GetString(context.RawContent);
            }
        }

        /// <summary>
        /// eSyncMate's own id for the transmission, both directions. Shaped like the trace
        /// contract's example and unique without needing a sequence table.
        /// </summary>
        public static string NewTransmissionReference()
        {
            return "TX-" + DateTime.UtcNow.ToString("yyyy-MM-dd") + "-" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant();
        }

        /// <summary>One per logical document exchange, stable across retries of the same document (X-04).</summary>
        public static string NewCorrelationId()
        {
            return Guid.NewGuid().ToString("D");
        }
    }
}
