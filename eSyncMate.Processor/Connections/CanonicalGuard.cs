using System.Globalization;
using System.Text.Json;

namespace eSyncMate.Processor.Connections
{
    /// <summary>One way a canonical payload breaks the conventions, and where.</summary>
    public sealed class CanonicalViolation
    {
        public string Path { get; init; } = string.Empty;
        public string Rule { get; init; } = string.Empty;
        public string Detail { get; init; } = string.Empty;

        public override string ToString() => $"{Path}: {Detail}";
    }

    /// <summary>
    /// Checks a canonical payload against the conventions in common.schema.json before it is
    /// handed to BizMate (X-02, X-03, and E7's "validate and report failures").
    ///
    /// The maps produce the canonical JSON, and a map is text in a database row with no schema and
    /// no compiled type behind it (finding F-1) - so nothing today stops one silently regressing to
    /// MM/dd/yyyy, or to money as a string, or to an empty string standing in for "not provided".
    /// This is the boundary that notices. A violation becomes a visible Failed ledger row rather
    /// than a document BizMate accepts and misreads.
    ///
    /// The rules are deliberately narrow, because a noisy guard gets switched off. Each one below
    /// is either unambiguous from the contract, or keyed to a field-name pattern that the canonical
    /// models use consistently. When W5-08 generates the canonical model from the M1 schemas, this
    /// should be regenerated from them too and the name matching dropped.
    /// </summary>
    public static class CanonicalGuard
    {
        /// <summary>
        /// Fields where an empty string is meaningful rather than a stand-in for "not provided".
        ///
        /// The 860's changeInstructions is the documented case: an empty string there genuinely
        /// blanks the field, which is exactly why W4-08 insists empty and absent stay distinct.
        /// </summary>
        /// Shared with the normaliser so the two can never disagree about which empties survive.
        private static readonly IReadOnlySet<string> _emptyIsMeaningful = CanonicalValues.EmptyIsMeaningful;

        /// <summary>
        /// The prefix BizMate puts in front of a value it has encrypted.
        ///
        /// Nothing in the M1 contract mentions encrypted or masked payload fields - not the
        /// OpenAPI document, not the schemas, not the handover note - and yet 18 of the 30 856s
        /// staged for BELL-D12 carry one at exactly $.shipment.shipTo.name, and nowhere else. That
        /// is the consignee name, destined for N1*ST of an ASN. Rendered as-is it would put
        /// "ENC:x8BS..." on a real delivery; and at 66 characters it does not even fit X12 element
        /// 93, so the interchange would be invalid as well as wrong.
        ///
        /// Raised with BizMate as an open question. Until it is answered, a document carrying one
        /// is a visible Failed ledger row rather than something a partner receives and acts on.
        /// </summary>
        private const string CiphertextPrefix = "ENC:";

        private static readonly string[] _instantSuffixes = { "At" };

        private static readonly string[] _dateSuffixes = { "Date" };

        /// <summary>
        /// Field names that carry money or a quantity in the canonical models.
        ///
        /// Note what is NOT here: the bare word "value". The canonical model uses it for reference
        /// and diagnostic strings - <c>references[].value</c> (a REF value) and
        /// <c>errors[].badValue</c> - so matching it flagged two of BizMate's own validated samples.
        /// A guard that cries wolf on valid documents is a guard somebody switches off.
        /// </summary>
        private static readonly string[] _numericNames =
        {
            "price", "unitPrice", "amount", "total", "subtotal", "charge", "charges", "discount",
            "tax", "freight", "allowance", "rebate", "quantity", "qty", "weight"
        };

        /// <summary>
        /// Walks the payload and returns every violation found. An empty list means the payload
        /// meets the conventions this guard can check - not that it satisfies the whole schema.
        /// </summary>
        public static List<CanonicalViolation> Inspect(JsonElement payload)
        {
            var l_Violations = new List<CanonicalViolation>();

            Walk(payload, "$", l_Violations);

            return l_Violations;
        }

        public static List<CanonicalViolation> Inspect(string payloadJson)
        {
            using JsonDocument l_Doc = JsonDocument.Parse(payloadJson);

            return Inspect(l_Doc.RootElement);
        }

        private static void Walk(JsonElement element, string path, List<CanonicalViolation> violations)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (JsonProperty l_Property in element.EnumerateObject())
                    {
                        Walk(l_Property.Value, path + "." + l_Property.Name, violations);
                    }

                    break;

                case JsonValueKind.Array:
                    int l_Index = 0;

                    foreach (JsonElement l_Item in element.EnumerateArray())
                    {
                        Walk(l_Item, path + "[" + l_Index++ + "]", violations);
                    }

                    break;

                case JsonValueKind.String:
                    CheckString(element.GetString(), path, violations);
                    break;

                case JsonValueKind.Number:
                    CheckNumber(element, path, violations);
                    break;
            }
        }

        private static void CheckString(string? value, string path, List<CanonicalViolation> violations)
        {
            string l_Field = FieldName(path);

            // --- An unknown value is null or omitted, never "" ---------------------------
            if (value is not null && value.Length == 0)
            {
                if (!_emptyIsMeaningful.Contains(l_Field))
                {
                    violations.Add(new CanonicalViolation
                    {
                        Path = path,
                        Rule = "empty-string",
                        Detail = "Empty string standing in for \"not provided\". Send null or omit the field."
                    });
                }

                return;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            // --- A canonical value is plaintext, never ciphertext ------------------------
            if (value!.StartsWith(CiphertextPrefix, StringComparison.Ordinal))
            {
                violations.Add(new CanonicalViolation
                {
                    Path = path,
                    Rule = "ciphertext",
                    Detail = $"Starts with \"{CiphertextPrefix}\", so this is ciphertext, not a value. " +
                             "The contract documents no encrypted or masked fields; rendering it would put " +
                             "the ciphertext itself in front of the partner."
                });

                return;
            }

            // --- Timestamps must be ISO-8601 UTC with Z (X-02) ---------------------------
            if (EndsWithAny(l_Field, _instantSuffixes) && !CanonicalValues.IsCanonicalInstant(value))
            {
                violations.Add(new CanonicalViolation
                {
                    Path = path,
                    Rule = "instant-format",
                    Detail = $"'{value}' is not ISO-8601 UTC with Z. Use CanonicalValues.ToInstant, " +
                             "stating the source zone rather than letting the server's be assumed."
                });
            }

            // --- Dates must be YYYY-MM-DD, or a canonical instant (X-02) -----------------
            // A *Date field is not always the schema's `date` type: BizMate's own samples carry a
            // full instant in shipment.shipDate and advice.adviceDate. Both are canonical, so the
            // rule is that the value must be one or the other - never a legacy shape.
            else if (EndsWithAny(l_Field, _dateSuffixes)
                     && !CanonicalValues.IsCanonicalDate(value)
                     && !CanonicalValues.IsCanonicalInstant(value))
            {
                string l_Hint = LooksLikeLegacyDate(value)
                    ? " This looks like a legacy map output - formatDate emits MM/dd/yyyy and a raw DTM is yyyyMMdd."
                    : string.Empty;

                violations.Add(new CanonicalViolation
                {
                    Path = path,
                    Rule = "date-format",
                    Detail = $"'{value}' is neither YYYY-MM-DD nor ISO-8601 UTC with Z.{l_Hint} " +
                             "Use CanonicalValues.ToDate or ToInstant."
                });
            }

            // --- Money and quantity are numbers, not strings (X-03) ----------------------
            if (IsNumericField(l_Field)
                && decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
            {
                violations.Add(new CanonicalViolation
                {
                    Path = path,
                    Rule = "numeric-as-string",
                    Detail = $"\"{value}\" is a JSON string. Money and quantities are JSON numbers - " +
                             "12.50, never \"12.50\". Use CanonicalValues.ToMoney or ToQuantity."
                });
            }
        }

        private static void CheckNumber(JsonElement element, string path, List<CanonicalViolation> violations)
        {
            string l_Field = FieldName(path);

            if (!IsNumericField(l_Field) || !element.TryGetDecimal(out decimal l_Value))
            {
                return;
            }

            if (IsQuantityField(l_Field) && l_Value < 0)
            {
                violations.Add(new CanonicalViolation
                {
                    Path = path,
                    Rule = "negative-quantity",
                    Detail = $"{l_Value} is negative. A canonical quantity has minimum 0."
                });

                return;
            }

            // More than four decimals is outside the money definition. Round at the boundary
            // rather than letting BizMate decide what to do with the extra places.
            if (!IsQuantityField(l_Field) && Decimals(l_Value) > CanonicalValues.MoneyDecimals)
            {
                violations.Add(new CanonicalViolation
                {
                    Path = path,
                    Rule = "money-precision",
                    Detail = $"{l_Value} carries more than {CanonicalValues.MoneyDecimals} decimal places."
                });
            }
        }

        private static string FieldName(string path)
        {
            int l_Dot = path.LastIndexOf('.');
            string l_Field = l_Dot >= 0 ? path.Substring(l_Dot + 1) : path;

            int l_Bracket = l_Field.IndexOf('[');

            return l_Bracket >= 0 ? l_Field.Substring(0, l_Bracket) : l_Field;
        }

        private static bool EndsWithAny(string field, string[] suffixes)
        {
            foreach (string l_Suffix in suffixes)
            {
                // Require the suffix to start a word, so "state" is not read as a date and
                // "format" is not read as an instant.
                if (field.Length > l_Suffix.Length
                    && field.EndsWith(l_Suffix, StringComparison.Ordinal)
                    && char.IsLower(field[field.Length - l_Suffix.Length - 1]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsNumericField(string field)
        {
            foreach (string l_Name in _numericNames)
            {
                if (field.Equals(l_Name, StringComparison.OrdinalIgnoreCase)
                    || field.EndsWith(char.ToUpperInvariant(l_Name[0]) + l_Name.Substring(1), StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsQuantityField(string field)
        {
            return field.Contains("quantity", StringComparison.OrdinalIgnoreCase)
                || field.Contains("qty", StringComparison.OrdinalIgnoreCase);
        }

        private static bool LooksLikeLegacyDate(string value)
        {
            return DateTime.TryParseExact(value, new[] { "MM/dd/yyyy", "M/d/yyyy", "yyyyMMdd", "dd/MM/yyyy" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }

        private static int Decimals(decimal value)
        {
            return (decimal.GetBits(value)[3] >> 16) & 0xFF;
        }
    }
}
