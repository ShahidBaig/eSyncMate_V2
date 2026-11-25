using eSyncMate.DB;
using System.Data;

namespace eSyncMate.Processor.Managers
{
    /// <summary>
    /// The two numbers of a Shopify order (Task 00006).
    ///   PO number     = the order name without its "#" ("#175205" -> "175205"): what the client, the Shopify admin
    ///                   search and SPARS (CustomerPO) use. Saved in Orders.OrderNumber, shown as Customer PO.
    ///   Shopify id    = legacyResourceId ("7490956558402"): the only number the Shopify API accepts
    ///                   (gid://shopify/Order/...). Saved in Orders.CustomerOrderNo and in the API-JSON.
    /// Orders saved before this change have them the other way round (OrderNumber = Shopify id,
    /// CustomerOrderNo = "#175205"); GetShopifyOrderId reads the id from the saved API-JSON, so both kinds work.
    /// </summary>
    public static class ShopifyOrderRef
    {
        public static string PoNumber(string p_Name, string p_ShopifyId)
        {
            string l_Name = (p_Name ?? string.Empty).Trim().TrimStart('#').Trim();

            return l_Name.Length > 0 ? l_Name : (p_ShopifyId ?? string.Empty).Trim();
        }

        // The Shopify order id of an eSyncMate order; p_OrderNumber is returned when nothing better is found
        public static string GetShopifyOrderId(string p_ConnectionString, int p_OrderId, string p_OrderNumber)
        {
            DataTable l_Table = new DataTable();

            try
            {
                new DBConnector(p_ConnectionString).GetData(
                    $@"SELECT O.CustomerOrderNo,
                              (SELECT TOP 1 CASE WHEN ISJSON(D.Data) = 1 THEN JSON_VALUE(D.Data, '$.legacyResourceId') END
                                 FROM OrderData D WITH (NOLOCK)
                                WHERE D.OrderId = O.Id AND D.Type = 'API-JSON'
                                ORDER BY D.Id DESC) AS ShopifyId
                         FROM Orders O WITH (NOLOCK)
                        WHERE O.Id = {p_OrderId}", ref l_Table);

                if (l_Table.Rows.Count > 0)
                {
                    string l_FromJson = Convert.ToString(l_Table.Rows[0]["ShopifyId"])?.Trim() ?? string.Empty;

                    if (IsId(l_FromJson))
                        return l_FromJson;

                    string l_FromOrder = Convert.ToString(l_Table.Rows[0]["CustomerOrderNo"])?.Trim() ?? string.Empty;

                    if (IsId(l_FromOrder))
                        return l_FromOrder;
                }
            }
            catch
            {
                // the caller's Shopify call then fails with "Shopify has no order ..." and is logged there
            }
            finally
            {
                l_Table.Dispose();
            }

            return (p_OrderNumber ?? string.Empty).Trim();
        }

        private static bool IsId(string p_Value)
        {
            return p_Value.Length > 0 && p_Value.All(char.IsAsciiDigit);
        }
    }
}
