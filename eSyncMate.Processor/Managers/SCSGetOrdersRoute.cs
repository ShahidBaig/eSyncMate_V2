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

namespace eSyncMate.Processor.Managers
{
    public class SCSGetOrders
    {
        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;
            DataTable l_data = new DataTable();
            Customers l_Customer = new Customers();
            Orders l_Orders = new Orders();
            DataTable l_OrderData = new DataTable();

            List<SCSGetOrderResponseModel> OrdersList = new List<SCSGetOrderResponseModel>();
            RestResponse sourceResponse = new RestResponse();

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
                    l_SourceConnector.Url = l_SourceConnector.BaseUrl + "orders?order_status=RELEASED_FOR_SHIPMENT&per_page=100";
                    sourceResponse = RestConnector.Execute(l_SourceConnector, string.Empty).GetAwaiter().GetResult();

                    route.SaveData("JSON-SNT", 0, l_SourceConnector.Url, userNo);

                    OrdersList = JsonConvert.DeserializeObject<List<SCSGetOrderResponseModel>>(sourceResponse.Content);
                    route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);

                    if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                    {
                        l_Customer.UseConnection(l_DestinationConnector.ConnectionString);
                        l_Customer.GetObject("ERPCustomerID", l_SourceConnector.CustomerID);
                        l_Orders.UseConnection(l_DestinationConnector.ConnectionString);

                        foreach (var order in OrdersList)
                        {
                            l_OrderData = new DataTable();
                            
                            l_Orders.GetViewList($"OrderNumber = '{order.id}'", string.Empty, ref l_OrderData);
                            
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

        public static string ExecuteSingle(IConfiguration config, int orderId, string customerName, string orderNumber)
        {
            int userNo = 1;
            Routes route = new Routes();

            route.UseConnection(CommonUtils.ConnectionString);

            //if (!route.GetObject("Name", "Get Orders").IsSuccess)
            //{
            //    return "Order processing route is not setup.";
            //}

            string routeName = string.Empty;

            if (customerName == "TAR6266P")
            {
                routeName = "Get Orders from Customer Portal";
            }
            else if (customerName == "TAR6266PAH")
            {
                routeName = "SEI - Get Orders from Customer Portal";
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

            SCSGetOrderResponseModel order = new SCSGetOrderResponseModel(); //GetOrdersData(route, orderNumber);

            return ExecuteSingle(config, route, order, userNo, orderId, orderNumber);
        }

        private static string ExecuteSingle(IConfiguration config, Routes route, SCSGetOrderResponseModel order, int userNo, int orderId, string orderNumber)
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

        private static SCSGetOrderResponseModel GetOrdersData(Routes route, string orderNumber)
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

            return JsonConvert.DeserializeObject<SCSGetOrderResponseModel>(response.Content);
        }

        private static void ProcessOrder(SCSGetOrderResponseModel order, Routes route, Customers customer, ConnectorDataModel sourceConnector, ConnectorDataModel destinationConnector, int userNo, int orderId = 0, string orderNumber = "", int singleProcess = 0)
        {
            RestResponse sourceResponse = new RestResponse();
            Addresses addresses = new Addresses();
            Orders l_Orders = new Orders();
            OrderData l_Data = null;
            string jsonString = string.Empty;

            if (singleProcess == 0)
            {
                sourceConnector.Method = "GET";
                sourceConnector.Url = sourceConnector.BaseUrl + "order_addresses/" + order.id;

                sourceResponse = RestConnector.Execute(sourceConnector, string.Empty).GetAwaiter().GetResult();
                route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);

                addresses = JsonConvert.DeserializeObject<Addresses>(sourceResponse.Content);
                order.addresses = addresses;

                foreach (var orderLine in order.order_lines)
                {
                    if (orderLine.total_item_discount > 0)
                        orderLine.total_item_discount_percentage = Math.Round((orderLine.total_item_discount / (orderLine.unit_price * orderLine.quantity)) * 100, 2);
                }

                jsonString = JsonConvert.SerializeObject(order);

                l_Orders.Status = "New";
                l_Orders.CustomerId = customer.Id;
                l_Orders.OrderDate = order.order_date;
                l_Orders.OrderNumber = order.id;
                l_Orders.VendorNumber = order.vmm_vendor_id;
                l_Orders.ShipToName = $"{order.addresses.shipping_address?.first_name ?? ""} {order.addresses.shipping_address?.last_name ?? ""}";
                l_Orders.ShipToAddress1 = order.addresses.shipping_address.address1;
                l_Orders.ShipToAddress2 = order.addresses.shipping_address.address2;
                l_Orders.ShipToCity = order.addresses.shipping_address.city;
                l_Orders.ShipToState = order.addresses.shipping_address.state;
                l_Orders.ShipToZip = order.addresses.shipping_address.postal_code;
                l_Orders.ShipToCountry = order.addresses.shipping_address.country_code;
                l_Orders.ShipToEmail = order.addresses.shipping_address.email;
                if (order.addresses.shipping_address?.phone_numbers != null && order.addresses.shipping_address.phone_numbers.Any())
                {
                    l_Orders.ShipToPhone = order.addresses.shipping_address.phone_numbers[0].number;
                }
                else
                {
                    l_Orders.ShipToPhone = string.Empty;
                }

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

                foreach (var orderLine in order.order_lines)
                {
                    OrderDetail l_OrderDetail = new OrderDetail();

                    l_OrderDetail.UseConnection(string.Empty, l_Orders.Connection);

                    for (int i = 1; i <= orderLine.quantity; i++)
                    {
                        l_OrderDetail.OrderId = l_Orders.Id;
                        l_OrderDetail.LineNo = Convert.ToInt32(orderLine.order_line_number);
                        l_OrderDetail.ItemID = orderLine.external_id;
                        l_OrderDetail.LineQty = 1;
                        l_OrderDetail.UnitPrice = orderLine.unit_price;
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
                l_Data.UseConnection(destinationConnector.ConnectionString);

                l_Orders.Id = orderId;
                l_Orders.OrderNumber = orderNumber;
            }
            else
            {
                l_Data = new OrderData();

                l_Data.UseConnection(string.Empty, l_Orders.Connection);
            }

            sourceConnector.Url = sourceConnector.BaseUrl + "order_statuses/" + order.id;
            sourceConnector.Method = "PUT";

            route.SaveData("JSON-SNT", 0, sourceConnector.Url, userNo);

            var data = new
            {
                status = "ACKNOWLEDGED_BY_SELLER",
            };

            var Requestdata = new
            {
                status = "ACKNOWLEDGED_BY_SELLER",
                URL = sourceConnector.Url
            };

            
            l_Data.DeleteWithType(l_Orders.Id, "API-ACK-SNT");

            l_Data.Type = "API-ACK-SNT";
            l_Data.Data = JsonConvert.SerializeObject(Requestdata); 
            l_Data.CreatedBy = userNo;
            l_Data.CreatedDate = DateTime.Now;
            l_Data.OrderId = l_Orders.Id;
            l_Data.OrderNumber = l_Orders.OrderNumber;

            l_Data.SaveNew();

            sourceResponse = RestConnector.Execute(sourceConnector, JsonConvert.SerializeObject(data)).GetAwaiter().GetResult();
            
            l_Data = new OrderData();

            if (sourceResponse.StatusCode != System.Net.HttpStatusCode.OK)
            {
                DBConnector connection = new DBConnector(destinationConnector.ConnectionString);
                string Command = string.Empty;

                Command = "EXEC SP_UpdateOrderStatus @p_CustomerID = '" + sourceConnector.CustomerID + "', @p_RouteType = '" + RouteTypesEnum.SCSGetOrders + "ACKError', @p_OrderId = '" + l_Orders.Id + "'";

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

                Command = "EXEC SP_UpdateOrderStatus @p_CustomerID = '" + sourceConnector.CustomerID + "', @p_RouteType = '" + RouteTypesEnum.SCSGetOrders + "ACKErrorNew', @p_OrderId = '" + l_Orders.Id + "'";

                connection.Execute(Command);
            }

            route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);
            route.SaveLog(LogTypeEnum.Debug, $"Processed Order [{l_Orders.OrderNumber}]", string.Empty, userNo);
        }
    }
}
