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

namespace eSyncMate.Processor.Managers
{
    public class SCSASNRoute
    {
        public static void Execute(IConfiguration config, Routes route)
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
                    
                    route.SaveLog(LogTypeEnum.Error, "Source Connector is not setup properly", string.Empty, userNo);
                    return;
                }

                if (l_DestinationConnector == null)
                {
                    
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

                    //foreach (DataRow l_Row in l_dataTable.Rows)
                    //{
                    //    ExecuteSingle(l_Row, route, l_DestinationConnector, l_SourceConnector, userNo);
                    //}
                    foreach (DataRow l_Row in l_dataTable.Rows)
                    {
                        try
                        {
                            ExecuteSingle(l_Row, route, l_DestinationConnector, l_SourceConnector, userNo);
                        }
                        catch (Exception ex)
                        {
                            string orderId = l_Row["Id"].ToString();
                            string orderNumber = l_Row["OrderNumber"].ToString();
                            string errorMsg = $"Error processing order [Id: {orderId}, OrderNumber: {orderNumber}]: {ex.Message}";

                            route.SaveLog(LogTypeEnum.Exception, errorMsg, ex.ToString(), userNo);
                            continue; // Skip and continue with the next order
                        }
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

            if (!route.GetObject("Name", "Get ASNs").IsSuccess)
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

            l_dataTable.Columns.Add("Id");
            l_dataTable.Columns.Add("ExternalId");
            l_dataTable.Columns.Add("OrderNumber");

            DataRow l_Row = l_dataTable.NewRow();

            l_Row["Id"] = "4938";
            l_Row["ExternalId"] = "122072329";
            l_Row["OrderNumber"] = "902001788678848-7275896692";

            l_dataTable.Rows.Add(l_Row);

            ExecuteSingle(l_dataTable.Rows[0], route, l_DestinationConnector, l_SourceConnector, userNo);

            l_dataTable.Dispose();

            return string.Empty;
        }

        private static void ExecuteSingle(DataRow l_Row, Routes route, ConnectorDataModel destinationConnector, ConnectorDataModel sourceConnector, int userNo)
        {
            int lineNo = 0;

            var data = new
            {
                Input = new
                {
                    OrderNo = l_Row["ExternalId"].ToString()
                }
            };

            string Body = JsonConvert.SerializeObject(data);
            route.SaveData("JSON-SNT", 0, Body, userNo);

            RestResponse sourceResponse = RestConnector.Execute(destinationConnector, Body).GetAwaiter().GetResult();

            route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);
            route.SaveLog(LogTypeEnum.Debug, $"SCSASN processed for order [{l_Row["Id"]}].", string.Empty, userNo);

            SCSASNResponse response = null;

            try
            {
                if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    response = JsonConvert.DeserializeObject<SCSASNResponse>(sourceResponse.Content);
                }
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"SCSASN exception for order [{l_Row["Id"]}].", ex.ToString(), userNo);
            }

            if (response != null && response.OutPut.Success && response.OutPut.TrackingInfo.Count > 0)
            {
                response.OutPut.TrackingInfo.RemoveAll(t => string.IsNullOrEmpty(t.TrackingNo));

                if (response.OutPut.TrackingInfo.Count > 0)
                {
                    //if (destinationConnector.CustomerID == "WAL4001MP" && string.IsNullOrEmpty(response.OutPut.TrackingInfo[0].APIOrderLineNo))
                    //{
                    //    response.OutPut.TrackingInfo[0].APIOrderLineNo = "1";
                    //}

                    if (!string.IsNullOrEmpty(response.OutPut.TrackingInfo[0].APIOrderLineNo))
                    {
                        Int32.TryParse(response.OutPut.TrackingInfo[0].APIOrderLineNo, out lineNo);
                    }

                    if (lineNo == 0)
                    {
                        SCSGetOrderResponseModel order = GetOrdersData(l_Row["OrderNumber"].ToString());

                        foreach (TrackingInfo line in response.OutPut.TrackingInfo)
                        {
                            line.OrderLineNo = order.order_lines[Convert.ToInt32(line.OrderLineNo) - 1].order_line_number;
                        }
                    }

                    DBConnector connection = new DBConnector(sourceConnector.ConnectionString);
                    string Command = string.Empty;

                    route.SaveLog(LogTypeEnum.Debug, "Update order status processing start.", string.Empty, userNo);

                    Command = "EXEC SP_UpdateOrderStatus @p_CustomerID =" + sourceConnector.CustomerID + ", @p_RouteType = " + RouteTypesEnum.SCSASN + ", @p_ExternalId = " + l_Row["ExternalId"];

                    connection.Execute(Command);

                    OrderData l_OrderData = new OrderData();

                    l_OrderData.UseConnection(sourceConnector.ConnectionString);

                    l_OrderData.Type = "ERPASN-JSON";
                    l_OrderData.Data = JsonConvert.SerializeObject(response);
                    l_OrderData.CreatedBy = userNo;
                    l_OrderData.CreatedDate = DateTime.Now;
                    l_OrderData.OrderId = Convert.ToInt32(l_Row["Id"]);
                    l_OrderData.OrderNumber = PublicFunctions.ConvertNullAsString(l_Row["OrderNumber"], string.Empty);
                    l_OrderData.Status = "ASNGEN";

                    l_OrderData.SaveNew();

                    OrderDetail l_OrderDetail = new OrderDetail();
                    DataTable asnData = new DataTable();

                    l_OrderDetail.UseConnection(sourceConnector.ConnectionString);

                    l_OrderDetail.GetList($"OrderId = {Convert.ToInt32(l_Row["Id"])}", "*", ref asnData);

                    foreach (TrackingInfo line in response.OutPut.TrackingInfo)
                    {
                        if (!string.IsNullOrEmpty(line.TrackingNo) && asnData.Select($"TrackingNo = '{line.TrackingNo}'")?.Length == 0)
                        {
                            if (!string.IsNullOrEmpty(line.APIOrderLineNo))
                            {
                                Int32.TryParse(line.APIOrderLineNo, out lineNo);
                            }

                            l_OrderDetail.UpdateASNInfo(Convert.ToInt32(l_Row["Id"]), lineNo == 0 ? Convert.ToInt32(line.OrderLineNo) : lineNo,
                                Convert.ToInt32(line.ShippedQty), line.TrackingNo, line.ShippingMethod, line.ShippedDate);
                        }
                    }

                    asnData.Dispose();

                    route.SaveLog(LogTypeEnum.Debug, "Update order status processed.", string.Empty, userNo);
                }
            }
            else
            {
                OrderData l_OrderData = new OrderData();

                l_OrderData.UseConnection(sourceConnector.ConnectionString);

                l_OrderData.Type = "ERPASN-ERR";
                l_OrderData.Data = sourceResponse.Content ?? string.Empty; ;
                l_OrderData.CreatedBy = userNo;
                l_OrderData.CreatedDate = DateTime.Now;
                l_OrderData.OrderId = Convert.ToInt32(l_Row["Id"]);
                l_OrderData.OrderNumber = PublicFunctions.ConvertNullAsString(l_Row["OrderNumber"], string.Empty);
                l_OrderData.Status = "";

                l_OrderData.SaveNew();

                route.SaveLog(LogTypeEnum.Debug, response?.OutPut.Message, sourceResponse.Content, userNo);
            }
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
