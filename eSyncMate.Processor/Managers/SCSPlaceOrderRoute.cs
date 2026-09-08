using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Connections;
using eSyncMate.Processor.Models;
using Intercom.Core;
using Intercom.Data;
using JUST;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators.OAuth;
using RestSharp.Authenticators;
using System.Data;
using static eSyncMate.DB.Declarations;
using static eSyncMate.Processor.Models.SCSPlaceOrderResponseModel;
using Hangfire.Storage;
using static eSyncMate.Processor.Models.MacysGetOrderResponseModel;
using DocumentFormat.OpenXml.Office2010.Excel;
using Nancy;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.Encodings.Web;
using DocumentFormat.OpenXml.Spreadsheet;
using static eSyncMate.Processor.Models.LowesGetOrderResponseModel;

namespace eSyncMate.Processor.Managers
{
    public class SCSPlaceOrderRoute
    {
        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;
            DataTable l_dataTable = new DataTable();

            try
            {
                ConnectorDataModel? l_SourceConnector = ConnectorDataModel.Deserialize(route.SourceConnectorObject.Data);
                ConnectorDataModel? l_DestinationConnector = ConnectorDataModel.Deserialize(route.DestinationConnectorObject.Data);

                route.SaveLog(LogTypeEnum.Info, $"Started executing route [{route.Id}]", string.Empty, userNo);

                if (l_SourceConnector == null)
                {
                    
                    route.SaveLog(LogTypeEnum.Error, "Source Connector is not setup properly", string.Empty, userNo);
                    return;
                }

                if (l_DestinationConnector == null)
                {
                    
                    route.SaveLog(LogTypeEnum.Error, "Destination Connector is not setup properly", string.Empty, userNo);
                    return;
                }

                if (l_SourceConnector.Parmeters != null)
                {
                    foreach (Models.Parameter l_Parameter in l_SourceConnector.Parmeters)
                    {
                        l_Parameter.Value = l_Parameter.Value.Replace("@CUSTOMERID@", route.SourcePartyObject.ERPCustomerID);
                    }
                }

                eSyncMate.DB.Entities.Maps map = new eSyncMate.DB.Entities.Maps();
                string l_TransformationMap = string.Empty;

                map.UseConnection(l_SourceConnector.ConnectionString);
                map.GetObject(route.MapId);

                l_TransformationMap = map.Map;

                if (string.IsNullOrEmpty(l_TransformationMap))
                {
                    route.SaveLog(LogTypeEnum.Error, $"Required map for order processing is missing.", string.Empty, userNo);
                    return;
                }

                if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                {
                    route.SaveLog(LogTypeEnum.Debug, "Source connector processing start...", string.Empty, userNo);

                    DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);
                    DataTable l_Data = new DataTable();

                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@DATATYPE@", "API-JSON");
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ORDERSTATUS@", "New,InProgress");

                    if (l_SourceConnector.CommandType == "SP")
                    {
                        connection.GetDataSP(l_SourceConnector.Command, ref l_dataTable);
                    }

                    route.SaveLog(LogTypeEnum.Debug, "Source connector processed.", string.Empty, userNo);
                }

                if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString() && l_dataTable.Rows.Count > 0)
                {
                    route.SaveLog(LogTypeEnum.Debug, "Destination connector processing start...", string.Empty, userNo);

                    foreach (DataRow l_Row in l_dataTable.Rows)
                    {
                        // One bad order used to abort the whole run: the only catch was around the
                        // entire route, so every order queued behind it silently never reached the
                        // ERP. Each order is isolated now — it is logged and the batch carries on.
                        try
                        {
                            ProcessOrder(l_Row, route, l_DestinationConnector, l_SourceConnector, l_TransformationMap, userNo);
                        }
                        catch (JsonReaderException exJson)
                        {
                            // The stored API-JSON will not parse. Called out separately because the
                            // raw exception names no order, which makes it unfindable in the log.
                            route.SaveLog(LogTypeEnum.Error,
                                $"Order [{PublicFunctions.ConvertNullAsString(l_Row["OrderNumber"], string.Empty)}] (Id {PublicFunctions.ConvertNullAsString(l_Row["Id"], string.Empty)}) has malformed API-JSON and was skipped. Fix OrderData.Data for this order, then re-process it.",
                                exJson.ToString(), userNo);
                        }
                        catch (Exception exOrder)
                        {
                            route.SaveLog(LogTypeEnum.Exception,
                                $"Error processing order [{PublicFunctions.ConvertNullAsString(l_Row["OrderNumber"], string.Empty)}] (Id {PublicFunctions.ConvertNullAsString(l_Row["Id"], string.Empty)}) — skipped, the remaining orders continue.",
                                exOrder.ToString(), userNo);
                        }
                    }

                    route.SaveLog(LogTypeEnum.Debug, "Destination connector processed.", string.Empty, userNo);
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

        public static string ExecuteSingle(IConfiguration config, int orderId, string customerName, bool isResubmit = false)
        {
            return ExecuteSingle(config, orderId, customerName, isResubmit, out _);
        }

        // infoMessage = a non-error outcome to show the user (e.g. the order already exists in the ERP
        // with a live status, so it was NOT re-placed).
        public static string ExecuteSingle(IConfiguration config, int orderId, string customerName, bool isResubmit, out string infoMessage)
        {
            infoMessage = string.Empty;

            Routes route = new Routes();

            route.UseConnection(CommonUtils.ConnectionString);

            // Place-Order route is identified by RouteType SCSPlaceOrder (9) + CustomerName —
            // no hardcoded route names, so new customers work without a code change.
            if (!route.GetObject("TypeId", (int)RouteTypesEnum.SCSPlaceOrder, "CustomerName", customerName).IsSuccess)
            {
                return $"No route configured for customer: {customerName}";
            }

            if (route.Status.ToUpper() == "IN-ACTIVE")
            {
                return "Order processing route is not active.";
            }

            return ExecuteSingle(config, route, orderId, isResubmit, out infoMessage);
        }

        // isResubmit = user-triggered resubmit of an already-SYNCED order. The SPARS existence/status
        // check STILL runs (only a missing or cancelled/void order is re-placed, exactly like Reprocess);
        // the flag only keeps the previous OrderData logs instead of replacing them.
        private static string ExecuteSingle(IConfiguration config, Routes route, int orderId, bool isResubmit, out string infoMessage)
        {
            infoMessage = string.Empty;

            int userNo = 1;
            DataTable l_dataTable = new DataTable();

            try
            {
                ConnectorDataModel? l_SourceConnector = ConnectorDataModel.Deserialize(route.SourceConnectorObject.Data);
                ConnectorDataModel? l_DestinationConnector = ConnectorDataModel.Deserialize(route.DestinationConnectorObject.Data);

                route.SaveLog(LogTypeEnum.Info, $"Started executing route [{route.Id}]", string.Empty, userNo);

                if (l_SourceConnector == null)
                {
                    route.SaveLog(LogTypeEnum.Error, "Source Connector is not setup properly", string.Empty, userNo);
                    return "Source Connector is not setup properly";
                }

                if (l_DestinationConnector == null)
                {
                    route.SaveLog(LogTypeEnum.Error, "Destination Connector is not setup properly", string.Empty, userNo);
                    return "Destination Connector is not setup properly";
                }

                eSyncMate.DB.Entities.Maps map = new eSyncMate.DB.Entities.Maps();
                string l_TransformationMap = string.Empty;

                map.UseConnection(l_SourceConnector.ConnectionString);
                map.GetObject(route.MapId);

                l_TransformationMap = map.Map;

                if (string.IsNullOrEmpty(l_TransformationMap))
                {
                    route.SaveLog(LogTypeEnum.Error, $"Required map for order processing is missing.", string.Empty, userNo);
                    return $"Required map for order processing is missing.";
                }

                if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                {
                    route.SaveLog(LogTypeEnum.Debug, "Source connector processing start...", string.Empty, userNo);

                    DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);
                    DataTable l_Data = new DataTable();

                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@DATATYPE@", "API-JSON");
                    // Resubmit picks up the order while it is still SYNCED — its status is only moved to
                    // InProgress later, at the moment the order is actually posted to the ERP.
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ORDERSTATUS@", isResubmit ? "InProgress,SYNCED" : "InProgress");
                    l_SourceConnector.Command += $", @p_OrderId = {orderId}";

                    if (l_SourceConnector.CommandType == "SP")
                    {
                        connection.GetDataSP(l_SourceConnector.Command, ref l_dataTable);
                    }

                    route.SaveLog(LogTypeEnum.Debug, "Source connector processed.", string.Empty, userNo);
                }

                if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString() && l_dataTable.Rows.Count > 0)
                {
                    route.SaveLog(LogTypeEnum.Debug, "Destination connector processing start...", string.Empty, userNo);

                    foreach (DataRow l_Row in l_dataTable.Rows)
                    {
                        string l_Info = ProcessOrder(l_Row, route, l_DestinationConnector, l_SourceConnector, l_TransformationMap, userNo, isResubmit);

                        if (!string.IsNullOrEmpty(l_Info))
                        {
                            infoMessage = l_Info;
                        }
                    }

                    route.SaveLog(LogTypeEnum.Debug, "Destination connector processed.", string.Empty, userNo);
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

            return string.Empty;
        }

        // Returns an informational message when the order was intentionally NOT posted (already exists
        // in the ERP with a live status); empty string means the order was posted.
        private static string ProcessOrder(DataRow l_Row, Routes route, ConnectorDataModel destinationConnector, ConnectorDataModel sourceConnector, string transformationMap, int userNo, bool isResubmit = false)
        {
            RestResponse sourceResponse = new RestResponse();
            SCSPlaceOrderResponse l_SCSPlaceOrderResponse = new SCSPlaceOrderResponse();

            SCSGetOrderInfoModel l_SCSGetOrderInfoModel = new SCSGetOrderInfoModel();

            l_SCSPlaceOrderResponse = new SCSPlaceOrderResponse();
            OrderData l_OrderData = new OrderData();
            string Body = PublicFunctions.ConvertNullAsString(l_Row["Data"], string.Empty);
            int l_ID = PublicFunctions.ConvertNullAsInteger(l_Row["Id"], 0);

            // A payload that will not parse can never be placed, so this order is skipped and the
            // batch carries on. Deliberately NOT repaired here — this route posts orders, it does
            // not rewrite stored data. Repair it with the Re-Map Item IDs action (or by fixing
            // OrderData.Data), then re-process the order.
            if (!OrderPayloadRepair.IsValid(Body))
            {
                route.SaveLog(LogTypeEnum.Error,
                    $"Order [{l_ID}] has an API-JSON payload that is not readable JSON — skipped, the remaining orders continue. Repair the payload, then re-process this order.",
                    Body != null && Body.Length > 2000 ? Body.Substring(0, 2000) : Body, userNo);

                // Moved to ERROR exactly like any other placement failure. Without this the order
                // keeps its New/InProgress status, so the route re-reads and re-fails it on every
                // run, it never shows as an error on the Orders screen, and the Re-Map Item IDs
                // action — which is what repairs it — never appears, because that action only
                // offers itself on error rows.
                DBConnector l_StatusConnection = new DBConnector(sourceConnector.ConnectionString);

                l_StatusConnection.Execute("EXEC SP_UpdateOrderStatus @p_CustomerID ='" + sourceConnector.CustomerID + "',@p_RouteType = '" + RouteTypesEnum.SCSPlaceOrder + "Error',@p_ExternalId = '',@p_OrderId = " + l_ID);

                string l_PayloadErrorContent = JsonConvert.SerializeObject(new
                {
                    message = "Order payload is not readable JSON — a product Title contains an unescaped quote. Use the Re-Map Item IDs action to repair it, then re-process the order."
                });

                l_OrderData.UseConnection(sourceConnector.ConnectionString);

                if (!isResubmit)
                {
                    l_OrderData.DeleteWithType(l_ID, "ERP-ERROR");
                }

                l_OrderData.Type = "ERP-ERROR";
                l_OrderData.Data = l_PayloadErrorContent;
                l_OrderData.CreatedBy = userNo;
                l_OrderData.CreatedDate = DateTime.Now;
                l_OrderData.OrderId = l_ID;
                l_OrderData.OrderNumber = PublicFunctions.ConvertNullAsString(l_Row["OrderNumber"], string.Empty);
                l_OrderData.SaveNew();

                // Informational, not an exception: the single-order paths (Re-Process / Resubmit)
                // surface this text to the user.
                return "Order payload is not readable JSON — not posted.";
            }

            string jsonTransformation = new JsonTransformer().Transform(transformationMap, Body);
            jsonTransformation = jsonTransformation.Replace("@CUSTOMERID@", destinationConnector.CustomerID);
            string OrderStatus = PublicFunctions.ConvertNullAsString(l_Row["Status"], "");

            try
            {
                if (isResubmit)
                {
                    route.SaveLog(LogTypeEnum.Info, $"Order [{l_ID}] is being RESUBMITTED to ERP by user — SPARS check applies and previous logs are kept.", string.Empty, userNo);
                }

                // The SPARS existence/cancelled/void check runs for BOTH Reprocess and Resubmit — an order
                // that already exists in the ERP with a live status is never posted again. On resubmit the
                // order is still SYNCED at this point, so the status condition is bypassed.
                if (OrderStatus.ToUpper() == "INPROGRESS" || isResubmit)
                {
                    destinationConnector.Url = "Get_OrderInfo";

                    var bodyObject = new
                    {
                        Input = new
                        {
                            CustomerPO = PublicFunctions.ConvertNullAsString(l_Row["OrderNumber"], string.Empty)
                        }
                    };

                    string jsonBody = JsonConvert.SerializeObject(bodyObject);

                    sourceResponse = RestConnector.Execute(destinationConnector, jsonBody).GetAwaiter().GetResult();
                    
                    if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        l_SCSGetOrderInfoModel = JsonConvert.DeserializeObject<SCSGetOrderInfoModel>(sourceResponse.Content);

                        if (l_SCSGetOrderInfoModel.OutPut.Order != null)
                        {
                            string l_SparsStatus = PublicFunctions.ConvertNullAsString(l_SCSGetOrderInfoModel.OutPut.Order.Header.Status, string.Empty).ToUpper();

                            // A cancelled/void order in SPARS is not a valid creation — do not mark it
                            // SYNCED; fall through and re-place it. Any other status = already created.
                            if (l_SparsStatus != "CANCELLED" &&  l_SparsStatus != "CANCEL" && l_SparsStatus != "VOID" && l_SparsStatus != "VOIDED")
                            {
                                Orders l_OrdersStaus = new Orders();
                                l_OrdersStaus.UseConnection(sourceConnector.ConnectionString);
                                string l_CustomerPO = PublicFunctions.ConvertNullAsString(l_Row["OrderNumber"], string.Empty);

                                bool success = l_OrdersStaus.UpdateStatusAndExternalID(l_ID, l_CustomerPO, Convert.ToString(l_SCSGetOrderInfoModel.OutPut.Order.Header.OrderNo), "SYNCED");

                                if (success)
                                {
                                    route.SaveLog(LogTypeEnum.Info, $"Order [{l_ID}] marked as SYNCED with SO# [{l_SCSGetOrderInfoModel.OutPut.Order.Header.OrderNo}]", "", userNo);
                                }
                                else
                                {
                                    route.SaveLog(LogTypeEnum.Warning, $"Order [{l_ID}] update failed (SO# = {l_SCSGetOrderInfoModel.OutPut.Order.Header.OrderNo})", "", userNo);
                                }

                                return $"Order already exists in ERP with SO# {l_SCSGetOrderInfoModel.OutPut.Order.Header.OrderNo} (status: {l_SparsStatus}) — not posted again.";
                            }

                            route.SaveLog(LogTypeEnum.Info, $"Order [{l_ID}] exists in SPARS but is [{l_SparsStatus}] — re-placing the order.", "", userNo);
                        }
                    }
                }

                DBConnector DBconnection = new DBConnector(sourceConnector.ConnectionString);
                string l_command = string.Empty;

                route.SaveLog(LogTypeEnum.Debug, $"Update order status In processing start for order [{l_ID}].", string.Empty, userNo);

                l_command = "EXEC SP_UpdateOrderStatus @p_CustomerID ='" + sourceConnector.CustomerID + "',@p_RouteType = '" + RouteTypesEnum.SCSPlaceOrder + "InProgress',@p_ExternalId = '',@p_OrderId = " + l_ID;

                DBconnection.Execute(l_command);

                route.SaveData("JSON-SNT", 0, jsonTransformation, userNo);

                l_OrderData.UseConnection(sourceConnector.ConnectionString);
                // Resubmit keeps the previous logs (append, don't replace) so the order history shows it was re-posted.
                if (!isResubmit)
                {
                    l_OrderData.DeleteWithType(l_ID, "ERP-SNT");
                }

                l_OrderData.Type = "ERP-SNT";
                l_OrderData.Data = jsonTransformation;
                l_OrderData.CreatedBy = userNo;
                l_OrderData.CreatedDate = DateTime.Now;
                l_OrderData.OrderId = l_ID;
                l_OrderData.OrderNumber = PublicFunctions.ConvertNullAsString(l_Row["OrderNumber"], string.Empty);

                l_OrderData.SaveNew();

                destinationConnector.Url = "Place_Order";

                sourceResponse = RestConnector.Execute(destinationConnector, jsonTransformation).GetAwaiter().GetResult();

                if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    l_SCSPlaceOrderResponse = JsonConvert.DeserializeObject<SCSPlaceOrderResponse>(sourceResponse.Content);
                }

                if (l_SCSPlaceOrderResponse.OutPut.Success == true)
                {
                    DBConnector connection = new DBConnector(sourceConnector.ConnectionString);
                    string command = string.Empty;

                    route.SaveLog(LogTypeEnum.Debug, $"Update order status processing start for order [{l_ID}].", string.Empty, userNo);

                    command = "EXEC SP_UpdateOrderStatus @p_CustomerID ='" + sourceConnector.CustomerID + "',@p_RouteType = '" + RouteTypesEnum.SCSPlaceOrder + "',@p_ExternalId = '" + l_SCSPlaceOrderResponse.OutPut.ObjectID + "',@p_OrderId = " + l_ID;

                    connection.Execute(command);

                    l_OrderData = new OrderData();

                    l_OrderData.UseConnection(sourceConnector.ConnectionString);
                    if (!isResubmit)
                    {
                        l_OrderData.DeleteWithType(l_ID, "ERP-JSON");
                    }

                    l_OrderData.Type = "ERP-JSON";
                    l_OrderData.Data = sourceResponse.Content;
                    l_OrderData.CreatedBy = userNo;
                    l_OrderData.CreatedDate = DateTime.Now;
                    l_OrderData.OrderId = l_ID;
                    l_OrderData.OrderNumber = PublicFunctions.ConvertNullAsString(l_Row["OrderNumber"], string.Empty);

                    l_OrderData.SaveNew();

                    route.SaveLog(LogTypeEnum.Info, $"Update order status processed for order [{l_ID}].", string.Empty, userNo);
                }
                else
                {
                    l_OrderData = new OrderData();
                    DBConnector connection = new DBConnector(sourceConnector.ConnectionString);
                    string command = string.Empty;

                    command = "EXEC SP_UpdateOrderStatus @p_CustomerID ='" + sourceConnector.CustomerID + "',@p_RouteType = '" + RouteTypesEnum.SCSPlaceOrder + "Error',@p_ExternalId = '',@p_OrderId = " + l_ID;

                    connection.Execute(command);

                    string errorContent = sourceResponse.Content ?? string.Empty;
                    string orderNumber = PublicFunctions.ConvertNullAsString(l_Row["OrderNumber"], string.Empty);

                    l_OrderData.UseConnection(sourceConnector.ConnectionString);
                    if (!isResubmit)
                    {
                        l_OrderData.DeleteWithType(l_ID, "ERP-ERROR");
                    }

                    l_OrderData.Type = "ERP-ERROR";
                    l_OrderData.Data = errorContent;
                    l_OrderData.CreatedBy = userNo;
                    l_OrderData.CreatedDate = DateTime.Now;
                    l_OrderData.OrderId = l_ID;
                    l_OrderData.OrderNumber = orderNumber;
                    l_OrderData.SaveNew();

                    // NEW LOGIC: Handle SPARS already exists error
                    if (errorContent.Contains("already exist in SPARS with SO#"))
                    {
                        try
                        {
                            string soNumber = "";
                            int soIndex = errorContent.IndexOf("SO#");
                            if (soIndex > -1)
                            {
                                soNumber = errorContent.Substring(soIndex + 3).Trim().Split('"', '}', ']')[0].Trim();
                            }

                            // CHECK IF THIS SO# ALREADY EXISTS IN ANOTHER ORDER
                            string checkSql = $@"
                                            SELECT COUNT(*) AS Cnt 
                                            FROM Orders 
                                            WHERE ExternalId = '{soNumber.Replace("'", "''")}'";

                            DataTable checkTable = new DataTable();
                            connection.GetData(checkSql, ref checkTable);

                            if (checkTable.Rows.Count > 0 && Convert.ToInt32(checkTable.Rows[0]["Cnt"]) == 0)
                            {
                                Orders l_Orders = new Orders();
                                l_Orders.UseConnection(sourceConnector.ConnectionString);

                                bool success = l_Orders.UpdateStatusAndExternalID(l_ID, orderNumber, soNumber, "SYNCED");

                                if (success)
                                {
                                    route.SaveLog(LogTypeEnum.Info, $"Order [{l_ID}] marked as SYNCED with SO# [{soNumber}]", "", userNo);
                                }
                                else
                                {
                                    route.SaveLog(LogTypeEnum.Warning, $"Order [{l_ID}] update failed (SO# = {soNumber})", "", userNo);
                                }
                            }
                            else
                            {
                                route.SaveLog(LogTypeEnum.Warning, $"Duplicate ExternalId [{soNumber}] found. Order [{l_ID}] skipped update.", "", userNo);
                            }
                        }
                        catch (Exception parseEx)
                        {
                            route.SaveLog(LogTypeEnum.Error, $"Failed to parse/update ExternalId for Order [{l_ID}]", parseEx.Message, userNo);
                        }
                    }
                }

                route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);
                route.SaveLog(LogTypeEnum.Debug, $"SCSPlaceOrder processed for order [{l_ID}].", string.Empty, userNo);

                return string.Empty;
            }
            catch (Exception)
            {
                l_OrderData = new OrderData();
                DBConnector connection = new DBConnector(sourceConnector.ConnectionString);
                string command = string.Empty;

                command = "EXEC SP_UpdateOrderStatus @p_CustomerID ='" + sourceConnector.CustomerID + "',@p_RouteType = '" + RouteTypesEnum.SCSPlaceOrder + "Error',@p_ExternalId = '',@p_OrderId = " + l_ID;

                connection.Execute(command);

                var fallback = new
                {
                    message = "No Response from SPARS, please reprocess the order."
                };

                string messageContent = JsonConvert.SerializeObject(fallback);

                string errorContent = sourceResponse.Content ?? messageContent;
                
                string orderNumber = PublicFunctions.ConvertNullAsString(l_Row["OrderNumber"], string.Empty);

                l_OrderData.UseConnection(sourceConnector.ConnectionString);
                if (!isResubmit)
                {
                    l_OrderData.DeleteWithType(l_ID, "ERP-ERROR");
                }

                l_OrderData.Type = "ERP-ERROR";
                l_OrderData.Data = errorContent;
                l_OrderData.CreatedBy = userNo;
                l_OrderData.CreatedDate = DateTime.Now;
                l_OrderData.OrderId = l_ID;
                l_OrderData.OrderNumber = orderNumber;
                l_OrderData.SaveNew();
            }

            return string.Empty;
        }
    }
}
