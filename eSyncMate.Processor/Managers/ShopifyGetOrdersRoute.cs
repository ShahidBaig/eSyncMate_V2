using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;
using Newtonsoft.Json;
using System.Data;
using static eSyncMate.DB.Declarations;
using SO = eSyncMate.Processor.Models.ShopifyOrdersResponseModel;

namespace eSyncMate.Processor.Managers
{
    /// <summary>
    /// Shopify - Get Orders (RouteType 82, Task 00006).
    /// Reads the store's orders (GraphQL orders, 25 per page, newest last) and saves each one that eSyncMate does not
    /// have yet into Orders (Status New), OrderData (API-JSON = the Shopify order) and OrderDetail (one row per unit),
    /// the same way the Walmart / Lowes / Amazon Get Orders routes do. Shopify has no acknowledge step.
    /// The filter comes from the source connector Parameters, so it changes without a code change:
    ///   query = Shopify order search, default "(financial_status:paid OR financial_status:pending) fulfillment_status:unfulfilled status:open"
    ///   days  = only orders created in the last N days, default 10
    /// Gift card and digital (no shipping) lines are left out; an order with nothing to ship is not saved.
    /// OrderNumber = the Shopify order id (e.g. 8690887852218): unique across stores, unlike the "#1001" name,
    /// which goes into CustomerOrderNo. ExternalId is left empty (Place Order sets it to the ERP's id);
    /// OrderDetail.order_line_id = the line item gid.
    /// </summary>
    public class ShopifyGetOrdersRoute
    {
        private const string DefaultQuery = "(financial_status:paid OR financial_status:pending) fulfillment_status:unfulfilled status:open";
        private const int DefaultDays = 10;

        // requested cost ~ 25 x (1 + 30 lines + 3 shipping lines) = ~850, under Shopify's 1,000 per query;
        // the bucket is charged the actual cost, which is far lower for normal 1-5 line orders
        private const string OrdersQuery = @"query Orders($q: String!, $after: String) {
  orders(first: 25, after: $after, query: $q, sortKey: CREATED_AT) {
    nodes {
      id legacyResourceId name createdAt displayFinancialStatus displayFulfillmentStatus email phone note currencyCode
      totalPriceSet { shopMoney { amount currencyCode } }
      shippingAddress { name company address1 address2 city province provinceCode zip country countryCodeV2 phone }
      billingAddress { name company address1 address2 city province provinceCode zip country countryCodeV2 phone }
      shippingLines(first: 3) { nodes { title code carrierIdentifier originalPriceSet { shopMoney { amount currencyCode } } } }
      lineItems(first: 30) {
        nodes {
          id sku name quantity currentQuantity isGiftCard requiresShipping
          originalUnitPriceSet { shopMoney { amount currencyCode } }
          discountedUnitPriceSet { shopMoney { amount currencyCode } }
          variant { id inventoryItem { id requiresShipping } }
        }
        pageInfo { hasNextPage endCursor }
      }
    }
    pageInfo { hasNextPage endCursor }
  }
}";

        // an order with more than 30 lines: the rest of its lines, 100 per call
        private const string MoreLinesQuery = @"query MoreLines($id: ID!, $after: String) {
  order(id: $id) {
    lineItems(first: 100, after: $after) {
      nodes {
        id sku name quantity currentQuantity isGiftCard requiresShipping
        originalUnitPriceSet { shopMoney { amount currencyCode } }
        discountedUnitPriceSet { shopMoney { amount currencyCode } }
        variant { id inventoryItem { id requiresShipping } }
      }
      pageInfo { hasNextPage endCursor }
    }
  }
}";

        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;
            DataTable l_Existing = new DataTable();
            Customers l_Customer = new Customers();
            Orders l_Orders = new Orders();

            try
            {
                ConnectorDataModel l_SourceConnector = ConnectorDataModel.Deserialize(route.SourceConnectorObject.Data);
                ConnectorDataModel l_DestinationConnector = ConnectorDataModel.Deserialize(route.DestinationConnectorObject.Data);

                route.SaveLog(LogTypeEnum.Info, $"Started executing route [{route.Id}]", string.Empty, userNo);

                if (l_SourceConnector == null || l_SourceConnector.ConnectivityType != ConnectorTypesEnum.Rest.ToString())
                {
                    route.SaveLog(LogTypeEnum.Error, "Source Connector is not setup properly (Shopify REST connector expected)", string.Empty, userNo);
                    return;
                }

                if (l_DestinationConnector == null || l_DestinationConnector.ConnectivityType != ConnectorTypesEnum.SqlServer.ToString())
                {
                    route.SaveLog(LogTypeEnum.Error, "Destination Connector is not setup properly (eSyncMate SQL Server connector expected)", string.Empty, userNo);
                    return;
                }

                string l_CustomerId = l_SourceConnector.CustomerID;

                l_Orders.UseConnection(l_DestinationConnector.ConnectionString);
                l_Customer.UseConnection(l_DestinationConnector.ConnectionString);

                if (!l_Customer.GetObject("ERPCustomerID", l_CustomerId).IsSuccess || l_Customer.Id <= 0)
                {
                    route.SaveLog(LogTypeEnum.Error, $"Customer [{l_CustomerId}] (source connector CustomerID) not found in Customers. Nothing was read.", string.Empty, userNo);
                    return;
                }

                string l_Filter = GetParameter(l_SourceConnector, "query", DefaultQuery);
                int l_Days = int.TryParse(GetParameter(l_SourceConnector, "days", DefaultDays.ToString()), out int l_D) && l_D > 0 ? l_D : DefaultDays;
                string l_Query = $"{l_Filter} created_at:>='{DateTime.UtcNow.AddDays(-l_Days):yyyy-MM-ddTHH:mm:ssZ}'";

                // RestConnector puts connector Parameters on the URL; these two are only the filter
                l_SourceConnector.Parmeters = new List<eSyncMate.Processor.Models.Parameter>();

                Dictionary<string, string> l_ItemCodes = LoadItemCodes(l_DestinationConnector.ConnectionString, l_CustomerId, route, userNo);

                int l_Pages = 0, l_Read = 0, l_Saved = 0, l_SavedWithError = 0, l_Known = 0, l_Skipped = 0, l_NothingToShip = 0;
                bool l_Completed = true;
                string l_After = null;

                do
                {
                    ShopifyGraphQLResult<SO> l_Page =
                        ShopifyGraphQL.Execute<SO>(l_SourceConnector, OrdersQuery, new { q = l_Query, after = l_After });

                    SO.Orders l_List = l_Page.Success ? l_Page.Response.data?.orders : null;

                    if (l_List == null)
                    {
                        route.SaveLog(LogTypeEnum.Error, $"Shopify orders failed for [{l_CustomerId}] on page {l_Pages + 1}: {(l_Page.Success ? "no data in the response" : l_Page.Error)}. Stopping; the next run reads them again.", l_Page.ResponseBody, userNo);
                        l_Completed = false;
                        break;
                    }

                    l_Pages++;

                    foreach (SO.Order l_Order in l_List.nodes)
                    {
                        l_Read++;

                        string l_OrderNumber = l_Order?.legacyResourceId?.Trim() ?? string.Empty;

                        if (string.IsNullOrEmpty(l_OrderNumber))
                            continue;

                        l_Existing = new DataTable();

                        if (l_Orders.GetViewList($"OrderNumber = '{l_OrderNumber.Replace("'", "''")}'", "Id", ref l_Existing))
                        {
                            l_Known++;
                            continue;
                        }

                        // a partial order would reach the ERP without some of its lines: the rest are read first,
                        // and if that fails nothing of the order is written (the next run tries again)
                        if (l_Order.lineItems?.pageInfo?.hasNextPage == true && !ReadRemainingLines(l_SourceConnector, l_Order, out string l_LinesError))
                        {
                            l_Skipped++;
                            route.SaveLog(LogTypeEnum.Error, $"Shopify order {l_Order.name} ({l_OrderNumber}) has more than 30 lines and the rest could not be read: {l_LinesError}. Nothing of it was written; the next run tries again.", string.Empty, userNo);
                            continue;
                        }

                        // only gift cards / digital products: nothing for the warehouse, the order is not saved
                        List<SO.LineItem> l_WithQty = (l_Order.lineItems?.nodes ?? new List<SO.LineItem>()).Where(l => l != null && l.currentQuantity > 0).ToList();

                        if (l_WithQty.Count > 0 && !l_WithQty.Any(IsShippable))
                        {
                            l_NothingToShip++;
                            continue;
                        }

                        string l_Problem = SaveOrder(l_Order, l_Customer, l_DestinationConnector, l_ItemCodes, route, userNo);

                        if (l_Problem == null)
                        {
                            l_Skipped++;
                            continue;
                        }

                        l_Saved++;

                        if (l_Problem.Length > 0)
                        {
                            l_SavedWithError++;
                            route.SaveLog(LogTypeEnum.Error, $"Shopify order {l_Order.name} ({l_OrderNumber}) saved with Status ERROR: {l_Problem}", string.Empty, userNo);
                        }
                    }

                    l_After = l_List.pageInfo?.hasNextPage == true ? l_List.pageInfo.endCursor : null;
                }
                while (!string.IsNullOrEmpty(l_After));

                // RouteInfo: SaveLog keeps only Error / Exception / RouteInfo
                route.SaveLog(LogTypeEnum.RouteInfo, $"Shopify orders for [{l_CustomerId}] ({l_Query}): {l_Read} read in {l_Pages} page(s){(l_Completed ? "" : " (stopped early)")}, {l_Saved} new saved ({l_SavedWithError} with Status ERROR), {l_Known} already in eSyncMate, {l_Skipped} not saved, {l_NothingToShip} gift card / digital only (skipped).", string.Empty, userNo);
                route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
            }
            finally
            {
                l_Existing.Dispose();
            }
        }

        // null = not saved; "" = saved New; text = saved with Status ERROR for that reason
        private static string SaveOrder(SO.Order p_Order, Customers p_Customer, ConnectorDataModel p_Destination, Dictionary<string, string> p_ItemCodes, Routes p_Route, int p_UserNo)
        {
            Orders l_Orders = new Orders();
            List<string> l_Problems = new List<string>();

            try
            {
                SO.Address l_Ship = p_Order.shippingAddress;
                SO.Address l_Bill = p_Order.billingAddress;
                SO.ShippingLine l_ShippingLine = p_Order.shippingLines?.nodes?.FirstOrDefault();
                List<SO.LineItem> l_Lines = (p_Order.lineItems?.nodes ?? new List<SO.LineItem>()).Where(l => l != null && l.currentQuantity > 0 && IsShippable(l)).ToList();

                if (l_Ship == null || string.IsNullOrWhiteSpace(l_Ship.address1))
                    l_Problems.Add("no shipping address");

                if (l_Lines.Count == 0)
                    l_Problems.Add("no line with a quantity left");

                List<string> l_NoSku = l_Lines.Where(l => string.IsNullOrWhiteSpace(l.sku)).Select(l => l.name ?? l.id).ToList();

                if (l_NoSku.Count > 0)
                    l_Problems.Add($"line(s) without a SKU: {string.Join(", ", l_NoSku)}");

                // What the ERP map reads, as AmazonGetOrdersRoute does for its lines:
                //   lineNo = 1, 2, 3 ... (Shopify has no line number)
                //   itemId = the ERP ItemId: the SCSInventoryFeed row whose CustomerItemCode is the line's inventory item
                //            number (or its SKU); the SKU itself when no row matches
                // Only shippable lines with a quantity left are kept, so a removed line, a gift card or a digital product
                // never reaches the ERP. The product name is
                // blanked: no map reads it, and a stray quote in a title once broke Amazon payloads.
                int l_No = 0;

                foreach (SO.LineItem l_Line in l_Lines)
                {
                    string l_Sku = l_Line.sku?.Trim() ?? string.Empty;
                    string l_InvNumber = (l_Line.variant?.inventoryItem?.id ?? string.Empty).Split('/').Last();

                    l_Line.lineNo = ++l_No;
                    l_Line.itemId = p_ItemCodes.TryGetValue(l_InvNumber, out string l_ByInv) ? l_ByInv
                                  : p_ItemCodes.TryGetValue(l_Sku, out string l_BySku) ? l_BySku
                                  : l_Sku;
                    l_Line.name = string.Empty;
                }

                if (p_Order.lineItems != null)
                    p_Order.lineItems.nodes = l_Lines;

                l_Orders.UseConnection(p_Destination.ConnectionString);

                l_Orders.Status = l_Problems.Count == 0 ? "New" : "ERROR";
                l_Orders.CustomerId = p_Customer.Id;
                l_Orders.OrderDate = DateTimeOffset.TryParse(p_Order.createdAt, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTimeOffset l_Created)
                    ? l_Created.UtcDateTime : DateTime.UtcNow;
                l_Orders.OrderNumber = p_Order.legacyResourceId;
                l_Orders.CustomerOrderNo = p_Order.name ?? string.Empty;
                l_Orders.ShippingMethod = l_ShippingLine?.code ?? l_ShippingLine?.title ?? string.Empty;
                l_Orders.ShipToName = l_Ship?.name ?? string.Empty;
                l_Orders.ShipToCompanyName = l_Ship?.company ?? string.Empty;
                l_Orders.ShipToAddress1 = l_Ship?.address1 ?? string.Empty;
                l_Orders.ShipToAddress2 = l_Ship?.address2 ?? string.Empty;
                l_Orders.ShipToCity = l_Ship?.city ?? string.Empty;
                l_Orders.ShipToState = l_Ship?.provinceCode ?? l_Ship?.province ?? string.Empty;
                l_Orders.ShipToZip = l_Ship?.zip ?? string.Empty;
                l_Orders.ShipToCountry = l_Ship?.countryCodeV2 ?? l_Ship?.country ?? string.Empty;
                l_Orders.ShipToPhone = l_Ship?.phone ?? p_Order.phone ?? string.Empty;
                l_Orders.ShipToEmail = p_Order.email ?? string.Empty;
                l_Orders.BillToName = l_Bill?.name ?? string.Empty;
                l_Orders.BillToAddress1 = l_Bill?.address1 ?? string.Empty;
                l_Orders.BillToAddress2 = l_Bill?.address2 ?? string.Empty;
                l_Orders.BillToCity = l_Bill?.city ?? string.Empty;
                l_Orders.BillToState = l_Bill?.provinceCode ?? l_Bill?.province ?? string.Empty;
                l_Orders.BillToZip = l_Bill?.zip ?? string.Empty;
                l_Orders.BillToCountry = l_Bill?.countryCodeV2 ?? l_Bill?.country ?? string.Empty;
                l_Orders.BillToPhone = l_Bill?.phone ?? string.Empty;
                l_Orders.BillToEmail = p_Order.email ?? string.Empty;
                l_Orders.IsStoreOrder = false;
                l_Orders.CreatedBy = p_UserNo;
                l_Orders.CreatedDate = DateTime.Now;

                if (!l_Orders.SaveNew().IsSuccess)
                {
                    p_Route.SaveLog(LogTypeEnum.Error, $"Shopify order {p_Order.name} ({p_Order.legacyResourceId}) could not be saved into Orders. The next run tries again.", JsonConvert.SerializeObject(p_Order), p_UserNo);
                    return null;
                }

                OrderData l_Data = new OrderData();

                l_Data.UseConnection(string.Empty, l_Orders.Connection);
                l_Data.DeleteWithType(l_Orders.Id, "API-JSON");

                l_Data.Type = "API-JSON";
                l_Data.Data = JsonConvert.SerializeObject(p_Order);
                l_Data.CreatedBy = p_UserNo;
                l_Data.CreatedDate = DateTime.Now;
                l_Data.OrderId = l_Orders.Id;
                l_Data.OrderNumber = l_Orders.OrderNumber;

                // a half-written order would count as "already in eSyncMate" and never be read again: take it out
                if (!l_Data.SaveNew().IsSuccess)
                    return RemoveOrder(l_Orders, p_Order, "OrderData (API-JSON)", p_Route, p_UserNo);

                // one OrderDetail row per unit, as the other marketplaces do (ASN / cancellation work per unit)
                int l_LineNo = 0;

                foreach (SO.LineItem l_Line in l_Lines)
                {
                    l_LineNo++;

                    for (int i = 1; i <= l_Line.currentQuantity; i++)
                    {
                        OrderDetail l_OrderDetail = new OrderDetail();

                        l_OrderDetail.UseConnection(string.Empty, l_Orders.Connection);

                        l_OrderDetail.OrderId = l_Orders.Id;
                        l_OrderDetail.LineNo = l_LineNo;
                        l_OrderDetail.LineQty = 1;
                        l_OrderDetail.ItemID = l_Line.itemId;
                        l_OrderDetail.UnitPrice = l_Line.discountedUnitPriceSet?.shopMoney?.amount ?? l_Line.originalUnitPriceSet?.shopMoney?.amount ?? 0;
                        // OrderDetail.ShippingMethod is varchar(50); the full text stays on Orders and in API-JSON
                        //l_OrderDetail.ShippingMethod = l_Orders.ShippingMethod.Length > 50 ? l_Orders.ShippingMethod.Substring(0, 50) : l_Orders.ShippingMethod;
                        l_OrderDetail.Status = "NEW";
                        l_OrderDetail.order_line_id = l_Line.id;
                        l_OrderDetail.CreatedBy = p_UserNo;
                        l_OrderDetail.CreatedDate = DateTime.Now;

                        if (!l_OrderDetail.SaveNew().IsSuccess)
                            return RemoveOrder(l_Orders, p_Order, $"OrderDetail (line {l_LineNo})", p_Route, p_UserNo);
                    }
                }

                return string.Join("; ", l_Problems);
            }
            catch (Exception ex)
            {
                p_Route.SaveLog(LogTypeEnum.Exception, $"Shopify order {p_Order?.name} ({p_Order?.legacyResourceId}) failed while saving.", ex.ToString(), p_UserNo);
                return null;
            }
        }

        // Reads the lines after the first 30 into p_Order.lineItems; false (and nothing changed for the caller to keep) on a failed call
        private static bool ReadRemainingLines(ConnectorDataModel p_Source, SO.Order p_Order, out string p_Error)
        {
            List<SO.LineItem> l_More = new List<SO.LineItem>();
            string l_After = p_Order.lineItems.pageInfo.endCursor;

            p_Error = string.Empty;

            while (!string.IsNullOrEmpty(l_After))
            {
                ShopifyGraphQLResult<SO> l_Page = ShopifyGraphQL.Execute<SO>(p_Source, MoreLinesQuery, new { id = p_Order.id, after = l_After });
                SO.LineItems l_Lines = l_Page.Success ? l_Page.Response.data?.order?.lineItems : null;

                if (l_Lines == null)
                {
                    p_Error = l_Page.Success ? "no lines in the response" : l_Page.Error;
                    return false;
                }

                l_More.AddRange(l_Lines.nodes ?? new List<SO.LineItem>());
                l_After = l_Lines.pageInfo?.hasNextPage == true ? l_Lines.pageInfo.endCursor : null;
            }

            p_Order.lineItems.nodes.AddRange(l_More);
            p_Order.lineItems.pageInfo.hasNextPage = false;

            return true;
        }

        // Deletes what was written of an order whose save failed half way; null = not saved, the next run reads it again
        private static string RemoveOrder(Orders p_Orders, SO.Order p_Order, string p_What, Routes p_Route, int p_UserNo)
        {
            try
            {
                p_Orders.Connection.Execute($"DELETE FROM OrderDetail WHERE OrderId = {p_Orders.Id}; DELETE FROM OrderData WHERE OrderId = {p_Orders.Id}; DELETE FROM Orders WHERE Id = {p_Orders.Id};");
                p_Route.SaveLog(LogTypeEnum.Error, $"Shopify order {p_Order.name} ({p_Order.legacyResourceId}): {p_What} could not be saved, so the order was taken out again. The next run tries again.", string.Empty, p_UserNo);
            }
            catch (Exception ex)
            {
                p_Route.SaveLog(LogTypeEnum.Exception, $"Shopify order {p_Order.name} ({p_Order.legacyResourceId}): {p_What} could not be saved and the order (Id {p_Orders.Id}) could not be taken out. Delete it by hand so the next run reads it again.", ex.ToString(), p_UserNo);
            }

            return null;
        }

        // CustomerItemCode -> ItemId of the customer's inventory feed, read once per run
        private static Dictionary<string, string> LoadItemCodes(string p_ConnectionString, string p_CustomerId, Routes p_Route, int p_UserNo)
        {
            Dictionary<string, string> l_Codes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            DataTable l_Feed = new DataTable();

            try
            {
                new DBConnector(p_ConnectionString).GetData(
                    $"SELECT ItemId, CustomerItemCode FROM SCSInventoryFeed WITH (NOLOCK) WHERE CustomerID = '{p_CustomerId.Replace("'", "''")}' AND ISNULL(LTRIM(RTRIM(CustomerItemCode)), '') <> ''", ref l_Feed);

                foreach (DataRow l_Row in l_Feed.Rows)
                {
                    string l_Code = Convert.ToString(l_Row["CustomerItemCode"])?.Trim() ?? string.Empty;
                    string l_ItemId = Convert.ToString(l_Row["ItemId"])?.Trim() ?? string.Empty;

                    if (l_Code.Length > 0 && l_ItemId.Length > 0 && !l_Codes.ContainsKey(l_Code))
                        l_Codes.Add(l_Code, l_ItemId);
                }
            }
            catch (Exception ex)
            {
                // not fatal: the orders are still saved, their lines keep the Shopify SKU as the item
                p_Route.SaveLog(LogTypeEnum.Error, $"Could not load SCSInventoryFeed for [{p_CustomerId}]. Order lines keep the Shopify SKU as their item, which the ERP may reject.", ex.ToString(), p_UserNo);
            }
            finally
            {
                l_Feed.Dispose();
            }

            return l_Codes;
        }

        // client (2026-10-26): gift cards and digital products stay out of the ERP.
        // "Digital" is the product's own setting (variant.inventoryItem.requiresShipping): the line's requiresShipping
        // depends on how the order was made - an order created through the API (another app, a B2B partner) shows
        // false even for a physical product (seen on the dev store). Only a line without a variant uses the line's value.
        private static bool IsShippable(SO.LineItem p_Line)
        {
            return !p_Line.isGiftCard && (p_Line.variant?.inventoryItem?.requiresShipping ?? p_Line.requiresShipping);
        }

        private static string GetParameter(ConnectorDataModel p_Connector, string p_Name, string p_Default)
        {
            string l_Value = p_Connector.Parmeters?.FirstOrDefault(p => string.Equals(p?.Name?.Trim(), p_Name, StringComparison.OrdinalIgnoreCase))?.Value;

            return string.IsNullOrWhiteSpace(l_Value) ? p_Default : l_Value.Trim();
        }
    }
}
