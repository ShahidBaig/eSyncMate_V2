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
using static eSyncMate.Processor.Models.InputCancellationLinesModel;
using System.Collections.Generic;

namespace eSyncMate.Processor.Managers
{
    public class CancellationLinesRoute
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
            InputCancellationLinesModel l_InputCancellationLinesModel = new InputCancellationLinesModel();
            Order_Line_Statuses l_Order_Line_Statuses = new Order_Line_Statuses();
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

                    foreach (DataRow l_Row in l_dataTable.Rows)
                    {
                        l_InputCancellationLinesModel = new InputCancellationLinesModel();

                        l_InputCancellationLinesModel.cancellation_reason = l_Row["Cancellation_Reason"].ToString();

                        if (Convert.ToInt32(l_Row["CancelQty"]) > 0)
                        {
                            l_Order_Line_Statuses = new Order_Line_Statuses();

                            l_Order_Line_Statuses.quantity = Convert.ToInt32(l_Row["CancelQty"]);
                            l_Order_Line_Statuses.status = l_Row["Status"].ToString();

                            l_InputCancellationLinesModel.order_line_statuses.Add(l_Order_Line_Statuses);
                        }
                        
                        if (Convert.ToInt32(l_Row["RemainingQty"]) > 0) 
                        {
                            l_Order_Line_Statuses = new Order_Line_Statuses();

                            l_Order_Line_Statuses.quantity = Convert.ToInt32(l_Row["RemainingQty"]);
                            l_Order_Line_Statuses.status = l_Row["RemainingStatus"].ToString();

                            l_InputCancellationLinesModel.order_line_statuses.Add(l_Order_Line_Statuses);
                        }

                        if (Convert.ToInt32(l_Row["ASNQty"]) > 0)
                        {
                            l_Order_Line_Statuses = new Order_Line_Statuses();

                            l_Order_Line_Statuses.quantity = Convert.ToInt32(l_Row["ASNQty"]);
                            l_Order_Line_Statuses.status = l_Row["ShippedStatus"].ToString();

                            l_InputCancellationLinesModel.order_line_statuses.Add(l_Order_Line_Statuses);
                        }

                        Body = JsonConvert.SerializeObject(l_InputCancellationLinesModel);
                        route.SaveData("JSONCANLN-SNT", 0, Body, userNo);

                        l_DestinationConnector.Url = l_DestinationConnector.BaseUrl + l_Row["OrderNumber"].ToString() + "/order_lines/" + l_Row["LineNo"].ToString();

                        sourceResponse = RestConnector.Execute(l_DestinationConnector, Body).GetAwaiter().GetResult();

                        if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK || sourceResponse.StatusCode == System.Net.HttpStatusCode.Created)
                        {
                            route.SaveData("JSONCANLN-RVD", 0, sourceResponse.Content, userNo);
                            route.SaveLog(LogTypeEnum.Debug, $"SCSCancelOrder processed for order [{l_Row["Id"]}].", string.Empty, userNo);

                            OrderData l_OrderData = new OrderData();

                            l_OrderData.UseConnection(l_SourceConnector.ConnectionString);

                            l_OrderData.Type = "ERPCANLN-JSON";
                            l_OrderData.Data = sourceResponse.Content;
                            l_OrderData.CreatedBy = userNo;
                            l_OrderData.CreatedDate = DateTime.Now;
                            l_OrderData.OrderId = Convert.ToInt32(l_Row["Id"]);
                            l_OrderData.OrderNumber = PublicFunctions.ConvertNullAsString(l_Row["OrderNumber"], string.Empty);

                            l_OrderData.SaveNew();

                            OrderDetail l_OrderDetail = new OrderDetail();

                            l_OrderDetail.UseConnection(l_SourceConnector.ConnectionString);

                            l_OrderDetail.UpdateOrderDetailStatus(Convert.ToInt32(l_Row["Id"]), Convert.ToInt32(l_Row["LineNo"]));
                            route.SaveLog(LogTypeEnum.Debug, "Update order status processed.", string.Empty, userNo);

                        }
                        else
                        {
                            OrderData l_OrderData = new OrderData();

                            l_OrderData.UseConnection(l_SourceConnector.ConnectionString);

                            l_OrderData.Type = "ERPCANLN-ERR";
                            l_OrderData.Data = sourceResponse.Content;
                            l_OrderData.CreatedBy = userNo;
                            l_OrderData.CreatedDate = DateTime.Now;
                            l_OrderData.OrderId = Convert.ToInt32(l_Row["Id"]);
                            l_OrderData.OrderNumber = PublicFunctions.ConvertNullAsString(l_Row["OrderNumber"], string.Empty);

                            l_OrderData.SaveNew();
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
    }
}
