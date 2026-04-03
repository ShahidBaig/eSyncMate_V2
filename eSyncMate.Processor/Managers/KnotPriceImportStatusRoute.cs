using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;
using Newtonsoft.Json;
using System.Data;
using static eSyncMate.DB.Declarations;

namespace eSyncMate.Processor.Managers
{
    /// <summary>
    /// Knott Price Import Status Check via Mirakl PRI02 API
    ///
    /// Flow:
    ///   1. Get pending imports from InventoryBatchWiseFeedDetail WHERE Status = 'NEW' AND CustomerID
    ///   2. For each FeedDocumentID (import_id):
    ///      - GET /api/offers/pricing/imports?import_id={id}
    ///      - Check status: WAITING, RUNNING, COMPLETE, FAILED
    ///      - Update InventoryBatchWiseFeedDetail with status + response data
    ///   3. Log results
    /// </summary>
    public class KnotPriceImportStatusRoute
    {
        private static HttpClient HttpClient => SharedHttpClientFactory.GetOrCreateClient("knot");

        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;

            try
            {
                ConnectorDataModel? l_SourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
                ConnectorDataModel? l_DestinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);

                route.SaveLog(LogTypeEnum.Info, $"Started executing Knot Price Import Status route [{route.Id}]", string.Empty, userNo);

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

                // ===== PHASE 1: GET PENDING IMPORTS FROM SOURCE CONNECTOR =====
                route.SaveLog(LogTypeEnum.Debug, "Source connector processing start...", string.Empty, userNo);

                DBConnector dbConn = new DBConnector(l_SourceConnector.ConnectionString);
                DataTable dtPending = new DataTable();

                l_SourceConnector.Command = l_SourceConnector.Command.Replace("@CUSTOMERID@", l_SourceConnector.CustomerID);
                l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ROUTETYPEID@", Convert.ToString(RouteTypesEnum.KnotPriceImportStatus));
                l_SourceConnector.Command = l_SourceConnector.Command.Replace("@USERNO@", Convert.ToString(userNo));

                if (l_SourceConnector.CommandType == "SP")
                {
                    dbConn.GetDataSP(l_SourceConnector.Command, ref dtPending);
                }

                route.SaveLog(LogTypeEnum.Debug, $"Source connector processing completed. Rows: {dtPending.Rows.Count}", string.Empty, userNo);

                if (dtPending.Rows.Count == 0)
                {
                    route.SaveLog(LogTypeEnum.Info, "No pending price imports to check.", string.Empty, userNo);
                    return;
                }

                route.SaveLog(LogTypeEnum.Info, $"Found {dtPending.Rows.Count} pending import(s) to check.", string.Empty, userNo);

                // ===== PHASE 2: CHECK STATUS FOR EACH IMPORT =====
                int completedCount = 0;
                int failedCount = 0;
                int waitingCount = 0;

                foreach (DataRow row in dtPending.Rows)
                {
                    int recordId = Convert.ToInt32(row["Id"]);
                    string batchId = Convert.ToString(row["BatchID"]) ?? "";
                    string importId = Convert.ToString(row["FeedDocumentID"]) ?? "";
                    string customerID = Convert.ToString(row["CustomerID"]) ?? "";

                    try
                    {
                        route.SaveLog(LogTypeEnum.Debug, $"Checking PRI02 status for import_id: {importId}", string.Empty, userNo);

                        string statusResult = CheckImportStatus(l_DestinationConnector, importId, route, userNo);

                        if (string.IsNullOrEmpty(statusResult))
                        {
                            route.SaveLog(LogTypeEnum.Error, $"PRI02 returned empty for import_id: {importId}", string.Empty, userNo);
                            continue;
                        }

                        var response = JsonConvert.DeserializeObject<KnotPriceImportStatusResponseModel>(statusResult);
                        var importData = response?.Data?.FirstOrDefault();

                        if (importData == null)
                        {
                            route.SaveLog(LogTypeEnum.Error, $"No data returned for import_id: {importId}", string.Empty, userNo);
                            continue;
                        }

                        string importStatus = importData.Status ?? "UNKNOWN";
                        string statusDetail = $"Status: {importStatus}, Success: {importData.LinesInSuccess}, Error: {importData.LinesInError}, Updated: {importData.OffersUpdated}, ErrorOffers: {importData.OffersInError}";

                        route.SaveLog(LogTypeEnum.Info, $"Import {importId}: {statusDetail}", string.Empty, userNo);

                        // ===== PHASE 3: UPDATE DB WITH STATUS =====
                        string newStatus = importStatus;
                        string responseData = statusResult.Length > 4000 ? statusResult.Substring(0, 4000) : statusResult;

                        dbConn.Execute($@"UPDATE InventoryBatchWiseFeedDetail
                            SET Status = '{newStatus}',
                                Data = '{responseData.Replace("'", "''")}'
                            WHERE Id = {recordId}");

                        if (importStatus == "COMPLETE")
                        {
                            completedCount++;
                            dbConn.Execute($@"UPDATE InventoryBatchWise
                                SET Status = 'Completed', FinishDate = GETDATE()
                                WHERE BatchID = '{batchId}' AND Status = 'Processing'");

                            route.SaveLog(LogTypeEnum.Info, $"Import {importId} COMPLETED. Offers updated: {importData.OffersUpdated}", string.Empty, userNo);
                        }
                        else if (importStatus == "FAILED")
                        {
                            failedCount++;
                            dbConn.Execute($@"UPDATE InventoryBatchWise
                                SET Status = 'Error', FinishDate = GETDATE()
                                WHERE BatchID = '{batchId}' AND Status = 'Processing'");

                            route.SaveLog(LogTypeEnum.Error, $"Import {importId} FAILED. Reason: {importData.ReasonStatus}", string.Empty, userNo);
                        }
                        else
                        {
                            waitingCount++;
                            route.SaveLog(LogTypeEnum.Debug, $"Import {importId} still {importStatus}. Will check next run.", string.Empty, userNo);
                        }
                    }
                    catch (Exception ex)
                    {
                        route.SaveLog(LogTypeEnum.Exception, $"Error checking import_id: {importId}", ex.ToString(), userNo);
                    }
                }

                route.SaveLog(LogTypeEnum.Info, $"Completed status check. Completed: {completedCount}, Failed: {failedCount}, Waiting/Running: {waitingCount}", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error executing Knot Price Import Status route [{route.Id}]", ex.ToString(), userNo);
            }
        }

        private static string CheckImportStatus(ConnectorDataModel connector, string importId, Routes route, int userNo)
        {
            try
            {
                string url = connector.BaseUrl.TrimEnd('/') + $"/api/offers/pricing/imports?import_id={importId}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);

                if (connector.Headers != null)
                {
                    foreach (var header in connector.Headers)
                    {
                        if (!string.IsNullOrEmpty(header.Name) && !string.IsNullOrEmpty(header.Value))
                        {
                            request.Headers.TryAddWithoutValidation(header.Name, header.Value);
                        }
                    }
                }

                var response = HttpClient.SendAsync(request).GetAwaiter().GetResult();
                string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                route.SaveData("JSON-RVD", 0, responseBody, userNo);

                if (response.IsSuccessStatusCode)
                {
                    return responseBody;
                }
                else
                {
                    route.SaveLog(LogTypeEnum.Error, $"PRI02 failed. HTTP {(int)response.StatusCode}", responseBody, userNo);
                    return "";
                }
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, "PRI02 request exception", ex.ToString(), userNo);
                return "";
            }
        }
    }
}
