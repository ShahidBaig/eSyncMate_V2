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
using static eSyncMate.Processor.Models.KnotGetOrderResponseModel;

namespace eSyncMate.Processor.Managers
{
    public class KnotGetOrderRoute
    {
        public static void Execute(IConfiguration config, Routes route)
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
            KnotGetOrderResponseModel ordersList = new KnotGetOrderResponseModel();
            RestResponse sourceResponse = new RestResponse();
            DataTable p_SCSInventoryFeedDt = new DataTable();

            try
            {
                string url = $"{sourceConnector.BaseUrl}/api/orders?order_state_codes={statusCode}&max=100";
                sourceConnector.Url = url;

                sourceResponse = RestConnector.Execute(sourceConnector, string.Empty).GetAwaiter().GetResult();

                route.SaveData("JSON-SNT", 0, url, userNo);
                route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);

                ordersList = JsonConvert.DeserializeObject<KnotGetOrderResponseModel>(sourceResponse.Content);

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

                    DBConnector DBConnection = new DBConnector(destinationConnector.ConnectionString);

                    try
                    {
                        DBConnection.GetData($"SELECT * FROM SCSInventoryFeed WHERE CustomerID = '{sourceConnector.CustomerID}' ", ref p_SCSInventoryFeedDt);

                    }
                    catch (Exception)
                    {

                    }

                    foreach (var order in ordersList.orders)
                    {
                        l_OrderData = new DataTable();

                        if (!l_Orders.GetViewList($"OrderNumber ='{order.order_id}'", string.Empty, ref l_OrderData))
                        {
                            ProcessOrder(order, route, l_Customer, sourceConnector, destinationConnector, userNo, p_SCSInventoryFeedDt);
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

        
        private static void ProcessOrder(KnotOrder order, Routes route, Customers customer, ConnectorDataModel sourceConnector, ConnectorDataModel destinationConnector, int userNo, DataTable p_SCSInventoryFeed)
        {
            RestResponse sourceResponse = new RestResponse();
            Orders l_Orders = new Orders();
            OrderData l_Data = null;
            string jsonString = string.Empty;
            string ackBody = string.Empty;
            string ackResponse = string.Empty;
            string orderGetUrl = string.Empty;
            string orderGetResponse = string.Empty;
            l_Orders.UseConnection(destinationConnector.ConnectionString);
            KnotGetOrderResponseModel OrdersList = new KnotGetOrderResponseModel();
            DataTable l_OrderData = new DataTable();

            try
            {
                // ===== STEP 1: SEND ORDER ACCEPT (ACK) — Mirakl requires ACK first =====
                KnotOrderAcceptInputModel l_KnotOrderAcceptInputModel = new KnotOrderAcceptInputModel();

                if (order.order_lines != null)
                {
                    foreach (var orderLine in order.order_lines)
                    {
                        var l_KnotAcceptedOrder_Lines = new KnotOrderAcceptInputModel.KnotAcceptedOrder_Lines
                        {
                            accepted = true,
                            id = orderLine.order_line_id
                        };

                        l_KnotOrderAcceptInputModel.order_lines.Add(l_KnotAcceptedOrder_Lines);
                    }
                }

                ackBody = JsonConvert.SerializeObject(l_KnotOrderAcceptInputModel);

                sourceConnector.Url = sourceConnector.BaseUrl + $"/api/orders/{order.order_id}/accept";
                sourceConnector.Method = "PUT";

                route.SaveData("JSON-SNT", 0, sourceConnector.Url, userNo);

                sourceResponse = RestConnector.Execute(sourceConnector, ackBody).GetAwaiter().GetResult();
                ackResponse = sourceResponse.Content ?? string.Empty;

                if (!sourceResponse.IsSuccessful)
                {
                    route.SaveLog(LogTypeEnum.Error, $"ACK failed for Order [{order.order_id}]. HTTP {(int)sourceResponse.StatusCode}", ackResponse, userNo);
                }

                Thread.Sleep(30000);

                // ===== STEP 2: GET FULL ORDER DATA =====
                orderGetUrl = sourceConnector.BaseUrl + $"/api/orders?order_ids={order.order_id}";
                sourceConnector.Url = orderGetUrl;
                sourceConnector.Method = "GET";

                sourceResponse = RestConnector.Execute(sourceConnector, "").GetAwaiter().GetResult();
                orderGetResponse = sourceResponse.Content;

                route.SaveData("JSON-SNT", 0, orderGetUrl, userNo);

                if (!string.IsNullOrEmpty(orderGetResponse))
                    route.SaveData("JSON-RVD", 0, orderGetResponse, userNo);

                if (!string.IsNullOrEmpty(orderGetResponse))
                {
                    OrdersList = JsonConvert.DeserializeObject<KnotGetOrderResponseModel>(orderGetResponse);

                    if (OrdersList?.orders != null && OrdersList.orders.Count > 0)
                    {
                        order = OrdersList.orders[0];

                        if (order?.customer?.shipping_address == null)
                        {
                            route.SaveLog(LogTypeEnum.Error, $"Order {order.order_id} missing shipping address", orderGetResponse, userNo);
                            return;
                        }

                        // Map offer_sku to ItemID via SCSInventoryFeed
                        if (order.order_lines != null)
                        {
                            foreach (var orderLine in order.order_lines)
                            {
                                var sku = orderLine?.offer_sku?.Trim();

                                DataRow row = p_SCSInventoryFeed.Select($"CustomerItemCode = '{sku}'").FirstOrDefault();

                                if (row != null)
                                {
                                    orderLine.ItemID = !string.IsNullOrWhiteSpace(Convert.ToString(row["ItemID"])) ? Convert.ToString(row["ItemID"]) : (sku ?? string.Empty);
                                }
                                else
                                {
                                    orderLine.ItemID = orderLine.offer_sku;
                                }
                            }
                        }

                        jsonString = JsonConvert.SerializeObject(order);

                        l_Orders.Status = "New";
                        l_Orders.CustomerId = customer.Id;
                        l_Orders.OrderDate = order.created_date;
                        l_Orders.OrderNumber = order.order_id;
                        l_Orders.ShipToName = $"{order.customer?.shipping_address?.firstname ?? ""} {order.customer?.shipping_address?.lastname ?? ""}";
                        l_Orders.ShipToAddress1 = order.customer?.shipping_address?.street_1 ?? "";
                        l_Orders.ShipToAddress2 = order.customer?.shipping_address?.street_2 ?? "";
                        l_Orders.ShipToCompanyName = Convert.ToString(order.customer?.shipping_address?.company) ?? "";
                        l_Orders.ShipToCity = order.customer?.shipping_address?.city ?? "";
                        l_Orders.ShipToState = order.customer?.shipping_address?.state ?? "";
                        l_Orders.ShipToZip = order.customer?.shipping_address?.zip_code ?? "";
                        l_Orders.ShipToCountry = order.customer?.shipping_address?.country ?? "";
                        l_Orders.IsStoreOrder = false;
                        l_Orders.CreatedBy = userNo;
                        l_Orders.CreatedDate = DateTime.Now;
                    }
                    else
                    {
                        route.SaveLog(LogTypeEnum.Error, $"GET returned empty orders list for [{order.order_id}]", orderGetResponse, userNo);
                        return;
                    }
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

                // Duplicate check
                if (l_Orders.GetViewList($"OrderNumber ='{order.order_id}'", string.Empty, ref l_OrderData))
                {
                    route.SaveLog(LogTypeEnum.Error, $"Order {order.order_id} already in eSyncmate", orderGetResponse, userNo);
                    return;
                }

                // ===== STEP 3: SAVE ORDER TO DB =====
                if (!l_Orders.SaveNew().IsSuccess)
                {
                    route.SaveLog(LogTypeEnum.Error, $"Failed to save Order [{order.order_id}]", string.Empty, userNo);
                    return;
                }

                // ===== STEP 4: SAVE ALL OrderData WITH CORRECT OrderId =====

                // 4a. JSON-SNT (Order GET request URL)
                l_Data = new OrderData();
                l_Data.UseConnection(string.Empty, l_Orders.Connection);
                l_Data.DeleteWithType(l_Orders.Id, "JSON-SNT");
                l_Data.Type = "JSON-SNT";
                l_Data.Data = orderGetUrl;
                l_Data.CreatedBy = userNo;
                l_Data.CreatedDate = DateTime.Now;
                l_Data.OrderId = l_Orders.Id;
                l_Data.OrderNumber = l_Orders.OrderNumber;
                l_Data.SaveNew();

                // 4b. JSON-RVD (Order GET response)
                if (!string.IsNullOrEmpty(orderGetResponse))
                {
                    l_Data = new OrderData();
                    l_Data.UseConnection(string.Empty, l_Orders.Connection);
                    l_Data.DeleteWithType(l_Orders.Id, "JSON-RVD");
                    l_Data.Type = "JSON-RVD";
                    l_Data.Data = orderGetResponse;
                    l_Data.CreatedBy = userNo;
                    l_Data.CreatedDate = DateTime.Now;
                    l_Data.OrderId = l_Orders.Id;
                    l_Data.OrderNumber = l_Orders.OrderNumber;
                    l_Data.SaveNew();
                }

                // 4c. API-ACK-SNT (accept request body)
                l_Data = new OrderData();
                l_Data.UseConnection(string.Empty, l_Orders.Connection);
                l_Data.DeleteWithType(l_Orders.Id, "API-ACK-SNT");
                l_Data.Type = "API-ACK-SNT";
                l_Data.Data = ackBody;
                l_Data.CreatedBy = userNo;
                l_Data.CreatedDate = DateTime.Now;
                l_Data.OrderId = l_Orders.Id;
                l_Data.OrderNumber = l_Orders.OrderNumber;
                l_Data.SaveNew();

                // 4d. API-ACK (accept response)
                if (!string.IsNullOrEmpty(ackResponse))
                {
                    l_Data = new OrderData();
                    l_Data.UseConnection(string.Empty, l_Orders.Connection);
                    l_Data.DeleteWithType(l_Orders.Id, "API-ACK");
                    l_Data.Type = "API-ACK";
                    l_Data.Data = ackResponse;
                    l_Data.CreatedBy = userNo;
                    l_Data.CreatedDate = DateTime.Now;
                    l_Data.OrderId = l_Orders.Id;
                    l_Data.OrderNumber = l_Orders.OrderNumber;
                    l_Data.SaveNew();
                }

                // 4e. API-JSON (parsed order JSON)
                if (!string.IsNullOrEmpty(jsonString))
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

                // ===== STEP 5: SAVE ORDER LINES =====
                if (order.order_lines == null || order.order_lines.Count == 0)
                {
                    route.SaveLog(LogTypeEnum.Error, $"No order lines found for Order [{l_Orders.OrderNumber}]", string.Empty, userNo);
                }

                foreach (var orderLine in order.order_lines ?? new List<KnotGetOrderResponseModel.KnotOrder_Lines>())
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

                route.SaveLog(LogTypeEnum.Debug, $"Processed Order [{l_Orders.OrderNumber}]", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Error, $"Processing Error [{l_Orders.OrderNumber}]", ex.Message, userNo);
            }
            finally
            {
                l_OrderData.Dispose();
            }
        }
    }
}
