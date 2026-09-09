using System.Globalization;
using System.Text.Json;
using EdiEngine;
using EdiEngine.Common.Definitions;
using EdiEngine.Runtime;
using eSyncMate.DB.Entities;
using SegmentDefs = EdiEngine.Standards.X12_004010.Segments;

namespace eSyncMate.Processor.Connections
{
    /// <summary>
    /// The X12 envelope identity for a document eSyncMate is sending to a partner.
    ///
    /// Read back from the last interchange that partner sent US, reversed: we address them the way
    /// they address us. That is not a shortcut, it is what the existing 855, 856 and 810 generators
    /// already do - OrderManager passes the inbound InboundEDIInfo receiver fields as the outbound
    /// sender fields - and it needs nothing configured per partner. The defaults below apply only
    /// when the partner has never sent us an interchange.
    /// </summary>
    public sealed class X12EnvelopeIdentity
    {
        public string SenderQualifier { get; set; } = "ZZ";
        public string SenderId { get; set; } = "ESYNCMATE";
        public string ReceiverQualifier { get; set; } = "ZZ";
        public string ReceiverId { get; set; } = string.Empty;
        public string GsSenderId { get; set; } = "ESYNCMATE";
        public string GsReceiverId { get; set; } = string.Empty;
        public string IsaVersion { get; set; } = "00401";
        public string GsVersion { get; set; } = "004010";

        /// <summary>T while the partner is in test. Mirrored from what they send us.</summary>
        public string UsageIndicator { get; set; } = "T";

        public string SegmentSeparator { get; set; } = "~";
        public string ElementSeparator { get; set; } = "*";

        // The house values, identical to every existing generator in OrderManager.
        public string RepeatCharacter { get; set; } = "^";
        public string SubElementSeparator { get; set; } = string.Empty;

        /// <summary>
        /// Reverses the most recent interchange from this partner into an outbound identity.
        ///
        /// <paramref name="preferredReceiverId"/> is the per-document-type override off the customer
        /// row - ISA856ReceiverId and its siblings. Null means "whatever they call themselves".
        /// </summary>
        public static X12EnvelopeIdentity ForPartner(
            Customers customer, string connectionString, string? preferredReceiverId = null)
        {
            string l_Fallback = FirstSet(preferredReceiverId, customer?.ISACustomerID, customer?.ERPCustomerID) ?? string.Empty;

            var l_Identity = new X12EnvelopeIdentity
            {
                ReceiverId = l_Fallback,
                GsReceiverId = l_Fallback,
            };

            string l_Sender = (customer?.ISACustomerID ?? customer?.ERPCustomerID ?? string.Empty).Trim().Replace("'", "''");

            if (l_Sender.Length == 0)
            {
                return l_Identity;
            }

            var l_Info = new InboundEDIInfo();

            l_Info.UseConnection(connectionString);

            // The newest interchange from this sender. Its RECEIVER fields are how the partner
            // addresses eSyncMate, which is exactly what goes in the SENDER fields going back.
            if (!l_Info.GetObjectFromQuery(
                    $"SELECT TOP 1 * FROM InboundEDIInfo WHERE ISASenderId = '{l_Sender}' ORDER BY Id DESC", true).IsSuccess)
            {
                return l_Identity;
            }

            if (!string.IsNullOrWhiteSpace(l_Info.ISAReceiverId))
            {
                l_Identity.SenderQualifier = Or(l_Info.ISAReceiverQual, l_Identity.SenderQualifier);
                l_Identity.SenderId = l_Info.ISAReceiverId.Trim();
                l_Identity.GsSenderId = Or(l_Info.GSReceiverId, l_Identity.SenderId);
            }

            if (!string.IsNullOrWhiteSpace(l_Info.ISASenderId))
            {
                l_Identity.ReceiverQualifier = Or(l_Info.ISASenderQual, l_Identity.ReceiverQualifier);
                l_Identity.ReceiverId = preferredReceiverId ?? l_Info.ISASenderId.Trim();
                l_Identity.GsReceiverId = preferredReceiverId ?? Or(l_Info.GSSenderId, l_Identity.ReceiverId);
            }

            l_Identity.IsaVersion = Or(l_Info.ISAEdiVersion, l_Identity.IsaVersion);
            l_Identity.GsVersion = Or(l_Info.GSEdiVersion, l_Identity.GsVersion);
            l_Identity.UsageIndicator = Or(l_Info.ISAUsageIndicator, l_Identity.UsageIndicator);
            l_Identity.SegmentSeparator = Or(l_Info.SegmentSeparator, l_Identity.SegmentSeparator, trim: false);
            l_Identity.ElementSeparator = Or(l_Info.ElementSeparator, l_Identity.ElementSeparator, trim: false);

            return l_Identity;
        }

        private static string Or(string? value, string fallback, bool trim = true)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            return trim ? value.Trim() : value;
        }

        private static string? FirstSet(params string?[] candidates)
        {
            foreach (string? l_Candidate in candidates)
            {
                if (!string.IsNullOrWhiteSpace(l_Candidate))
                {
                    return l_Candidate.Trim();
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Raised when a renderer, or the engine's own validator, rejects what was built. The message
    /// names the segment and element, so the ledger's ErrorDetail says what is wrong rather than
    /// only that something is.
    /// </summary>
    public sealed class X12RenderException : Exception
    {
        public X12RenderException(string message) : base(message) { }
    }

    /// <summary>
    /// Shared X12 building for the outbound renderers (W3).
    ///
    /// Two things about the engine shape everything here.
    ///
    /// Segments go into <see cref="EdiTrans.Content"/> as a FLAT list. An 856's hierarchy is carried
    /// by the HL01 and HL02 values inside the HL segments, not by nesting runtime objects - the
    /// writer serialises Content in order and does nothing else with it. That is already how
    /// OrderManager.AddASNSegments works.
    ///
    /// The writer validates as it writes, into <see cref="EdiTrans.ValidationErrors"/>, and then
    /// returns the string anyway. Left alone that means an invalid interchange goes to the partner
    /// in silence. <see cref="Write"/> turns those errors into a failure instead, so a bad rendering
    /// becomes a visible Failed ledger row - the same rule the inbound canonical guard follows.
    /// </summary>
    public static class X12Render
    {
        /// <summary>
        /// Finds a segment definition by name anywhere in the map, top level or inside any loop.
        ///
        /// The definition supplies only the element metadata the writer needs; which loop it was
        /// found in does not matter, because what places a segment is the order it is added.
        /// </summary>
        public static MapSegment Def(MapLoop map, string name)
        {
            return Search(map, name)
                ?? throw new X12RenderException($"No definition for segment [{name}] in map [{map.EdiName}].");
        }

        private static MapSegment? Search(MapLoop loop, string name)
        {
            foreach (MapBaseEntity l_Entity in loop.Content)
            {
                if (l_Entity is MapSegment l_Segment && l_Segment.Name == name)
                {
                    return l_Segment;
                }

                if (l_Entity is MapLoop l_Child && Search(l_Child, name) is MapSegment l_Deep)
                {
                    return l_Deep;
                }
            }

            return null;
        }

        /// <summary>
        /// Appends a segment, dropping trailing empty elements.
        ///
        /// X12 treats a trailing empty element as absent, so writing them only produces segments
        /// that end in a run of separators. Empty elements BETWEEN populated ones are kept - they
        /// are positional and carry meaning by where they sit. A segment with nothing in it at all
        /// is not written: a bare tag says less than saying nothing.
        /// </summary>
        public static void Add(EdiTrans trans, MapLoop map, string name, params string?[] values)
        {
            MapSegment l_Def = Def(map, name);

            int l_Last = -1;

            for (int i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrEmpty(values[i]))
                {
                    l_Last = i;
                }
            }

            if (l_Last < 0)
            {
                return;
            }

            var l_Segment = new EdiSegment(l_Def);

            for (int i = 0; i <= l_Last; i++)
            {
                if (i >= l_Def.Content.Count)
                {
                    throw new X12RenderException(
                        $"Segment [{name}] has {l_Def.Content.Count} elements; element {i + 1} does not exist.");
                }

                if (l_Def.Content[i] is not MapSimpleDataElement l_Element)
                {
                    // A composite sitting between two populated positions - POC05's unit of measure
                    // is the one that turns up in practice. It still has to occupy its place or
                    // everything after it shifts, and an empty composite writes as nothing at all.
                    if (!string.IsNullOrEmpty(values[i]))
                    {
                        throw new X12RenderException(
                            $"Element {i + 1} of segment [{name}] is composite; renderers here write simple elements only.");
                    }

                    l_Segment.Content.Add(new EdiCompositeDataElement(l_Def.Content[i]));

                    continue;
                }

                l_Segment.Content.Add(new EdiSimpleDataElement(l_Element, values[i] ?? string.Empty));
            }

            trans.Content.Add(l_Segment);
        }

        /// <summary>
        /// Wraps the transaction in its group and interchange and writes the whole thing.
        ///
        /// The control number is eSyncMate's own (EQ-01) and is the ledger row's id: unique,
        /// monotonic, and the number an inbound 997 will quote back - which is what
        /// IX_EDILedger_InterchangeControlNo exists to look up (W2-10).
        /// </summary>
        public static string Write(EdiTrans trans, string functionalGroupCode, X12EnvelopeIdentity identity, long controlNumber)
        {
            var l_Group = new EdiGroup(functionalGroupCode);
            l_Group.Transactions.Add(trans);

            var l_Interchange = new EdiInterchange();
            l_Interchange.Groups.Add(l_Group);

            var l_Batch = new EdiBatch();
            l_Batch.Interchanges.Add(l_Interchange);

            int l_Control = ControlNumber(controlNumber);

            var l_Settings = new EdiDataWriterSettings(
                new SegmentDefs.ISA(), new SegmentDefs.IEA(),
                new SegmentDefs.GS(), new SegmentDefs.GE(),
                new SegmentDefs.ST(), new SegmentDefs.SE(),
                identity.SenderQualifier, identity.SenderId,
                identity.ReceiverQualifier, identity.ReceiverId,
                identity.GsSenderId, identity.GsReceiverId,
                identity.IsaVersion, identity.GsVersion, identity.UsageIndicator,
                l_Control, l_Control,
                identity.SegmentSeparator, identity.ElementSeparator,
                identity.RepeatCharacter, identity.SubElementSeparator);

            string l_Text = new EdiDataWriter(l_Settings).WriteToString(l_Batch);

            if (trans.ValidationErrors.Count > 0)
            {
                string l_Detail = string.Join("; ", trans.ValidationErrors
                    .Take(10)
                    .Select(e => $"{e.SegmentName}{(e.ElementPos.HasValue ? e.ElementPos.Value.ToString("00") : string.Empty)} {e.Message}"));

                throw new X12RenderException(
                    $"The rendered {trans.Definition.EdiName} failed X12 validation and was not sent: {l_Detail}" +
                    (trans.ValidationErrors.Count > 10 ? $" (+{trans.ValidationErrors.Count - 10} more)" : string.Empty));
            }

            return l_Text;
        }

        /// <summary>
        /// The ledger id folded into the nine numeric digits ISA13 and GS06 allow. Monotonic across
        /// any run of a billion documents, far beyond the window a 997 correlates over.
        /// </summary>
        public static int ControlNumber(long ledgerId)
        {
            long l_Control = ledgerId % 999999999L;

            return (int)(l_Control <= 0 ? 1 : l_Control);
        }

        // ---- canonical readers: null for anything absent, never an exception -----------------

        public static string? Str(JsonElement parent, string name)
        {
            if (parent.ValueKind != JsonValueKind.Object || !parent.TryGetProperty(name, out JsonElement l_Value))
            {
                return null;
            }

            return l_Value.ValueKind switch
            {
                JsonValueKind.String => l_Value.GetString(),
                JsonValueKind.Number => l_Value.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null,
            };
        }

        /// <summary>A value two levels down, for the measure and dimension wrappers.</summary>
        public static string? Str(JsonElement parent, string name, string child)
        {
            JsonElement? l_Object = Obj(parent, name);

            return l_Object.HasValue ? Str(l_Object.Value, child) : null;
        }

        public static JsonElement? Obj(JsonElement parent, string name)
        {
            if (parent.ValueKind == JsonValueKind.Object
                && parent.TryGetProperty(name, out JsonElement l_Value)
                && l_Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
            {
                return l_Value;
            }

            return null;
        }

        public static IReadOnlyList<JsonElement> Array(JsonElement parent, string name)
        {
            JsonElement? l_Value = Obj(parent, name);

            return l_Value.HasValue && l_Value.Value.ValueKind == JsonValueKind.Array
                ? l_Value.Value.EnumerateArray().ToList()
                : System.Array.Empty<JsonElement>();
        }

        /// <summary>YYYY-MM-DD or an ISO instant to X12 CCYYMMDD. Null when there is nothing to write.</summary>
        public static string? Date8(string? canonical)
        {
            return TryInstant(canonical, out DateTime l_Parsed) ? l_Parsed.ToString("yyyyMMdd") : null;
        }

        /// <summary>An ISO instant to X12 HHMM. Null for a bare date, which carries no time.</summary>
        public static string? Time4(string? canonical)
        {
            if (canonical is not null && CanonicalValues.IsCanonicalDate(canonical))
            {
                return null;
            }

            return TryInstant(canonical, out DateTime l_Parsed) ? l_Parsed.ToString("HHmm") : null;
        }

        private static bool TryInstant(string? canonical, out DateTime parsed)
        {
            parsed = default;

            return !string.IsNullOrWhiteSpace(canonical)
                && DateTime.TryParse(canonical, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out parsed);
        }

        /// <summary>
        /// A canonical number as X12 writes it: no trailing zeros, no thousands separator, no
        /// currency symbol. Anything unparseable passes through, so the validator decides rather
        /// than this quietly dropping it.
        /// </summary>
        public static string? Num(JsonElement parent, string name)
        {
            return Num(Str(parent, name));
        }

        public static string? Num(JsonElement parent, string name, string child)
        {
            return Num(Str(parent, name, child));
        }

        public static string? Num(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal l_Value)
                ? l_Value.ToString("0.####", CultureInfo.InvariantCulture)
                : raw;
        }

        /// <summary>
        /// Trims free text to what the element allows.
        ///
        /// Only ever used on prose - descriptions, party names, reason text. Identifiers are never
        /// trimmed: a truncated PO number or SKU is a wrong document that looks right, so those go
        /// through at full length and the validator fails the whole rendering instead.
        /// </summary>
        public static string? Text(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
