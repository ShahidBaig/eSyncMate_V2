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
using Microsoft.SqlServer.Server;
using Nancy;
using static eSyncMate.Processor.Models.SCS_ProductTypeAttributeReponseModel;
using Microsoft.AspNetCore.Mvc;
using static eSyncMate.Processor.Models.SCS_VAPProductCatalogModel;
using static eSyncMate.Processor.Models.SCS_ProductCatalogStatusResponseModel;
using System.Net.Http.Json;
using static eSyncMate.Processor.Models.SCSGetOrderResponseModel;
using Namotion.Reflection;
using static eSyncMate.Processor.Models.WalmartGetOrderResponseModel;
using System.Linq;
using static eSyncMate.Processor.Models.MacysGetOrderResponseModel;
using static eSyncMate.Processor.Models.MacysOrderAcceptInputModel;
using static eSyncMate.Processor.Models.LowesGetOrderResponseModel;

namespace eSyncMate.Processor.Managers
{
    public class LowesGetOrderRoute
    {
        public static void Execute(IConfiguration config, ILogger logger, Routes route)
        {
            int userNo = 1;
            DateTime currentDateTime = DateTime.UtcNow;
            DateTime startDateTime = currentDateTime.AddHours(-500);
            string formattedStartDate = startDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");

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

                if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString())
                {
                    var orderStatuses = new[] { "SHIPPING", "WAITING_ACCEPTANCE" };

                    foreach (var status in orderStatuses)
                    {
                        ProcessOrdersByStatus(status, formattedStartDate, route, l_SourceConnector, l_DestinationConnector, userNo);
                    }
                }

                route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
            }
            finally
            {
                //l_data.Dispose();
                //l_OrderData.Dispose();
            }
        }

        private static void ProcessOrdersByStatus(string statusCode, string formattedStartDate, Routes route, ConnectorDataModel sourceConnector, ConnectorDataModel destinationConnector, int userNo)
        {
            Customers l_Customer = new Customers();
            Orders l_Orders = new Orders();
            DataTable l_OrderData = new DataTable();
            LowesGetOrderResponseModel ordersList = new LowesGetOrderResponseModel();
            RestResponse sourceResponse = new RestResponse();

            try
            {
                string url = $"{sourceConnector.BaseUrl}/api/orders?start_date={formattedStartDate}&order_state_codes={statusCode}";
                sourceConnector.Url = url;

                sourceResponse = RestConnector.Execute(sourceConnector, string.Empty).GetAwaiter().GetResult();

                route.SaveData("JSON-SNT", 0, url, userNo);
                route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);

                ordersList = JsonConvert.DeserializeObject<LowesGetOrderResponseModel>(sourceResponse.Content);

                if (ordersList?.orders == null || ordersList.orders.Count == 0)
                {
                    route.SaveLog(LogTypeEnum.Info, $"No orders found for status: {statusCode}", string.Empty, userNo);
                    return;
                }

                if (destinationConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                {
                    l_Orders.UseConnection(destinationConnector.ConnectionString);
                    l_Customer.UseConnection(destinationConnector.ConnectionString);

                    l_Customer.GetObject("ERPCustomerID", sourceConnector.CustomerID);

                    foreach (var order in ordersList.orders)
                    {
                        l_OrderData = new DataTable();

                        if (!l_Orders.GetViewList($"OrderNumber ='{order.order_id}'", string.Empty, ref l_OrderData))
                        {
                            ProcessOrder(order, route, l_Customer, sourceConnector, destinationConnector, userNo);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error processing orders for status [{statusCode}]", ex.ToString(), userNo);
            }
            finally
            {
                l_OrderData.Dispose();
            }
        }


        //public static string ExecuteSingle(IConfiguration config, string orderNumber)
        //{
        //    int userNo = 1;
        //    Routes route = new Routes();

        //    route.UseConnection(CommonUtils.ConnectionString);

        //    if (!route.GetObject("Name", "Get Orders").IsSuccess)
        //    {
        //        return "Order processing route is not setup.";
        //    }

        //    if (route.Status.ToUpper() == "IN-ACTIVE")
        //    {
        //        return "Order processing route is not active.";
        //    }

        //    MacysOrder order = GetOrdersData(route, orderNumber);

        //    return ExecuteSingle(config, route, order, userNo);
        //}

        //private static string ExecuteSingle(IConfiguration config, Routes route, WalmartOrder order, int userNo)
        //{
        //    try
        //    {
        //        Customers customer = new Customers();

        //        ConnectorDataModel? sourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
        //        ConnectorDataModel? destinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);

        //        customer.UseConnection(destinationConnector.ConnectionString);
        //        customer.GetObject("ERPCustomerID", sourceConnector.CustomerID);

        //        ProcessOrder(order, route, customer, sourceConnector, destinationConnector, userNo);
        //    }
        //    catch (Exception ex)
        //    {
        //        route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
        //    }

        //    return string.Empty;
        //}

        private static LowesOrder GetOrdersData(Routes route, string orderNumber)
        {
            RestClient client;
            RestRequest request;
            RestResponse response;
            RestClientOptions options = new RestClientOptions("https://marketplace.walmartapis.com/v3")
            {
                MaxTimeout = -1,
            };
            ConnectorDataModel? sourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
            ConnectorDataModel? destinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);

            sourceConnector.Url = sourceConnector.BaseUrl + $"orders/{orderNumber}";
            response = RestConnector.Execute(sourceConnector, string.Empty).GetAwaiter().GetResult();

            return JsonConvert.DeserializeObject<LowesOrder>(response.Content);
        }

        private static void ProcessOrder(LowesOrder order, Routes route, Customers customer, ConnectorDataModel sourceConnector, ConnectorDataModel destinationConnector, int userNo)
        {
            RestResponse sourceResponse = new RestResponse();
            Addresses addresses = new Addresses();
            Orders l_Orders = new Orders();
            OrderData l_Data = null;
            string jsonString = string.Empty;
            LowesOrderAcceptInputModel l_LowesOrderAcceptInputModel = new LowesOrderAcceptInputModel();
            l_Orders.UseConnection(destinationConnector.ConnectionString);
            //jsonString = JsonConvert.SerializeObject(order);
            LowesGetOrderResponseModel OrdersList = new LowesGetOrderResponseModel();

            try
            {
                sourceConnector.Url = sourceConnector.BaseUrl + $"/api/orders/{order.order_id}/accept";
                sourceConnector.Method = "PUT";

                foreach (var orderLine in order.order_lines)
                {
                    var l_LowesAcceptedOrder_Lines = new LowesOrderAcceptInputModel.LowesAcceptedOrder_Lines
                    {
                        accepted = true,
                        id = orderLine.order_line_id
                    };

                    l_LowesOrderAcceptInputModel.order_lines.Add(l_LowesAcceptedOrder_Lines);
                }

                string Body = JsonConvert.SerializeObject(l_LowesOrderAcceptInputModel);

                route.SaveData("JSON-SNT", 0, sourceConnector.Url, userNo);

                l_Data = new OrderData();

                l_Data.UseConnection(string.Empty, l_Orders.Connection);
                l_Data.DeleteWithType(l_Orders.Id, "API-ACK-SNT");

                l_Data.Type = "API-ACK-SNT";
                l_Data.Data = Body;
                l_Data.CreatedBy = userNo;
                l_Data.CreatedDate = DateTime.Now;
                l_Data.OrderId = l_Orders.Id;
                l_Data.OrderNumber = order.order_id;

                l_Data.SaveNew();

                sourceResponse = RestConnector.Execute(sourceConnector, Body).GetAwaiter().GetResult();

                l_Data = new OrderData();

                l_Data.UseConnection(string.Empty, l_Orders.Connection);
                l_Data.DeleteWithType(l_Orders.Id, "API-ACK");

                l_Data.Type = "API-ACK";
                l_Data.Data = sourceResponse.Content;
                l_Data.CreatedBy = userNo;
                l_Data.CreatedDate = DateTime.Now;
                l_Data.OrderId = l_Orders.Id;
                l_Data.OrderNumber = order.order_id;

                l_Data.SaveNew();

                Thread.Sleep(500);

                sourceConnector.Url = sourceConnector.BaseUrl + $"/api/orders?order_ids={order.order_id}";
                sourceConnector.Method = "GET";

                sourceResponse = RestConnector.Execute(sourceConnector, "").GetAwaiter().GetResult();

                route.SaveData("JSON-SNT", 0, sourceConnector.Url, userNo);

                if (sourceResponse.Content != null)
                {
                    OrdersList = JsonConvert.DeserializeObject<LowesGetOrderResponseModel>(sourceResponse.Content);
                    route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);

                    order = new LowesOrder();
                    order = OrdersList.orders[0];

                    jsonString = JsonConvert.SerializeObject(order);

                    l_Orders.Status = order.customer.shipping_address == null ? "ERROR" : "New";
                    l_Orders.CustomerId = customer.Id;
                    l_Orders.OrderDate = order.created_date;
                    l_Orders.OrderNumber = order.order_id;
                    l_Orders.ShipToName = $"{order.customer.shipping_address?.firstname ?? ""} {order.customer.shipping_address?.lastname ?? ""}";
                    l_Orders.ShipToAddress1 = order.customer.shipping_address?.street_1 ?? "";
                    l_Orders.ShipToAddress2 = order.customer.shipping_address?.street_2 ?? "";
                    l_Orders.ShipToCity = order.customer.shipping_address?.city ?? "";
                    l_Orders.ShipToState = order.customer.shipping_address?.state ?? "";
                    l_Orders.ShipToZip = order.customer.shipping_address?.zip_code ?? "";
                    l_Orders.ShipToCountry = order.customer.shipping_address?.country ?? "";
                    l_Orders.IsStoreOrder = false;
                    l_Orders.CreatedBy = userNo;
                    l_Orders.CreatedDate = DateTime.Now;
                }
                else
                {
                    l_Orders.Status = "ERROR";
                    l_Orders.CustomerId = customer.Id;
                    l_Orders.OrderDate = order.created_date;
                    l_Orders.OrderNumber = order.order_id;
                    l_Orders.ShipToName = "";
                    l_Orders.ShipToAddress1 = "";
                    l_Orders.ShipToAddress2 = "";
                    l_Orders.ShipToCity = "";
                    l_Orders.ShipToState = "";
                    l_Orders.ShipToZip = "";
                    l_Orders.ShipToCountry = "";
                    l_Orders.IsStoreOrder = false;
                    l_Orders.CreatedBy = userNo;
                    l_Orders.CreatedDate = DateTime.Now;
                }

                if (l_Orders.SaveNew().IsSuccess)
                {
                    l_Data = new OrderData();

                    l_Data.UseConnection(string.Empty, l_Orders.Connection);
                    l_Data.DeleteWithType(l_Orders.Id, "API-JSON");

                    l_Data.Type = "API-JSON";
                    l_Data.Data = jsonString;
                    l_Data.CreatedBy = userNo;
                    l_Data.CreatedDate = DateTime.Now;
                    l_Data.OrderId = l_Orders.Id;
                    l_Data.OrderNumber = l_Orders.OrderNumber;

                    l_Data.SaveNew();

                    l_Data.UpdateOrderDataOrderID(l_Data.OrderNumber, l_Data.OrderId);
                }

                foreach (var orderLine in order.order_lines)
                {
                    OrderDetail l_OrderDetail = new OrderDetail();

                    l_OrderDetail.UseConnection(string.Empty, l_Orders.Connection);

                    for (int i = 1; i <= Convert.ToInt32(orderLine.quantity); i++)
                    {
                        l_OrderDetail.OrderId = l_Orders.Id;
                        l_OrderDetail.LineNo = Convert.ToInt32(orderLine.order_line_index);
                        l_OrderDetail.LineQty = 1;
                        l_OrderDetail.ItemID = orderLine.offer_sku;
                        l_OrderDetail.UnitPrice = Convert.ToDecimal(orderLine.price_unit);
                        l_OrderDetail.Status = "NEW";
                        l_OrderDetail.order_line_id = orderLine.order_line_id;
                        l_OrderDetail.CreatedBy = userNo;
                        l_OrderDetail.CreatedDate = DateTime.Now;

                        l_OrderDetail.SaveNew();
                    }
                }

                route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);
                route.SaveLog(LogTypeEnum.Debug, $"Processed Order [{l_Orders.OrderNumber}]", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Error, $"Processing Error [{l_Orders.OrderNumber}]", ex.Message, userNo);
            }
        }
    }
}
