using System.Text.Json;

namespace eSyncMate.Processor.Connections
{
    /// <summary>Everything a renderer needs about the document and the partner it is going to.</summary>
    public sealed class OutboundRenderContext
    {
        /// <summary>The canonical document BizMate staged. This is what gets rendered.</summary>
        public JsonElement Payload { get; set; }

        /// <summary>850, 855, 856, 810, ... the Document Catalog code.</summary>
        public string DocumentType { get; set; } = string.Empty;

        /// <summary>The partner as eSyncMate and BizMate both know it.</summary>
        public string PartnerId { get; set; } = string.Empty;

        /// <summary>BizMate's own control number for this message. Echoed, never used as ours.</summary>
        public string? PartnerControlNo { get; set; }

        /// <summary>The eSyncMate customer row for the partner, carrying the ISA identities.</summary>
        public DB.Entities.Customers Customer { get; set; } = null!;

        /// <summary>Order or Consignment, when BizMate said which.</summary>
        public string? Family { get; set; }
    }

    /// <summary>
    /// Canonical document to partner document, one renderer per document type (W1-18).
    ///
    /// The registry exists so the collector route has something to ask, and so that a document
    /// type nobody has built a renderer for is a **known** gap rather than a failure: the route
    /// checks first and leaves the document staged in BizMate's outbox, which is recoverable,
    /// instead of fetching it and then failing, which is not. BizMate re-serves a Staged document
    /// on the next poll; a Fetched one needs the redelivery path.
    ///
    /// The renderers themselves are W3 work and are deliberately not here yet:
    ///
    ///   855  W3-03      856  W3-05      810  W3-07
    ///   860  W3-09      865  W3-11      870  W3-13
    ///
    /// Two rules a renderer must follow, both enforced downstream rather than trusted:
    ///
    ///   It must set InterchangeControlNo for X12 and EDIFACT. eSyncMate assigns its own ISA and
    ///   GS control numbers (EQ-01), and BizMateOutboundPipeline refuses to transmit a raw
    ///   interchange without one, because the partner's 997 would quote a number we never
    ///   recorded and W2-10 would have nothing to correlate.
    ///
    ///   It should set OrderId when the document answers an order eSyncMate holds. With it, the
    ///   rendering is also written to OutboundEDI and linked from the ledger, which is what puts
    ///   it on the existing order screens (AD-02). Without it only the artifact is kept, because
    ///   OutboundEDI.orderId is NOT NULL (F-22).
    /// </summary>
    public static class BizMateDocumentRenderers
    {
        private static readonly Dictionary<string, Func<OutboundRenderContext, RenderedDocument>> _renderers =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Registers the renderer for a document type, replacing any already registered.</summary>
        public static void Register(string documentType, Func<OutboundRenderContext, RenderedDocument> renderer)
        {
            if (string.IsNullOrWhiteSpace(documentType))
            {
                throw new ArgumentException("A document type is required.", nameof(documentType));
            }

            _renderers[documentType] = renderer ?? throw new ArgumentNullException(nameof(renderer));
        }

        /// <summary>True when something can render this document type.</summary>
        public static bool CanRender(string documentType)
        {
            return !string.IsNullOrWhiteSpace(documentType) && _renderers.ContainsKey(documentType);
        }

        public static bool TryGet(string documentType, out Func<OutboundRenderContext, RenderedDocument> renderer)
        {
            if (!string.IsNullOrWhiteSpace(documentType) && _renderers.TryGetValue(documentType, out var l_Found))
            {
                renderer = l_Found;
                return true;
            }

            renderer = null!;
            return false;
        }

        /// <summary>The document types that can be rendered today, for logging what was skipped and why.</summary>
        public static IReadOnlyCollection<string> Registered => _renderers.Keys.ToList();
    }
}
