using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;
using Newtonsoft.Json;
using System.Data;
using static eSyncMate.DB.Declarations;
using CO = eSyncMate.Processor.Models.ShopifyCancelledOrdersResponseModel;

namespace eSyncMate.Processor.Managers
{
    /// <summary>
    /// Shopify - Get Cancelled Orders (RouteType 85, Task 00006): route 1 of the Shopify -> ERP cancellation, the
    /// Shopify twin of WalmartGetCancelledOrdersRoute (RouteType 78). Route 2 is the existing RouteType 79
    /// (WalmartSendCancellationsToERPRoute: PortalCancel -> ERP Cancel_Order), which only filters by customer.
    /// Reads the ids of the orders Shopify updated in the last N days, keeps the ones eSyncMate has, reads them again
    /// with their lines (nodes, 15 per call) and hands each cancel to Sp_SetWalmartPortalCancel, which decides from
    /// the eSyncMate status (not in ERP yet -> CANCELLED, in ERP -> PortalCancel, shipped / partial -> CancelError).
    ///   whole order: Shopify cancelledAt is set, or every line still open in eSyncMate is down to 0 in Shopify
    ///   partial:     a line with units the ERP has not shipped yet (OrderDetail NEW) has fewer units in Shopify
    ///                (currentQuantity) than eSyncMate still has (units not CANRVD / CANSNT / CANERR)
    /// Units the ERP cancelled itself (CAN*) are not counted, so the "Send Cancellation to Portal" route's own
    /// Shopify edits / cancels are never taken for a customer cancel. Returns after shipping are not cancels.
    /// Order lines are never changed. OrderData PortalCancel-JSON = the Shopify order as read.
    /// Source connector = Shopify (Parameter days, default 3); destination = the eSyncMate SQL connector.
    /// </summary>
    public class ShopifyGetCancelledOrdersRoute
    {
        private const int DefaultDays = 3;
        private const int MaxPages = 40;
        private const int BatchSize = 15;

        // ids only: 250 per page is cheap
        private const string UpdatedOrdersQuery = @"query Updated($q: String!, $after: String) {
  orders(first: 250, after: $after, query: $q, sortKey: UPDATED_AT) {
    nodes { legacyResourceId }
    pageInfo { hasNextPage endCursor }
  }
}";

        // ~15 x (1 + 50) = ~765 requested cost points, under the 1,000 limit
        private const string OrdersByIdQuery = @"query OrdersById($ids: [ID!]!) {
  nodes(ids: $ids) {
    ... on Order {
      id legacyResourceId name cancelledAt cancelReason displayFinancialStatus displayFulfillmentStatus
      lineItems(first: 50) { nodes { id quantity currentQuantity unfulfilledQuantity } pageInfo { hasNextPage } }
    }
  }
}";

        private class Line
        {
            public int Active;
            public int NewUnits;
        }

        private class Candidate
        {
            public int Id;
            public string OrderNumber = string.Empty;
            public Dictionary<string, Line> Lines = new Dictionary<string, Line>(StringComparer.OrdinalIgnoreCase);
        }

        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;
            int l_Updated = 0, l_Known = 0, l_Full = 0, l_Partial = 0;
            Dictionary<string, int> l_Outcomes = new Dictionary<string, int>();

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
                string l_DaysText = l_SourceConnector.Parmeters?.FirstOrDefault(p => string.Equals(p?.Name?.Trim(), "days", StringComparison.OrdinalIgnoreCase))?.Value;
                int l_Days = int.TryParse(l_DaysText, out int l_D) && l_D > 0 ? l_D : DefaultDays;
                string l_From = DateTime.UtcNow.AddDays(-l_Days).ToString("yyyy-MM-ddTHH:mm:ssZ");
                string l_Query = $"updated_at:>='{l_From}' (status:open OR status:closed OR status:cancelled)";

                // RestConnector puts connector Parameters on the URL; days is only a setting
                l_SourceConnector.Parmeters = new List<eSyncMate.Processor.Models.Parameter>();

                DBConnector l_Connection = new DBConnector(l_DestinationConnector.ConnectionString);

                // 1. the order ids Shopify updated (a cancel, an edit, a refund, a fulfillment ...)
                List<string> l_Ids = new List<string>();
                string l_After = null;
                int l_Pages = 0;
                bool l_Completed = true;

                do
                {
                    ShopifyGraphQLResult<CO> l_Page = ShopifyGraphQL.Execute<CO>(l_SourceConnector, UpdatedOrdersQuery, new { q = l_Query, after = l_After });
                    CO.Orders l_List = l_Page.Success ? l_Page.Response.data?.orders : null;

                    if (l_List == null)
                    {
                        route.SaveLog(LogTypeEnum.Error, $"Shopify updated orders failed for [{l_CustomerId}] on page {l_Pages + 1}: {(l_Page.Success ? "no data in the response" : l_Page.Error)}. The orders read so far are checked; the next run reads the rest.", l_Page.ResponseBody, userNo);
                        l_Completed = false;
                        break;
                    }

                    l_Pages++;
                    l_Ids.AddRange(l_List.nodes.Select(n => n?.legacyResourceId?.Trim() ?? string.Empty).Where(n => n.Length > 0 && n.All(char.IsAsciiDigit)));
                    l_After = l_List.pageInfo?.hasNextPage == true ? l_List.pageInfo.endCursor : null;
                }
                while (!string.IsNullOrEmpty(l_After) && l_Pages < MaxPages);

                if (!string.IsNullOrEmpty(l_After))
                {
                    l_Completed = false;
                    route.SaveLog(LogTypeEnum.Error, $"More than {MaxPages * 250} Shopify orders were updated since {l_From}; only the first {l_Ids.Count} were checked. Lower the days Parameter or run the route more often.", string.Empty, userNo);
                }

                l_Ids = l_Ids.Distinct().ToList();
                l_Updated = l_Ids.Count;

                // 2. the ones eSyncMate has and has not closed as cancelled yet, read again with their lines
                foreach (List<string> l_Chunk in l_Ids.Chunk(BatchSize).Select(c => c.ToList()))
                {
                    Dictionary<string, Candidate> l_Candidates = LoadCandidates(l_Connection, l_CustomerId, l_Chunk);

                    if (l_Candidates.Count == 0)
                        continue;

                    l_Known += l_Candidates.Count;

                    ShopifyGraphQLResult<CO> l_Result = ShopifyGraphQL.Execute<CO>(l_SourceConnector, OrdersByIdQuery,
                        new { ids = l_Candidates.Keys.Select(n => "gid://shopify/Order/" + n).ToList() });

                    if (!l_Result.Success || l_Result.Response.data?.nodes == null)
                    {
                        route.SaveLog(LogTypeEnum.Error, $"Shopify orders {string.Join(", ", l_Candidates.Keys)} could not be read for [{l_CustomerId}]: {(l_Result.Success ? "no data in the response" : l_Result.Error)}. They are checked again next run.", l_Result.ResponseBody, userNo);
                        continue;
                    }

                    foreach (CO.Order l_Order in l_Result.Response.data.nodes.Where(n => n != null))
                    {
                        if (!l_Candidates.TryGetValue(l_Order.legacyResourceId ?? string.Empty, out Candidate l_Candidate))
                            continue;

                        string l_Outcome = ProcessOrder(l_Order, l_Candidate, l_Connection, l_CustomerId, l_DestinationConnector.ConnectionString, route, userNo, ref l_Full, ref l_Partial);

                        if (l_Outcome.Length > 0)
                            l_Outcomes[l_Outcome] = l_Outcomes.GetValueOrDefault(l_Outcome) + 1;
                    }
                }

                // RouteInfo: SaveLog keeps only Error / Exception / RouteInfo
                route.SaveLog(LogTypeEnum.RouteInfo, $"Shopify cancelled orders for [{l_CustomerId}] since {l_From}: {l_Updated} order(s) updated in Shopify{(l_Completed ? "" : " (not all read)")}, {l_Known} of them open in eSyncMate; {l_Full} fully cancelled, {l_Partial} partially cancelled. " +
                    $"{l_Outcomes.GetValueOrDefault("CANCELLED")} CANCELLED, {l_Outcomes.GetValueOrDefault("PORTALCANCEL")} PortalCancel, {l_Outcomes.GetValueOrDefault("CANCELERROR")} CancelError, " +
                    $"{l_Outcomes.GetValueOrDefault("SKIPPED")} skipped, {l_Outcomes.GetValueOrDefault("NOTFOUND")} not found.", string.Empty, userNo);
                route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
            }
        }

        // OrderNumber -> the order's open units per Shopify line; orders already CANCELLED / PortalCancel / CancelError are left out
        private static Dictionary<string, Candidate> LoadCandidates(DBConnector p_Connection, string p_CustomerId, List<string> p_OrderNumbers)
        {
            Dictionary<string, Candidate> l_Candidates = new Dictionary<string, Candidate>(StringComparer.OrdinalIgnoreCase);
            DataTable l_Table = new DataTable();

            // the numbers are digits only (checked when read)
            p_Connection.GetData($@"SELECT O.Id, O.OrderNumber, D.order_line_id,
                                           SUM(CASE WHEN ISNULL(D.[Status], '') IN ('CANRVD', 'CANSNT', 'CANERR') THEN 0 ELSE D.LineQty END) AS Active,
                                           SUM(CASE WHEN ISNULL(D.[Status], '') = 'NEW' THEN D.LineQty ELSE 0 END) AS NewUnits
                                      FROM Orders O WITH (NOLOCK)
                                      JOIN Customers C WITH (NOLOCK) ON C.Id = O.CustomerId
                                      JOIN OrderDetail D WITH (NOLOCK) ON D.OrderId = O.Id
                                     WHERE C.ERPCustomerID = '{p_CustomerId.Replace("'", "''")}'
                                       AND O.OrderNumber IN ({string.Join(",", p_OrderNumbers.Select(n => $"'{n}'"))})
                                       AND O.[Status] NOT IN ('CANCELLED', 'PortalCancel', 'CancelError')
                                     GROUP BY O.Id, O.OrderNumber, D.order_line_id", ref l_Table);

            foreach (DataRow l_Row in l_Table.Rows)
            {
                string l_Number = Convert.ToString(l_Row["OrderNumber"])?.Trim() ?? string.Empty;

                if (!l_Candidates.TryGetValue(l_Number, out Candidate l_Candidate))
                {
                    l_Candidate = new Candidate { Id = Convert.ToInt32(l_Row["Id"]), OrderNumber = l_Number };
                    l_Candidates.Add(l_Number, l_Candidate);
                }

                l_Candidate.Lines[Convert.ToString(l_Row["order_line_id"])?.Trim() ?? string.Empty] =
                    new Line { Active = Convert.ToInt32(l_Row["Active"]), NewUnits = Convert.ToInt32(l_Row["NewUnits"]) };
            }

            l_Table.Dispose();

            return l_Candidates;
        }

        // Returns the SP outcome (CANCELLED / PORTALCANCEL / CANCELERROR / SKIPPED / NOTFOUND), or "" when Shopify shows no cancel
        private static string ProcessOrder(CO.Order p_Order, Candidate p_Candidate, DBConnector p_Connection, string p_CustomerId, string p_ConnectionString,
                                           Routes p_Route, int p_UserNo, ref int p_Full, ref int p_Partial)
        {
            List<KeyValuePair<string, Line>> l_Open = p_Candidate.Lines.Where(l => l.Value.Active > 0).ToList();

            // every unit was cancelled by the ERP: a Shopify cancel now is the one "Send Cancellation to Portal" made
            if (l_Open.Count == 0)
                return string.Empty;

            bool l_Cancelled = !string.IsNullOrEmpty(p_Order.cancelledAt);

            if (!l_Cancelled && p_Order.lineItems?.pageInfo?.hasNextPage == true)
            {
                p_Route.SaveLog(LogTypeEnum.Error, $"Shopify order {p_Order.name} ({p_Order.legacyResourceId}) has more than 50 lines; a partial cancel on it is not checked. Check it in Shopify by hand.", string.Empty, p_UserNo);
                return string.Empty;
            }

            Dictionary<string, int> l_ShopifyQty = (p_Order.lineItems?.nodes ?? new List<CO.LineItem>())
                .Where(l => !string.IsNullOrEmpty(l?.id))
                .ToDictionary(l => l.id, l => l.currentQuantity, StringComparer.OrdinalIgnoreCase);

            // a line counts as cancelled by the customer only while the ERP has not shipped all of it (NEW units left)
            List<KeyValuePair<string, Line>> l_CancelledLines = l_Open
                .Where(l => l.Value.NewUnits > 0 && l_ShopifyQty.GetValueOrDefault(l.Key) < l.Value.Active)
                .ToList();

            bool l_Full = l_Cancelled || (l_CancelledLines.Count == l_Open.Count && l_CancelledLines.All(l => l_ShopifyQty.GetValueOrDefault(l.Key) == 0));

            if (!l_Full && l_CancelledLines.Count == 0)
                return string.Empty;

            if (l_Full)
                p_Full++;
            else
                p_Partial++;

            string l_CancelDate = DateTimeOffset.TryParse(p_Order.cancelledAt, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTimeOffset l_At)
                ? l_At.LocalDateTime.ToString("MM/dd/yyyy hh:mm tt", System.Globalization.CultureInfo.InvariantCulture)
                : string.Empty;

            DataTable l_Result = new DataTable();

            p_Connection.GetDataSP($"EXEC Sp_SetWalmartPortalCancel @p_CustomerID = '{p_CustomerId.Replace("'", "''")}', @p_OrderNumber = '{p_Candidate.OrderNumber}', " +
                $"@p_IsPartial = {(l_Full ? 0 : 1)}, @p_CancelledLines = {(l_Full ? l_Open.Count : l_CancelledLines.Count)}, @p_TotalLines = {l_Open.Count}, @p_CancelDate = '{l_CancelDate}'", ref l_Result);

            if (l_Result.Rows.Count == 0)
                return "SKIPPED";

            DataRow l_Row = l_Result.Rows[0];
            string l_Outcome = Convert.ToString(l_Row["Result"]) ?? "SKIPPED";

            if (l_Outcome == "SKIPPED" || l_Outcome == "NOTFOUND")
                return l_Outcome;

            OrderData l_OrderData = new OrderData();

            l_OrderData.UseConnection(p_ConnectionString);
            l_OrderData.DeleteWithType(p_Candidate.Id, "PortalCancel-JSON");

            l_OrderData.Type = "PortalCancel-JSON";
            l_OrderData.Data = JsonConvert.SerializeObject(p_Order);
            l_OrderData.CreatedBy = p_UserNo;
            l_OrderData.CreatedDate = DateTime.Now;
            l_OrderData.OrderId = p_Candidate.Id;
            l_OrderData.OrderNumber = p_Candidate.OrderNumber;

            l_OrderData.SaveNew();

            // CancelError needs a person; it is logged once, later runs leave the order out
            if (l_Outcome == "CANCELERROR")
                p_Route.SaveLog(LogTypeEnum.Error, $"Shopify cancelled order {p_Order.name} ({p_Candidate.OrderNumber}, {Convert.ToString(l_Row["PreviousStatus"])} in eSyncMate), marked CancelError: {Convert.ToString(l_Row["Message"])}", string.Empty, p_UserNo);

            return l_Outcome;
        }
    }
}
