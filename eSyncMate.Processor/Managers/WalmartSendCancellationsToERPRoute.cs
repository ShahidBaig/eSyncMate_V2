using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Connections;
using eSyncMate.Processor.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Data;
using static eSyncMate.DB.Declarations;

namespace eSyncMate.Processor.Managers
{
    /// <summary>
    /// Route 2 of the Walmart cancellation sync (Walmart US + Walmart Canada).
    /// Picks the orders Route 1 marked PortalCancel and cancels each whole order in SPARS (Cancel_Order, OrderNo = ExternalId).
    /// SPARS Success = true -> CANCELLED; anything else -> CancelError, which is not picked again (the operator uses Re-Process).
    /// An order whose ERP ASN arrived after Route 1 is not sent: CancelError straight away.
    /// Only Orders.Status changes; order lines are never touched.
    /// </summary>
    public class WalmartSendCancellationsToERPRoute
    {
        private class CancelResult
        {
            public bool Success { get; set; }
            public bool Responded { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;
            int l_Cancelled = 0;
            int l_Failed = 0;
            DataTable l_dataTable = new DataTable();

            try
            {
                ConnectorDataModel? l_SourceConnector = ConnectorDataModel.Deserialize(route.SourceConnectorObject.Data);
                ConnectorDataModel? l_DestinationConnector = ConnectorDataModel.Deserialize(route.DestinationConnectorObject.Data);

                route.SaveLog(LogTypeEnum.Info, $"Started executing route [{route.Id}]", string.Empty, userNo);

                if (l_SourceConnector == null || l_SourceConnector.ConnectivityType != ConnectorTypesEnum.SqlServer.ToString())
                {
                    route.SaveLog(LogTypeEnum.Error, "Source Connector is not setup properly (eSyncMate SQL Server connector expected)", string.Empty, userNo);
                    return;
                }

                if (l_DestinationConnector == null || l_DestinationConnector.ConnectivityType != ConnectorTypesEnum.Rest.ToString())
                {
                    route.SaveLog(LogTypeEnum.Error, "Destination Connector is not setup properly (SPARS REST connector expected)", string.Empty, userNo);
                    return;
                }

                DBConnector l_Connection = new DBConnector(l_SourceConnector.ConnectionString);

                l_SourceConnector.Command = l_SourceConnector.Command.Replace("@CUSTOMERID@", l_SourceConnector.CustomerID);

                if (l_SourceConnector.CommandType == "SP")
                {
                    l_Connection.GetDataSP(l_SourceConnector.Command, ref l_dataTable);
                }

                if (l_dataTable.Rows.Count > 0)
                {
                    l_DestinationConnector.Url = l_DestinationConnector.BaseUrl + l_DestinationConnector.Url;

                    foreach (DataRow l_Row in l_dataTable.Rows)
                    {
                        string l_OrderNumber = Convert.ToString(l_Row["OrderNumber"]) ?? string.Empty;

                        // ASN arrived between Route 1 and now: the order has shipped, do not ask ERP to cancel it
                        if (Convert.ToInt32(l_Row["HasShipped"]) == 1)
                        {
                            SaveResult(l_Connection, Convert.ToInt32(l_Row["Id"]), false, string.Empty, true, false);
                            route.SaveLog(LogTypeEnum.Error, $"Order [{l_OrderNumber}] was shipped by ERP before the cancellation could be sent, marked CancelError.", string.Empty, userNo);
                            l_Failed++;
                            continue;
                        }

                        CancelResult l_Result = CancelInERP(l_Row, route, l_Connection, l_DestinationConnector, l_SourceConnector.ConnectionString, false, userNo);

                        if (l_Result.Success)
                            l_Cancelled++;
                        else
                            l_Failed++;
                    }

                    route.SaveLog(LogTypeEnum.RouteInfo, $"SPARS Cancel_Order for [{l_SourceConnector.CustomerID}]: {l_dataTable.Rows.Count} order(s), {l_Cancelled} CANCELLED, {l_Failed} CancelError.", string.Empty, userNo);
                }

                route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
            }
            finally
            {
                l_dataTable.Dispose();
            }
        }

        /// <summary>
        /// Re-Process of one CancelError order: sends Cancel_Order to SPARS again with the customer's Route 2 connectors.
        /// Success -> CANCELLED; otherwise the order stays CancelError. The status is NOT moved to InProgress while the
        /// call runs (Place Order picks InProgress orders). Returns an empty string on success, else the message for the user.
        /// </summary>
        public static string ExecuteSingle(IConfiguration config, int orderId, string customerName)
        {
            int userNo = 1;
            DataTable l_dataTable = new DataTable();
            Routes route = new Routes();

            route.UseConnection(CommonUtils.ConnectionString);

            // Route 2 is identified by RouteType 79 + CustomerName, like SCSPlaceOrderRoute.ExecuteSingle
            if (!route.GetObject("TypeId", (int)RouteTypesEnum.WalmartSendCancellationsToERP, "CustomerName", customerName).IsSuccess)
            {
                return $"No cancellation route configured for customer: {customerName}";
            }

            try
            {
                ConnectorDataModel? l_SourceConnector = ConnectorDataModel.Deserialize(route.SourceConnectorObject.Data);
                ConnectorDataModel? l_DestinationConnector = ConnectorDataModel.Deserialize(route.DestinationConnectorObject.Data);

                if (l_SourceConnector == null || l_SourceConnector.ConnectivityType != ConnectorTypesEnum.SqlServer.ToString()
                    || l_DestinationConnector == null || l_DestinationConnector.ConnectivityType != ConnectorTypesEnum.Rest.ToString())
                {
                    return "The cancellation route connectors are not setup properly.";
                }

                DBConnector l_Connection = new DBConnector(l_SourceConnector.ConnectionString);

                l_Connection.GetDataSP($"EXEC Sp_GetWalmartOrdersToCancelInERP @p_CustomerID = '{customerName.Replace("'", "''")}', @p_OrderId = {orderId}", ref l_dataTable);

                if (l_dataTable.Rows.Count == 0)
                {
                    return $"Order {orderId} was not found for customer {customerName}.";
                }

                DataRow l_Row = l_dataTable.Rows[0];
                string l_OrderNumber = Convert.ToString(l_Row["OrderNumber"]) ?? string.Empty;
                string l_Status = Convert.ToString(l_Row["Status"]) ?? string.Empty;

                // Another user or route changed it since the grid was loaded
                if (l_Status != "CancelError")
                {
                    return $"Order {l_OrderNumber} is no longer in Cancel Error (current status: {l_Status}). Nothing was sent to ERP.";
                }

                // Cancel_Order cancels the whole order; the customer cancelled only some lines
                if (Convert.ToString(l_Row["MessageCode"]) == "M10")
                {
                    return $"Order {l_OrderNumber} was only partly cancelled by the customer. Re-Process cannot cancel part of an order in ERP; please update the order lines in ERP manually.";
                }

                if (string.IsNullOrWhiteSpace(Convert.ToString(l_Row["ExternalId"])))
                {
                    return $"Order {l_OrderNumber} has no ERP order number, so it cannot be cancelled in ERP.";
                }

                l_DestinationConnector.Url = l_DestinationConnector.BaseUrl + l_DestinationConnector.Url;

                CancelResult l_Result = CancelInERP(l_Row, route, l_Connection, l_DestinationConnector, l_SourceConnector.ConnectionString, true, userNo);

                if (l_Result.Success)
                    return string.Empty;

                if (!l_Result.Responded)
                    return $"No response from ERP for order {l_OrderNumber}. Please try again in a few minutes.";

                return $"ERP could not cancel order {l_OrderNumber}: {l_Result.Message}";
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error reprocessing the cancellation of order [{orderId}]", ex.ToString(), userNo);
                return ex.Message;
            }
            finally
            {
                l_dataTable.Dispose();
            }
        }

        private static void SaveResult(DBConnector connection, int orderId, bool success, string erpMessage, bool shipped, bool isReprocess)
        {
            connection.Execute($"EXEC Sp_SetWalmartERPCancelResult @p_OrderId = {orderId}, @p_Success = {(success ? 1 : 0)}, " +
                $"@p_ERPMessage = N'{(erpMessage ?? string.Empty).Replace("'", "''")}', @p_Shipped = {(shipped ? 1 : 0)}, @p_IsReprocess = {(isReprocess ? 1 : 0)}");
        }

        private static CancelResult CancelInERP(DataRow l_Row, Routes route, DBConnector connection, ConnectorDataModel destinationConnector, string connectionString, bool isReprocess, int userNo)
        {
            int l_OrderId = Convert.ToInt32(l_Row["Id"]);
            string l_OrderNumber = Convert.ToString(l_Row["OrderNumber"]) ?? string.Empty;
            string l_Response = string.Empty;
            CancelResult l_Result = new CancelResult();

            var data = new
            {
                Input = new
                {
                    OrderNo = Convert.ToString(l_Row["ExternalId"]),
                    CustomerPO = "",
                    ExternalID = ""
                }
            };

            string Body = JsonConvert.SerializeObject(data);

            try
            {
                route.SaveData("JSON-SNT", 0, Body, userNo);

                RestResponse sourceResponse = RestConnector.Execute(destinationConnector, Body).GetAwaiter().GetResult();

                l_Response = LogJson.Ensure(string.IsNullOrEmpty(sourceResponse.Content) ? $"{(int)sourceResponse.StatusCode} {sourceResponse.StatusDescription} - {sourceResponse.ErrorMessage}" : sourceResponse.Content);
                route.SaveData("JSON-RVD", 0, l_Response, userNo);

                if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    JObject l_Json = JObject.Parse(sourceResponse.Content ?? "{}");

                    l_Result.Responded = true;
                    l_Result.Success = l_Json["OutPut"]?["Success"]?.Value<bool>() == true;
                    l_Result.Message = (l_Json["OutPut"]?["Message"]?.ToString() ?? string.Empty).Trim();
                }
                else
                {
                    l_Result.Message = $"no response from ERP (HTTP {(int)sourceResponse.StatusCode} {sourceResponse.StatusCode})";
                }
            }
            catch (Exception ex)
            {
                // Unknown whether SPARS cancelled it -> CancelError, so a person checks
                l_Result.Success = false;
                l_Result.Responded = false;
                l_Result.Message = $"no response from ERP ({ex.Message})";
                l_Response = string.IsNullOrEmpty(l_Response) ? JsonConvert.SerializeObject(new { message = ex.Message, exception = ex.ToString() }) : l_Response;
            }

            SaveResult(connection, l_OrderId, l_Result.Success, l_Result.Message, false, isReprocess);

            // request and response apart: ERPCancel-SNT = the Cancel_Order body sent, then the answer (latest of each kept)
            OrderData l_Request = new OrderData();

            l_Request.UseConnection(connectionString);
            l_Request.DeleteWithType(l_OrderId, "ERPCancel-SNT");

            l_Request.Type = "ERPCancel-SNT";
            l_Request.Data = Body;
            l_Request.CreatedBy = userNo;
            l_Request.CreatedDate = DateTime.Now;
            l_Request.OrderId = l_OrderId;
            l_Request.OrderNumber = l_OrderNumber;

            l_Request.SaveNew();

            string l_Type = l_Result.Success ? "ERPCancel-RVD" : "ERPCancel-ERR";
            OrderData l_OrderData = new OrderData();

            l_OrderData.UseConnection(connectionString);
            l_OrderData.DeleteWithType(l_OrderId, l_Type);

            l_OrderData.Type = l_Type;
            l_OrderData.Data = LogJson.Ensure(l_Response);
            l_OrderData.CreatedBy = userNo;
            l_OrderData.CreatedDate = DateTime.Now;
            l_OrderData.OrderId = l_OrderId;
            l_OrderData.OrderNumber = l_OrderNumber;

            l_OrderData.SaveNew();

            if (!l_Result.Success)
            {
                route.SaveLog(LogTypeEnum.Error, $"SPARS Cancel_Order failed for order [{l_OrderNumber}]{(isReprocess ? " on Re-Process" : string.Empty)}, CancelError: {l_Result.Message}", l_Response, userNo);
            }

            return l_Result;
        }
    }
}
