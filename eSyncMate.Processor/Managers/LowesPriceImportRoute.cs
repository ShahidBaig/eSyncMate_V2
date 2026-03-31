using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Connections;
using eSyncMate.Processor.Models;
using Newtonsoft.Json;
using System.Data;
using System.Net.Http.Headers;
using System.Text;
using System.Data.SqlClient;
using static eSyncMate.DB.Declarations;

namespace eSyncMate.Processor.Managers
{
    /// <summary>
    /// Lowes Price Import via Mirakl PRI01 API
    /// Uploads prices as CSV file to /api/offers/pricing/imports
    ///
    /// Flow:
    ///   1. Source SP → Get APPROVED prices from SCS_ProductPrices
    ///   2. Build CSV (offer-sku;price) — ALL prices (APPROVED + SYNCED) to prevent delete & replace issues
    ///   3. POST multipart/form-data to PRI01 → get import_id
    ///   4. Update APPROVED → SYNCED in SCS_ProductPrices
    ///   5. Log import_id for reference
    ///
    /// Re-sync: User updates price + sets Status = APPROVED → next run picks it up
    /// </summary>
    public class LowesPriceImportRoute
    {
        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;
            DataTable l_data = new DataTable();
            SCSInventoryFeed l_SCSInventoryFeed = new SCSInventoryFeed();

            try
            {
                ConnectorDataModel? l_SourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
                ConnectorDataModel? l_DestinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);

                route.SaveLog(LogTypeEnum.Info, $"Started executing Lowes Price Import route [{route.Id}]", string.Empty, userNo);

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

                // ===== PHASE 1: GET DATA FROM SOURCE =====
                if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                {
                    route.SaveLog(LogTypeEnum.Debug, "Source connector processing start...", string.Empty, userNo);

                    DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);

                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@CUSTOMERID@", l_SourceConnector.CustomerID);
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ROUTETYPEID@", Convert.ToString(RouteTypesEnum.LowesPriceImport));
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@USERNO@", Convert.ToString(userNo));

                    if (l_SourceConnector.CommandType == "SP")
                    {
                        connection.GetDataSP(l_SourceConnector.Command, ref l_data);
                    }

                    route.SaveLog(LogTypeEnum.Debug, $"Source connector processing completed. Rows: {l_data.Rows.Count}", string.Empty, userNo);
                }

                if (l_data.Rows.Count == 0)
                {
                    route.SaveLog(LogTypeEnum.Info, "No APPROVED prices found to upload.", string.Empty, userNo);
                    return;
                }

                // ===== PHASE 2: BUILD CSV =====
                route.SaveLog(LogTypeEnum.Debug, $"Building CSV for {l_data.Rows.Count} price records...", string.Empty, userNo);

                StringBuilder csvBuilder = new StringBuilder();
                csvBuilder.AppendLine("offer-sku;price");

                int rowCount = 0;
                List<string> approvedItemIds = new List<string>();

                foreach (DataRow row in l_data.Rows)
                {
                    string offerSku = Convert.ToString(row["ItemId"]) ?? "";
                    string price = Convert.ToString(row["ListPrice"]) ?? "0";
                    string syncStatus = Convert.ToString(row["SyncStatus"]) ?? "";

                    if (!string.IsNullOrEmpty(offerSku))
                    {
                        csvBuilder.AppendLine($"{offerSku};{price}");
                        rowCount++;

                        // Track only APPROVED items for status update after upload
                        if (syncStatus.Equals("APPROVED", StringComparison.OrdinalIgnoreCase))
                        {
                            approvedItemIds.Add(offerSku.Replace("'", "''"));
                        }
                    }
                }

                string csvContent = csvBuilder.ToString();
                route.SaveLog(LogTypeEnum.Debug, $"CSV built with {rowCount} rows.", string.Empty, userNo);
                route.SaveData("CSV-SNT", 0, csvContent.Length > 5000 ? csvContent.Substring(0, 5000) + "... [truncated]" : csvContent, userNo);

                // ===== PHASE 3: CREATE BATCH TRACKING =====
                InventoryBatchWise l_Batch = new InventoryBatchWise();
                l_Batch.BatchID = Guid.NewGuid().ToString();
                l_Batch.Status = "Processing";
                l_Batch.StartDate = DateTime.Now;
                l_Batch.RouteType = "LowesPriceImport";
                l_Batch.CustomerID = l_SourceConnector.CustomerID;
                l_SCSInventoryFeed.UseConnection(l_SourceConnector.ConnectionString);
                l_SCSInventoryFeed.InsertInventoryBatchWise(l_Batch);

                // ===== PHASE 4: UPLOAD CSV VIA PRI01 =====
                route.SaveLog(LogTypeEnum.Debug, "Uploading CSV to Mirakl PRI01 endpoint...", string.Empty, userNo);

                string importId = UploadPriceFile(l_DestinationConnector, csvContent, route, userNo);

                if (string.IsNullOrEmpty(importId))
                {
                    l_Batch.Status = "Error";
                    l_Batch.FinishDate = DateTime.Now;
                    l_SCSInventoryFeed.UpdateInventoryBatchWise(l_Batch);
                    route.SaveLog(LogTypeEnum.Error, "PRI01 upload failed. No import_id returned.", string.Empty, userNo);
                    return;
                }

                route.SaveLog(LogTypeEnum.Info, $"PRI01 upload successful. Import ID: {importId}", string.Empty, userNo);

                // Save import_id to batch for reference
                l_SCSInventoryFeed.InsertInventoryBatchWiseFeedDetail(l_Batch.BatchID, "NEW", importId, l_SourceConnector.CustomerID);

                // ===== PHASE 5: UPDATE STATUS APPROVED → SYNCED (via TVP SP) =====
                if (approvedItemIds.Count > 0)
                {
                    route.SaveLog(LogTypeEnum.Debug, $"Updating {approvedItemIds.Count} APPROVED prices to SYNCED...", string.Empty, userNo);

                    int updatedCount = UpdateSyncStatusViaTVP(l_SourceConnector.ConnectionString, l_SourceConnector.CustomerID, approvedItemIds);
                    route.SaveLog(LogTypeEnum.Info, $"Updated {updatedCount} prices to SYNCED status.", string.Empty, userNo);
                }
                else
                {
                    route.SaveLog(LogTypeEnum.Info, "No APPROVED prices to update — all were already SYNCED.", string.Empty, userNo);
                }

                // ===== PHASE 6: COMPLETE BATCH =====
                l_Batch.Status = "Completed";
                l_Batch.FinishDate = DateTime.Now;
                l_SCSInventoryFeed.UpdateInventoryBatchWise(l_Batch);

                route.SaveLog(LogTypeEnum.Info, $"Completed Lowes Price Import route [{route.Id}]. Import ID: {importId}, Rows: {rowCount}", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error executing Lowes Price Import route [{route.Id}]", ex.ToString(), userNo);

                try
                {
                    // Try to mark batch as Error
                    InventoryBatchWise l_ErrorBatch = new InventoryBatchWise();
                    l_ErrorBatch.Status = "Error";
                    l_ErrorBatch.FinishDate = DateTime.Now;
                    l_SCSInventoryFeed.UpdateInventoryBatchWise(l_ErrorBatch);
                }
                catch { }
            }
            finally
            {
                l_data.Dispose();
            }
        }

        /// <summary>
        /// Uploads CSV price file to Mirakl PRI01 endpoint using HttpClient (multipart/form-data)
        /// Returns import_id on success, empty string on failure
        /// </summary>
        private static string UploadPriceFile(ConnectorDataModel connector, string csvContent, Routes route, int userNo)
        {
            try
            {
                HttpClient client = SharedHttpClientFactory.Lowes;
                string url = connector.BaseUrl.TrimEnd('/') + "/api/offers/pricing/imports";

                using var content = new MultipartFormDataContent();
                var csvBytes = Encoding.UTF8.GetBytes(csvContent);
                var fileContent = new ByteArrayContent(csvBytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
                content.Add(fileContent, "file", "prices.csv");

                // Add auth headers from connector configuration
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = content;

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

                route.SaveLog(LogTypeEnum.Debug, $"Sending PRI01 request to: {url}", string.Empty, userNo);

                var response = client.SendAsync(request).GetAwaiter().GetResult();
                string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                route.SaveData("CSV-RVD", 0, responseBody, userNo);

                if (response.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    var result = JsonConvert.DeserializeObject<LowesPriceImportResponseModel>(responseBody);
                    return result?.ImportId ?? "";
                }
                else
                {
                    route.SaveLog(LogTypeEnum.Error,
                        $"PRI01 failed. HTTP {(int)response.StatusCode} {response.StatusCode}",
                        responseBody, userNo);
                    return "";
                }
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, "PRI01 upload exception", ex.ToString(), userNo);
                return "";
            }
        }
        /// <summary>
        /// Calls Sp_UpdateLowesPriceSyncStatus with Table-Valued Parameter (TVP)
        /// Passes ItemIDs as a DataTable instead of comma-separated string
        /// </summary>
        private static int UpdateSyncStatusViaTVP(string connectionString, string customerID, List<string> itemIds)
        {
            try
            {
                // Build DataTable matching SyncStatusUpdateType (ItemID, CustomerID)
                DataTable tvp = new DataTable();
                tvp.Columns.Add("ItemID", typeof(string));
                tvp.Columns.Add("CustomerID", typeof(string));

                // Bulk load — disable change tracking for speed
                tvp.BeginLoadData();
                foreach (var itemId in itemIds)
                {
                    tvp.Rows.Add(itemId, customerID);
                }
                tvp.EndLoadData();

                using var conn = new SqlConnection(connectionString);
                conn.Open();

                using var cmd = new SqlCommand("Sp_UpdateLowesPriceSyncStatus", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 300; // 5 min timeout for 100K rows

                var tvpParam = cmd.Parameters.AddWithValue("@ItemIDs", tvp);
                tvpParam.SqlDbType = SqlDbType.Structured;
                tvpParam.TypeName = "SyncStatusUpdateType";

                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] UpdateSyncStatusViaTVP failed: {ex.Message}");
                throw;
            }
        }
    }
}
