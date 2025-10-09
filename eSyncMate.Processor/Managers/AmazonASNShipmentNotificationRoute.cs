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
using static eSyncMate.Processor.Models.MacysAsnRequestModel;

namespace eSyncMate.Processor.Managers
{
    public class AmazonASNShipmentNotificationRoute
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

                eSyncMate.DB.Entities.Maps map = new eSyncMate.DB.Entities.Maps();
                string l_TransformationMap = string.Empty;

                map.UseConnection(l_SourceConnector.ConnectionString);
                map.GetObject(route.MapId);

                l_TransformationMap = map.Map;

                if (string.IsNullOrEmpty(l_TransformationMap))
                {
                    route.SaveLog(LogTypeEnum.Error, $"Required map for ASN processing is missing.", string.Empty, userNo);
                    return;
                }

                if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                {
                    if (l_SourceConnector.Parmeters != null)
                    {
                        foreach (Models.Parameter l_Parameter in l_SourceConnector.Parmeters)
                        {
                            l_Parameter.Value = l_Parameter.Value.Replace("@CUSTOMERID@", route.SourcePartyObject.ERPCustomerID);
                        }
                    }

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

                        l_dataTable.DefaultView.RowFilter = $"Id = {l_Row["Id"].ToString()}";

                        foreach (DataRowView l_VRow in l_dataTable.DefaultView)
                        {
                            var shipmentData = new
                            {
                                marketplaceId = "ATVPDKIKX0DER", // Replace with your marketplace ID
                                shipmentRequestDetails = new
                                {
                                    packageDetail = new
                                    {
                                        packageId = "PKG123456",
                                        carrierCode = "UPS", // 
                                        trackingNumber = "1Z12345E0205271688",
                                        shipDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                                    }
                                }
                            };

                            trackings += $"{l_VRow["TrackingNo"].ToString()},";
                            l_Row["Trackings"] = trackings;
                            l_Row["Data"] = JsonConvert.SerializeObject(shipmentData);
                        }
                    }

                    foreach (DataRow l_Row in l_Orders.Rows)
                    {
                        Body = PublicFunctions.ConvertNullAsString(l_Row["Data"], string.Empty);
                        l_ID = PublicFunctions.ConvertNullAsInteger(l_Row["Id"], 0);

                        route.SaveData("JSON-SNT", 0, Body, userNo);

                        l_DestinationConnector.Url = l_DestinationConnector.BaseUrl + $"/orders/v0/orders/{l_Row["OrderNumber"]}/shipment";

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
                        route.SaveLog(LogTypeEnum.Debug, $"SCSASN processed for order [{l_Row["Id"]}].", string.Empty, userNo);

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
                            l_OrderData.Data = sourceResponse.Content;
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
    }
}
