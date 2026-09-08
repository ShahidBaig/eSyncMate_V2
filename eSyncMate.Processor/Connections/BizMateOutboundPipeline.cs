using System.Text;
using System.Text.Json;
using eSyncMate.DB.Entities;

namespace eSyncMate.Processor.Connections
{
    /// <summary>
    /// What a route produced from a canonical document, ready to go to the partner.
    /// </summary>
    public sealed class RenderedDocument
    {
        /// <summary>The artifact exactly as it will cross the wire.</summary>
        public byte[] Content { get; set; } = Array.Empty<byte>();

        public string ContentEncoding { get; set; } = "utf-8";

        /// <summary>What the artifact actually is. Mechanism derives from it, never chosen separately.</summary>
        public string Format { get; set; } = BizMateFormats.X12;

        /// <summary>
        /// The control number eSyncMate put in the envelope. Required on RawEDI, because the
        /// partner's 997 will quote it and W2-10 has nothing to correlate on without it.
        /// </summary>
        public string? InterchangeControlNo { get; set; }

        /// <summary>
        /// The eSyncMate order this document belongs to, when the renderer resolved one (an 855,
        /// 856 or 810 answers a specific 850). With it, an X12 rendering is also written to
        /// <c>OutboundEDI</c>, the table the existing 855/856/810 generators write, and the ledger
        /// links to that row (AD-02). Without it - an 846 inventory feed, an 824 - the rendering is
        /// kept in EDILedgerArtifact alone, because <c>OutboundEDI.orderId</c> is NOT NULL (F-22).
        /// </summary>
        public int? OrderId { get; set; }

        public string? MapName { get; set; }
        public string? MapVersion { get; set; }
        public string? FileName { get; set; }
    }

    /// <summary>
    /// The outbound path: collect from BizMate's outbox, render, transmit, confirm
    /// (W1-08, W1-10, W1-11, W2-02, W2-03).
    ///
    /// Fetched and Delivered stay distinct facts throughout, because they answer different
    /// questions - "did eSyncMate take it" and "did the partner receive it" - and collapsing them
    /// loses the crash-after-fetch case entirely.
    ///
    /// The same ledger-first rule as inbound applies here: the row and the artifact exist before
    /// the transmission is attempted, so a document that fails on the way to the partner is a
    /// visible record rather than an absence. The as-sent artifact is always kept, byte-exact and
    /// hashed, in EDILedgerArtifact; where the document belongs to an order, the X12 rendering is
    /// also written to OutboundEDI and linked, so the existing order screens see it (AD-02).
    /// </summary>
    public sealed class BizMateOutboundPipeline
    {
        private readonly BizMateConnector _connector;
        private readonly string _connectionString;
        private readonly int _userNo;

        public BizMateOutboundPipeline(BizMateConnector connector, string connectionString, int userNo)
        {
            _connector = connector ?? throw new ArgumentNullException(nameof(connector));
            _connectionString = connectionString;
            _userNo = userNo;
        }

        /// <summary>
        /// Collects what BizMate has staged.
        ///
        /// A customer configured on BOTH channels needs a second pass with channel=API, or its
        /// documents sit unseen: BizMate's poll defaults to EDI. Callers that serve such a customer
        /// must ask for both.
        /// </summary>
        public Task<OutboundPage> CollectAsync(
            string correlationId,
            string? partnerId = null,
            string? docType = null,
            string? family = null,
            string? channel = null,
            int? waitSeconds = null,
            CancellationToken cancellationToken = default)
        {
            return _connector.GetOutboundPendingAsync(
                correlationId, partnerId, docType, family, channel, waitSeconds, false, cancellationToken);
        }

        /// <summary>
        /// Recovers the crash-after-fetch case (W1-11): a row BizMate marked Fetched that we never
        /// confirmed as Delivered. BizMate serves it untouched and records a Redelivered event.
        /// </summary>
        public Task<OutboundPage> CollectRedeliveriesAsync(
            string correlationId, string? partnerId = null, CancellationToken cancellationToken = default)
        {
            return _connector.GetOutboundPendingAsync(
                correlationId, partnerId, null, null, null, null, true, cancellationToken);
        }

        /// <summary>
        /// Runs one staged document through: record it, mark it fetched, render, transmit, confirm.
        ///
        /// <paramref name="render"/> turns the canonical payload into what the partner receives.
        /// <paramref name="transmit"/> puts it on the wire and returns when the partner has it;
        /// throwing means it did not arrive. Both are delegates because they vary per partner and
        /// per carrier, while the ordering and the recording around them must not.
        /// </summary>
        public async Task<EDILedger> ProcessAsync(
            OutboundDocument document,
            Func<JsonElement, RenderedDocument> render,
            Func<RenderedDocument, CancellationToken, Task> transmit,
            CancellationToken cancellationToken = default)
        {
            if (document is null) throw new ArgumentNullException(nameof(document));
            if (render is null) throw new ArgumentNullException(nameof(render));
            if (transmit is null) throw new ArgumentNullException(nameof(transmit));

            string l_CorrelationId = BizMateInboundPipeline.NewCorrelationId();

            // ---- 1. Record it. ReceivedAt is the collection time on an outbound document, which
            // is what the trace contract means by receivedAt in this direction. ----
            EDILedger l_Ledger = CreateLedgerRow(document, l_CorrelationId);

            // ---- 2. Tell BizMate we have it. Distinct from delivered, on purpose. ----
            try
            {
                await _connector.MarkFetchedAsync(document.MessageId, l_CorrelationId, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (BizMateException ex)
            {
                // We hold the document but BizMate does not know. Leaving the row Pending is right:
                // the redelivery path (W1-11) is what reconciles this, not a silent retry here.
                l_Ledger.ErrorDetail = "Could not mark fetched, will reconcile by redelivery: " + ex.Message;
                l_Ledger.Modify();

                return l_Ledger;
            }

            // ---- 3. Render. A failure here is a first-class record (W2-08). ----
            RenderedDocument l_Rendered;

            try
            {
                l_Rendered = render(document.Payload);
                l_Ledger.TranslatedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                return Fail(l_Ledger, "Failed", "Rendering failed: " + ex.Message);
            }

            // A RawEDI transmission with no control number cannot be acknowledged: the partner's
            // 997 will quote a number we never recorded, and W2-10 will have nothing to match.
            // Better to fail here, visibly, than to send something unacknowledgeable.
            if (IsRawEdi(l_Rendered.Format) && string.IsNullOrWhiteSpace(l_Rendered.InterchangeControlNo))
            {
                return Fail(l_Ledger, "Failed",
                    "The rendered interchange carries no control number, so an inbound 997 could never be " +
                    "correlated back to it (W2-10).");
            }

            l_Ledger.Format = l_Rendered.Format;
            l_Ledger.Mechanism = BizMateFormats.ToMechanism(l_Rendered.Format);
            l_Ledger.InterchangeControlNo = l_Rendered.InterchangeControlNo;
            l_Ledger.MapName = l_Rendered.MapName;
            l_Ledger.MapVersion = l_Rendered.MapVersion;

            // ---- 4. Capture what we are about to send, before we send it (AD-02). ----
            // Always the byte-exact, hashed as-sent artifact; additionally the OutboundEDI row the
            // existing generators write, when the document belongs to an order we hold.
            long l_ArtifactId = SaveArtifact(l_Ledger.Id, l_Rendered);
            l_Ledger.RawArtifactRef = "artifact:" + l_ArtifactId;

            if (IsRawEdi(l_Rendered.Format) && l_Rendered.OrderId.HasValue)
            {
                LinkOutboundEDI(l_Ledger, l_Rendered);
            }

            l_Ledger.Modify();

            // ---- 5. Transmit. ----
            try
            {
                await transmit(l_Rendered, cancellationToken).ConfigureAwait(false);

                l_Ledger.DeliveredToPartnerAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                return Fail(l_Ledger, "Failed", "Transmission to the partner failed: " + ex.Message);
            }

            // ---- 6. Confirm, carrying our transmission reference so BizMate can find this row. ----
            try
            {
                await _connector.MarkDeliveredAsync(
                    document.MessageId,
                    l_Ledger.TransmissionReference,
                    l_CorrelationId,
                    l_Ledger.DeliveredToPartnerAt,
                    cancellationToken).ConfigureAwait(false);

                l_Ledger.Outcome = "Translated";
                l_Ledger.Modify();
            }
            catch (BizMateException ex)
            {
                // The partner has the document; only the confirmation failed. Do not mark this
                // Failed - that would misreport a delivered document as undelivered. The row stays
                // Pending with the delivery timestamp already set, and redelivery reconciles it.
                l_Ledger.ErrorDetail =
                    "Delivered to the partner, but BizMate was not confirmed: " + ex.Message;
                l_Ledger.Modify();
            }

            return l_Ledger;
        }

        private static bool IsRawEdi(string format)
        {
            return format is BizMateFormats.X12 or BizMateFormats.EDIFACT;
        }

        private EDILedger CreateLedgerRow(OutboundDocument document, string correlationId)
        {
            var l_Ledger = new EDILedger();

            l_Ledger.UseConnection(_connectionString);

            l_Ledger.TransmissionReference = BizMateInboundPipeline.NewTransmissionReference();
            l_Ledger.PartnerId = document.PartnerId;

            // Echo BizMate's control number, never invent one in this field - the trace contract
            // asks for theirs on an outbound record. Ours goes in InterchangeControlNo once the
            // document is rendered.
            l_Ledger.PartnerControlNo = document.PartnerControlNo;

            l_Ledger.CustomerNo = document.CustomerNo?.ToString();
            l_Ledger.CorrelationId = correlationId;
            l_Ledger.BizMateMessageId = document.MessageId;
            l_Ledger.BizMateDuplicate = false;

            // 'Out' is BizMate -> partner, from BizMate's point of view.
            l_Ledger.Direction = "Out";
            l_Ledger.DocumentType = document.DocType;
            l_Ledger.Family = document.SourceFamily;

            // Provisional until the render says what it actually produced. BizMate's coarse
            // mechanism does not tell us the format, so assume the configured standard and correct
            // it in step 3 rather than guessing a format we have not built yet.
            l_Ledger.Format = BizMateFormats.X12;
            l_Ledger.Mechanism = BizMateMechanisms.RawEdi;
            l_Ledger.Channel = string.IsNullOrEmpty(document.Channel) ? BizMateChannels.Edi : document.Channel;
            l_Ledger.Provenance = "Outbox";

            l_Ledger.Outcome = "Pending";

            // On an outbound document, receivedAt is when we collected it from the outbox.
            l_Ledger.ReceivedAt = DateTime.UtcNow;

            l_Ledger.CreatedDate = DateTime.UtcNow;
            l_Ledger.CreatedBy = _userNo;

            l_Ledger.SaveNew();

            return l_Ledger;
        }

        /// <summary>The as-sent artifact, byte-exact and hashed. Returns the artifact id.</summary>
        private long SaveArtifact(long ledgerId, RenderedDocument rendered)
        {
            var l_Artifact = new EDILedgerArtifact();

            l_Artifact.UseConnection(_connectionString);

            l_Artifact.LedgerId = ledgerId;
            l_Artifact.Stage = "AsSent";
            l_Artifact.FormatLabel = rendered.Format;
            l_Artifact.ContentEncoding = rendered.ContentEncoding;
            l_Artifact.ContentHash = BizMateRequestSigner.HashBody(rendered.Content);
            l_Artifact.SizeBytes = rendered.Content.LongLength;
            l_Artifact.Content = rendered.Content;
            l_Artifact.CreatedDate = DateTime.UtcNow;
            l_Artifact.CreatedBy = _userNo;

            l_Artifact.SaveNew();

            return l_Artifact.Id;
        }

        /// <summary>
        /// Writes the X12 rendering to <c>OutboundEDI</c> - the row the existing 855, 856 and 810
        /// generators write, in their vocabulary - and links the ledger to it (AD-02). Only possible
        /// when the document resolved to an order, because OutboundEDI.orderId is NOT NULL (F-22).
        /// </summary>
        private void LinkOutboundEDI(EDILedger ledger, RenderedDocument rendered)
        {
            var l_Outbound = new OutboundEDI();

            l_Outbound.UseConnection(_connectionString);

            l_Outbound.OrderId = rendered.OrderId!.Value;
            l_Outbound.Status = "NEW";
            l_Outbound.Data = DecodeForStorage(rendered);
            l_Outbound.CreatedBy = _userNo;
            l_Outbound.CreatedDate = DateTime.Now;   // the existing generators write local time here; kept identical

            l_Outbound.SaveNew();

            ledger.OutboundEDIId = l_Outbound.Id;
            ledger.OrderId = rendered.OrderId;
            ledger.RawArtifactRef = "outbound-edi:" + l_Outbound.Id;
        }

        private static string DecodeForStorage(RenderedDocument rendered)
        {
            try
            {
                return Encoding.GetEncoding(rendered.ContentEncoding).GetString(rendered.Content);
            }
            catch (ArgumentException)
            {
                return Encoding.UTF8.GetString(rendered.Content);
            }
        }

        private static EDILedger Fail(EDILedger ledger, string outcome, string detail)
        {
            ledger.Outcome = outcome;
            ledger.ErrorDetail = string.IsNullOrWhiteSpace(detail) ? "No reason recorded." : detail;

            ledger.Modify();

            return ledger;
        }
    }
}
