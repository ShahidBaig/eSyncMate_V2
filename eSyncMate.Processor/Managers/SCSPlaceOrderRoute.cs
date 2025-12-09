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
        public static void Execute(IConfiguration config, ILogger logger, Routes route)
        {
            int userNo = 1;
            DataTable l_dataTable = new DataTable();

            try
            {
                ConnectorDataModel? l_SourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
                ConnectorDataModel? l_DestinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);

                route.SaveLog(LogTypeEnum.Info, $"Started executing route [{route.Id}]", string.Empty, userNo);

                if (l_SourceConnector == null)
                {
                    logger.LogError("Source Connector is not setup properly");
                    route.SaveLog(LogTypeEnum.Error, "Source Connector is not setup properly", string.Empty, userNo);
                    return;
                }

                if (l_DestinationConnector == null)
                {
                    logger.LogError("Destination Connector is not setup properly");
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


                        ProcessOrder(l_Row, route, l_DestinationConnector, l_SourceConnector, l_TransformationMap, userNo);
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

        public static string ExecuteSingle(IConfiguration config, int orderId, string customerName)
        {
            Routes route = new Routes();

            route.UseConnection(CommonUtils.ConnectionString);

            string routeName = string.Empty;

            if (customerName == "TAR6266P" || customerName == "WAL4001MP")
            {
                routeName = "Create Orders in ERP";
            }
            else if (customerName == "MAC0149M")
            {
                routeName = "Macys Create Orders";
            }
            else if (customerName == "TAR6266PAH")
            {
                routeName = "SEI - Create Orders in ERP";
            }
            else if (customerName == "AMA1005")
            {
                routeName = "Amazon - Create Orders";
            }
            else if (customerName == "LOW2221MP")
            {
                routeName = "Lowes Create Orders";
            }
            else
            {
                return $"No route configured for customer: {customerName}";
            }

            if (!route.GetObject("Name", routeName, "CustomerName", customerName).IsSuccess)
            {
                return "Order processing route is not setup.";
            }

            if (route.Status.ToUpper() == "IN-ACTIVE")
            {
                return "Order processing route is not active.";
            }

            return ExecuteSingle(config, route, orderId);
        }

        private static string ExecuteSingle(IConfiguration config, Routes route, int orderId)
        {
            int userNo = 1;
            DataTable l_dataTable = new DataTable();

            try
            {
                ConnectorDataModel? l_SourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
                ConnectorDataModel? l_DestinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);

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

                //l_SourceConnector.ConnectionString = "Server=110.93.227.0,1433;Database=ESYNCMATE;UID=sa;PWD=eSoft#123456;";
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
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ORDERSTATUS@", "InProgress");
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
                        ProcessOrder(l_Row, route, l_DestinationConnector, l_SourceConnector, l_TransformationMap, userNo);
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

        private static void ProcessOrder(DataRow l_Row, Routes route, ConnectorDataModel destinationConnector, ConnectorDataModel sourceConnector, string transformationMap, int userNo)
        {
            RestResponse sourceResponse = new RestResponse();
            SCSPlaceOrderResponse l_SCSPlaceOrderResponse = new SCSPlaceOrderResponse();

            SCSGetOrderInfoModel l_SCSGetOrderInfoModel = new SCSGetOrderInfoModel();

            l_SCSPlaceOrderResponse = new SCSPlaceOrderResponse();
            OrderData l_OrderData = new OrderData();
            string Body = PublicFunctions.ConvertNullAsString(l_Row["Data"], string.Empty);
            int l_ID = PublicFunctions.ConvertNullAsInteger(l_Row["Id"], 0);

            string jsonTransformation = new JsonTransformer().Transform(transformationMap, Body);
            jsonTransformation = jsonTransformation.Replace("@CUSTOMERID@", destinationConnector.CustomerID);
            string OrderStatus = PublicFunctions.ConvertNullAsString(l_Row["Status"], "");

            try
            {
                if (OrderStatus.ToUpper() == "INPROGRESS")
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

                            return;
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
                l_OrderData.DeleteWithType(l_ID, "ERP-SNT");

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
                    l_OrderData.DeleteWithType(l_ID, "ERP-JSON");

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
                    l_OrderData.DeleteWithType(l_ID, "ERP-ERROR");

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
            }
            catch (Exception)
            {
                l_OrderData = new OrderData();
                DBConnector connection = new DBConnector(sourceConnector.ConnectionString);
                string command = string.Empty;

                command = "EXEC SP_UpdateOrderStatus @p_CustomerID ='" + sourceConnector.CustomerID + "',@p_RouteType = '" + RouteTypesEnum.SCSPlaceOrder + "Error',@p_ExternalId = '',@p_OrderId = " + l_ID;

                connection.Execute(command);

                string errorContent = sourceResponse.Content ?? "No Response from SPARS, please verify this order in SPARS before reprocessing.";
                string orderNumber = PublicFunctions.ConvertNullAsString(l_Row["OrderNumber"], string.Empty);

                l_OrderData.UseConnection(sourceConnector.ConnectionString);
                l_OrderData.DeleteWithType(l_ID, "ERP-ERROR");

                l_OrderData.Type = "ERP-ERROR";
                l_OrderData.Data = errorContent;
                l_OrderData.CreatedBy = userNo;
                l_OrderData.CreatedDate = DateTime.Now;
                l_OrderData.OrderId = l_ID;
                l_OrderData.OrderNumber = orderNumber;
                l_OrderData.SaveNew();
            }

        }
    }
}
