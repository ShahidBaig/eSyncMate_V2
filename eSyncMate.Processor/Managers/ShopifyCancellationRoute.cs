using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using static eSyncMate.DB.Declarations;
using OL = eSyncMate.Processor.Models.ShopifyOrderLinesResponseModel;
using OE = eSyncMate.Processor.Models.ShopifyOrderEditResponseModel;

namespace eSyncMate.Processor.Managers
{
    /// <summary>
    /// Shopify - Send Cancellation to Portal (RouteType 84, Task 00006).
    /// Source SP_SyncCancelQty (the same SP as the Target / Macys / Lowes cancellation routes): the lines whose units
    /// the ERP cancelled (OrderDetail CANRVD, written by "Fetch Cancellations from ERP"), one row per order line.
    /// Line-wise: each line's Shopify quantity is set to what stays, the line's OrderDetail units that are not cancelled
    /// (order edit: orderEditBegin / SetQuantity / Commit). An order edit does not refund: a refund owed is logged as an
    /// Error for finance. When nothing stays on the whole Shopify order -> orderCancel instead; Shopify runs it in the
    /// background, so the units become CANSNT only once the order shows cancelled (else CANRVD, checked next run).
    /// Quantities are set, never subtracted, so a run that repeats after a lost DB update changes nothing in Shopify;
    /// a quantity is never raised (a line already lower in Shopify counts as done).
    /// Sent (or already so in Shopify) -> units CANSNT. Shopify said no -> units CANERR (not retried; set them
    /// back to CANRVD to send again). A failed call (token / network / HTTP / throttle) leaves them CANRVD for the next run.
    /// Every Shopify call of an order is logged, request and response apart, in every outcome:
    /// OrderData ERPCANLN-SNT (the requests) + ERPCANLN-JSON (sent: result + responses) or ERPCANLN-ERR (not sent).
    /// Destination connector Parameters: notifyCustomer (default false), restock (default true),
    /// refund (whole order only, default false), reason (CUSTOMER / DECLINED / FRAUD / INVENTORY / OTHER / STAFF, default OTHER).
    /// </summary>
    public class ShopifyCancellationRoute
    {
        private const string OrderLinesQuery = @"query OrderLines($id: ID!) {
  order(id: $id) {
    id name cancelledAt displayFulfillmentStatus
    totalOutstandingSet { shopMoney { amount currencyCode } }
    lineItems(first: 100) { nodes { id currentQuantity unfulfilledQuantity } }
  }
}";

        private const string OrderCancelMutation = @"mutation Cancel($orderId: ID!, $reason: OrderCancelReason!, $restock: Boolean!, $notifyCustomer: Boolean, $refundMethod: OrderCancelRefundMethodInput, $staffNote: String) {
  orderCancel(orderId: $orderId, reason: $reason, restock: $restock, notifyCustomer: $notifyCustomer, refundMethod: $refundMethod, staffNote: $staffNote) {
    job { id done }
    orderCancelUserErrors { field message code }
  }
}";

        private const string OrderEditBeginMutation = @"mutation Begin($id: ID!) {
  orderEditBegin(id: $id) {
    calculatedOrder { id lineItems(first: 100) { nodes { id quantity editableQuantity } } }
    userErrors { field message }
  }
}";

        private const string OrderEditSetQuantityMutation = @"mutation SetQuantity($id: ID!, $lineItemId: ID!, $quantity: Int!, $restock: Boolean) {
  orderEditSetQuantity(id: $id, lineItemId: $lineItemId, quantity: $quantity, restock: $restock) {
    calculatedOrder { id }
    userErrors { field message }
  }
}";

        private const string OrderEditCommitMutation = @"mutation Commit($id: ID!, $notifyCustomer: Boolean, $staffNote: String) {
  orderEditCommit(id: $id, notifyCustomer: $notifyCustomer, staffNote: $staffNote) {
    order { id name totalOutstandingSet { shopMoney { amount currencyCode } } }
    userErrors { field message }
  }
}";

        private const string JobQuery = @"query Job($id: ID!) { job(id: $id) { id done } }";

        // orderCancel runs in the background: wait up to JobChecks x JobWaitMs for it before reading the order again
        private const int JobChecks = 5;
        private const int JobWaitMs = 2000;

        private const string StaffNote = "Cancelled by the ERP (eSyncMate)";

        private static readonly HashSet<string> Reasons = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CUSTOMER", "DECLINED", "FRAUD", "INVENTORY", "OTHER", "STAFF" };

        private enum Outcome { Sent, Rejected, CallFailed }

        private class Settings
        {
            public bool Notify;
            public bool Restock = true;
            public bool Refund;
            public string Reason = "OTHER";
        }

        // The Shopify calls made for one order, kept apart as requests and responses for OrderData
        private class CallLog
        {
            public readonly List<object> Requests = new List<object>();
            public readonly List<object> Responses = new List<object>();

            public ShopifyGraphQLResult<T> Run<T>(ConnectorDataModel p_Connector, string p_Step, string p_Query, object p_Variables) where T : ShopifyGraphQLResponseModel
            {
                ShopifyGraphQLResult<T> l_Result = ShopifyGraphQL.Execute<T>(p_Connector, p_Query, p_Variables);

                Requests.Add(new { step = p_Step, at = DateTime.Now, request = LogJson.Embed(l_Result.RequestBody) });
                Responses.Add(new { step = p_Step, at = DateTime.Now, success = l_Result.Success, error = l_Result.Success ? null : l_Result.Error, response = LogJson.Embed(l_Result.ResponseBody) });

                return l_Result;
            }

            // the last request, for the route log of a failure
            public string LastRequest()
            {
                return Requests.Count == 0 ? string.Empty : JsonConvert.SerializeObject(Requests[Requests.Count - 1]);
            }
        }

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

                Settings l_Settings = new Settings
                {
                    Notify = GetBool(l_DestinationConnector, "notifyCustomer", false),
                    Restock = GetBool(l_DestinationConnector, "restock", true),
                    Refund = GetBool(l_DestinationConnector, "refund", false)
                };

                string l_Reason = GetParameter(l_DestinationConnector, "reason");

                if (Reasons.Contains(l_Reason))
                    l_Settings.Reason = l_Reason.ToUpperInvariant();

                // RestConnector puts connector Parameters on the URL; these are only settings
                l_DestinationConnector.Parmeters = new List<eSyncMate.Processor.Models.Parameter>();

                l_SourceConnector.Command = l_SourceConnector.Command.Replace("@CUSTOMERID@", l_SourceConnector.CustomerID);

                if (l_SourceConnector.CommandType == "SP")
                    new DBConnector(l_SourceConnector.ConnectionString).GetDataSP(l_SourceConnector.Command, ref l_Data);

                if (l_Data.Rows.Count == 0)
                {
                    route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]: no cancelled lines waiting", string.Empty, userNo);
                    return;
                }

                int l_Sent = 0, l_Rejected = 0, l_CallFailed = 0;

                foreach (IGrouping<int, DataRow> l_Order in l_Data.Rows.Cast<DataRow>().GroupBy(r => Convert.ToInt32(r["Id"])))
                {
                    Outcome l_Outcome = ProcessOrder(l_Order.Key, l_Order.ToList(), l_SourceConnector, l_DestinationConnector, l_Settings, route, userNo);

                    if (l_Outcome == Outcome.Sent) l_Sent++;
                    else if (l_Outcome == Outcome.Rejected) l_Rejected++;
                    else l_CallFailed++;
                }

                // RouteInfo: SaveLog keeps only Error / Exception / RouteInfo
                route.SaveLog(LogTypeEnum.RouteInfo, $"Shopify cancellations for [{l_SourceConnector.CustomerID}]: {l_Sent + l_Rejected + l_CallFailed} order(s), {l_Sent} sent, {l_Rejected} rejected by Shopify (CANERR), {l_CallFailed} call failure(s) (retried next run).", string.Empty, userNo);
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

        private static Outcome ProcessOrder(int p_OrderId, List<DataRow> p_Rows, ConnectorDataModel p_Source, ConnectorDataModel p_Destination,
                                            Settings p_Settings, Routes p_Route, int p_UserNo)
        {
            string l_OrderNumber = Convert.ToString(p_Rows[0]["OrderNumber"]) ?? string.Empty;
            string l_OrderGid = "gid://shopify/Order/" + l_OrderNumber;
            List<int> l_LineNos = p_Rows.Select(r => Convert.ToInt32(r["LineNo"])).ToList();
            CallLog l_Log = new CallLog();

            ShopifyGraphQLResult<OL> l_OrderResult = l_Log.Run<OL>(p_Destination, "readOrder", OrderLinesQuery, new { id = l_OrderGid });
            OL.Order l_ShopifyOrder = l_OrderResult.Success ? l_OrderResult.Response.data?.order : null;

            if (l_ShopifyOrder == null)
            {
                if (!l_OrderResult.Success)
                    return CallFailed(p_Source, p_Route, p_OrderId, l_OrderNumber, $"the order could not be read: {l_OrderResult.Error}", l_Log, p_UserNo);

                return Rejected(p_Source, p_Route, p_OrderId, l_OrderNumber, l_LineNos, $"Shopify has no order {l_OrderNumber}", l_Log, p_UserNo);
            }

            // cancelled in Shopify already (by hand, or an earlier run whose DB update was lost): nothing left to do
            if (!string.IsNullOrEmpty(l_ShopifyOrder.cancelledAt))
                return Sent(p_Source, p_OrderId, l_OrderNumber, l_LineNos,
                            new { message = $"Order {l_ShopifyOrder.name} is already cancelled in Shopify ({l_ShopifyOrder.cancelledAt}); nothing sent." }, l_Log, p_UserNo);

            return EditOrder(p_OrderId, l_OrderNumber, l_OrderGid, p_Rows, l_ShopifyOrder, p_Source, p_Destination, p_Settings, p_Route, l_Log, p_UserNo);
        }

        // no unit left on the whole Shopify order -> orderCancel
        private static Outcome CancelOrder(int p_OrderId, string p_OrderNumber, string p_OrderGid, List<int> p_LineNos, ConnectorDataModel p_Source,
                                           ConnectorDataModel p_Destination, Settings p_Settings, Routes p_Route, CallLog p_Log, int p_UserNo)
        {
            var l_Variables = new
            {
                orderId = p_OrderGid,
                reason = p_Settings.Reason,
                restock = p_Settings.Restock,
                notifyCustomer = p_Settings.Notify,
                refundMethod = new { originalPaymentMethodsRefund = p_Settings.Refund },
                staffNote = StaffNote
            };

            ShopifyGraphQLResult<ShopifyOrderCancelResponseModel> l_Result = p_Log.Run<ShopifyOrderCancelResponseModel>(p_Destination, "orderCancel", OrderCancelMutation, l_Variables);

            if (!l_Result.Success)
                return CallFailed(p_Source, p_Route, p_OrderId, p_OrderNumber, $"orderCancel failed: {l_Result.Error}", p_Log, p_UserNo);

            ShopifyOrderCancelResponseModel.OrderCancel l_Payload = l_Result.Response.data?.orderCancel;

            if (l_Payload == null || l_Payload.orderCancelUserErrors?.Count > 0)
            {
                string l_Why = l_Payload == null ? "no orderCancel in the response" : JoinErrors(l_Payload.orderCancelUserErrors);

                return Rejected(p_Source, p_Route, p_OrderId, p_OrderNumber, p_LineNos, $"orderCancel rejected: {l_Why}", p_Log, p_UserNo);
            }

            // the job can still fail in Shopify (e.g. while a payment is processing): sent only once the order shows cancelled
            bool l_Done = l_Payload.job == null || l_Payload.job.done;

            for (int i = 0; i < JobChecks && !l_Done; i++)
            {
                Thread.Sleep(JobWaitMs);

                ShopifyGraphQLResult<ShopifyOrderCancelResponseModel> l_Job = p_Log.Run<ShopifyOrderCancelResponseModel>(p_Destination, "job", JobQuery, new { id = l_Payload.job.id });

                l_Done = l_Job.Success && l_Job.Response.data?.job?.done == true;
            }

            ShopifyGraphQLResult<OL> l_After = p_Log.Run<OL>(p_Destination, "readOrderAfterCancel", OrderLinesQuery, new { id = p_OrderGid });
            OL.Order l_Cancelled = l_After.Success ? l_After.Response.data?.order : null;

            if (!string.IsNullOrEmpty(l_Cancelled?.cancelledAt))
            {
                LogRefundOwed(p_Route, p_OrderId, l_Cancelled.name ?? p_OrderNumber, l_Cancelled.totalOutstandingSet, "order cancelled by the ERP", p_UserNo);

                return Sent(p_Source, p_OrderId, p_OrderNumber, p_LineNos, new { cancelledAt = l_Cancelled.cancelledAt }, p_Log, p_UserNo);
            }

            // still running (or the check failed): the units stay CANRVD; the next run finds the order cancelled and records it
            if (!l_Done || !l_After.Success)
                return CallFailed(p_Source, p_Route, p_OrderId, p_OrderNumber, $"orderCancel accepted (job {l_Payload.job?.id}) but the order is not cancelled yet; checked again next run", p_Log, p_UserNo);

            return Rejected(p_Source, p_Route, p_OrderId, p_OrderNumber, p_LineNos, $"orderCancel job {l_Payload.job?.id} finished but Shopify did not cancel the order", p_Log, p_UserNo);
        }

        // line-wise: each cancelled line set to the quantity that stays (order edit); nothing left on the order -> orderCancel
        private static Outcome EditOrder(int p_OrderId, string p_OrderNumber, string p_OrderGid, List<DataRow> p_Rows, OL.Order p_ShopifyOrder,
                                         ConnectorDataModel p_Source, ConnectorDataModel p_Destination, Settings p_Settings, Routes p_Route, CallLog p_Log, int p_UserNo)
        {
            List<int> l_LineNos = p_Rows.Select(r => Convert.ToInt32(r["LineNo"])).ToList();
            List<string> l_Problems = new List<string>();
            List<(DataRow Row, string LineId, int Keep)> l_Changes = new List<(DataRow, string, int)>();
            Dictionary<string, int> l_Targets = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Dictionary<int, int> l_UnitsToKeep = GetUnitsToKeep(p_Source, p_OrderId);

            foreach (DataRow l_Row in p_Rows)
            {
                string l_LineId = GetLineId(p_Source, p_OrderId, l_Row);
                // what stays = the line's units the ERP did not cancel, counted from OrderDetail (one row per unit), so a
                // second cancel on the same line lands too, whatever SP_SyncCancelQty counts in LineQty / CancelQty
                int l_Keep = l_UnitsToKeep.GetValueOrDefault(Convert.ToInt32(l_Row["LineNo"]));
                OL.LineItem l_Line = p_ShopifyOrder.lineItems?.nodes?.FirstOrDefault(l => string.Equals(l?.id, l_LineId, StringComparison.OrdinalIgnoreCase));

                if (l_Line == null)
                {
                    l_Problems.Add($"line {l_Row["LineNo"]} ({l_LineId}) is not on the Shopify order");
                    continue;
                }

                int l_Fulfilled = l_Line.currentQuantity - l_Line.unfulfilledQuantity;

                l_Targets[l_Line.id] = Math.Min(l_Keep, l_Line.currentQuantity);

                // already that low in Shopify: done (a quantity is never raised)
                if (l_Line.currentQuantity <= l_Keep)
                    continue;

                if (l_Keep < l_Fulfilled)
                {
                    l_Problems.Add($"line {l_Row["LineNo"]} ({l_LineId}): {l_Fulfilled} unit(s) already fulfilled in Shopify, cannot go down to {l_Keep}");
                    continue;
                }

                l_Changes.Add((l_Row, l_LineId, l_Keep));
            }

            if (l_Problems.Count > 0)
                return Rejected(p_Source, p_Route, p_OrderId, p_OrderNumber, l_LineNos, string.Join("; ", l_Problems), p_Log, p_UserNo);

            // after these lines nothing stays on the Shopify order (lines not in this run keep their current quantity)
            bool l_NothingLeft = (p_ShopifyOrder.lineItems?.nodes ?? new List<OL.LineItem>())
                .All(l => (l_Targets.TryGetValue(l.id ?? string.Empty, out int l_T) ? l_T : l.currentQuantity) == 0);

            if (l_Changes.Count > 0 && l_NothingLeft)
                return CancelOrder(p_OrderId, p_OrderNumber, p_OrderGid, l_LineNos, p_Source, p_Destination, p_Settings, p_Route, p_Log, p_UserNo);

            if (l_Changes.Count == 0)
                return Sent(p_Source, p_OrderId, p_OrderNumber, l_LineNos,
                            new { message = "Shopify already shows these quantities; nothing sent." }, p_Log, p_UserNo);

            ShopifyGraphQLResult<OE> l_Begin = p_Log.Run<OE>(p_Destination, "orderEditBegin", OrderEditBeginMutation, new { id = p_OrderGid });

            if (!l_Begin.Success)
                return CallFailed(p_Source, p_Route, p_OrderId, p_OrderNumber, $"orderEditBegin failed: {l_Begin.Error}", p_Log, p_UserNo);

            OE.OrderEdit l_Edit = l_Begin.Response.data?.orderEditBegin;

            if (l_Edit?.calculatedOrder == null || l_Edit.userErrors?.Count > 0)
                return Rejected(p_Source, p_Route, p_OrderId, p_OrderNumber, l_LineNos,
                                $"orderEditBegin rejected: {(l_Edit == null ? "no orderEditBegin in the response" : JoinErrors(l_Edit.userErrors))}", p_Log, p_UserNo);

            string l_CalculatedOrderId = l_Edit.calculatedOrder.id;
            List<object> l_Sent = new List<object>();

            foreach ((DataRow Row, string LineId, int Keep) l_Change in l_Changes)
            {
                // a calculated line item carries the number of its order line item
                string l_Number = l_Change.LineId.Split('/').Last();
                OE.CalculatedLineItem l_Calculated = l_Edit.calculatedOrder.lineItems?.nodes?.FirstOrDefault(c => (c?.id ?? string.Empty).Split('/').Last() == l_Number);

                if (l_Calculated == null)
                    return Rejected(p_Source, p_Route, p_OrderId, p_OrderNumber, l_LineNos,
                                    $"line {l_Change.Row["LineNo"]} ({l_Change.LineId}) is not in the Shopify order edit", p_Log, p_UserNo);

                var l_Variables = new { id = l_CalculatedOrderId, lineItemId = l_Calculated.id, quantity = l_Change.Keep, restock = p_Settings.Restock };

                ShopifyGraphQLResult<OE> l_Set = p_Log.Run<OE>(p_Destination, $"orderEditSetQuantity line {l_Change.Row["LineNo"]}", OrderEditSetQuantityMutation, l_Variables);

                // nothing is committed yet: an edit left open changes nothing in Shopify
                if (!l_Set.Success)
                    return CallFailed(p_Source, p_Route, p_OrderId, p_OrderNumber, $"orderEditSetQuantity failed: {l_Set.Error}", p_Log, p_UserNo);

                OE.OrderEdit l_SetPayload = l_Set.Response.data?.orderEditSetQuantity;

                if (l_SetPayload == null || l_SetPayload.userErrors?.Count > 0)
                    return Rejected(p_Source, p_Route, p_OrderId, p_OrderNumber, l_LineNos,
                                    $"orderEditSetQuantity rejected for line {l_Change.Row["LineNo"]}: {(l_SetPayload == null ? "no orderEditSetQuantity in the response" : JoinErrors(l_SetPayload.userErrors))}",
                                    p_Log, p_UserNo);

                l_Sent.Add(new { lineNo = l_Change.Row["LineNo"], lineItemId = l_Change.LineId, from = l_Calculated.quantity, to = l_Change.Keep });
            }

            ShopifyGraphQLResult<OE> l_Commit = p_Log.Run<OE>(p_Destination, "orderEditCommit", OrderEditCommitMutation,
                                                             new { id = l_CalculatedOrderId, notifyCustomer = p_Settings.Notify, staffNote = StaffNote });

            if (!l_Commit.Success)
                return CallFailed(p_Source, p_Route, p_OrderId, p_OrderNumber, $"orderEditCommit failed: {l_Commit.Error}", p_Log, p_UserNo);

            OE.OrderEditCommit l_CommitPayload = l_Commit.Response.data?.orderEditCommit;

            if (l_CommitPayload?.order == null || l_CommitPayload.userErrors?.Count > 0)
                return Rejected(p_Source, p_Route, p_OrderId, p_OrderNumber, l_LineNos,
                                $"orderEditCommit rejected: {(l_CommitPayload == null ? "no orderEditCommit in the response" : JoinErrors(l_CommitPayload.userErrors))}",
                                p_Log, p_UserNo);

            // an order edit never refunds: when the customer had paid for the removed units, Shopify now owes them money
            decimal l_Outstanding = LogRefundOwed(p_Route, p_OrderId, l_CommitPayload.order.name ?? p_OrderNumber, l_CommitPayload.order.totalOutstandingSet, "lines cancelled by the ERP", p_UserNo);

            return Sent(p_Source, p_OrderId, p_OrderNumber, l_LineNos, new { lines = l_Sent, refundOwed = l_Outstanding < 0 ? -l_Outstanding : 0 }, p_Log, p_UserNo);
        }

        // Refunding is the client's decision (not made yet), so a refund Shopify owes the customer (negative
        // totalOutstandingSet) is an Error in the route log for finance. Returns the outstanding amount.
        private static decimal LogRefundOwed(Routes p_Route, int p_OrderId, string p_OrderName, ShopifyOrdersResponseModel.MoneyBag p_Outstanding, string p_What, int p_UserNo)
        {
            decimal l_Amount = p_Outstanding?.shopMoney?.amount ?? 0;

            if (l_Amount < 0)
                p_Route.SaveLog(LogTypeEnum.Error, $"Shopify order {p_OrderName} (Id {p_OrderId}): {p_What}, refund owed to the customer {-l_Amount:0.00} {p_Outstanding.shopMoney.currencyCode}. Nothing was refunded automatically; refund it in Shopify admin.", string.Empty, p_UserNo);

            return l_Amount;
        }

        // LineNo -> units the ERP still wants (OrderDetail rows not cancelled: neither CANRVD, CANSNT nor CANERR)
        private static Dictionary<int, int> GetUnitsToKeep(ConnectorDataModel p_Source, int p_OrderId)
        {
            Dictionary<int, int> l_Keep = new Dictionary<int, int>();
            DataTable l_Table = new DataTable();

            new DBConnector(p_Source.ConnectionString).GetData(
                $"SELECT [LineNo], SUM(CASE WHEN ISNULL(Status, '') IN ('CANRVD', 'CANSNT', 'CANERR') THEN 0 ELSE LineQty END) AS Keep FROM OrderDetail WITH (NOLOCK) WHERE OrderId = {p_OrderId} GROUP BY [LineNo]", ref l_Table);

            foreach (DataRow l_Row in l_Table.Rows)
                l_Keep[Convert.ToInt32(l_Row["LineNo"])] = Convert.ToInt32(l_Row["Keep"]);

            l_Table.Dispose();

            return l_Keep;
        }

        private static Outcome Sent(ConnectorDataModel p_Source, int p_OrderId, string p_OrderNumber, List<int> p_LineNos, object p_Result, CallLog p_Log, int p_UserNo)
        {
            OrderDetail l_Detail = new OrderDetail();
            l_Detail.UseConnection(p_Source.ConnectionString);

            // CANRVD -> CANSNT per line, as the other marketplaces' cancellation routes do
            foreach (int l_LineNo in p_LineNos.Distinct())
                l_Detail.UpdateOrderDetailStatus(p_OrderId, l_LineNo);

            SaveCalls(p_Source, p_OrderId, p_OrderNumber, "ERPCANLN-JSON", new { outcome = "SENT", result = p_Result, responses = p_Log.Responses }, p_Log, p_UserNo);

            return Outcome.Sent;
        }

        private static Outcome Rejected(ConnectorDataModel p_Source, Routes p_Route, int p_OrderId, string p_OrderNumber, List<int> p_LineNos,
                                        string p_Why, CallLog p_Log, int p_UserNo)
        {
            // CANERR: not picked again, so a rejected cancellation does not write an error row every run
            new DBConnector(p_Source.ConnectionString).Execute(
                $"UPDATE OrderDetail SET Status = 'CANERR' WHERE OrderId = {p_OrderId} AND [LineNo] IN ({string.Join(",", p_LineNos.Distinct())}) AND Status = 'CANRVD'");

            SaveCalls(p_Source, p_OrderId, p_OrderNumber, "ERPCANLN-ERR", new { outcome = "REJECTED", message = p_Why, responses = p_Log.Responses }, p_Log, p_UserNo);
            p_Route.SaveLog(LogTypeEnum.Error, $"Shopify cancellation for order {p_OrderNumber} (Id {p_OrderId}) not sent, lines set to CANERR: {p_Why}", p_Log.LastRequest(), p_UserNo);

            return Outcome.Rejected;
        }

        private static Outcome CallFailed(ConnectorDataModel p_Source, Routes p_Route, int p_OrderId, string p_OrderNumber, string p_Why, CallLog p_Log, int p_UserNo)
        {
            // units stay CANRVD: the next run tries again
            SaveCalls(p_Source, p_OrderId, p_OrderNumber, "ERPCANLN-ERR", new { outcome = "CALL_FAILED", retryNextRun = true, message = p_Why, responses = p_Log.Responses }, p_Log, p_UserNo);
            p_Route.SaveLog(LogTypeEnum.Error, $"Shopify cancellation for order {p_OrderNumber} (Id {p_OrderId}) not sent, retried next run: {p_Why}", p_Log.LastRequest(), p_UserNo);

            return Outcome.CallFailed;
        }

        // request and response apart: ERPCANLN-SNT = every request made for the order, p_Type = the outcome + every response
        private static void SaveCalls(ConnectorDataModel p_Source, int p_OrderId, string p_OrderNumber, string p_Type, object p_Response, CallLog p_Log, int p_UserNo)
        {
            if (p_Log.Requests.Count > 0)
                SaveOrderData(p_Source, p_OrderId, p_OrderNumber, "ERPCANLN-SNT", JsonConvert.SerializeObject(new { requests = p_Log.Requests }), p_UserNo);

            SaveOrderData(p_Source, p_OrderId, p_OrderNumber, p_Type, JsonConvert.SerializeObject(p_Response), p_UserNo);
        }

        // the Shopify line item gid: from the SP row when it has the column, else from OrderDetail
        private static string GetLineId(ConnectorDataModel p_Source, int p_OrderId, DataRow p_Row)
        {
            if (p_Row.Table.Columns.Contains("order_line_id") && !string.IsNullOrWhiteSpace(Convert.ToString(p_Row["order_line_id"])))
                return Convert.ToString(p_Row["order_line_id"]).Trim();

            DataTable l_Table = new DataTable();

            new DBConnector(p_Source.ConnectionString).GetData(
                $"SELECT MAX(order_line_id) AS order_line_id FROM OrderDetail WITH (NOLOCK) WHERE OrderId = {p_OrderId} AND [LineNo] = {Convert.ToInt32(p_Row["LineNo"])}", ref l_Table);

            string l_Id = l_Table.Rows.Count > 0 ? Convert.ToString(l_Table.Rows[0]["order_line_id"])?.Trim() ?? string.Empty : string.Empty;

            l_Table.Dispose();

            return l_Id;
        }

        private static string JoinErrors(List<ShopifyGraphQLResponseModel.UserError> p_Errors)
        {
            return string.Join("; ", (p_Errors ?? new List<ShopifyGraphQLResponseModel.UserError>()).Select(e => string.IsNullOrEmpty(e?.code) ? e?.message : $"{e.code}: {e.message}"));
        }

        private static string GetParameter(ConnectorDataModel p_Connector, string p_Name)
        {
            return p_Connector.Parmeters?.FirstOrDefault(p => string.Equals(p?.Name?.Trim(), p_Name, StringComparison.OrdinalIgnoreCase))?.Value?.Trim() ?? string.Empty;
        }

        private static bool GetBool(ConnectorDataModel p_Connector, string p_Name, bool p_Default)
        {
            return bool.TryParse(GetParameter(p_Connector, p_Name), out bool l_Value) ? l_Value : p_Default;
        }

        private static void SaveOrderData(ConnectorDataModel p_Source, int p_OrderId, string p_OrderNumber, string p_Type, string p_Data, int p_UserNo)
        {
            OrderData l_OrderData = new OrderData();

            l_OrderData.UseConnection(p_Source.ConnectionString);
            l_OrderData.Type = p_Type;
            l_OrderData.Data = LogJson.Ensure(p_Data);
            l_OrderData.CreatedBy = p_UserNo;
            l_OrderData.CreatedDate = DateTime.Now;
            l_OrderData.OrderId = p_OrderId;
            l_OrderData.OrderNumber = p_OrderNumber;

            l_OrderData.SaveNew();
        }
    }
}
