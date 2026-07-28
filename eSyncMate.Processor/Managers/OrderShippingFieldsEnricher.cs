using Newtonsoft.Json.Linq;
using System.Globalization;

namespace eSyncMate.Processor.Managers
{
    /// <summary>
    /// Normalises four shipping fields onto the stored marketplace order JSON (OrderData 'API-JSON')
    /// so every partner map can read them from the same place:
    ///
    ///     $.WarehouseCode      -> detail WHSID
    ///     $.ShippingCode       -> header ShipViaCode
    ///     $.ShippingAgentCode  -> header ship agent
    ///     $.ShipDate           -> header ShipDate (already formatted)
    ///
    /// Each partner exposes these differently — or not at all. A value already present on the JSON
    /// is never overwritten, which is what lets a user correct one from the Order screen and
    /// re-process without the enrichment putting the old value back.
    /// </summary>
    public static class OrderShippingFieldsEnricher
    {
        public const string KeyWarehouse = "WarehouseCode";
        public const string KeyShippingCode = "ShippingCode";
        public const string KeyShippingAgent = "ShippingAgentCode";
        public const string KeyShipDate = "ShipDate";

        /// <summary>Date format the ERP payload currently uses for OrderDate/ShipDate/CancelDate.</summary>
        public const string ShipDateFormat = "MM/dd/yyyy";

        public class EnrichResult
        {
            public string Json { get; set; }
            public List<string> Missing { get; } = new List<string>();
            public bool IsComplete { get { return Missing.Count == 0; } }
        }

        public static EnrichResult Enrich(string p_ApiJson, string p_CustomerID)
        {
            var l_Result = new EnrichResult { Json = p_ApiJson };

            if (string.IsNullOrWhiteSpace(p_ApiJson))
            {
                l_Result.Missing.Add("Order payload");
                return l_Result;
            }

            JObject l_Root;

            try
            {
                l_Root = JObject.Parse(p_ApiJson);
            }
            catch
            {
                l_Result.Missing.Add("Order payload (not readable)");
                return l_Result;
            }

            // Only what the marketplace actually sent — no defaults, no lookups. Anything the
            // payload does not carry stays empty and is filled by the user from the Orders screen.
            SetIfEmpty(l_Root, KeyWarehouse, ReadWarehouse(l_Root));
            SetIfEmpty(l_Root, KeyShipDate, ReadShipDate(l_Root));
            SetIfEmpty(l_Root, KeyShippingCode, ReadShippingCode(l_Root));
            SetIfEmpty(l_Root, KeyShippingAgent, ReadShippingAgentCode(l_Root));

            foreach (string l_Key in new[] { KeyWarehouse, KeyShipDate, KeyShippingCode, KeyShippingAgent })
            {
                if (string.IsNullOrWhiteSpace(Read(l_Root, l_Key)))
                {
                    l_Result.Missing.Add(Label(l_Key));
                }
            }

            l_Result.Json = l_Root.ToString(Newtonsoft.Json.Formatting.None);

            return l_Result;
        }

        public static string Label(string p_Key)
        {
            switch (p_Key)
            {
                case KeyWarehouse: return "Warehouse Code";
                case KeyShippingCode: return "Shipping Code";
                case KeyShippingAgent: return "Shipping Agent Code";
                case KeyShipDate: return "Ship Date";
                default: return p_Key;
            }
        }

        public static string Read(JObject p_Root, string p_Key)
        {
            JToken l_Token = p_Root[p_Key];
            return l_Token == null || l_Token.Type == JTokenType.Null ? string.Empty : l_Token.ToString().Trim();
        }

        private static void SetIfEmpty(JObject p_Root, string p_Key, string p_Value)
        {
            if (string.IsNullOrWhiteSpace(p_Value)) return;
            if (!string.IsNullOrWhiteSpace(Read(p_Root, p_Key))) return;

            p_Root[p_Key] = p_Value;
        }

        /// <summary>
        /// Carrier, if the payload names one. None of the current marketplaces send a carrier —
        /// they send a service level — so in practice this stays empty and the user supplies it.
        /// </summary>
        private static string ReadShippingCode(JObject p_Root)
        {
            string l_Value = p_Root.SelectToken("$.shipping_carrier_code")?.ToString()            // Mirakl
                          ?? p_Root.SelectToken("$.shipping_company")?.ToString()                 // Mirakl
                          ?? p_Root.SelectToken("$.orderLines.orderLine[0].orderLineStatuses.orderLineStatus[0].trackingInfo.carrierName.carrier")?.ToString(); // Walmart

            return string.IsNullOrWhiteSpace(l_Value) ? string.Empty : l_Value.Trim();
        }

        /// <summary>Service level, where the payload carries one.</summary>
        private static string ReadShippingAgentCode(JObject p_Root)
        {
            string l_Value = p_Root.SelectToken("$.ShipmentServiceLevelCategory")?.ToString()      // Amazon
                          ?? p_Root.SelectToken("$.shipping_type_code")?.ToString()                // Mirakl
                          ?? p_Root.SelectToken("$.shippingInfo.methodCode")?.ToString()           // Walmart
                          ?? p_Root.SelectToken("$.orderLines[0].serviceLevel")?.ToString();       // Michaels

            return string.IsNullOrWhiteSpace(l_Value) ? string.Empty : l_Value.Trim();
        }

        // ---------------- Warehouse ----------------

        private static string ReadWarehouse(JObject p_Root)
        {
            // Mirakl (Knot / Lowe's / Macy's): order_lines[].shipping_from.warehouse.code
            string l_Value = p_Root.SelectToken("$.order_lines[0].shipping_from.warehouse.code")?.ToString();
            if (!string.IsNullOrWhiteSpace(l_Value)) return l_Value.Trim();

            // Amazon: "L55 Whitestown IN" / "L65, Patterson CA" -> leading warehouse token
            l_Value = p_Root.SelectToken("$.DefaultShipFromLocationAddress.Name")?.ToString();
            if (!string.IsNullOrWhiteSpace(l_Value)) return LeadingWarehouseCode(l_Value);

            return string.Empty;
        }

        /// <summary>
        /// Amazon names start with the warehouse code — the separator varies ("L55 Whitestown IN",
        /// "L65, Patterson CA"), so the first token is taken rather than matching the whole name.
        /// </summary>
        private static string LeadingWarehouseCode(string p_Name)
        {
            string l_Token = p_Name.Trim().Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            l_Token = l_Token.Trim().TrimEnd(',');

            if (l_Token.Length >= 2 && char.ToUpperInvariant(l_Token[0]) == 'L' && l_Token.Skip(1).All(char.IsDigit))
            {
                return l_Token.ToUpperInvariant();
            }

            return string.Empty;
        }

        // ---------------- Ship date ----------------

        private static string ReadShipDate(JObject p_Root)
        {
            // Amazon
            string l_Value = p_Root.SelectToken("$.LatestShipDate")?.ToString();
            if (TryFormat(l_Value, out string l_Formatted)) return l_Formatted;

            // Target
            l_Value = p_Root.SelectToken("$.requested_shipment_date")?.ToString();
            if (TryFormat(l_Value, out l_Formatted)) return l_Formatted;

            // Mirakl (Knot / Lowe's / Macy's)
            l_Value = p_Root.SelectToken("$.shipping_deadline")?.ToString();
            if (TryFormat(l_Value, out l_Formatted)) return l_Formatted;

            // Walmart and Michaels send epoch milliseconds
            l_Value = p_Root.SelectToken("$.shippingInfo.estimatedShipDate")?.ToString()
                   ?? p_Root.SelectToken("$.promiseShipDate")?.ToString();
            if (TryFormatEpoch(l_Value, out l_Formatted)) return l_Formatted;

            return string.Empty;
        }

        private static bool TryFormat(string p_Value, out string p_Formatted)
        {
            p_Formatted = string.Empty;

            if (string.IsNullOrWhiteSpace(p_Value)) return false;

            if (DateTime.TryParse(p_Value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime l_Date))
            {
                p_Formatted = l_Date.ToString(ShipDateFormat);
                return true;
            }

            return false;
        }

        private static bool TryFormatEpoch(string p_Value, out string p_Formatted)
        {
            p_Formatted = string.Empty;

            if (string.IsNullOrWhiteSpace(p_Value)) return false;

            if (long.TryParse(p_Value, out long l_Epoch) && l_Epoch > 0)
            {
                p_Formatted = DateTimeOffset.FromUnixTimeMilliseconds(l_Epoch).UtcDateTime.ToString(ShipDateFormat);
                return true;
            }

            return TryFormat(p_Value, out p_Formatted);
        }
    }
}
