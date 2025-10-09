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
using static eSyncMate.Processor.Models.SCSCancelOrderResponse;

namespace eSyncMate.Processor.Managers
{
    public class SCSCancelOrderRoute
    {
        public static void Execute(IConfiguration config, ILogger logger, Routes route)
        {
            int userNo = 1;
            string destinationData = string.Empty;
            string sourceData = string.Empty;
            string Body = string.Empty;
            int l_ID = 0;
            DataTable l_dataTable = new DataTable();
            RestResponse sourceResponse = new RestResponse();
            SCSPlaceOrderResponse l_SCSPlaceOrderResponse = new SCSPlaceOrderResponse();

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

                if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                {
                    route.SaveLog(LogTypeEnum.Debug, "Source connector processing start...", string.Empty, userNo);

                    if (l_SourceConnector.Parmeters != null)
                    {
                        foreach (Models.Parameter l_Parameter in l_SourceConnector.Parmeters)
                        {
                            l_Parameter.Value = l_Parameter.Value.Replace("@CUSTOMERID@", route.SourcePartyObject.ERPCustomerID);
                        }
                    }

                    DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);

                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@CUSTOMERID@", l_SourceConnector.CustomerID);

                    if (l_SourceConnector.CommandType == "SP")
                    {
                       connection.GetDataSP(l_SourceConnector.Command,ref l_dataTable);
                    }

                    route.SaveLog(LogTypeEnum.Debug, "Source connector processed.", string.Empty, userNo);
                }

                if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString() && l_dataTable.Rows.Count > 0)
                {
                    route.SaveLog(LogTypeEnum.Debug, "Destination connector processing start...", string.Empty, userNo);

                    l_DestinationConnector.Url = l_DestinationConnector.BaseUrl + l_DestinationConnector.Url;

                    foreach (DataRow l_Row in l_dataTable.Rows)
                    {
                        ExecuteSingle(l_Row, route, l_DestinationConnector, l_SourceConnector, userNo);
                    }

                    route.SaveLog(LogTypeEnum.Debug, "Destination connector processed..", string.Empty, userNo);
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

        public static string ExecuteSingle(IConfiguration config, string orderNumber)
        {
            int userNo = 1;
            Routes route = new Routes();

            route.UseConnection(CommonUtils.ConnectionString);

            if (!route.GetObject("Name", "Get Cancel Order").IsSuccess)
            {
                return "Order processing route is not setup.";
            }

            if (route.Status.ToUpper() == "IN-ACTIVE")
            {
                return "Order processing route is not active.";
            }

            ConnectorDataModel? l_SourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
            ConnectorDataModel? l_DestinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);

            l_SourceConnector.ConnectionString = CommonUtils.ConnectionString;

            DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);
            DataTable l_dataTable = new DataTable();    

            connection.GetDataSP($@"SELECT O.ExternalId,O.Id,O.OrderNumber
                                    FROM Orders O WITH (NOLOCK)
                                        INNER JOIN Customers C WITH (NOLOCK) ON  O.CustomerId= C.Id
                                        INNER JOIN (SELECT OrderId, SUM(LineQty - ISNULL(ASNQty,0) - ISNULL(CancelQty,0)) TotalQty FROM OrderDetail GROUP BY OrderId) 
                                            D ON O.Id = D.OrderId
                                    WHERE C.ERPCustomerID = '{l_SourceConnector.CustomerID}' AND O.Status = 'SYNCED' AND D.TotalQty > 0 AND O.OrderNumber = '{orderNumber}'", ref l_dataTable);

            string result = ExecuteSingle(l_dataTable.Rows[0], route, l_DestinationConnector, l_SourceConnector, userNo);

            l_dataTable.Dispose();

            return result;
        }

        private static string ExecuteSingle(DataRow l_Row, Routes route, ConnectorDataModel destinationConnector, ConnectorDataModel sourceConnector, int userNo)
        {
            var data = new
            {
                Input = new
                {
                    OrderNo = l_Row["ExternalId"],
                    CustomerPO = "",
                    ExternalID = ""
                }
            };

            string Body = JsonConvert.SerializeObject(data);
            route.SaveData("JSON-SNT", 0, Body, userNo);

            RestResponse sourceResponse = RestConnector.Execute(destinationConnector, Body).GetAwaiter().GetResult();

            route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);
            route.SaveLog(LogTypeEnum.Debug, $"SCSCancelOrder processed for order [{l_Row["Id"]}].", string.Empty, userNo);

            SCSCancelOrderResponse response = null;

            try
            {
                if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    response = JsonConvert.DeserializeObject<SCSCancelOrderResponse>(sourceResponse.Content);
                }
            }
            catch (Exception ex)
            {
                var logData = new
                {
                    exception = ex.ToString(),
                    data = sourceResponse.Content
                };

                route.SaveLog(LogTypeEnum.Exception, $"SCSCancelOrder exception for order [{l_Row["Id"]}].", JsonConvert.SerializeObject(logData), userNo);
            }

            if (response != null && response.OutPut.Success && response.OutPut.Order.Header.Status.ToUpper() == "CANCELLED")
            {
                DBConnector connection = new DBConnector(sourceConnector.ConnectionString);
                string Command = string.Empty;

                route.SaveLog(LogTypeEnum.Debug, "Update order status processing start.", string.Empty, userNo);

                Command = "EXEC SP_UpdateOrderStatus @p_CustomerID =" + sourceConnector.CustomerID + ", @p_RouteType = " + RouteTypesEnum.SCSCancelOrder + ", @p_ExternalId = " + l_Row["ExternalId"];

                connection.Execute(Command);

                OrderData l_OrderData = new OrderData();

                l_OrderData.UseConnection(sourceConnector.ConnectionString);
                l_OrderData.DeleteWithType(Convert.ToInt32(l_Row["Id"]), "ERPCancelOrder-JSON");

                l_OrderData.Type = "ERPCancelOrder-JSON";
                l_OrderData.Data = sourceResponse.Content;
                l_OrderData.CreatedBy = userNo;
                l_OrderData.CreatedDate = DateTime.Now;
                l_OrderData.OrderId = Convert.ToInt32(l_Row["Id"]);
                l_OrderData.OrderNumber = PublicFunctions.ConvertNullAsString(l_Row["OrderNumber"], string.Empty);

                l_OrderData.SaveNew();

                OrderDetail l_OrderDetail = new OrderDetail();

                l_OrderDetail.UseConnection(sourceConnector.ConnectionString);
                l_OrderDetail.UpdateCancelQty(Convert.ToInt32(l_Row["Id"]), 0, 0);

                route.SaveLog(LogTypeEnum.Debug, "Update order status processed.", string.Empty, userNo);
            }
            else if (response != null && response.OutPut.Success)
            {
                bool isCancelLine = false;
                OrderDetail l_OrderDetail = new OrderDetail();
                SCSGetOrderResponseModel order = GetOrdersData(l_Row["OrderNumber"].ToString());

                l_OrderDetail.UseConnection(sourceConnector.ConnectionString);

                foreach (CancelOrderDetail l_detail in response.OutPut.Order.Detail)
                {
                    if ((l_detail.Status.ToUpper() == "CANCELLED" || l_detail.Remarks.Contains("CANCEL")))
                    {
                        Int32.TryParse(l_detail.APIOrderLineNo, out int lineNo);
                        if(lineNo == 0)
                            lineNo = Convert.ToInt32(order.order_lines[l_detail.Line_No - 1].order_line_number);

                        isCancelLine = true;
                        l_OrderDetail.UpdateCancelQty(Convert.ToInt32(l_Row["Id"]), lineNo, l_detail.OrderQty);
                    }
                }

                if (isCancelLine)
                {
                    OrderData l_OrderData = new OrderData();

                    l_OrderData.UseConnection(sourceConnector.ConnectionString);
                    l_OrderData.DeleteWithType(Convert.ToInt32(l_Row["Id"]), "ERPCancelOrder-JSON");

                    l_OrderData.Type = "ERPCancelOrder-JSON";
                    l_OrderData.Data = sourceResponse.Content;
                    l_OrderData.CreatedBy = userNo;
                    l_OrderData.CreatedDate = DateTime.Now;
                    l_OrderData.OrderId = Convert.ToInt32(l_Row["Id"]);
                    l_OrderData.OrderNumber = PublicFunctions.ConvertNullAsString(l_Row["OrderNumber"], string.Empty);

                    l_OrderData.SaveNew();
                }

                route.SaveLog(LogTypeEnum.Debug, response.OutPut.Message, sourceResponse.Content, userNo);
            }
            else
            {
                route.SaveLog(LogTypeEnum.Debug, response?.OutPut.Message, sourceResponse.Content, userNo);
            }

            return "";
        }

        private static SCSGetOrderResponseModel GetOrdersData(string orderNumber)
        {
            RestClient client;
            RestRequest request;
            RestResponse response;
            RestClientOptions options = new RestClientOptions("https://api.target.com/seller_orders/v1")
            {
                MaxTimeout = -1,
            };

            client = new RestClient(options);

            request = new RestRequest($"sellers/5d949496fcd4b70097dfad5e/orders/{orderNumber}", Method.Get);

            request.AddHeader("x-api-key", "64dd4d52f0e4a4ffa1c25cbdca78d33906cc3af8");
            request.AddHeader("x-seller-token", "b6151b11b08e43ac81706a41a0d4ac00");
            request.AddHeader("x-seller-id", "5d949496fcd4b70097dfad5e");

            response = client.Execute(request);

            return JsonConvert.DeserializeObject<SCSGetOrderResponseModel>(response.Content);
        }
    }
}
