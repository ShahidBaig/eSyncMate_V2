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
            DateTime currentDateTime = DateTime.Now;
            DateTime startDateTime = currentDateTime.AddMinutes(-600);
            string formattedStartDate = "2025-11-05T00:00:00Z";
            DataTable p_SCSInventoryFeedDt = new DataTable();
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

                    var allOrders = new List<AmazonOrder>();
                    string nextToken = null;
                    int page = 0;

                    do
                    {
                        page++;

                        if (string.IsNullOrEmpty(nextToken))
                        {
                            // your original first-page query (keep as-is)
                            l_SourceConnector.Url = l_SourceConnector.BaseUrl
                                + $"/orders/v0/orders?MarketplaceIds=ATVPDKIKX0DER&CreatedAfter={formattedStartDate}&OrderStatuses=Unshipped";
                        }
                        else
                        {
                            // IMPORTANT: when paging, only send NextToken
                            l_SourceConnector.Url = l_SourceConnector.BaseUrl
                                + $"/orders/v0/orders?NextToken={Uri.EscapeDataString(nextToken)}";
                        }

                        route.SaveData("JSON-SNT", 0, l_SourceConnector.Url, userNo);

                        sourceResponse = RestConnector.Execute(l_SourceConnector, string.Empty).GetAwaiter().GetResult();
                        route.SaveData("JSON-RVD", 0,sourceResponse.Content, userNo);

                        var pageResult = JsonConvert.DeserializeObject<AmazonGetOrdersResponseModel>(sourceResponse.Content);

                        if (pageResult?.payload?.Orders != null && pageResult.payload.Orders.Any())
                        {
                            allOrders.AddRange(pageResult.payload.Orders);
                        }

                        nextToken = pageResult?.payload?.NextToken;

                    } while (!string.IsNullOrWhiteSpace(nextToken));


                    //l_SourceConnector.Url = l_SourceConnector.BaseUrl + "/orders/v0/orders?MarketplaceIds=ATVPDKIKX0DER&CreatedAfter=2025-01-01T12:36:00Z&OrderStatuses=Unshipped";
                    //sourceResponse = RestConnector.Execute(l_SourceConnector, string.Empty).GetAwaiter().GetResult();

                    //route.SaveData("JSON-SNT", 0, l_SourceConnector.Url, userNo);

                    //OrdersList = JsonConvert.DeserializeObject<AmazonGetOrdersResponseModel>(sourceResponse.Content);
                    //route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);

                    if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                    {
                        l_Customer.UseConnection(l_DestinationConnector.ConnectionString);
                        l_Customer.GetObject("ERPCustomerID", l_SourceConnector.CustomerID);
                        l_Orders.UseConnection(l_DestinationConnector.ConnectionString);

                        DBConnector DBConnection = new DBConnector(l_DestinationConnector.ConnectionString);

                        try
                        {
                            DBConnection.GetData($"SELECT * FROM SCSInventoryFeed WHERE CustomerID = '{l_SourceConnector.CustomerID}' ", ref p_SCSInventoryFeedDt);

                        }
                        catch (Exception)
                        {

                        }
                        

                        foreach (var order in allOrders)
                        {
                            l_OrderData = new DataTable();

                            l_Orders.GetViewList($"OrderNumber = '{order.AmazonOrderId}'", string.Empty, ref l_OrderData);

                            if (l_OrderData.Rows.Count == 0)
                            {
                                ProcessOrder(order, route, l_Customer, l_SourceConnector, l_DestinationConnector, userNo, p_SCSInventoryFeedDt);
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

        //    AmazonOrder order = GetOrdersData(route, orderNumber);

        //    return ExecuteSingle(config, route, order, userNo);
        //}

        //private static string ExecuteSingle(IConfiguration config, Routes route, AmazonOrder order, int userNo)
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

        private static void ProcessOrder(AmazonOrder order, Routes route, Customers customer, ConnectorDataModel sourceConnector, ConnectorDataModel destinationConnector, int userNo, DataTable p_SCSInventoryFeed)
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

            int l_OrderLineNo = 1;
            foreach (var orderLine in OrderDetail.payload.OrderItems)
            {
                orderLine.LineNo = l_OrderLineNo;
                var sku = orderLine?.SellerSKU?.Trim();

                DataRow row = p_SCSInventoryFeed.Select($"CustomerItemCode = '{sku}'").FirstOrDefault();

                if (row != null)
                {
                    orderLine.ItemID = !string.IsNullOrWhiteSpace(Convert.ToString(row["ItemID"])) ? Convert.ToString(row["ItemID"]) : (sku ?? string.Empty);

                }
                else
                {
                    orderLine.ItemID = sku;
                }


                l_OrderLineNo++;
            }

            jsonString = JsonConvert.SerializeObject(order);

            l_Orders.Status = "New";
            l_Orders.CustomerId = customer.Id;
            l_Orders.OrderDate = order.PurchaseDate;
            l_Orders.OrderNumber = order.AmazonOrderId;
            //l_Orders.VendorNumber = order.vmm_vendor_id;
            l_Orders.ShipToName = $"{order.OrderAddress.payload.ShippingAddress.Name ?? ""}";
            l_Orders.ShipToAddress1 = order.OrderAddress.payload.ShippingAddress.AddressLine1 ?? "";
            l_Orders.ShipToAddress2 = order.OrderAddress.payload.ShippingAddress.AddressLine2 ?? "";
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
                    //l_OrderDetail.ItemID = orderLine.SellerSKU.Replace($"{orderLine.ASIN}", "").Trim();
                    l_OrderDetail.ItemID = orderLine.ItemID;
                    //l_OrderDetail.ItemID = orderLine.ASIN;
                    l_OrderDetail.LineQty = 1;
                    l_OrderDetail.UnitPrice = Math.Round(Convert.ToDecimal(orderLine.ItemPrice.Amount) / Convert.ToDecimal(orderLine.QuantityOrdered),2);
                    l_OrderDetail.order_line_id = orderLine.OrderItemId;
                    l_OrderDetail.Status = "NEW";
                    l_OrderDetail.CreatedBy = userNo;
                    l_OrderDetail.CreatedDate = DateTime.Now;
                    

                    l_OrderDetail.SaveNew();
                }

                l_LineNo++;
            }

           

            route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);
            route.SaveLog(LogTypeEnum.Debug, $"Processed Order [{l_Orders.OrderNumber}]", string.Empty, userNo);
        }
    }
}
