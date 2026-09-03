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
    /// Lowes Warehouse-wise Inventory Status Route (Mirakl)
    /// - STO02: GET  /api/offers/stock/imports/{import_id}/status
    /// - STO03: GET  /api/offers/stock/imports/{import_id}/error_report
    /// </summary>
    public class LowesWHSWInventoryStatusRoute
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
                ConnectorDataModel? l_SourceConnector = ConnectorDataModel.Deserialize(route.SourceConnectorObject.Data);
                ConnectorDataModel? l_DestinationConnector = ConnectorDataModel.Deserialize(route.DestinationConnectorObject.Data);

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
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ROUTETYPEID@", Convert.ToString(RouteTypesEnum.LowesWHSWInventoryStatus));
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

                    // A status call that never answers would park this Hangfire worker for ever, so the
                    // connector used here is given a bounded timeout instead of RestSharp's default wait.
                    l_DestinationConnector.TimeoutSeconds = CommonUtils.MiraklStatusTimeoutSeconds;

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
                            sourceResponse = RestConnector.ExecuteWithRetry(l_DestinationConnector, string.Empty, CommonUtils.MiraklStatusMaxAttempts,
                                (p_Attempt, p_Wait, p_Response) => route.SaveLog(LogTypeEnum.Warning, $"Lowes STO02 for import_id [{importId}] answered HTTP {(int)p_Response.StatusCode} {p_Response.StatusCode}. Attempt {p_Attempt} of {CommonUtils.MiraklStatusMaxAttempts}, retrying in {p_Wait.TotalSeconds:0} seconds.", p_Response.Content ?? p_Response.ErrorMessage, userNo))
                                .GetAwaiter().GetResult();

                            if (sourceResponse.StatusCode != System.Net.HttpStatusCode.OK)
                            {
                                // A 429 or a 5xx says nothing about the import itself. The batch keeps its
                                // current status, so the next run picks it up again -- that is a warning, not
                                // a failure. Only a definite error (401/403/404) is logged as one.
                                if (CommonUtils.IsTransientResponse(sourceResponse))
                                    route.SaveLog(LogTypeEnum.Warning, $"Lowes STO02 could not be reached for import_id [{importId}] after {CommonUtils.MiraklStatusMaxAttempts} attempts. HTTP {(int)sourceResponse.StatusCode} {sourceResponse.StatusCode}. It will be retried on the next run.", sourceResponse.Content ?? sourceResponse.ErrorMessage, userNo);
                                else
                                    route.SaveLog(LogTypeEnum.Error, $"Lowes STO02 failed for import_id [{importId}]. HTTP {(int)sourceResponse.StatusCode} {sourceResponse.StatusCode}.", sourceResponse.Content ?? sourceResponse.ErrorMessage, userNo);

                                continue;
                            }

                            JObject statusJson = JObject.Parse(sourceResponse.Content);
                            string importStatus = statusJson["status"]?.ToString() ?? "";
                            bool hasErrorReport = statusJson["has_error_report"]?.Value<bool>() ?? false;

                            if (importStatus == "WAITING" || importStatus == "RUNNING" || importStatus == "QUEUED")
                            {
                                route.SaveLog(LogTypeEnum.Info, $"Lowes STO02 import is still in progress for import_id [{importId}] with status [{importStatus}]. HTTP {(int)sourceResponse.StatusCode} {sourceResponse.StatusCode}.", sourceResponse.Content ?? sourceResponse.ErrorMessage, userNo);
                                continue;
                            }

                            //if (importStatus == "FAILED" || hasErrorReport)
                            //{
                            //    feed.LowesUpdateStatusSCSInventoryFeed(customerId, batchId, importId);
                            //}

                            if (hasErrorReport)
                            {
                                Thread.Sleep(TimeSpan.FromSeconds(CommonUtils.MiraklStatusCallDelaySeconds));
                                // STO03: GET /api/offers/stock/imports/{import_id}/error_report
                                url = $"{l_DestinationConnector.BaseUrl}/api/offers/stock/imports/{importId}/error_report";
                                l_DestinationConnector.Url = url;
                                l_DestinationConnector.Method = "GET";
                                sourceResponse = RestConnector.ExecuteWithRetry(l_DestinationConnector, string.Empty, CommonUtils.MiraklStatusMaxAttempts,
                                    (p_Attempt, p_Wait, p_Response) => route.SaveLog(LogTypeEnum.Warning, $"Lowes STO03 for import_id [{importId}] answered HTTP {(int)p_Response.StatusCode} {p_Response.StatusCode}. Attempt {p_Attempt} of {CommonUtils.MiraklStatusMaxAttempts}, retrying in {p_Wait.TotalSeconds:0} seconds.", p_Response.Content ?? p_Response.ErrorMessage, userNo))
                                    .GetAwaiter().GetResult();

                                if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    l_CustomerProductCatalog.UpdateInventoryBatchWiseStatus(batchId, importId, "Error", customerId, sourceResponse.Content ?? "");
                                }
                                else if (CommonUtils.IsTransientResponse(sourceResponse))
                                {
                                    // This import DOES have an error report. Closing the batch on a rate limit
                                    // or a gateway error would throw those item errors away and leave the feed
                                    // looking clean, so the batch is left open and the report is fetched again
                                    // on the next run.
                                    route.SaveLog(LogTypeEnum.Warning, $"Lowes STO03 error report could not be reached for import_id [{importId}] after {CommonUtils.MiraklStatusMaxAttempts} attempts. HTTP {(int)sourceResponse.StatusCode} {sourceResponse.StatusCode}. The batch is left open and will be retried on the next run.", sourceResponse.Content ?? sourceResponse.ErrorMessage, userNo);
                                }
                                else
                                {
                                    route.SaveLog(LogTypeEnum.Error, $"Lowes STO03 failed for import_id [{importId}]. HTTP {(int)sourceResponse.StatusCode} {sourceResponse.StatusCode}.", sourceResponse.Content ?? sourceResponse.ErrorMessage, userNo);
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
                            route.SaveLog(LogTypeEnum.Exception, $"Error processing Lowes import_id [{Convert.ToString(item["FeedDocumentID"])}]", exItem.ToString(), userNo);
                        }

                        // Pause between batches -- this is what keeps the route inside Mirakl's quota.
                        Thread.Sleep(TimeSpan.FromSeconds(CommonUtils.MiraklStatusCallDelaySeconds));
                    }
                }
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
            }
            finally
            {
                l_data.Dispose();
            }
        }
    }
}
