using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Connections;
using eSyncMate.Processor.Models;
using Newtonsoft.Json;
using RestSharp;
using System.Data;
using static eSyncMate.DB.Declarations;
using static eSyncMate.Processor.Models.WalmartGetOrderResponseModel;

namespace eSyncMate.Processor.Managers
{
    /// <summary>
    /// Route 1 of the Walmart cancellation sync (Walmart US + Walmart Canada).
    /// Reads the Walmart orders cancelled in the last few days and hands each PO to Sp_SetWalmartPortalCancel,
    /// which decides from the eSyncMate status what the cancel means (case analysis, Task 00005):
    /// not yet in ERP -> CANCELLED, in ERP and not shipped -> PortalCancel (Route 2 cancels it in ERP),
    /// shipped / partially cancelled -> CancelError for the operator. Order lines are never touched.
    /// </summary>
    public class WalmartGetCancelledOrdersRoute
    {
        // A cancel can arrive days after the order; repeat runs are safe because a handled order is skipped
        private const int LookbackDays = 3;
        private const int MaxPages = 50;

        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;
            int l_Pages = 0;
            int l_FullyCancelled = 0;
            int l_Partial = 0;
            Dictionary<string, int> l_Outcomes = new Dictionary<string, int>();

            try
            {
                ConnectorDataModel? l_SourceConnector = ConnectorDataModel.Deserialize(route.SourceConnectorObject.Data);
                ConnectorDataModel? l_DestinationConnector = ConnectorDataModel.Deserialize(route.DestinationConnectorObject.Data);

                route.SaveLog(LogTypeEnum.Info, $"Started executing route [{route.Id}]", string.Empty, userNo);

                if (l_SourceConnector == null || l_SourceConnector.ConnectivityType != ConnectorTypesEnum.Rest.ToString())
                {
                    route.SaveLog(LogTypeEnum.Error, "Source Connector is not setup properly (Walmart REST connector expected)", string.Empty, userNo);
                    return;
                }

                if (l_DestinationConnector == null || l_DestinationConnector.ConnectivityType != ConnectorTypesEnum.SqlServer.ToString())
                {
                    route.SaveLog(LogTypeEnum.Error, "Destination Connector is not setup properly (eSyncMate SQL Server connector expected)", string.Empty, userNo);
                    return;
                }

                // Global Marketplace (connector has a WM_MARKET header, e.g. Walmart Canada) uses UPPERCASE status values
                bool l_IsGlobalMarket = l_SourceConnector.Headers?.Any(h => string.Equals(h?.Name?.Trim(), "WM_MARKET", StringComparison.OrdinalIgnoreCase)) == true;
                string l_Status = l_IsGlobalMarket ? "CANCELLED" : "Cancelled";
                string l_From = DateTime.UtcNow.AddDays(-LookbackDays).ToString("yyyy-MM-ddTHH:mm:ssZ");
                string l_BaseUrl = l_SourceConnector.BaseUrl;
                string l_Query = $"orders?status={l_Status}&limit=200&lastModifiedStartDate={l_From}";

                DBConnector l_Connection = new DBConnector(l_DestinationConnector.ConnectionString);

                while (!string.IsNullOrEmpty(l_Query) && l_Pages < MaxPages)
                {
                    l_Pages++;
                    l_SourceConnector.Url = l_BaseUrl + l_Query;

                    RestResponse sourceResponse = RestConnector.Execute(l_SourceConnector, string.Empty).GetAwaiter().GetResult();

                    route.SaveData("JSON-SNT", 0, JsonConvert.SerializeObject(new { method = "GET", url = l_SourceConnector.Url }), userNo);
                    route.SaveData("JSON-RVD", 0, LogJson.Ensure(sourceResponse.Content), userNo);

                    // Walmart answers 404 CONTENT_NOT_FOUND when no order matches the filter
                    if (sourceResponse.StatusCode == System.Net.HttpStatusCode.NotFound && (sourceResponse.Content ?? string.Empty).Contains("CONTENT_NOT_FOUND"))
                        break;

                    WalmartGetOrderResponseModel? l_List;

                    try
                    {
                        l_List = JsonConvert.DeserializeObject<WalmartGetOrderResponseModel>(sourceResponse.Content ?? string.Empty);
                    }
                    catch (JsonException)
                    {
                        l_List = null;
                    }

                    if (l_List?.list?.elements?.order == null)
                    {
                        route.SaveLog(LogTypeEnum.Error, $"Walmart did not return an order list [{(int)sourceResponse.StatusCode} {sourceResponse.StatusCode}]",
                            sourceResponse.Content ?? $"{sourceResponse.StatusDescription} - {sourceResponse.ErrorMessage}", userNo);
                        break;
                    }

                    foreach (WalmartOrder order in l_List.list.elements.order)
                    {
                        CancelInfo l_Info = GetCancelInfo(order);

                        if (l_Info.CancelledLines == 0)
                            continue;

                        if (l_Info.IsFull)
                            l_FullyCancelled++;
                        else
                            l_Partial++;

                        string l_Outcome = ProcessOrder(order, l_Info, route, l_Connection, l_SourceConnector.CustomerID, l_DestinationConnector.ConnectionString, userNo);

                        l_Outcomes[l_Outcome] = l_Outcomes.GetValueOrDefault(l_Outcome) + 1;
                    }

                    l_Query = GetNextQuery(l_List.list.meta?.nextCursor);
                }

                route.SaveLog(LogTypeEnum.RouteInfo, $"Walmart cancelled orders for [{l_SourceConnector.CustomerID}] since {l_From}: {l_FullyCancelled} fully cancelled, {l_Partial} partially cancelled. " +
                    $"{l_Outcomes.GetValueOrDefault("CANCELLED")} CANCELLED, {l_Outcomes.GetValueOrDefault("PORTALCANCEL")} PortalCancel, {l_Outcomes.GetValueOrDefault("CANCELERROR")} CancelError, " +
                    $"{l_Outcomes.GetValueOrDefault("SKIPPED")} skipped, {l_Outcomes.GetValueOrDefault("NOTFOUND")} not found. Pages read: {l_Pages}.", string.Empty, userNo);
                route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
            }
        }

        private class CancelInfo
        {
            public bool IsFull { get; set; }
            public int CancelledLines { get; set; }
            public int TotalLines { get; set; }
            public string CancelDate { get; set; } = string.Empty;
        }

        // Full = every status row of every line is Cancelled (Walmart has no order-level status).
        // A line counts as cancelled when any of its status rows is Cancelled.
        private static CancelInfo GetCancelInfo(WalmartOrder order)
        {
            CancelInfo l_Info = new CancelInfo();
            List<Orderline>? l_Lines = order?.orderLines?.orderLine;
            long? l_LatestDate = null;
            bool l_AllCancelled = true;

            if (l_Lines == null || l_Lines.Count == 0)
                return l_Info;

            foreach (Orderline l_Line in l_Lines)
            {
                List<Orderlinestatu>? l_Statuses = l_Line?.orderLineStatuses?.orderLineStatus;
                bool l_HasCancel = l_Statuses?.Any(s => string.Equals(s?.status, "Cancelled", StringComparison.OrdinalIgnoreCase)) == true;

                l_Info.TotalLines++;

                if (l_Statuses == null || l_Statuses.Count == 0 || l_Statuses.Any(s => !string.Equals(s?.status, "Cancelled", StringComparison.OrdinalIgnoreCase)))
                    l_AllCancelled = false;

                if (l_HasCancel)
                {
                    l_Info.CancelledLines++;

                    if (l_Line?.statusDate != null && (l_LatestDate == null || l_Line.statusDate > l_LatestDate))
                        l_LatestDate = l_Line.statusDate;
                }
            }

            l_Info.IsFull = l_AllCancelled;

            if (l_LatestDate != null)
                l_Info.CancelDate = DateTimeOffset.FromUnixTimeMilliseconds(l_LatestDate.Value).LocalDateTime.ToString("MM/dd/yyyy hh:mm tt", System.Globalization.CultureInfo.InvariantCulture);

            return l_Info;
        }

        // US returns nextCursor as a ready query string ("?limit=...&hasMoreElements=true&..."); otherwise pass it as cursor
        private static string GetNextQuery(string? nextCursor)
        {
            if (string.IsNullOrWhiteSpace(nextCursor))
                return string.Empty;

            if (nextCursor.StartsWith("?"))
                return "orders" + nextCursor;

            return "orders?cursor=" + Uri.EscapeDataString(nextCursor);
        }

        // Returns the SP outcome: CANCELLED / PORTALCANCEL / CANCELERROR / SKIPPED / NOTFOUND
        private static string ProcessOrder(WalmartOrder order, CancelInfo info, Routes route, DBConnector connection, string customerId, string connectionString, int userNo)
        {
            DataTable l_Result = new DataTable();
            string l_OrderNumber = (order.purchaseOrderId ?? string.Empty).Replace("'", "''");

            connection.GetDataSP($"EXEC Sp_SetWalmartPortalCancel @p_CustomerID = '{customerId.Replace("'", "''")}', @p_OrderNumber = '{l_OrderNumber}', " +
                $"@p_IsPartial = {(info.IsFull ? 0 : 1)}, @p_CancelledLines = {info.CancelledLines}, @p_TotalLines = {info.TotalLines}, @p_CancelDate = '{info.CancelDate}'", ref l_Result);

            if (l_Result.Rows.Count == 0)
                return "SKIPPED";

            DataRow l_Row = l_Result.Rows[0];
            string l_Outcome = Convert.ToString(l_Row["Result"]) ?? "SKIPPED";

            if (l_Outcome == "SKIPPED" || l_Outcome == "NOTFOUND")
                return l_Outcome;

            OrderData l_OrderData = new OrderData();

            l_OrderData.UseConnection(connectionString);
            l_OrderData.DeleteWithType(Convert.ToInt32(l_Row["OrderId"]), "PortalCancel-JSON");

            l_OrderData.Type = "PortalCancel-JSON";
            l_OrderData.Data = JsonConvert.SerializeObject(order);
            l_OrderData.CreatedBy = userNo;
            l_OrderData.CreatedDate = DateTime.Now;
            l_OrderData.OrderId = Convert.ToInt32(l_Row["OrderId"]);
            l_OrderData.OrderNumber = order.purchaseOrderId;

            l_OrderData.SaveNew();

            // CancelError needs a person; it is logged once, later runs skip the order
            if (l_Outcome == "CANCELERROR")
            {
                route.SaveLog(LogTypeEnum.Error, $"Walmart cancelled order [{order.purchaseOrderId}] ({Convert.ToString(l_Row["PreviousStatus"])} in eSyncMate), marked CancelError: {Convert.ToString(l_Row["Message"])}", string.Empty, userNo);
            }

            return l_Outcome;
        }
    }
}
