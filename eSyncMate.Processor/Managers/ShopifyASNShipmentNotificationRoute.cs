using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;
using Newtonsoft.Json;
using System.Data;
using static eSyncMate.DB.Declarations;
using FO = eSyncMate.Processor.Models.ShopifyFulfillmentOrdersResponseModel;
using FC = eSyncMate.Processor.Models.ShopifyFulfillmentCreateRequestModel;

namespace eSyncMate.Processor.Managers
{
    /// <summary>
    /// Shopify - Send ASN Shipment Notification (RouteType 83, Task 00006).
    /// Source SP_OrdersData (branch SHOPIFY01, ASN mode): the SYNCED orders whose units the ERP shipped
    /// (OrderDetail ASNRVD, filled by "Fetch ASN from ERP"), one row per line + tracking number.
    /// Per order the Shopify fulfillment orders are read, then one fulfillmentCreate per tracking number ships
    /// those quantities from the fulfillment order line items of the order lines (OrderDetail.order_line_id).
    /// A tracking number Shopify accepts -> its units ASNSNT; SP_UpdateOrderStatus 'SCSASN' turns the order SHIPPED
    /// once every unit is sent. Nothing sent and Shopify rejected it -> 'SCSASNError' (ASNERROR); a failed call leaves the order SYNCED for the next run; a tracking that failed while
    /// another one landed keeps its units ASNRVD, so the next run sends it again.
    /// Quantities Shopify already shows as fulfilled are not sent twice: that tracking counts as sent.
    /// Logs per tracking: OrderData ASN-SNT (request), ASN-RES (response) or ASN-ERR.
    /// The destination connector Parameter notifyCustomer (true / false, default false) decides whether Shopify
    /// e-mails the customer the shipment.
    /// </summary>
    public class ShopifyASNShipmentNotificationRoute
    {
        // ~10 x (1 + 50) = ~510 requested cost points, under the 1,000 limit
        private const string FulfillmentOrdersQuery = @"query FulfillmentOrders($id: ID!) {
  order(id: $id) {
    id name
    fulfillmentOrders(first: 10) {
      nodes {
        id status
        lineItems(first: 50) { nodes { id remainingQuantity totalQuantity lineItem { id } } }
      }
    }
  }
}";

        private const string FulfillmentCreateMutation = @"mutation Ship($fulfillment: FulfillmentInput!) {
  fulfillmentCreate(fulfillment: $fulfillment) {
    fulfillment { id status }
    userErrors { field message }
  }
}";

        // fulfillment orders a fulfillment can still be created from
        private static readonly HashSet<string> OpenStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "OPEN", "IN_PROGRESS" };

        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;
            DataTable l_Data = new DataTable();

            try
            {
                ConnectorDataModel l_SourceConnector = ConnectorDataModel.Deserialize(route.SourceConnectorObject.Data);
                ConnectorDataModel l_DestinationConnector = ConnectorDataModel.Deserialize(route.DestinationConnectorObject.Data);

                route.SaveLog(LogTypeEnum.Info, $"Started executing route [{route.Id}]", string.Empty, userNo);

                if (l_SourceConnector == null || l_SourceConnector.ConnectivityType != ConnectorTypesEnum.SqlServer.ToString())
                {
                    route.SaveLog(LogTypeEnum.Error, "Source Connector is not setup properly (eSyncMate SQL Server connector expected)", string.Empty, userNo);
                    return;
                }

                if (l_DestinationConnector == null || l_DestinationConnector.ConnectivityType != ConnectorTypesEnum.Rest.ToString())
                {
                    route.SaveLog(LogTypeEnum.Error, "Destination Connector is not setup properly (Shopify REST connector expected)", string.Empty, userNo);
                    return;
                }

                bool l_Notify = bool.TryParse(l_DestinationConnector.Parmeters?.FirstOrDefault(p => string.Equals(p?.Name?.Trim(), "notifyCustomer", StringComparison.OrdinalIgnoreCase))?.Value, out bool l_N) && l_N;

                // RestConnector puts connector Parameters on the URL; notifyCustomer is only a setting
                l_DestinationConnector.Parmeters = new List<eSyncMate.Processor.Models.Parameter>();

                l_SourceConnector.Command = l_SourceConnector.Command.Replace("@DATATYPE@", "ERPASN-JSON");
                l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ORDERSTATUS@", "SYNCED");
                l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ORDERDATASTATUS@", "ASNGEN");

                if (l_SourceConnector.CommandType == "SP")
                    new DBConnector(l_SourceConnector.ConnectionString).GetDataSP(l_SourceConnector.Command, ref l_Data);

                if (l_Data.Rows.Count == 0)
                {
                    route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]: no shipped lines waiting", string.Empty, userNo);
                    return;
                }

                int l_OrdersShipped = 0, l_OrdersFailed = 0, l_TrackingsSent = 0, l_TrackingsFailed = 0;

                foreach (IGrouping<int, DataRow> l_Order in l_Data.Rows.Cast<DataRow>().GroupBy(r => Convert.ToInt32(r["Id"])))
                {
                    (int Sent, int Failed) = ProcessOrder(l_Order.Key, l_Order.ToList(), l_SourceConnector, l_DestinationConnector, l_Notify, route, userNo);

                    l_TrackingsSent += Sent;
                    l_TrackingsFailed += Failed;

                    if (Sent > 0) l_OrdersShipped++;
                    else l_OrdersFailed++;
                }

                // RouteInfo: SaveLog keeps only Error / Exception / RouteInfo
                route.SaveLog(LogTypeEnum.RouteInfo, $"Shopify ASN for [{l_SourceConnector.CustomerID}]: {l_OrdersShipped + l_OrdersFailed} order(s), {l_TrackingsSent} tracking number(s) sent, {l_TrackingsFailed} failed ({l_OrdersFailed} order(s) with nothing sent: ASNERROR when Shopify rejected them, otherwise retried next run).", string.Empty, userNo);
                route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
            }
            finally
            {
                l_Data.Dispose();
            }
        }

        private static (int Sent, int Failed) ProcessOrder(int p_OrderId, List<DataRow> p_Rows, ConnectorDataModel p_Source, ConnectorDataModel p_Destination,
                                                           bool p_Notify, Routes p_Route, int p_UserNo)
        {
            string l_OrderNumber = Convert.ToString(p_Rows[0]["OrderNumber"]) ?? string.Empty;
            string l_ExternalId = Convert.ToString(p_Rows[0]["ExternalId"]) ?? string.Empty;
            int l_Sent = 0, l_Failed = 0, l_Rejected = 0;

            OrderDetail l_Detail = new OrderDetail();
            l_Detail.UseConnection(p_Source.ConnectionString);

            ShopifyGraphQLResult<FO> l_FoResult = ShopifyGraphQL.Execute<FO>(p_Destination, FulfillmentOrdersQuery, new { id = "gid://shopify/Order/" + l_OrderNumber });
            FO.Order l_ShopifyOrder = l_FoResult.Success ? l_FoResult.Response.data?.order : null;

            List<string> l_Trackings = p_Rows.Select(r => Convert.ToString(r["TrackingNo"])?.Trim() ?? string.Empty).Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (l_ShopifyOrder == null)
            {
                string l_Why = l_FoResult.Success ? $"Shopify has no order {l_OrderNumber}" : l_FoResult.Error;

                SaveOrderData(p_Source, p_OrderId, l_OrderNumber, "ASN-ERR", JsonConvert.SerializeObject(new { message = $"Fulfillment orders could not be read: {l_Why}" }), p_UserNo);
                p_Route.SaveLog(LogTypeEnum.Error, $"Shopify ASN for order {l_OrderNumber} (Id {p_OrderId}): fulfillment orders could not be read: {l_Why}", l_FoResult.ResponseBody, p_UserNo);

                // a call problem is retried next run; an order Shopify does not know will not get better
                if (l_FoResult.Success)
                    SetOrderStatus(p_Source, "Error", l_ExternalId);

                return (0, l_Trackings.Count);
            }

            // what is still open to ship, per Shopify order line (fulfillment order line items, in order)
            List<(string FoId, FO.FulfillmentOrderLineItem Item)> l_Open = l_ShopifyOrder.fulfillmentOrders?.nodes
                .Where(f => f != null && OpenStatuses.Contains(f.status ?? string.Empty))
                .SelectMany(f => (f.lineItems?.nodes ?? new List<FO.FulfillmentOrderLineItem>()).Select(i => (f.id, i)))
                .ToList() ?? new List<(string, FO.FulfillmentOrderLineItem)>();

            HashSet<string> l_KnownLines = new HashSet<string>(
                (l_ShopifyOrder.fulfillmentOrders?.nodes ?? new List<FO.FulfillmentOrder>())
                    .SelectMany(f => f?.lineItems?.nodes ?? new List<FO.FulfillmentOrderLineItem>())
                    .Select(i => i?.lineItem?.id ?? string.Empty),
                StringComparer.OrdinalIgnoreCase);

            foreach (string l_Tracking in l_Trackings)
            {
                List<DataRow> l_Lines = p_Rows.Where(r => string.Equals(Convert.ToString(r["TrackingNo"])?.Trim(), l_Tracking, StringComparison.OrdinalIgnoreCase)).ToList();
                FC l_Request = new FC();
                List<string> l_Problems = new List<string>();
                int l_AlreadyFulfilled = 0;

                l_Request.fulfillment.notifyCustomer = p_Notify;
                l_Request.fulfillment.trackingInfo.number = l_Tracking;
                l_Request.fulfillment.trackingInfo.company = Convert.ToString(l_Lines[0]["LevelOfService"])?.Trim() ?? string.Empty;

                foreach (DataRow l_Line in l_Lines)
                {
                    string l_LineId = Convert.ToString(l_Line["order_line_id"])?.Trim() ?? string.Empty;
                    int l_Need = Convert.ToInt32(l_Line["LineQty"]);

                    if (!l_KnownLines.Contains(l_LineId))
                    {
                        l_Problems.Add($"line {l_Line["LineNo"]} ({l_Line["ItemID"]}, {l_LineId}) is not on any Shopify fulfillment order of this order");
                        continue;
                    }

                    // take the quantity from the open fulfillment order line items of this order line
                    foreach ((string FoId, FO.FulfillmentOrderLineItem Item) l_Open1 in l_Open.Where(o => string.Equals(o.Item?.lineItem?.id, l_LineId, StringComparison.OrdinalIgnoreCase) && o.Item.remainingQuantity > 0))
                    {
                        if (l_Need == 0)
                            break;

                        int l_Take = Math.Min(l_Need, l_Open1.Item.remainingQuantity);

                        FC.LineItemsByFulfillmentOrder l_Group = l_Request.fulfillment.lineItemsByFulfillmentOrder.FirstOrDefault(g => g.fulfillmentOrderId == l_Open1.FoId);

                        if (l_Group == null)
                        {
                            l_Group = new FC.LineItemsByFulfillmentOrder { fulfillmentOrderId = l_Open1.FoId };
                            l_Request.fulfillment.lineItemsByFulfillmentOrder.Add(l_Group);
                        }

                        FC.FulfillmentOrderLineItem l_Existing = l_Group.fulfillmentOrderLineItems.FirstOrDefault(x => x.id == l_Open1.Item.id);

                        if (l_Existing == null)
                            l_Group.fulfillmentOrderLineItems.Add(new FC.FulfillmentOrderLineItem { id = l_Open1.Item.id, quantity = l_Take });
                        else
                            l_Existing.quantity += l_Take;

                        // the next tracking number of this order must not take the same units again
                        l_Open1.Item.remainingQuantity -= l_Take;
                        l_Need -= l_Take;
                    }

                    // nothing left open in Shopify for these units: shipped already (e.g. by hand, or an earlier run whose DB update was lost)
                    if (l_Need > 0)
                        l_AlreadyFulfilled += l_Need;
                }

                string l_Body = JsonConvert.SerializeObject(l_Request);

                if (l_Problems.Count > 0)
                {
                    l_Failed++;
                    l_Rejected++;
                    GiveBack(l_Request, l_Open);
                    SaveOrderData(p_Source, p_OrderId, l_OrderNumber, "ASN-ERR", JsonConvert.SerializeObject(new { tracking = l_Tracking, errors = l_Problems }), p_UserNo);
                    p_Route.SaveLog(LogTypeEnum.Error, $"Shopify ASN for order {l_OrderNumber} (Id {p_OrderId}), tracking {l_Tracking} not sent: {string.Join("; ", l_Problems)}", l_Body, p_UserNo);
                    continue;
                }

                if (l_Request.fulfillment.lineItemsByFulfillmentOrder.Count == 0)
                {
                    // every unit of this tracking is already fulfilled in Shopify: no call, just record it as sent
                    l_Sent++;
                    l_Detail.UpdateASNSent(p_OrderId, l_Tracking);
                    SaveOrderData(p_Source, p_OrderId, l_OrderNumber, "ASN-RES", JsonConvert.SerializeObject(new { tracking = l_Tracking, message = $"Already fulfilled in Shopify ({l_AlreadyFulfilled} unit(s)); nothing sent." }), p_UserNo);
                    continue;
                }

                SaveOrderData(p_Source, p_OrderId, l_OrderNumber, "ASN-SNT", l_Body, p_UserNo);

                ShopifyGraphQLResult<ShopifyFulfillmentCreateResponseModel> l_Result =
                    ShopifyGraphQL.Execute<ShopifyFulfillmentCreateResponseModel>(p_Destination, FulfillmentCreateMutation, l_Request);

                ShopifyFulfillmentCreateResponseModel.FulfillmentCreate l_Payload = l_Result.Success ? l_Result.Response.data?.fulfillmentCreate : null;

                if (l_Payload?.fulfillment != null && (l_Payload.userErrors == null || l_Payload.userErrors.Count == 0))
                {
                    l_Sent++;
                    l_Detail.UpdateASNSent(p_OrderId, l_Tracking);
                    SaveOrderData(p_Source, p_OrderId, l_OrderNumber, "ASN-RES", l_Result.ResponseBody, p_UserNo);
                }
                else
                {
                    l_Failed++;

                    // Shopify said no (userErrors) -> the data is wrong; the call itself failed -> try again next run
                    if (l_Result.Success)
                        l_Rejected++;

                    GiveBack(l_Request, l_Open);

                    string l_Why = !l_Result.Success ? l_Result.Error
                                 : l_Payload?.userErrors?.Count > 0 ? string.Join("; ", l_Payload.userErrors.Select(e => e.message))
                                 : "no fulfillment in the response";

                    SaveOrderData(p_Source, p_OrderId, l_OrderNumber, "ASN-ERR", string.IsNullOrEmpty(l_Result.ResponseBody) ? JsonConvert.SerializeObject(new { message = l_Why }) : l_Result.ResponseBody, p_UserNo);
                    p_Route.SaveLog(LogTypeEnum.Error, $"Shopify ASN for order {l_OrderNumber} (Id {p_OrderId}), tracking {l_Tracking} rejected: {l_Why}", l_Body, p_UserNo);
                }
            }

            // like the Amazon / Walmart ASN routes: SCSASN marks the order SHIPPED once every unit is ASNSNT.
            // Nothing sent: ASNERROR only when Shopify rejected something; a failed call leaves the order for the next run.
            if (l_Sent > 0)
                SetOrderStatus(p_Source, string.Empty, l_ExternalId);
            else if (l_Rejected > 0)
                SetOrderStatus(p_Source, "Error", l_ExternalId);

            return (l_Sent, l_Failed);
        }

        // units a tracking number took but did not ship are open again for the next tracking number of the order
        private static void GiveBack(FC p_Request, List<(string FoId, FO.FulfillmentOrderLineItem Item)> p_Open)
        {
            foreach (FC.LineItemsByFulfillmentOrder l_Group in p_Request.fulfillment.lineItemsByFulfillmentOrder)
                foreach (FC.FulfillmentOrderLineItem l_Item in l_Group.fulfillmentOrderLineItems)
                {
                    (string FoId, FO.FulfillmentOrderLineItem Item) l_Open = p_Open.FirstOrDefault(o => o.Item.id == l_Item.id);

                    if (l_Open.Item != null)
                        l_Open.Item.remainingQuantity += l_Item.quantity;
                }
        }

        private static void SetOrderStatus(ConnectorDataModel p_Source, string p_Suffix, string p_ExternalId)
        {
            new DBConnector(p_Source.ConnectionString).Execute("EXEC SP_UpdateOrderStatus @p_CustomerID = '" + p_Source.CustomerID + "', @p_RouteType = '" + RouteTypesEnum.SCSASN + p_Suffix + "', @p_ExternalId = '" + p_ExternalId.Replace("'", "''") + "'");
        }

        private static void SaveOrderData(ConnectorDataModel p_Source, int p_OrderId, string p_OrderNumber, string p_Type, string p_Data, int p_UserNo)
        {
            OrderData l_OrderData = new OrderData();

            l_OrderData.UseConnection(p_Source.ConnectionString);
            l_OrderData.Type = p_Type;
            l_OrderData.Data = p_Data;
            l_OrderData.CreatedBy = p_UserNo;
            l_OrderData.CreatedDate = DateTime.Now;
            l_OrderData.OrderId = p_OrderId;
            l_OrderData.OrderNumber = p_OrderNumber;

            l_OrderData.SaveNew();
        }
    }
}
