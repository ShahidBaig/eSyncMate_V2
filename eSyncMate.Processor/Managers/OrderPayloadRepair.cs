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
        private const string TitleKey = "\"Title\":\"";

        // "IsGift" always follows Title in the Amazon order-items payload, so the end of the value
        // is found without trusting the (broken) title text itself.
        private const string NextKey = "\",\"IsGift\"";

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
                int l_Start = l_Json.IndexOf(TitleKey, l_From, StringComparison.Ordinal);
                if (l_Start < 0)
                    break;

                int l_ValueAt = l_Start + TitleKey.Length;

                int l_End = l_Json.IndexOf(NextKey, l_ValueAt, StringComparison.Ordinal);
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
    }
}
