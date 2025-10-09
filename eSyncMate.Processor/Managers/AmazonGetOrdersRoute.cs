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
using static eSyncMate.Processor.Models.AmazonGetOrdersResponseModel;

namespace eSyncMate.Processor.Managers
{
    public class AmazonGetOrdersRoute
    {
        public static void Execute(IConfiguration config, ILogger logger, Routes route)
        {
            int userNo = 1;
            DataTable l_data = new DataTable();
            Customers l_Customer = new Customers();
            Orders l_Orders = new Orders();
            DataTable l_OrderData = new DataTable();

            AmazonGetOrdersResponseModel OrdersList = new AmazonGetOrdersResponseModel();
            RestResponse sourceResponse = new RestResponse();

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
                    l_SourceConnector.Url = l_SourceConnector.BaseUrl + "/orders/v0/orders?MarketplaceIds=ATVPDKIKX0DER&CreatedAfter=2025-01-01T12:36:00Z&OrderStatuses=Unshipped";
                    sourceResponse = RestConnector.Execute(l_SourceConnector, string.Empty).GetAwaiter().GetResult();

                    route.SaveData("JSON-SNT", 0, l_SourceConnector.Url, userNo);

                    OrdersList = JsonConvert.DeserializeObject<AmazonGetOrdersResponseModel>(sourceResponse.Content);
                    route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);

                    if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                    {
                        l_Customer.UseConnection(l_DestinationConnector.ConnectionString);
                        l_Customer.GetObject("ERPCustomerID", l_SourceConnector.CustomerID);
                        l_Orders.UseConnection(l_DestinationConnector.ConnectionString);

                        foreach (var order in OrdersList.payload.Orders)
                        {
                            l_OrderData = new DataTable();

                            l_Orders.GetViewList($"OrderNumber = '{order.AmazonOrderId}'", string.Empty, ref l_OrderData);

                            if (l_OrderData.Rows.Count == 0)
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

        public static string ExecuteSingle(IConfiguration config, string orderNumber)
        {
            int userNo = 1;
            Routes route = new Routes();

            route.UseConnection(CommonUtils.ConnectionString);

            if (!route.GetObject("Name", "Get Orders").IsSuccess)
            {
                return "Order processing route is not setup.";
            }

            if (route.Status.ToUpper() == "IN-ACTIVE")
            {
                return "Order processing route is not active.";
            }

            AmazonOrder order = GetOrdersData(route, orderNumber);

            return ExecuteSingle(config, route, order, userNo);
        }

        private static string ExecuteSingle(IConfiguration config, Routes route, AmazonOrder order, int userNo)
        {
            try
            {
                Customers customer = new Customers();

                ConnectorDataModel? sourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
                ConnectorDataModel? destinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);

                customer.UseConnection(destinationConnector.ConnectionString);
                customer.GetObject("ERPCustomerID", sourceConnector.CustomerID);

                ProcessOrder(order, route, customer, sourceConnector, destinationConnector, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
            }

            return string.Empty;
        }

        private static AmazonOrder GetOrdersData(Routes route, string orderNumber)
        {
            RestClient client;
            RestRequest request;
            RestResponse response;
            RestClientOptions options = new RestClientOptions("https://api.target.com/seller_orders/v1")
            {
                MaxTimeout = -1,
            };
            ConnectorDataModel? sourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
            ConnectorDataModel? destinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);

            sourceConnector.Url = sourceConnector.BaseUrl + $"orders/{orderNumber}";
            response = RestConnector.Execute(sourceConnector, string.Empty).GetAwaiter().GetResult();

            return JsonConvert.DeserializeObject<AmazonOrder>(response.Content);
        }

        private static void ProcessOrder(AmazonOrder order, Routes route, Customers customer, ConnectorDataModel sourceConnector, ConnectorDataModel destinationConnector, int userNo)
        {
            RestResponse sourceResponse = new RestResponse();
            AmazonOrderAddressResponseModel addresses = new AmazonOrderAddressResponseModel();
            AmazonOrderDetailResponseModel OrderDetail = new AmazonOrderDetailResponseModel();
            Orders l_Orders = new Orders();
            OrderData l_Data = null;
            string jsonString = string.Empty;
            try
            {

                sourceConnector.Method = "GET";
                sourceConnector.Url = sourceConnector.BaseUrl + $"/orders/v0/orders/{order.AmazonOrderId}/address";

                sourceResponse = RestConnector.Execute(sourceConnector, string.Empty).GetAwaiter().GetResult();
                route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);

                if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    addresses = JsonConvert.DeserializeObject<AmazonOrderAddressResponseModel>(sourceResponse.Content);
                    order.OrderAddress = addresses;
                }
                else 
                { 
                    return;
                }
            }
            catch (Exception)
            {
                throw;
            }


            try
            {
                sourceConnector.Method = "GET";
                sourceConnector.Url = sourceConnector.BaseUrl + $"/orders/v0/orders/{order.AmazonOrderId}/orderItems";

                sourceResponse = RestConnector.Execute(sourceConnector, string.Empty).GetAwaiter().GetResult();
                route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);

                if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    OrderDetail = JsonConvert.DeserializeObject<AmazonOrderDetailResponseModel>(sourceResponse.Content);
                    order.OrderDetail = OrderDetail;
                }
                else
                {
                    return ;
                }
            }
            catch (Exception)
            {

                throw;
            }


            jsonString = JsonConvert.SerializeObject(order);

            l_Orders.Status = "New";
            l_Orders.CustomerId = customer.Id;
            l_Orders.OrderDate = order.PurchaseDate;
            l_Orders.OrderNumber = order.AmazonOrderId;
            //l_Orders.VendorNumber = order.vmm_vendor_id;
            l_Orders.ShipToName = $"{order.OrderAddress.payload.ShippingAddress.Name ?? ""}";
            l_Orders.ShipToAddress1 = "";
            l_Orders.ShipToAddress2 = "";
            l_Orders.ShipToCity = order.OrderAddress.payload.ShippingAddress.City;
            l_Orders.ShipToState = order.OrderAddress.payload.ShippingAddress.StateOrRegion;
            l_Orders.ShipToZip = order.OrderAddress.payload.ShippingAddress.PostalCode;
            l_Orders.ShipToCountry = order.OrderAddress.payload.ShippingAddress.CountryCode;
            //l_Orders.ShipToEmail = order.addresses.shipping_address.email;
            //l_Orders.ShipToPhone = order.addresses.shipping_address.phone_numbers[0].number;
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

            int l_LineNo = 1;

            foreach (var orderLine in OrderDetail.payload.OrderItems)
            {
                OrderDetail l_OrderDetail = new OrderDetail();

                l_OrderDetail.UseConnection(string.Empty, l_Orders.Connection);

                for (int i = 1; i <= orderLine.QuantityOrdered; i++)
                {
                    l_OrderDetail.OrderId = l_Orders.Id;
                    l_OrderDetail.LineNo = l_LineNo;
                    l_OrderDetail.ItemID = orderLine.SellerSKU.Replace($"{orderLine.ASIN}", "").Trim();
                    l_OrderDetail.LineQty = 1;
                    l_OrderDetail.UnitPrice = Math.Round(Convert.ToDecimal(orderLine.ItemPrice.Amount) / Convert.ToDecimal(orderLine.QuantityOrdered),2);
                    l_OrderDetail.order_line_id = orderLine.OrderItemId;
                    l_OrderDetail.Status = "NEW";
                    l_OrderDetail.CreatedBy = userNo;
                    l_OrderDetail.CreatedDate = DateTime.Now;
                    l_LineNo++;

                    l_OrderDetail.SaveNew();
                }
            }

            //sourceConnector.Url = sourceConnector.BaseUrl + "order_statuses/" + order.id;
            //sourceConnector.Method = "PUT";

            //route.SaveData("JSON-SNT", 0, sourceConnector.Url, userNo);

            //var data = new
            //{
            //    status = "ACKNOWLEDGED_BY_SELLER",
            //};

            //var Requestdata = new
            //{
            //    status = "ACKNOWLEDGED_BY_SELLER",
            //    URL = sourceConnector.Url
            //};

            //l_Data = new OrderData();

            //l_Data.UseConnection(string.Empty, l_Orders.Connection);
            //l_Data.DeleteWithType(l_Orders.Id, "API-ACK-SNT");

            //l_Data.Type = "API-ACK-SNT";
            //l_Data.Data = JsonConvert.SerializeObject(Requestdata);
            //l_Data.CreatedBy = userNo;
            //l_Data.CreatedDate = DateTime.Now;
            //l_Data.OrderId = l_Orders.Id;
            //l_Data.OrderNumber = l_Orders.OrderNumber;

            //l_Data.SaveNew();

            //sourceResponse = RestConnector.Execute(sourceConnector, JsonConvert.SerializeObject(data)).GetAwaiter().GetResult();

            //l_Data = new OrderData();

            //if (sourceResponse == null || string.IsNullOrEmpty(sourceResponse.Content))
            //{
            //    DBConnector connection = new DBConnector(destinationConnector.ConnectionString);
            //    string Command = string.Empty;

            //    Command = "EXEC SP_UpdateOrderStatus @p_CustomerID = '" + sourceConnector.CustomerID + "', @p_RouteType = '" + RouteTypesEnum.SCSGetOrders + "ACKError', @p_OrderId = '" + l_Orders.Id + "'";

            //    connection.Execute(Command);

            //    l_Data.UseConnection(string.Empty, l_Orders.Connection);
            //    l_Data.DeleteWithType(l_Orders.Id, "ACK-ERR");

            //    l_Data.Type = "ACK-ERR";
            //    l_Data.Data = sourceResponse?.Content ?? $"{sourceResponse?.StatusDescription} - {sourceResponse?.ErrorMessage} - {sourceResponse?.ErrorException}";
            //    l_Data.CreatedBy = userNo;
            //    l_Data.CreatedDate = DateTime.Now;
            //    l_Data.OrderId = l_Orders.Id;
            //    l_Data.OrderNumber = l_Orders.OrderNumber;

            //    l_Data.SaveNew();
            //}
            //else
            //{
            //    l_Data.UseConnection(string.Empty, l_Orders.Connection);
            //    l_Data.DeleteWithType(l_Orders.Id, "API-ACK");

            //    l_Data.Type = "API-ACK";
            //    l_Data.Data = sourceResponse.Content;
            //    l_Data.CreatedBy = userNo;
            //    l_Data.CreatedDate = DateTime.Now;
            //    l_Data.OrderId = l_Orders.Id;
            //    l_Data.OrderNumber = l_Orders.OrderNumber;

            //    l_Data.SaveNew();
            //}

            route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);
            route.SaveLog(LogTypeEnum.Debug, $"Processed Order [{l_Orders.OrderNumber}]", string.Empty, userNo);
        }
    }
}
