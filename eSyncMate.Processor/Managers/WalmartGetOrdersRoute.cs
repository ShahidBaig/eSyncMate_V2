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
using Microsoft.AspNetCore.Routing;

namespace eSyncMate.Processor.Managers
{
    public class WalmartGetOrdersRoute
    {
        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;
            DataTable l_data = new DataTable();
            Customers l_Customer = new Customers();
            Orders l_Orders = new Orders();
            DataTable l_OrderData = new DataTable();

            WalmartGetOrderResponseModel OrdersList = new WalmartGetOrderResponseModel();
            RestResponse sourceResponse = new RestResponse();
            DateTime dateFrom10DaysAgo = DateTime.UtcNow.AddDays(-10);
            string formattedDateFrom10DaysAgo = dateFrom10DaysAgo.ToString("yyyy-MM-ddTHH:mm:ssZ");

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

                if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString())
                {
                    l_SourceConnector.Url = l_SourceConnector.BaseUrl + $"orders?status=Created&limit=100&createdStartDate={formattedDateFrom10DaysAgo}";
                    sourceResponse = RestConnector.Execute(l_SourceConnector, string.Empty).GetAwaiter().GetResult();

                    route.SaveData("JSON-SNT", 0, l_SourceConnector.Url, userNo);
                    OrdersList = JsonConvert.DeserializeObject<WalmartGetOrderResponseModel>(sourceResponse.Content);
                    route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);

                    if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                    {
                        l_Orders.UseConnection(l_DestinationConnector.ConnectionString);
                        l_Customer.UseConnection(l_DestinationConnector.ConnectionString);

                        l_Customer.GetObject("ERPCustomerID", l_SourceConnector.CustomerID);

                        foreach (var order in OrdersList.list.elements.order)
                        {
                            l_OrderData = new DataTable();

                            if (!l_Orders.GetViewList($"OrderNumber ='{order.purchaseOrderId}'", string.Empty, ref l_OrderData))
                            {
                                ProcessOrder(order, route, l_Customer, l_SourceConnector, l_DestinationConnector, userNo);
                            }
                        }
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
                l_data.Dispose();
                l_OrderData.Dispose();
            }
        }

        public static string ExecuteSingle(IConfiguration config, int orderId, string customerName, string orderNumber)
        {
            int userNo = 1;
            Routes route = new Routes();
            string routeName = string.Empty;

            route.UseConnection(CommonUtils.ConnectionString);

            if (customerName == "WAL4001MP")
            {
                routeName = "Get Orders from Customer Portal";
            }
            else
            {
                return $"No route configured for customer: {customerName}";
            }

            if (!route.GetObject("Name", routeName, "CustomerName", customerName).IsSuccess)
            {
                return "Order processing route is not setup.";
            }

            //if (!route.GetObject("Name", "Get Orders").IsSuccess)
            //{
            //    return "Order processing route is not setup.";
            //}

            if (route.Status.ToUpper() == "IN-ACTIVE")
            {
                return "Order processing route is not active.";
            }

            WalmartOrder order = new WalmartOrder(); //GetOrdersData(route, orderNumber);

            return ExecuteSingle(config, route, order, userNo, orderId, orderNumber);
        }

        private static string ExecuteSingle(IConfiguration config, Routes route, WalmartOrder order, int userNo, int orderId, string orderNumber)
        {
            try
            {
                Customers customer = new Customers();

                ConnectorDataModel? sourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
                ConnectorDataModel? destinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);
               
                customer.UseConnection(destinationConnector.ConnectionString);
                customer.GetObject("ERPCustomerID", sourceConnector.CustomerID);

                ProcessOrder(order, route, customer, sourceConnector, destinationConnector, userNo, orderId, orderNumber, 1);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
            }

            return string.Empty;
        }

        private static WalmartOrder GetOrdersData(Routes route, string orderNumber)
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

            return JsonConvert.DeserializeObject<WalmartOrder>(response.Content);
        }

        private static void ProcessOrder(WalmartOrder order, Routes route, Customers customer, ConnectorDataModel sourceConnector, ConnectorDataModel destinationConnector, int userNo, int orderId = 0, string orderNumber = "", int singleProcess = 0)
        {
            RestResponse sourceResponse = new RestResponse();
            Addresses addresses = new Addresses();
            Orders l_Orders = new Orders();
            OrderData l_Data = null;
            string jsonString = string.Empty;

            if (singleProcess == 0)
            {
                jsonString = JsonConvert.SerializeObject(order);

                l_Orders.Status = "New";
                l_Orders.CustomerId = customer.Id;
                l_Orders.OrderDate = DateTimeOffset.FromUnixTimeMilliseconds(order.orderDate).DateTime;
                l_Orders.OrderNumber = order.purchaseOrderId;
                l_Orders.ShipToName = order.shippingInfo.postalAddress.name;
                l_Orders.ShipToAddress1 = order.shippingInfo.postalAddress.address1;
                l_Orders.ShipToAddress2 = order.shippingInfo.methodCode;
                l_Orders.ShipToCity = order.shippingInfo.postalAddress.city;
                l_Orders.ShipToState = order.shippingInfo.postalAddress.state;
                l_Orders.ShipToZip = order.shippingInfo.postalAddress.postalCode;
                l_Orders.ShipToCountry = order.shippingInfo.postalAddress.country;
                l_Orders.ShipToPhone = order.shippingInfo.phone;
                l_Orders.IsStoreOrder = false;
                l_Orders.CreatedBy = userNo;
                l_Orders.CreatedDate = DateTime.Now;

                l_Orders.UseConnection(destinationConnector.ConnectionString);

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
                }

                foreach (var orderLine in order.orderLines.orderLine)
                {
                    OrderDetail l_OrderDetail = new OrderDetail();

                    l_OrderDetail.UseConnection(string.Empty, l_Orders.Connection);

                    for (int i = 1; i <= Convert.ToInt32(orderLine.orderLineQuantity.amount); i++)
                    {
                        l_OrderDetail.OrderId = l_Orders.Id;
                        l_OrderDetail.LineNo = Convert.ToInt32(orderLine.lineNumber);
                        l_OrderDetail.LineQty = 1;
                        l_OrderDetail.ItemID = orderLine.item.sku;
                        l_OrderDetail.UnitPrice = Convert.ToDecimal(orderLine.charges.charge[0].chargeAmount.amount);
                        l_OrderDetail.Status = "NEW";
                        l_OrderDetail.CreatedBy = userNo;
                        l_OrderDetail.CreatedDate = DateTime.Now;

                        l_OrderDetail.SaveNew();
                    }
                }
            }

            if (singleProcess == 1)
            {
                l_Orders.UseConnection(destinationConnector.ConnectionString);
                l_Data = new OrderData();
                l_Data.UseConnection(string.Empty, l_Orders.Connection);

                l_Orders.Id = orderId;
                l_Orders.OrderNumber = orderNumber;
                order.purchaseOrderId = orderNumber;
            }
            else
            {
                l_Data = new OrderData();

                l_Data.UseConnection(string.Empty, l_Orders.Connection);
            }

            sourceConnector.Url = sourceConnector.BaseUrl + $"orders/{order.purchaseOrderId}/acknowledge";
            sourceConnector.Method = "POST";

            route.SaveData("JSON-SNT", 0, sourceConnector.Url, userNo);
            l_Data.DeleteWithType(l_Orders.Id, "API-ACK-SNT");

            l_Data.Type = "API-ACK-SNT";
            l_Data.Data = sourceConnector.Url;
            l_Data.CreatedBy = userNo;
            l_Data.CreatedDate = DateTime.Now;
            l_Data.OrderId = l_Orders.Id;
            l_Data.OrderNumber = l_Orders.OrderNumber;

            l_Data.SaveNew();

            sourceResponse = RestConnector.Execute(sourceConnector, "").GetAwaiter().GetResult();

            l_Data = new OrderData();

            if (sourceResponse.StatusCode != System.Net.HttpStatusCode.OK)
            {
                DBConnector connection = new DBConnector(destinationConnector.ConnectionString);
                string Command = string.Empty;

                Command = "EXEC SP_UpdateOrderStatus @p_CustomerID = '" + sourceConnector.CustomerID + "', @p_RouteType = '" + RouteTypesEnum.WalmartGetOrders + "ACKError', @p_OrderId = '" + l_Orders.Id + "'";

                connection.Execute(Command);

                l_Data.UseConnection(string.Empty, l_Orders.Connection);
                l_Data.DeleteWithType(l_Orders.Id, "ACK-ERR");

                l_Data.Type = "ACK-ERR";
                l_Data.Data = sourceResponse?.Content ?? $"{sourceResponse?.StatusDescription} - {sourceResponse?.ErrorMessage} - {sourceResponse?.ErrorException}";
                l_Data.CreatedBy = userNo;
                l_Data.CreatedDate = DateTime.Now;
                l_Data.OrderId = l_Orders.Id;
                l_Data.OrderNumber = l_Orders.OrderNumber;

                l_Data.SaveNew();

            }
            else 
            {
                l_Data.UseConnection(string.Empty, l_Orders.Connection);
                l_Data.DeleteWithType(l_Orders.Id, "API-ACK");

                l_Data.Type = "API-ACK";
                l_Data.Data = sourceResponse.Content;
                l_Data.CreatedBy = userNo;
                l_Data.CreatedDate = DateTime.Now;
                l_Data.OrderId = l_Orders.Id;
                l_Data.OrderNumber = l_Orders.OrderNumber;

                l_Data.SaveNew();

                DBConnector connection = new DBConnector(destinationConnector.ConnectionString);
                string Command = string.Empty;

                Command = "EXEC SP_UpdateOrderStatus @p_CustomerID = '" + sourceConnector.CustomerID + "', @p_RouteType = '" + RouteTypesEnum.WalmartGetOrders + "ACKErrorNew', @p_OrderId = '" + l_Orders.Id + "'";

                connection.Execute(Command);

                route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);
                route.SaveLog(LogTypeEnum.Debug, $"Processed Order [{l_Orders.OrderNumber}]", string.Empty, userNo);

            }
        }
    }
}
