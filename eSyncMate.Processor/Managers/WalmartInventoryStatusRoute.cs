using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Connections;
using eSyncMate.Processor.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using static eSyncMate.DB.Declarations;

namespace eSyncMate.Processor.Managers
{
    /// <summary>
    /// Walmart Inventory Status Route - Checks feed status and gets item-level results
    /// Similar to AmazonInventoryStatusRoute
    /// </summary>
    public class WalmartInventoryStatusRoute
    {
        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;
            string Body = string.Empty;
            DataTable l_data = new DataTable();
            RestResponse sourceResponse = new RestResponse();
            CustomerProductCatalog l_CustomerProductCatalog = new CustomerProductCatalog();

            try
            {
                ConnectorDataModel? l_SourceConnector = ConnectorDataModel.Deserialize(route.SourceConnectorObject.Data);
                ConnectorDataModel? l_DestinationConnector = ConnectorDataModel.Deserialize(route.DestinationConnectorObject.Data);

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

                // The feed status API is a GET; the destination connector is shared with the upload route (PUT/POST).
                l_DestinationConnector.Method = "GET";

                // Step 1: Get pending feed records from database
                if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                {
                    route.SaveLog(LogTypeEnum.Debug, $"Source connector processing start...", string.Empty, userNo);

                    DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);

                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@CUSTOMERID@", l_SourceConnector.CustomerID);
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ROUTETYPEID@", Convert.ToString(RouteTypesEnum.WalmartBulkInventoryStatus));
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@USERNO@", Convert.ToString(userNo));

                    if (l_SourceConnector.CommandType == "SP")
                    {
                        connection.GetDataSP(l_SourceConnector.Command, ref l_data);
                    }

                    route.SaveLog(LogTypeEnum.Debug, $"Source connector processing completed. Pending feeds: {l_data.Rows.Count}", string.Empty, userNo);
                }

                // Step 2: Process each pending feed
                if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString() && l_data.Rows.Count > 0)
                {
                    route.SaveLog(LogTypeEnum.Debug, $"Destination connector processing start...", string.Empty, userNo);

                    l_CustomerProductCatalog.UseConnection(l_SourceConnector.ConnectionString);

                    foreach (DataRow item in l_data.Rows)
                    {
                        string feedId = Convert.ToString(item["FeedDocumentID"]);
                        string batchId = Convert.ToString(item["BatchID"]);
                        string customerId = l_SourceConnector.CustomerID;

                        try
                        {
                            // Step 3: Call Walmart Feed Status API
                            // GET /v3/feeds/{feedId}?includeDetails=true&offset=&limit= — item details come 50 per page,
                            // and feed IDs contain '@', which Walmart wants escaped (%40).
                            string l_FeedUrl = GetFeedsUrl(l_DestinationConnector.BaseUrl) + Uri.EscapeDataString(feedId);

                            route.SaveLog(LogTypeEnum.Debug, $"Checking feed status for FeedId: {feedId}", string.Empty, userNo);

                            WalmartFeedStatusResponse feedStatusResponse = null;
                            List<WalmartItemIngestionStatus> l_AllItems = new List<WalmartItemIngestionStatus>();
                            List<JToken> l_AllRawItems = new List<JToken>();   // same order as l_AllItems, kept as Walmart sent them
                            int l_Offset = 0;
                            bool l_PageFailed = false;

                            while (true)
                            {
                                l_DestinationConnector.Url = $"{l_FeedUrl}?includeDetails=true&offset={l_Offset}&limit={StatusPageSize}";
                                route.SaveData("JSON-SNT", 0, l_DestinationConnector.Url, userNo);

                                sourceResponse = RestConnector.Execute(l_DestinationConnector, Body).GetAwaiter().GetResult();

                                WalmartFeedStatusResponse l_Page = sourceResponse.StatusCode == System.Net.HttpStatusCode.OK
                                    ? JsonConvert.DeserializeObject<WalmartFeedStatusResponse>(sourceResponse.Content)
                                    : null;

                                if (l_Page == null)
                                {
                                    l_PageFailed = true;
                                    break;
                                }

                                feedStatusResponse ??= l_Page;

                                if (l_Page.feedStatus != "PROCESSED")
                                    break;

                                List<WalmartItemIngestionStatus> l_PageItems = l_Page.itemDetails?.itemIngestionStatus ?? new List<WalmartItemIngestionStatus>();
                                l_AllItems.AddRange(l_PageItems);

                                JArray l_PageRaw = JObject.Parse(sourceResponse.Content)["itemDetails"]?["itemIngestionStatus"] as JArray;
                                l_AllRawItems.AddRange(l_PageItems.Select((_, i) => l_PageRaw != null && i < l_PageRaw.Count ? l_PageRaw[i] : JToken.FromObject(l_PageItems[i])));
                                l_Offset += StatusPageSize;

                                if (l_PageItems.Count == 0 || l_Offset >= l_Page.itemsReceived)
                                    break;

                                // Walmart rejects offset > 10000 (INVALID_REQUEST_PARAM "max offset limit 10000"); the entries
                                // past it cannot be read, so the feed is closed with what was read instead of failing every run.
                                if (l_Offset > MaxStatusOffset)
                                {
                                    route.SaveLog(LogTypeEnum.Error, $"Feed {feedId}: Walmart reports {l_Page.itemsReceived} entries but returns at most {l_AllItems.Count} (offset limit {MaxStatusOffset}); the rest were not checked and stay SYNCED.", string.Empty, userNo);
                                    break;
                                }
                            }

                            // A page that failed half way would leave items unchecked; leave the feed pending for the next run.
                            if (l_PageFailed)
                                feedStatusResponse = null;

                            if (feedStatusResponse != null)
                            {
                                if (feedStatusResponse.itemDetails != null)
                                    feedStatusResponse.itemDetails.itemIngestionStatus = l_AllItems;

                                if (feedStatusResponse != null)
                                {
                                    route.SaveLog(LogTypeEnum.Info,
                                        $"Feed Status: {feedStatusResponse.feedStatus}, " +
                                        $"Received: {feedStatusResponse.itemsReceived}, " +
                                        $"Succeeded: {feedStatusResponse.itemsSucceeded}, " +
                                        $"Failed: {feedStatusResponse.itemsFailed}",
                                        string.Empty, userNo);

                                    // Check if feed is processed
                                    if (feedStatusResponse.feedStatus == "PROCESSED")
                                    {
                                        // Step 4: Process item-level results
                                        if (l_AllItems.Count > 0)
                                        {
                                            // Walmart returns one entry per SKU per ship node; each SKU gets ONE result row
                                            // (FEED-SUCCESS or FEED-ERROR) holding the entries of all its ship nodes.
                                            var l_BySku = l_AllItems
                                                .Select((entry, i) => new { entry, raw = l_AllRawItems[i] })
                                                .GroupBy(x => x.entry.sku ?? string.Empty)
                                                .ToList();

                                            var successSkus   = new List<string>();
                                            var dataErrorSkus = new List<string>();
                                            var retrySkus     = new List<string>();
                                            DataTable l_ResultRows = CreateResultDataTable();
                                            string l_ProcessedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                                            foreach (var sku in l_BySku)
                                            {
                                                var failed = sku.Where(x => x.entry.ingestionStatus != "SUCCESS").ToList();
                                                string status;

                                                if (failed.Count == 0)
                                                {
                                                    status = "SUCCESS";
                                                    successSkus.Add(sku.Key);
                                                }
                                                else if (failed.Any(x => x.entry.ingestionStatus == "DATA_ERROR"))
                                                {
                                                    status = "DATA_ERROR";
                                                    dataErrorSkus.Add(sku.Key);
                                                }
                                                else
                                                {
                                                    status = failed[0].entry.ingestionStatus;
                                                    retrySkus.Add(sku.Key);
                                                }

                                                string errorMessage = string.Join("; ", failed
                                                    .SelectMany(x => x.entry.ingestionErrors?.ingestionError ?? new List<WalmartIngestionError>())
                                                    .Select(e => $"{e.code}: {e.description}")
                                                    .Distinct());

                                                DataRow l_Row = l_ResultRows.NewRow();
                                                l_Row["CustomerId"]  = customerId;
                                                l_Row["ItemId"]      = sku.Key;
                                                l_Row["Type"]        = failed.Count == 0 ? "FEED-SUCCESS" : "FEED-ERROR";
                                                l_Row["Data"]        = JsonConvert.SerializeObject(new
                                                {
                                                    status,
                                                    errorMessage,
                                                    processedAt = l_ProcessedAt,
                                                    shipNodes   = new JArray(sku.Select(x => x.raw))
                                                });
                                                l_Row["CreatedDate"] = DateTime.Now;
                                                l_Row["CreatedBy"]   = userNo;
                                                l_Row["BatchID"]     = batchId;
                                                l_ResultRows.Rows.Add(l_Row);

                                                if (failed.Count > 0)
                                                    route.SaveLog(LogTypeEnum.Debug, $"Item Error - SKU: {sku.Key}, Status: {status}, Error: {errorMessage}", string.Empty, userNo);
                                            }

                                            new SCSInventoryFeed().BulkNewInsertData(l_SourceConnector.ConnectionString, "SCSInventoryFeedData_" + customerId, l_ResultRows);

                                            if (dataErrorSkus.Count > 0 || retrySkus.Count > 0)
                                            {
                                                // The upload marked these SYNCED. A data error is permanent (ERROR); a system or
                                                // timeout error is temporary, so the item goes back to UPDATED and is sent again.
                                                UpdateInventoryFeedStatus(l_SourceConnector.ConnectionString, customerId, dataErrorSkus, "ERROR");
                                                UpdateInventoryFeedStatus(l_SourceConnector.ConnectionString, customerId, retrySkus, "UPDATED");

                                                route.SaveLog(LogTypeEnum.Error, $"Feed {feedId}: {dataErrorSkus.Count} items marked ERROR (data error), {retrySkus.Count} items set back to UPDATED to be sent again.", string.Empty, userNo);
                                            }

                                            if (successSkus.Count > 0)
                                                route.SaveLog(LogTypeEnum.Info, $"Successfully processed {successSkus.Count} items", string.Empty, userNo);
                                        }

                                        // Step 5: Update batch feed detail status to Completed
                                        string summaryData = JsonConvert.SerializeObject(new
                                        {
                                            feedStatus = feedStatusResponse.feedStatus,
                                            itemsReceived = feedStatusResponse.itemsReceived,
                                            itemsSucceeded = feedStatusResponse.itemsSucceeded,
                                            itemsFailed = feedStatusResponse.itemsFailed,
                                            itemsProcessing = feedStatusResponse.itemsProcessing
                                        });

                                        l_CustomerProductCatalog.UpdateInventoryBacthwiseStatus(batchId, feedId, "Completed", customerId, summaryData);

                                        route.SaveLog(LogTypeEnum.Debug, $"Walmart Inventory Status updated for FeedId [{feedId}].", string.Empty, userNo);
                                    }
                                    else if (feedStatusResponse.feedStatus == "ERROR")
                                    {
                                        // Feed-level error: Walmart processed none of it, so every item of this feed is sent again.
                                        l_CustomerProductCatalog.UpdateInventoryBacthwiseStatus(batchId, feedId, "Error", customerId, sourceResponse.Content);
                                        int l_Reset = ResetFeedItems(l_SourceConnector.ConnectionString, customerId, batchId, feedId);
                                        route.SaveLog(LogTypeEnum.Error, $"Feed {feedId} has error status; {l_Reset} items set back to UPDATED to be sent again.", sourceResponse.Content, userNo);
                                    }
                                    else
                                    {
                                        // Still processing (RECEIVED, INPROGRESS)
                                        route.SaveLog(LogTypeEnum.Debug, $"Feed {feedId} is still processing. Status: {feedStatusResponse.feedStatus}", string.Empty, userNo);
                                    }
                                }
                            }
                            else
                            {
                                route.SaveLog(LogTypeEnum.Error, $"Unable to get Walmart Inventory Status for FeedId [{feedId}]. Status: {sourceResponse.StatusCode}", sourceResponse.Content, userNo);
                            }

                            route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);

                            // Small delay between feed status checks
                            Thread.Sleep(TimeSpan.FromSeconds(2));
                        }
                        catch (Exception feedEx)
                        {
                            route.SaveLog(LogTypeEnum.Exception, $"Error processing feed {feedId}", feedEx.ToString(), userNo);
                        }
                    }

                    route.SaveLog(LogTypeEnum.Debug, $"Destination connector processing completed", string.Empty, userNo);
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
            }
        }

        // Walmart returns item details 50 per page when includeDetails=true.
        private const int StatusPageSize = 50;

        // Highest offset Walmart accepts on GET /v3/feeds/{feedId}.
        private const int MaxStatusOffset = 10000;

        // SQL Server allows 1000 rows per VALUES list; stay well under it.
        private const int SqlBatchSize = 500;

        // Walmart connectors keep BaseUrl as ".../v3/"; older ones may stop at the host.
        private static string GetFeedsUrl(string baseUrl)
        {
            string l_Base = (baseUrl ?? string.Empty).TrimEnd('/');

            return l_Base.EndsWith("/v3", StringComparison.OrdinalIgnoreCase) ? l_Base + "/feeds/" : l_Base + "/v3/feeds/";
        }

        // Sets SCSInventoryFeed.Status for the given SKUs of one customer.
        private static void UpdateInventoryFeedStatus(string connectionString, string customerId, List<string> skus, string status)
        {
            if (skus == null || skus.Count == 0) return;

            DBConnector connection = new DBConnector(connectionString);
            string safeCustomerId = customerId?.Replace("'", "''") ?? "";

            foreach (var batch in skus.Chunk(SqlBatchSize))
            {
                string inList = string.Join(", ", batch.Select(sku => $"'{(sku ?? "").Replace("'", "''")}'"));

                connection.Execute($"UPDATE SCSInventoryFeed SET Status = '{status}', ModifiedDate = GETDATE() " +
                                   $"WHERE CustomerID = '{safeCustomerId}' AND ItemID IN ({inList})");
            }
        }

        // A feed Walmart rejected as a whole: every item the upload sent in it goes back to UPDATED.
        // The upload stored Walmart's reply (which holds the feed ID) as a JSON-RVD row per item.
        private static int ResetFeedItems(string connectionString, string customerId, string batchId, string feedId)
        {
            string safeCustomerId = customerId?.Replace("'", "''") ?? "";
            string query = $@"UPDATE f SET f.Status = 'UPDATED', f.ModifiedDate = GETDATE()
                              FROM SCSInventoryFeed f
                              JOIN [SCSInventoryFeedData_{safeCustomerId}] d ON d.ItemId = f.ItemID AND d.CustomerId = f.CustomerID
                              WHERE f.CustomerID = @CustomerID AND d.BatchID = @BatchID AND d.[Type] = 'JSON-RVD'
                                AND d.[Data] LIKE '%' + @FeedId + '%';
                              SELECT @@ROWCOUNT;";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.CommandTimeout = 600;
                cmd.Parameters.AddWithValue("@CustomerID", customerId ?? "");
                cmd.Parameters.AddWithValue("@BatchID", batchId ?? "");
                cmd.Parameters.AddWithValue("@FeedId", feedId ?? "");
                conn.Open();

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        // One FEED-SUCCESS / FEED-ERROR row per SKU, written with SqlBulkCopy (BulkNewInsertData).
        private static DataTable CreateResultDataTable()
        {
            DataTable table = new DataTable();
            table.Columns.Add("CustomerId", typeof(string));
            table.Columns.Add("ItemId", typeof(string));
            table.Columns.Add("Type", typeof(string));
            table.Columns.Add("Data", typeof(string));
            table.Columns.Add("CreatedDate", typeof(DateTime));
            table.Columns.Add("CreatedBy", typeof(int));
            table.Columns.Add("BatchID", typeof(string));
            return table;
        }
    }

    #region Walmart Feed Status Response Models

    /// <summary>
    /// Walmart Feed Status Response Model
    /// GET /v3/feeds/{feedId}?includeDetails=true
    /// </summary>
    public class WalmartFeedStatusResponse
    {
        public string feedId { get; set; }
        public string feedType { get; set; }
        public string partnerId { get; set; }
        public int itemsReceived { get; set; }
        public int itemsSucceeded { get; set; }
        public int itemsFailed { get; set; }
        public int itemsProcessing { get; set; }
        public string feedStatus { get; set; }
        public DateTime? feedDate { get; set; }
        public DateTime? modifiedDtm { get; set; }
        public int itemDataErrorCount { get; set; }
        public int itemSystemErrorCount { get; set; }
        public int itemTimeoutErrorCount { get; set; }
        public WalmartItemDetails itemDetails { get; set; }
    }

    public class WalmartItemDetails
    {
        public List<WalmartItemIngestionStatus> itemIngestionStatus { get; set; }
    }

    public class WalmartItemIngestionStatus
    {
        public string martId { get; set; }
        public string sku { get; set; }
        public string wpid { get; set; }
        public int index { get; set; }
        public string ingestionStatus { get; set; }
        public WalmartIngestionErrors ingestionErrors { get; set; }
    }

    // Walmart wraps the list: "ingestionErrors": { "ingestionError": [ ... ] }
    public class WalmartIngestionErrors
    {
        public List<WalmartIngestionError> ingestionError { get; set; }
    }

    public class WalmartIngestionError
    {
        public string type { get; set; }
        public string code { get; set; }
        public string field { get; set; }
        public string description { get; set; }
    }

    #endregion
}
