using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Connections;
using eSyncMate.Processor.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Data;
using static eSyncMate.DB.Declarations;

namespace eSyncMate.Processor.Managers
{
    /// <summary>
    /// Knott Inventory Status Route (Mirakl)
    /// - STO02: GET  /api/offers/stock/imports/{import_id}/status
    /// - STO03: GET  /api/offers/stock/imports/{import_id}/error_report
    /// </summary>
    public class KnotStockImportStatusRoute
    {
        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;
            DataTable l_data = new();
            SCSInventoryFeed feed = new();
            CustomerProductCatalog l_CustomerProductCatalog = new();
            RestResponse sourceResponse = new();

            try
            {
                ConnectorDataModel? l_SourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
                ConnectorDataModel? l_DestinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);

                route.SaveLog(LogTypeEnum.Info, $"Started executing Knot Stock Import Status route [{route.Id}]", string.Empty, userNo);

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
                    DBConnector connection = new(l_SourceConnector.ConnectionString);
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@CUSTOMERID@", l_SourceConnector.CustomerID);
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ROUTETYPEID@", Convert.ToString(RouteTypesEnum.KnotWHSWInventoryStatus));
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@USERNO@", Convert.ToString(userNo));

                    if (l_SourceConnector.CommandType == "SP")
                    {
                        connection.GetDataSP(l_SourceConnector.Command, ref l_data);
                    }
                }

                if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString() && l_data.Rows.Count > 0)
                {
                    feed.UseConnection(l_SourceConnector.ConnectionString);
                    l_CustomerProductCatalog.UseConnection(l_SourceConnector.ConnectionString);

                    foreach (DataRow item in l_data.Rows)
                    {
                        string batchId = Convert.ToString(item["BatchID"]);
                        string importId = Convert.ToString(item["FeedDocumentID"]);
                        string customerId = Convert.ToString(item["CustomerID"]);

                        if (string.IsNullOrWhiteSpace(batchId) || string.IsNullOrWhiteSpace(importId) || string.IsNullOrWhiteSpace(customerId))
                            continue;

                        try
                        {
                            // STO02: GET /api/offers/stock/imports/{import_id}/status
                            string url = $"{l_DestinationConnector.BaseUrl}/api/offers/stock/imports/{importId}/status";
                            l_DestinationConnector.Url = url;
                            l_DestinationConnector.Method = "GET";
                            sourceResponse = RestConnector.Execute(l_DestinationConnector, string.Empty).GetAwaiter().GetResult();

                            if (sourceResponse.StatusCode != System.Net.HttpStatusCode.OK)
                            {
                                route.SaveLog(LogTypeEnum.Error, $"Knot STO02 failed for import_id [{importId}]. HTTP {(int)sourceResponse.StatusCode} {sourceResponse.StatusCode}.", sourceResponse.Content ?? sourceResponse.ErrorMessage, userNo);
                                continue;
                            }

                            JObject statusJson = JObject.Parse(sourceResponse.Content);
                            string importStatus = statusJson["status"]?.ToString() ?? "";
                            bool hasErrorReport = statusJson["has_error_report"]?.Value<bool>() ?? false;

                            if (importStatus == "WAITING" || importStatus == "RUNNING" || importStatus == "QUEUED")
                            {
                                route.SaveLog(LogTypeEnum.Info, $"Knot STO02 import is still in progress for import_id [{importId}] with status [{importStatus}].", sourceResponse.Content ?? sourceResponse.ErrorMessage, userNo);
                                continue;
                            }

                            if (hasErrorReport)
                            {
                                Thread.Sleep(TimeSpan.FromSeconds(30));
                                // STO03: GET /api/offers/stock/imports/{import_id}/error_report
                                url = $"{l_DestinationConnector.BaseUrl}/api/offers/stock/imports/{importId}/error_report";
                                l_DestinationConnector.Url = url;
                                l_DestinationConnector.Method = "GET";
                                sourceResponse = RestConnector.Execute(l_DestinationConnector, string.Empty).GetAwaiter().GetResult();

                                if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    l_CustomerProductCatalog.UpdateInventoryBatchWiseStatus(batchId, importId, "Error", customerId, sourceResponse.Content ?? "");
                                }
                                else
                                {
                                    route.SaveLog(LogTypeEnum.Error, $"Knot STO03 failed for import_id [{importId}]. HTTP {(int)sourceResponse.StatusCode} {sourceResponse.StatusCode}.", sourceResponse.Content ?? sourceResponse.ErrorMessage, userNo);
                                    l_CustomerProductCatalog.UpdateInventoryBatchWiseStatus(batchId, importId, "Completed", customerId, sourceResponse.Content ?? sourceResponse.ErrorMessage ?? "");
                                }
                            }
                            else if (importStatus == "FAILED")
                            {
                                route.SaveLog(LogTypeEnum.Error, $"Import [{importId}] FAILED without error report.", sourceResponse.Content, userNo);
                                l_CustomerProductCatalog.UpdateInventoryBatchWiseStatus(batchId, importId, "Completed", customerId, sourceResponse.Content ?? "");
                            }
                            else
                            {
                                l_CustomerProductCatalog.UpdateInventoryBatchWiseStatus(batchId, importId, "Completed", customerId, sourceResponse.Content ?? "");
                            }
                        }
                        catch (Exception exItem)
                        {
                            route.SaveLog(LogTypeEnum.Exception, $"Error processing Knot import_id [{Convert.ToString(item["FeedDocumentID"])}]", exItem.ToString(), userNo);
                        }

                        Thread.Sleep(TimeSpan.FromSeconds(30));
                    }
                }

                route.SaveLog(LogTypeEnum.Info, $"Completed Knot Stock Import Status route [{route.Id}]", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error executing Knot Stock Import Status route [{route.Id}]", ex.ToString(), userNo);
            }
            finally
            {
                l_data.Dispose();
            }
        }
    }
}
