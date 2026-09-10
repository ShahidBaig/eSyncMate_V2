using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace eSyncMate.Processor.Managers
{
    /// <summary>
    /// Repairs a stored marketplace order payload (OrderData 'API-JSON') that will not parse.
    ///
    /// Seen on Amazon lighting orders: the product Title carries an inch mark that reaches the
    /// stored JSON unescaped —
    ///
    ///     "Title":"SAFAVIEH Lighting Davielle Wall Sconce, 21"MetalChromeSCN4120C","IsGift":"false"
    ///
    /// The quote ends the string early, so the WHOLE document is unreadable and the order can
    /// never be placed (JsonReaderException out of JUST, naming no order). Title is decorative —
    /// no code and no ERP map reads it — so the quotes inside it are neutralised, which is the
    /// same rule AmazonGetOrdersRoute now applies at ingestion.
    ///
    /// Only the Title value is touched. Everything else is left byte for byte as it was, and a
    /// payload that does not become valid JSON is rejected rather than saved half-fixed.
    /// </summary>
    public static class OrderPayloadRepair
    {
        private const string TitleKey = "\"Title\"";

        // "IsGift" always follows Title in the Amazon order-items payload, so the end of the value
        // is found without trusting the (broken) title text itself.
        private const string NextKey = "\"IsGift\"";

        private const int MaxLines = 100;

        public static bool IsValid(string p_Json)
        {
            if (string.IsNullOrWhiteSpace(p_Json))
                return false;

            try
            {
                JToken.Parse(p_Json);
                return true;
            }
            catch (JsonReaderException)
            {
                return false;
            }
        }

        /// <summary>
        /// True when the payload was unreadable and is now valid; p_Repaired then holds the fixed
        /// JSON. False when it is already valid (nothing to do) or could not be salvaged — in both
        /// of those cases p_Repaired is the original string.
        /// </summary>
        public static bool TryRepair(string p_Json, out string p_Repaired)
        {
            p_Repaired = p_Json;

            if (string.IsNullOrWhiteSpace(p_Json) || IsValid(p_Json))
                return false;

            string l_Json = p_Json;
            int l_From = 0;

            for (int l_Line = 0; l_Line < MaxLines; l_Line++)
            {
                int l_ValueAt = FindValueStart(l_Json, l_From);
                if (l_ValueAt < 0)
                    break;

                int l_End = FindValueEnd(l_Json, l_ValueAt);
                if (l_End < 0)
                    break;

                string l_Clean = l_Json.Substring(l_ValueAt, l_End - l_ValueAt)
                                       .Replace("\\", " ")
                                       .Replace("\"", " in ")
                                       .Trim();

                l_Json = l_Json.Substring(0, l_ValueAt) + l_Clean + l_Json.Substring(l_End);
                l_From = l_ValueAt + l_Clean.Length;
            }

            if (!IsValid(l_Json))
                return false;

            p_Repaired = l_Json;
            return true;
        }

        /// <summary>
        /// Index of the first character of the Title value. A payload that has been through a
        /// JObject round trip is indented — "Title": "…" — so the whitespace either side of the
        /// colon is stepped over rather than assumed away. -1 when there is no further Title.
        /// </summary>
        private static int FindValueStart(string p_Json, int p_From)
        {
            int l_At = p_From;

            while (true)
            {
                int l_Key = p_Json.IndexOf(TitleKey, l_At, StringComparison.Ordinal);
                if (l_Key < 0)
                    return -1;

                int l_Colon = SkipWhitespace(p_Json, l_Key + TitleKey.Length);

                if (l_Colon >= 0 && p_Json[l_Colon] == ':')
                {
                    int l_Quote = SkipWhitespace(p_Json, l_Colon + 1);

                    if (l_Quote >= 0 && p_Json[l_Quote] == '"')
                        return l_Quote + 1;
                }

                l_At = l_Key + TitleKey.Length;
            }
        }

        /// <summary>
        /// Index of the quote that closes the Title value, located from the "IsGift" key that
        /// follows it so the broken title text itself is never trusted. -1 unless the shape really
        /// is value","IsGift", whitespace around the comma allowed.
        /// </summary>
        private static int FindValueEnd(string p_Json, int p_ValueAt)
        {
            int l_Key = p_Json.IndexOf(NextKey, p_ValueAt, StringComparison.Ordinal);
            if (l_Key < 0)
                return -1;

            int l_At = SkipWhitespaceBack(p_Json, l_Key - 1);
            if (l_At < p_ValueAt || p_Json[l_At] != ',')
                return -1;

            l_At = SkipWhitespaceBack(p_Json, l_At - 1);
            if (l_At < p_ValueAt || p_Json[l_At] != '"')
                return -1;

            return l_At;
        }

        private static int SkipWhitespace(string p_Json, int p_At)
        {
            while (p_At < p_Json.Length && char.IsWhiteSpace(p_Json[p_At]))
                p_At++;

            return p_At < p_Json.Length ? p_At : -1;
        }

        private static int SkipWhitespaceBack(string p_Json, int p_At)
        {
            while (p_At >= 0 && char.IsWhiteSpace(p_Json[p_At]))
                p_At--;

            return p_At;
        }
    }
}
