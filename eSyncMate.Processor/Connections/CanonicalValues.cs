using System.Buffers;
using System.Globalization;
using System.Text.Json;

namespace eSyncMate.Processor.Connections
{
    /// <summary>
    /// Conversions at the canonical boundary (X-02, X-03, requirement E1).
    ///
    /// Conventions, from M1-Contracts/schemas/common.schema.json:
    ///   date      YYYY-MM-DD, date-only, no timezone
    ///   dateTime  ISO-8601 UTC with Z, e.g. 2026-09-02T09:14:51Z
    ///   money     JSON number with an explicit decimal point - 12.50, never 1250, never "12.50"
    ///   quantity  JSON number, not negative
    ///   unknown   null or the field omitted - NEVER an empty string standing in for "not provided"
    ///
    /// Why this class exists rather than a set of ad-hoc conversions at each call site: the legacy
    /// map helpers do the opposite of every one of those rules, and they do it quietly.
    ///
    ///   Transformations.formatTotalAmount("12.50") returns the STRING "1250" - it strips the
    ///     decimal point, produces implicit cents, and hands back text. Run it on a canonical
    ///     document and 12.50 becomes "1250": the exact failure X-03 exists to prevent.
    ///   Transformations.getCurrentDate() and getCurrentTime() both use DateTime.Now, so they carry
    ///     the server's offset and no zone marker.
    ///   Transformations.UnixTimeStampToDateTime() calls ToLocalTime(), converting a UTC input INTO
    ///     server-local - the wrong direction entirely.
    ///   The production 850 map emits MM/dd/yyyy for most dates but passes shipNotBefore through as
    ///     the raw DTM yyyyMMdd, so it is not even internally consistent.
    ///
    /// None of those helpers may run on a canonical payload. Use the conversions here instead.
    /// </summary>
    public static class CanonicalValues
    {
        /// <summary>
        /// Fields where an empty string is meaningful rather than a stand-in for "not provided",
        /// and so must survive <see cref="PruneEmpty"/> and pass the guard.
        ///
        /// The 860's changeInstructions is the documented case: an empty string there genuinely
        /// blanks the field, which is exactly why W4-08 insists empty and absent stay distinct.
        /// Shared with CanonicalGuard so the normaliser and the check can never disagree.
        /// </summary>
        public static readonly IReadOnlySet<string> EmptyIsMeaningful =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "changeInstructions" };

        /// <summary>
        /// Drops object properties whose value is an empty string, which the canonical contract
        /// forbids: an unknown value is null or the field is omitted, never "".
        ///
        /// This is done at the boundary rather than in each map on purpose. The maps are text in a
        /// database row with no schema and no compiled type behind them (F-1), and an X12 element
        /// that is absent and one that is present but empty are the same thing on the wire - so
        /// "" is what a map naturally produces for anything the partner did not send. Requiring
        /// every map, for fifteen document types and every partner, to remember the distinction is
        /// the fragile arrangement; normalising once here is not. The guard still runs afterwards
        /// and still fails anything genuinely wrong, a legacy date or money-as-a-string included.
        ///
        /// Empty strings inside an ARRAY are left alone: dropping one would shift the indices of
        /// everything after it, which changes meaning rather than tidying it.
        /// </summary>
        public static JsonElement PruneEmpty(JsonElement payload)
        {
            var l_Buffer = new ArrayBufferWriter<byte>();

            using (var l_Writer = new Utf8JsonWriter(l_Buffer))
            {
                WritePruned(payload, l_Writer);
            }

            var l_Reader = new Utf8JsonReader(l_Buffer.WrittenSpan);

            using JsonDocument l_Document = JsonDocument.ParseValue(ref l_Reader);

            return l_Document.RootElement.Clone();
        }

        private static void WritePruned(JsonElement element, Utf8JsonWriter writer)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    writer.WriteStartObject();

                    foreach (JsonProperty l_Property in element.EnumerateObject())
                    {
                        if (l_Property.Value.ValueKind == JsonValueKind.String
                            && l_Property.Value.GetString()?.Length == 0
                            && !EmptyIsMeaningful.Contains(l_Property.Name))
                        {
                            continue;
                        }

                        writer.WritePropertyName(l_Property.Name);
                        WritePruned(l_Property.Value, writer);
                    }

                    writer.WriteEndObject();
                    break;

                case JsonValueKind.Array:
                    writer.WriteStartArray();

                    foreach (JsonElement l_Item in element.EnumerateArray())
                    {
                        WritePruned(l_Item, writer);
                    }

                    writer.WriteEndArray();
                    break;

                default:
                    element.WriteTo(writer);
                    break;
            }
        }

        /// <summary>ISO-8601 UTC with Z, to the second, as the contract's example shows.</summary>
        public const string InstantFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

        public const string DateFormat = "yyyy-MM-dd";

        /// <summary>Money carries up to 4 decimals (common.schema.json).</summary>
        public const int MoneyDecimals = 4;

        /// <summary>
        /// The legacy shapes a map may hand us, in the order they are tried. X12 dates are
        /// yyyyMMdd; formatDate emits MM/dd/yyyy; a few partner feeds send dd/MM/yyyy.
        /// </summary>
        private static readonly string[] _dateFormats =
        {
            "yyyy-MM-dd", "yyyyMMdd", "MM/dd/yyyy", "M/d/yyyy", "dd/MM/yyyy", "yyMMdd"
        };

        private static readonly string[] _dateTimeFormats =
        {
            "yyyy-MM-dd'T'HH:mm:ss'Z'", "yyyy-MM-dd'T'HH:mm:ss.fff'Z'", "yyyy-MM-dd'T'HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss", "yyyyMMddHHmmss", "yyyyMMddHHmm", "MM/dd/yyyy HH:mm:ss",
            "MM/dd/yyyy hh:mm tt", "MM/dd/yyyy"
        };

        // -----------------------------------------------------------------------------------
        // Unknown values
        // -----------------------------------------------------------------------------------

        /// <summary>
        /// The contract is explicit: an unknown value is null or omitted, never an empty string.
        /// The production 850 map breaks this in at least six places (externalid, vendorStyle,
        /// acceptedQuantity, plannedDeliveryDate, supplier.code, supplier.phone), so every string
        /// heading for a canonical payload passes through here.
        /// </summary>
        public static string? NullIfBlank(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        // -----------------------------------------------------------------------------------
        // Timestamps (X-02)
        // -----------------------------------------------------------------------------------

        /// <summary>
        /// A canonical instant, in UTC with Z.
        ///
        /// <paramref name="sourceZone"/> is required and has no default on purpose. A legacy value
        /// carries no offset, so something has to say what it meant - and silently assuming the
        /// server's zone is the bug X-02 exists to remove. State the partner's zone, or pass
        /// <see cref="TimeZoneInfo.Utc"/> when the source really is UTC.
        /// </summary>
        public static string? ToInstant(string? value, TimeZoneInfo sourceZone)
        {
            if (sourceZone is null)
            {
                throw new ArgumentNullException(nameof(sourceZone),
                    "The source zone must be stated. Assuming the server's zone is exactly what X-02 removes.");
            }

            string? l_Value = NullIfBlank(value);

            if (l_Value is null)
            {
                return null;
            }

            // Already an instant with an offset: honour it and normalise to UTC.
            if (DateTimeOffset.TryParse(l_Value, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTimeOffset l_Offset)
                && HasExplicitOffset(l_Value))
            {
                return l_Offset.UtcDateTime.ToString(InstantFormat, CultureInfo.InvariantCulture);
            }

            if (!TryParseNaive(l_Value, out DateTime l_Naive))
            {
                return null;
            }

            return ToInstant(l_Naive, sourceZone);
        }

        /// <summary>
        /// A canonical instant from a <see cref="DateTime"/>.
        ///
        /// An Unspecified kind is interpreted in <paramref name="sourceZone"/> rather than assumed
        /// to be anything. A Local kind is converted from the server's zone, which is correct but
        /// usually means the value was captured with DateTime.Now and should not have been.
        /// </summary>
        public static string ToInstant(DateTime value, TimeZoneInfo sourceZone)
        {
            if (sourceZone is null)
            {
                throw new ArgumentNullException(nameof(sourceZone));
            }

            DateTime l_Utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), sourceZone)
            };

            return l_Utc.ToString(InstantFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>Now, as a canonical instant. The replacement for getCurrentDate/getCurrentTime.</summary>
        public static string NowInstant()
        {
            return DateTime.UtcNow.ToString(InstantFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// A canonical date - YYYY-MM-DD, no timezone. A date has no zone by definition, so unlike
        /// <see cref="ToInstant(string?, TimeZoneInfo)"/> nothing needs to be stated: shifting a
        /// purchase-order date by an offset would be wrong, not more precise.
        /// </summary>
        public static string? ToDate(string? value)
        {
            string? l_Value = NullIfBlank(value);

            if (l_Value is null)
            {
                return null;
            }

            return TryParseNaive(l_Value, out DateTime l_Parsed)
                ? l_Parsed.ToString(DateFormat, CultureInfo.InvariantCulture)
                : null;
        }

        public static string ToDate(DateTime value)
        {
            return value.ToString(DateFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>True when the text already meets the canonical instant convention.</summary>
        public static bool IsCanonicalInstant(string? value)
        {
            return value is not null
                && DateTime.TryParseExact(value, new[] { "yyyy-MM-dd'T'HH:mm:ss'Z'", "yyyy-MM-dd'T'HH:mm:ss.fff'Z'" },
                    CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out _);
        }

        /// <summary>True when the text already meets the canonical date convention.</summary>
        public static bool IsCanonicalDate(string? value)
        {
            return value is not null
                && DateTime.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }

        // -----------------------------------------------------------------------------------
        // Money and quantity (X-03)
        // -----------------------------------------------------------------------------------

        /// <summary>
        /// A canonical money value: a decimal with an explicit point, up to 4 places.
        ///
        /// Never call Transformations.formatTotalAmount on anything heading for a canonical
        /// payload. It returns the string "1250" for 12.50 - implicit cents, as text - which is
        /// both of the forms the contract forbids.
        /// </summary>
        public static decimal? ToMoney(string? value)
        {
            string? l_Value = NullIfBlank(value);

            if (l_Value is null)
            {
                return null;
            }

            if (!decimal.TryParse(l_Value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal l_Parsed))
            {
                return null;
            }

            return Math.Round(l_Parsed, MoneyDecimals, MidpointRounding.ToEven);
        }

        public static decimal ToMoney(decimal value)
        {
            return Math.Round(value, MoneyDecimals, MidpointRounding.ToEven);
        }

        /// <summary>
        /// Reverses implicit cents, for a partner feed that genuinely sends them.
        ///
        /// This is the only place in the canonical path that may divide by 100, and it exists so
        /// that a map which must handle an implicit-cents partner says so explicitly at the call
        /// site rather than reaching for formatTotalAmount and getting a string back.
        /// </summary>
        public static decimal? FromImplicitCents(string? value)
        {
            string? l_Value = NullIfBlank(value);

            if (l_Value is null)
            {
                return null;
            }

            if (l_Value.Contains('.'))
            {
                throw new ArgumentException(
                    $"'{l_Value}' already carries a decimal point, so it is not implicit cents. " +
                    "Use ToMoney instead - dividing this by 100 would silently make it a hundredth of itself.",
                    nameof(value));
            }

            return long.TryParse(l_Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l_Cents)
                ? Math.Round(l_Cents / 100m, MoneyDecimals, MidpointRounding.ToEven)
                : null;
        }

        /// <summary>A canonical quantity: a number, never negative (common.schema.json minimum 0).</summary>
        public static decimal? ToQuantity(string? value)
        {
            string? l_Value = NullIfBlank(value);

            if (l_Value is null)
            {
                return null;
            }

            if (!decimal.TryParse(l_Value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal l_Parsed))
            {
                return null;
            }

            return l_Parsed < 0 ? null : l_Parsed;
        }

        // -----------------------------------------------------------------------------------

        private static bool HasExplicitOffset(string value)
        {
            // Z, +hh:mm or -hh:mm at the end. Without one, DateTimeOffset.TryParse would happily
            // invent an offset from the machine, which is the assumption being removed.
            if (value.EndsWith("Z", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            int l_T = value.IndexOf('T');

            if (l_T < 0)
            {
                l_T = value.IndexOf(' ');
            }

            if (l_T < 0)
            {
                return false;
            }

            string l_Time = value.Substring(l_T);

            return l_Time.Contains('+') || l_Time.LastIndexOf('-') > 0;
        }

        private static bool TryParseNaive(string value, out DateTime parsed)
        {
            if (DateTime.TryParseExact(value, _dateTimeFormats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out parsed))
            {
                return true;
            }

            if (DateTime.TryParseExact(value, _dateFormats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out parsed))
            {
                return true;
            }

            // Unix milliseconds, which some marketplace feeds send. Note this is genuinely UTC, so
            // it must NOT go through ToLocalTime the way UnixTimeStampToDateTime does.
            if (value.Length >= 10 && value.All(char.IsDigit)
                && long.TryParse(value, out long l_Unix) && l_Unix > 0)
            {
                parsed = DateTimeOffset.FromUnixTimeMilliseconds(
                    value.Length <= 11 ? l_Unix * 1000 : l_Unix).UtcDateTime;

                return true;
            }

            parsed = default;

            return false;
        }
    }
}
