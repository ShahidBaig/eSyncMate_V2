using System.Text.Json;
using System.Text.Json.Serialization;

namespace eSyncMate.Processor.Connections
{
    /// <summary>
    /// Wire models for the BizMate integration endpoints.
    /// Contract: M1-Contracts/openapi.yaml, contract version 1.0 (2026-09-02).
    ///
    /// Canonical payloads are carried as JsonElement rather than a typed object on purpose. BizMate
    /// stores the payload verbatim on its ledger, so round-tripping it through a POCO would risk
    /// re-ordering, re-formatting or silently dropping a property the schema gained in a 1.x
    /// addition - and a 1.x addition is explicitly meant to pass through untouched (X-05).
    /// </summary>
    public static class BizMateFormats
    {
        public const string X12 = "X12";
        public const string EDIFACT = "EDIFACT";
        public const string Json = "JSON";
        public const string Csv = "CSV";
        public const string FixedWidth = "FixedWidth";
        public const string Xml = "XML";
        public const string DbMap = "DBMap";

        /// <summary>
        /// The Format -> Mechanism derivation, for the three formats it is defined for.
        ///
        /// Deriving rather than accepting is what stops a flat file that AD-01 staged through the
        /// DB-map tables from being relabelled DBMap and misreporting the partner relationship on
        /// BizMate's boards (ER-07).
        ///
        /// JSON is deliberately not derivable. A marketplace document arrives as JSON like any
        /// other API push, so PartnerAPI cannot be inferred from the artifact - eSyncMate declares
        /// it on the inbound call. Use ResolveMechanism for that path.
        /// </summary>
        public static string ToMechanism(string format) => format switch
        {
            X12 or EDIFACT => BizMateMechanisms.RawEdi,
            Csv or FixedWidth or Xml => BizMateMechanisms.FlatFile,
            DbMap => BizMateMechanisms.DbMap,
            _ => throw new ArgumentOutOfRangeException(
                nameof(format),
                format,
                "This format has no mechanism derivation. PartnerAPI is not derivable from an artifact " +
                "format - a marketplace document arrives as JSON like any other API push - so it must " +
                "be declared explicitly. Use ResolveMechanism.")
        };

        /// <summary>
        /// The mechanism for a document, preferring one the caller declared.
        ///
        /// Marketplace traffic must declare <c>PartnerAPI</c> because BizMate cannot infer it: the
        /// artifact is JSON, indistinguishable from any other API push. Everything else derives
        /// from the format and may not be overridden, or the mislabelling ER-07 warns about is back.
        /// </summary>
        public static string ResolveMechanism(string format, string? declaredMechanism)
        {
            if (string.IsNullOrWhiteSpace(declaredMechanism))
            {
                return ToMechanism(format);
            }

            if (!BizMateMechanisms.IsValid(declaredMechanism))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(declaredMechanism), declaredMechanism,
                    "Not a mechanism BizMate accepts. Valid values are RawEDI, FlatFile, DBMap and PartnerAPI.");
            }

            if (declaredMechanism != BizMateMechanisms.PartnerApi)
            {
                string l_Derived = ToMechanism(format);

                if (declaredMechanism != l_Derived)
                {
                    throw new ArgumentException(
                        $"Format '{format}' derives mechanism '{l_Derived}', but '{declaredMechanism}' was declared. " +
                        "Only PartnerAPI may be declared; every other mechanism follows from the format.",
                        nameof(declaredMechanism));
                }
            }

            return declaredMechanism;
        }
    }

    public static class BizMateMechanisms
    {
        public const string RawEdi = "RawEDI";
        public const string FlatFile = "FlatFile";
        public const string DbMap = "DBMap";

        /// <summary>
        /// Marketplace and third-party API traffic (D-31, BizMate task script 40).
        ///
        /// Added by BizMate AFTER the M1 contract was frozen, so openapi.yaml 1.0 still enumerates
        /// only the first three while their shipped database allows four. Declared by eSyncMate on
        /// the inbound call, never derived from the artifact.
        /// </summary>
        public const string PartnerApi = "PartnerAPI";

        public static bool IsValid(string? mechanism) =>
            mechanism is RawEdi or FlatFile or DbMap or PartnerApi;
    }

    public static class BizMateChannels
    {
        public const string Edi = "EDI";
        public const string Api = "API";
    }

    // ---------------------------------------------------------------------------------------
    // POST /v1/integration/raw  (W1-06, E10)
    // ---------------------------------------------------------------------------------------

    public sealed class RegisterRawFileRequest
    {
        [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;
        [JsonPropertyName("format")] public string Format { get; set; } = string.Empty;
        [JsonPropertyName("direction")] public string Direction { get; set; } = "In";

        [JsonPropertyName("fileName")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? FileName { get; set; }

        [JsonPropertyName("partnerId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PartnerId { get; set; }

        /// <summary>Optional, but always send it: BizMate rejects a mismatch, which catches corruption in transit.</summary>
        [JsonPropertyName("contentHash")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ContentHash { get; set; }

        [JsonPropertyName("receivedAt")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? ReceivedAt { get; set; }
    }

    public sealed class RegisterRawFileResponse
    {
        [JsonPropertyName("rawFileRef")] public long RawFileRef { get; set; }
        [JsonPropertyName("contentHash")] public string? ContentHash { get; set; }
        [JsonPropertyName("contentLength")] public long ContentLength { get; set; }
    }

    // ---------------------------------------------------------------------------------------
    // POST /v1/integration/inbound/{docType}  (W1-07, E9)
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// The inbound wrapper. Transport identity rides here and never inside the payload (D-22) -
    /// the canonical document stays a pure business document.
    /// </summary>
    public sealed class InboundDocumentRequest
    {
        [JsonPropertyName("rawFileRef")] public long RawFileRef { get; set; }
        [JsonPropertyName("partnerId")] public string PartnerId { get; set; } = string.Empty;

        /// <summary>RawEDI: interchange or group control number. FlatFile/DBMap: the batch identifier. The idempotency key.</summary>
        [JsonPropertyName("partnerControlNo")] public string PartnerControlNo { get; set; } = string.Empty;

        [JsonPropertyName("format")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Format { get; set; }

        [JsonPropertyName("mechanism")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Mechanism { get; set; }

        /// <summary>Required for 856 and 810 only (E8). Never inferred from the transaction set.</summary>
        [JsonPropertyName("sourceFamily")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SourceFamily { get; set; }

        [JsonPropertyName("channel")] public string Channel { get; set; } = BizMateChannels.Edi;

        [JsonPropertyName("customerNo")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? CustomerNo { get; set; }

        [JsonPropertyName("partyId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PartyId { get; set; }

        /// <summary>Our ledger id, so BizMate can cross-reference back (E17).</summary>
        [JsonPropertyName("sourceReference")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SourceReference { get; set; }

        [JsonPropertyName("payload")] public JsonElement Payload { get; set; }
    }

    public sealed class InboundDocumentResponse
    {
        [JsonPropertyName("messageId")] public long MessageId { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }

        /// <summary>True is a SUCCESS, not a failure - BizMate already has this document (W1-15).</summary>
        [JsonPropertyName("duplicate")] public bool Duplicate { get; set; }

        [JsonPropertyName("mechanism")] public string? Mechanism { get; set; }
    }

    // ---------------------------------------------------------------------------------------
    // POST /v1/integration/ack  (W1-12, E11)
    // ---------------------------------------------------------------------------------------

    public sealed class AcknowledgementRequest
    {
        [JsonPropertyName("partnerId")] public string PartnerId { get; set; } = string.Empty;

        /// <summary>The control number of BizMate's outbound transmission being acknowledged.</summary>
        [JsonPropertyName("partnerControlNo")] public string PartnerControlNo { get; set; } = string.Empty;

        /// <summary>Accepted | AcceptedWithErrors | Rejected.</summary>
        [JsonPropertyName("ackResult")] public string AckResult { get; set; } = string.Empty;

        [JsonPropertyName("receivedAt")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? ReceivedAt { get; set; }

        /// <summary>AK5/AK9 codes or free text. Capped at 2000 by the contract.</summary>
        [JsonPropertyName("detail")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Detail { get; set; }

        [JsonPropertyName("rawFileRef")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? RawFileRef { get; set; }
    }

    public sealed class AcknowledgementResponse
    {
        [JsonPropertyName("messageId")] public long MessageId { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }

        /// <summary>Received | NotTracked. NotTracked means BizMate has no clock for this document.</summary>
        [JsonPropertyName("ackStatus")] public string? AckStatus { get; set; }
    }

    // ---------------------------------------------------------------------------------------
    // GET /v1/integration/outbound/pending and the two confirmations  (W1-08, W1-10, E9)
    // ---------------------------------------------------------------------------------------

    public sealed class OutboundCorrelation
    {
        [JsonPropertyName("document")] public string? Document { get; set; }
        [JsonPropertyName("no")] public long? No { get; set; }
    }

    /// <summary>
    /// One page of the outbox. The endpoint answers an OBJECT carrying items and a total, not a
    /// bare array - openapi.yaml, GET /v1/integration/outbound/pending, the 200 response.
    ///
    /// total is every row the call would serve regardless of limit, which makes it the partner's
    /// real backlog depth and therefore what X-07 reports to BizMate as QueueBacklog. Keeping it
    /// rather than returning only the items is the difference between "we collected five" and
    /// "we collected five of seventy-one".
    /// </summary>
    public sealed class OutboundPage
    {
        [JsonPropertyName("items")] public List<OutboundDocument> Items { get; set; } = new();

        [JsonPropertyName("total")] public int Total { get; set; }
    }

    public sealed class OutboundDocument
    {
        [JsonPropertyName("messageId")] public long MessageId { get; set; }
        [JsonPropertyName("docType")] public string DocType { get; set; } = string.Empty;
        [JsonPropertyName("sourceFamily")] public string? SourceFamily { get; set; }
        [JsonPropertyName("customerNo")] public long? CustomerNo { get; set; }
        [JsonPropertyName("partnerId")] public string PartnerId { get; set; } = string.Empty;

        /// <summary>BizMate-assigned. Echo it on the acknowledgement.</summary>
        [JsonPropertyName("partnerControlNo")] public string? PartnerControlNo { get; set; }

        [JsonPropertyName("mechanism")] public string? Mechanism { get; set; }
        [JsonPropertyName("channel")] public string? Channel { get; set; }
        [JsonPropertyName("stagedAt")] public DateTimeOffset StagedAt { get; set; }
        [JsonPropertyName("correlated")] public OutboundCorrelation? Correlated { get; set; }
        [JsonPropertyName("payload")] public JsonElement Payload { get; set; }
    }

    public sealed class StatusResponse
    {
        [JsonPropertyName("messageId")] public long MessageId { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
    }

    /// <summary>
    /// Body of POST /outbound/{id}/delivered. transmissionReference is how BizMate finds the
    /// eSyncMate record for an outbound document, so it is never omitted (trace contract checklist).
    /// </summary>
    public sealed class DeliveredRequest
    {
        [JsonPropertyName("transmissionReference")] public string TransmissionReference { get; set; } = string.Empty;

        [JsonPropertyName("deliveredAt")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? DeliveredAt { get; set; }
    }

    // ---------------------------------------------------------------------------------------
    // POST /v1/integration/partner-state  (W2-17, E18)
    // ---------------------------------------------------------------------------------------

    public static class PartnerStates
    {
        public const string Up = "Up";
        public const string Down = "Down";
        public const string MapDisabled = "MapDisabled";
        public const string QueueBacklog = "QueueBacklog";
    }

    public sealed class PartnerStateRequest
    {
        [JsonPropertyName("partnerId")] public string PartnerId { get; set; } = string.Empty;
        [JsonPropertyName("state")] public string State { get; set; } = string.Empty;

        [JsonPropertyName("detail")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Detail { get; set; }

        /// <summary>When observed, UTC. Never in the future.</summary>
        [JsonPropertyName("at")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? At { get; set; }
    }

    public sealed class PartnerStateResponse
    {
        [JsonPropertyName("partnerId")] public string? PartnerId { get; set; }
        [JsonPropertyName("state")] public string? State { get; set; }
        [JsonPropertyName("recordedAt")] public DateTimeOffset RecordedAt { get; set; }
    }

    // ---------------------------------------------------------------------------------------
    // GET /v1/integration/config/{customerNo}  (W1-13, E9, E21)
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// BizMate is the authority on enablement. Do not cache beyond cacheTtlSeconds and never keep a
    /// local copy - a change can take BizMate's own 120 s to appear, and ours must not add to it.
    /// </summary>
    public sealed class CustomerIntegrationConfig
    {
        [JsonPropertyName("customerNo")] public long CustomerNo { get; set; }
        [JsonPropertyName("customerId")] public string? CustomerId { get; set; }
        [JsonPropertyName("ediEnabled")] public bool EdiEnabled { get; set; }
        [JsonPropertyName("apiEnabled")] public bool ApiEnabled { get; set; }
        [JsonPropertyName("standard")] public string? Standard { get; set; }
        [JsonPropertyName("partnerQualifier")] public string? PartnerQualifier { get; set; }
        [JsonPropertyName("partnerId")] public string? PartnerId { get; set; }
        [JsonPropertyName("mechanism")] public string? Mechanism { get; set; }
        [JsonPropertyName("orderCancelWindow")] public string? OrderCancelWindow { get; set; }
        [JsonPropertyName("cacheTtlSeconds")] public int? CacheTtlSeconds { get; set; }
    }
}
