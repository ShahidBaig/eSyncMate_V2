using System.Text;
using System.Text.Json;
using eSyncMate.DB;
using eSyncMate.DB.Entities;

namespace eSyncMate.Processor.Connections
{
    /// <summary>What a route hands the pipeline about a document that has arrived from a partner.</summary>
    public sealed class InboundDocumentContext
    {
        /// <summary>The trading partner as eSyncMate knows it. Stable per partner.</summary>
        public string PartnerId { get; set; } = string.Empty;

        /// <summary>RawEDI: interchange or group control number. FlatFile/DBMap: the batch identifier.</summary>
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

    /// <summary>
    /// The inbound path, in the order the requirements demand (W2-02, W1-06, W1-07, W2-08).
    ///
    /// The ordering is the point. The ledger row and the registered artifact both exist before any
    /// translation runs, so a document that dies in translation still dies visibly - which is the
    /// whole difference between "we never received it" and "it arrived and broke, here is why"
    /// (E10, E12). Every exit from this method leaves a ledger row that says what happened.
    ///
    /// It also closes the gap F-3 found: retention today hangs off OrderData, whose OrderId is NOT
    /// NULL, so a document that never becomes an order has nowhere to live. Nothing here needs an
    /// order to exist.
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
        /// Runs one document through: record it, register the artifact, translate, deliver.
        ///
        /// <paramref name="translate"/> turns the raw artifact into the canonical payload. It is a
        /// delegate rather than a dependency because every carrier and partner translates
        /// differently, and the ordering guarantee here must hold whichever one runs.
        /// </summary>
        public async Task<EDILedger> ProcessAsync(
            InboundDocumentContext context,
            Func<byte[], JsonElement> translate,
            CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (translate is null) throw new ArgumentNullException(nameof(translate));

            string l_CorrelationId = NewCorrelationId();

            // ---- 1. The record exists first. Nothing below this line can lose the document. ----
            EDILedger l_Ledger = CreateLedgerRow(context, l_CorrelationId);

            // ---- 2. The artifact, exactly as it crossed the wire, with its hash. ----
            SaveArtifact(l_Ledger.Id, context, BizMateRequestSigner.HashBody(context.RawContent));

            // ---- 3. Register it with BizMate before anything is interpreted. ----
            try
            {
                RegisterRawFileResponse l_Raw = await _connector.RegisterRawAsync(
                    new RegisterRawFileRequest
                    {
                        Content = DecodeForTransport(context),
                        Format = context.Format,
                        Direction = "In",
                        FileName = context.FileName,
                        PartnerId = context.PartnerId,
                        ReceivedAt = l_Ledger.ReceivedAt
                    },
                    l_CorrelationId,
                    cancellationToken).ConfigureAwait(false);

                l_Ledger.RawArtifactRef = l_Raw.RawFileRef.ToString();
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
                l_Payload = translate(context.RawContent);
                l_Ledger.TranslatedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                return Fail(l_Ledger, "Failed", "Translation failed: " + ex.Message);
            }

            // ---- 5. Deliver the canonical document. ----
            try
            {
                InboundDocumentResponse l_Response = await _connector.PostInboundAsync(
                    context.DocumentType,
                    new InboundDocumentRequest
                    {
                        RawFileRef = ParseRef(l_Ledger.RawArtifactRef),
                        PartnerId = context.PartnerId,
                        PartnerControlNo = context.PartnerControlNo,
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

        private void SaveArtifact(long ledgerId, InboundDocumentContext context, string contentHash)
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
        }

        /// <summary>
        /// Records the failure on the ledger and returns. A document that broke is registered with
        /// its reason rather than dropped, which is the whole of E12.
        /// </summary>
        private static EDILedger Fail(EDILedger ledger, string outcome, string detail)
        {
            ledger.Outcome = outcome;

            // CK_EDILedger_ErrorDetail refuses a non-Translated row with no reason, so a silent
            // failure cannot reach the table even if a future caller forgets to say why.
            ledger.ErrorDetail = string.IsNullOrWhiteSpace(detail) ? "No reason recorded." : detail;

            ledger.Modify();

            return ledger;
        }

        /// <summary>
        /// BizMate's /raw takes the artifact as text. Decoding here rather than at the call site
        /// keeps the stored artifact as raw bytes, so its hash stays reproducible (W2-03).
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

        private static long ParseRef(string? rawArtifactRef)
        {
            return long.TryParse(rawArtifactRef, out long l_Ref) ? l_Ref : 0;
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
