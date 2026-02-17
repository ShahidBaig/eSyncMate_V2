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
using Nancy;
using System;

namespace eSyncMate.Processor.Managers
{
    public class WalmartASNShipmentNotificationRoute
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

                eSyncMate.DB.Entities.Maps map = new eSyncMate.DB.Entities.Maps();
               
                if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                {
                    route.SaveLog(LogTypeEnum.Debug, "Source connector processing start...", string.Empty, userNo);

                    DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);

                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@DATATYPE@", "ERPASN-JSON");
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ORDERSTATUS@", "SYNCED");
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ORDERDATASTATUS@", "ASNGEN");

                    if (l_SourceConnector.CommandType == "SP")
                    {
                        connection.GetDataSP(l_SourceConnector.Command, ref l_dataTable);
                    }

                    route.SaveLog(LogTypeEnum.Debug, "Source connector processed.", string.Empty, userNo);
                }

                if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString() && l_dataTable.Rows.Count > 0)
                {
                    route.SaveLog(LogTypeEnum.Debug, "Destination connector processing start...", string.Empty, userNo);

                    DataTable l_Orders = l_dataTable.DefaultView.ToTable(true, new string[] { "Id", "OrderNumber", "ExternalId" });

                    l_Orders.Columns.Add("Data", typeof(string));
                    l_Orders.Columns.Add("Trackings", typeof(string));

                    foreach (DataRow l_Row in l_Orders.Rows)
                    {
                        string trackings = string.Empty;
                        var asnShipmentNotification = new WalmartAsnShipmentNotificationInputModel();

                        l_dataTable.DefaultView.RowFilter = $"Id = {l_Row["Id"].ToString()}";

                        DataTable l_Order = l_dataTable.DefaultView.ToTable("Orders", true, new string[] { "LineNo", "SellerOrderId" });
                        if (l_Order.Rows.Count > 0)
                        {
                            foreach(DataRow l_Line in l_Order.Rows)
                            {
                                var orderLine = new WalmartAsnShipmentNotificationInputModel.Orderline
                                {
                                    lineNumber = l_Line["LineNo"].ToString(),
                                    intentToCancelOverride = false, // assuming false, change as per your logic
                                    sellerOrderId = l_Line["SellerOrderId"].ToString(),
                                    orderLineStatuses = new WalmartAsnShipmentNotificationInputModel.Orderlinestatuses()
                                };

                                l_dataTable.DefaultView.RowFilter = $"Id = {l_Row["Id"].ToString()} AND LineNo = {orderLine.lineNumber}";
                                foreach (DataRowView l_VRow in l_dataTable.DefaultView)
                                {
                                    var orderLineStatus = new WalmartAsnShipmentNotificationInputModel.Orderlinestatu
                                    {
                                        status = "Shipped",
                                        statusQuantity = new WalmartAsnShipmentNotificationInputModel.Statusquantity
                                        {
                                            unitOfMeasurement = "EACH",
                                            amount = l_VRow["LineQty"].ToString()
                                        },
                                        trackingInfo = new WalmartAsnShipmentNotificationInputModel.Trackinginfo
                                        {
                                            shipDateTime = DateTimeOffset.Parse(l_VRow["ShippedDate"].ToString()).ToUnixTimeMilliseconds(),
                                            carrierName = new WalmartAsnShipmentNotificationInputModel.Carriername
                                            {
                                                carrier = "FedEx"
                                            },
                                            methodCode = l_VRow["ShippingMethod"].ToString(),
                                            trackingNumber = l_VRow["TrackingNo"].ToString(),
                                            trackingURL = ""
                                        }
                                    };

                                    orderLine.orderLineStatuses.orderLineStatus.Add(orderLineStatus);

                                    trackings += $"{l_VRow["TrackingNo"].ToString()},";
                                }

                                asnShipmentNotification.orderShipment.orderLines.orderLine.Add(orderLine);
                            }
                        }

                        l_Row["Trackings"] = trackings;
                        l_Row["Data"] = JsonConvert.SerializeObject(asnShipmentNotification);
                    }

                    foreach (DataRow l_Row in l_Orders.Rows)
                    {
                        Body = PublicFunctions.ConvertNullAsString(l_Row["Data"], string.Empty);
                        l_ID = PublicFunctions.ConvertNullAsInteger(l_Row["Id"], 0);

                        route.SaveData("JSON-SNT", 0, Body, userNo);

                        l_DestinationConnector.Url = l_DestinationConnector.BaseUrl+ "orders/" + l_Row["OrderNumber"] + "/shipping";
                    
                        OrderData l_OrderData = new OrderData();

                        l_OrderData.UseConnection(l_SourceConnector.ConnectionString);

                        l_OrderData.Type = "ASN-SNT";
                        l_OrderData.Data = Body;
                        l_OrderData.CreatedBy = userNo;
                        l_OrderData.CreatedDate = DateTime.Now;
                        l_OrderData.OrderId = Convert.ToInt32(l_Row["Id"]);
                        l_OrderData.OrderNumber = PublicFunctions.ConvertNullAsString(l_Row["OrderNumber"], string.Empty);

                        l_OrderData.SaveNew();

                        sourceResponse = RestConnector.Execute(l_DestinationConnector, Body).GetAwaiter().GetResult();

                        route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);
                        route.SaveLog(LogTypeEnum.Debug, $"WalmartASN processed for order [{l_Row["Id"]}].", string.Empty, userNo);

                        if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK || sourceResponse.StatusCode == System.Net.HttpStatusCode.Created)
                        {
                            DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);
                            string Command = string.Empty;

                            route.SaveLog(LogTypeEnum.Debug, $"Update order status processing start for order [{l_Row["Id"]}].", string.Empty, userNo);

                            OrderDetail l_Detail = new OrderDetail();

                            l_Detail.UseConnection(l_SourceConnector.ConnectionString);
                            foreach (string tracking in l_Row["Trackings"].ToString().Split(','))
                            {
                                if (!string.IsNullOrEmpty(tracking))
                                    l_Detail.UpdateASNSent(Convert.ToInt32(l_Row["Id"]), tracking);
                            }

                            Command = "EXEC SP_UpdateOrderStatus @p_CustomerID = '" + l_SourceConnector.CustomerID + "', @p_RouteType = '" + RouteTypesEnum.SCSASN + "', @p_ExternalId = '" + l_Row["ExternalId"] + "'";

                            connection.Execute(Command);

                            l_OrderData = new OrderData();

                            l_OrderData.UseConnection(l_SourceConnector.ConnectionString);
                            l_OrderData.DeleteWithType(Convert.ToInt32(l_Row["Id"]), "ASN-RES", "Bad Request");

                            l_OrderData.Type = "ASN-RES";
                            l_OrderData.Data = sourceResponse.Content;
                            l_OrderData.CreatedBy = userNo;
                            l_OrderData.CreatedDate = DateTime.Now;
                            l_OrderData.OrderId = Convert.ToInt32(l_Row["Id"]);
                            l_OrderData.OrderNumber = PublicFunctions.ConvertNullAsString(l_Row["OrderNumber"], string.Empty);

                            l_OrderData.SaveNew();

                            route.SaveLog(LogTypeEnum.Debug, $"Update order status processed for order [{l_Row["Id"]}].", string.Empty, userNo);
                        }
                        else
                        {
                            DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);
                            string Command = string.Empty;

                            Command = "EXEC SP_UpdateOrderStatus @p_CustomerID = '" + l_SourceConnector.CustomerID + "', @p_RouteType = '" + RouteTypesEnum.SCSASN + "Error', @p_ExternalId = '" + l_Row["ExternalId"] + "'";

                            connection.Execute(Command);

                            l_OrderData = new OrderData();

                            l_OrderData.UseConnection(l_SourceConnector.ConnectionString);

                            l_OrderData.Type = "ASN-ERR";
                            l_OrderData.Data = sourceResponse.Content ?? string.Empty;
                            l_OrderData.CreatedBy = userNo;
                            l_OrderData.CreatedDate = DateTime.Now;
                            l_OrderData.OrderId = Convert.ToInt32(l_Row["Id"]);
                            l_OrderData.OrderNumber = PublicFunctions.ConvertNullAsString(l_Row["OrderNumber"], string.Empty);

                            l_OrderData.SaveNew();
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

        public static ResponseModel ExecuteShipment(string OrderNumber)
        {
            ResponseModel l_Response = new ResponseModel();
            string Body = "";
            int orderId = 0;

            string sql = $@"SELECT O.Id, O.OrderNumber, O.ExternalId, D.[LineNo], D.TrackingNo, D.ShippedDate + 'T00:00:00.000Z' ShippedDate, SUM(D.LineQty) LineQty,
				                MAX(CASE WHEN D.ShippingMethod = 'FEDEX HOME DELIVERY' THEN 'FH' WHEN D.ShippingMethod = 'FEDEX GROUND' THEN 'G2' ELSE 'FH' END) LevelOfService,
				                MAX(CASE WHEN D.ShippingMethod = 'FEDEX HOME DELIVERY' THEN 'FedExGroundHomeDelivery' WHEN D.ShippingMethod = 'FEDEX GROUND' THEN 'FedExGround' ELSE 'FedExGround' END) ShippingMethod
			                FROM Orders O WITH (NOLOCK)
				                INNER JOIN OrderDetail D WITH (NOLOCK) ON O.Id = D.OrderId
			                WHERE O.OrderNUmber = '{OrderNumber}' AND O.Status IN ('SYNCED', 'SHIPPED') AND ISNULL(D.TrackingNo, '') <> ''
				                AND ISNULL(D.[Status], '') IN ('ASNRVD')
			                GROUP BY O.Id, O.OrderNumber, O.ExternalId, D.[LineNo], D.TrackingNo, D.ShippedDate";

            DBConnector conn = new DBConnector(CommonUtils.ConnectionString);
            DataTable l_Data = new DataTable();

            conn.GetData(sql, ref l_Data);

            DataTable l_Orders = l_Data.DefaultView.ToTable(true, new string[] { "Id", "OrderNumber", "ExternalId" });

            l_Orders.Columns.Add("Data", typeof(string));
            l_Orders.Columns.Add("Trackings", typeof(string));

            foreach (DataRow l_Row in l_Orders.Rows)
            {
                string trackings = string.Empty;
                var l_Body = new
                {
                    items = new List<dynamic>()
                };

                l_Data.DefaultView.RowFilter = $"Id = {l_Row["Id"].ToString()}";
                foreach (DataRowView l_VRow in l_Data.DefaultView)
                {
                    var tracking = new
                    {
                        level_of_service = l_VRow["LevelOfService"].ToString(),
                        order_line_number = l_VRow["LineNo"].ToString(),
                        quantity = l_VRow["LineQty"].ToString(),
                        shipped_date = l_VRow["ShippedDate"].ToString(),
                        shipping_method = l_VRow["ShippingMethod"].ToString(),
                        tracking_number = l_VRow["TrackingNo"].ToString()
                    };

                    trackings += $"{l_VRow["TrackingNo"].ToString()},";
                    l_Body.items.Add(tracking);
                }

                l_Row["Trackings"] = trackings;
                l_Row["Data"] = JsonConvert.SerializeObject(l_Body);
            }

            foreach (DataRow l_Row in l_Orders.Rows)
            {
                Body = l_Row["Data"].ToString();
                orderId = Convert.ToInt32(l_Row["Id"].ToString());

                OrderData l_OrderData = new OrderData();

                l_OrderData.UseConnection(CommonUtils.ConnectionString);

                l_OrderData.Type = "ASN-SNT";
                l_OrderData.Data = Body;
                l_OrderData.CreatedBy = 1;
                l_OrderData.CreatedDate = DateTime.Now;
                l_OrderData.OrderId = orderId;
                l_OrderData.OrderNumber = OrderNumber;

                l_OrderData.SaveNew();

                string asnResponse = CreateShipment(OrderNumber, Body);

                l_OrderData = new OrderData();

                l_OrderData.UseConnection(CommonUtils.ConnectionString);

                l_OrderData.Type = "ASN-RES";
                l_OrderData.Data = asnResponse;
                l_OrderData.CreatedBy = 1;
                l_OrderData.CreatedDate = DateTime.Now;
                l_OrderData.OrderId = orderId;
                l_OrderData.OrderNumber = OrderNumber;

                l_OrderData.SaveNew();

                conn.Execute($"UPDATE Orders SET Status = 'SHIPPED' WHERE ID = {orderId}");

                OrderDetail l_Detail = new OrderDetail();

                l_Detail.UseConnection(CommonUtils.ConnectionString);
                foreach (string tracking in l_Row["Trackings"].ToString().Split(','))
                {
                    if (!string.IsNullOrEmpty(tracking))
                        l_Detail.UpdateASNSent(Convert.ToInt32(l_Row["Id"]), tracking);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Order shipment created successfully!";
            }

            if (l_Orders.Rows.Count == 0)
            {
                l_Response.Code = (int)ResponseCodes.Error;
                l_Response.Message = "Shipment info is not ready!";
            }

            return l_Response;
        }

        private static string CreateShipment(string orderNumber, string body)
        {
            RestClient client;
            RestRequest request;
            RestResponse response;
            RestClientOptions options = new RestClientOptions("https://api.target.com/seller_orders/v1/")
            {
                MaxTimeout = -1,
            };

            client = new RestClient(options);

            request = new RestRequest($"sellers/5d949496fcd4b70097dfad5e/orders/{orderNumber}/bulk_fulfillments_create", Method.Post);

            request.AddHeader("x-api-key", "64dd4d52f0e4a4ffa1c25cbdca78d33906cc3af8");
            request.AddHeader("x-seller-token", "b6151b11b08e43ac81706a41a0d4ac00");
            request.AddHeader("x-seller-id", "5d949496fcd4b70097dfad5e");

            request.AddStringBody(body, RestSharp.DataFormat.Json);

            response = client.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Created || response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return response.Content;
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

        private static List<ASNResponse> GetASNData(string orderNumber)
        {
            RestClient client;
            RestRequest request;
            RestResponse response;
            RestClientOptions options = new RestClientOptions("https://api.target.com/seller_orders/v1")
            {
                MaxTimeout = -1,
            };

            client = new RestClient(options);

            request = new RestRequest($"sellers/5d949496fcd4b70097dfad5e/orders/{orderNumber}/fulfillments", Method.Get);

            request.AddHeader("x-api-key", "64dd4d52f0e4a4ffa1c25cbdca78d33906cc3af8");
            request.AddHeader("x-seller-token", "b6151b11b08e43ac81706a41a0d4ac00");
            request.AddHeader("x-seller-id", "5d949496fcd4b70097dfad5e");

            response = client.Execute(request);

            return JsonConvert.DeserializeObject<List<ASNResponse>>(response.Content);
        }
    }
}
