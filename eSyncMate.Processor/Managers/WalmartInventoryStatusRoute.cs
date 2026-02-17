using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Connections;
using eSyncMate.Processor.Models;
using Newtonsoft.Json;
using RestSharp;
using System.Data;
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

                // Step 1: Get pending feed records from database
                if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                {
                    route.SaveLog(LogTypeEnum.Debug, $"Source connector processing start...", string.Empty, userNo);

                    DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);

                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@CUSTOMERID@", l_SourceConnector.CustomerID);
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ROUTETYPEID@", Convert.ToString(RouteTypesEnum.WalmartInventoryStatus));
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
                            // GET /v3/feeds/{feedId}?includeDetails=true
                            l_DestinationConnector.Url = l_DestinationConnector.BaseUrl.TrimEnd('/') + $"/v3/feeds/{feedId}?includeDetails=true";

                            route.SaveLog(LogTypeEnum.Debug, $"Checking feed status for FeedId: {feedId}", string.Empty, userNo);
                            route.SaveData("JSON-SNT", 0, l_DestinationConnector.Url, userNo);

                            sourceResponse = RestConnector.Execute(l_DestinationConnector, Body).GetAwaiter().GetResult();

                            if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                WalmartFeedStatusResponse feedStatusResponse = JsonConvert.DeserializeObject<WalmartFeedStatusResponse>(sourceResponse.Content);

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
                                        if (feedStatusResponse.itemDetails?.itemIngestionStatus != null &&
                                            feedStatusResponse.itemDetails.itemIngestionStatus.Count > 0)
                                        {
                                            // Get items with errors
                                            var errorItems = feedStatusResponse.itemDetails.itemIngestionStatus
                                                .Where(i => i.ingestionStatus != "SUCCESS")
                                                .ToList();

                                            if (errorItems.Count > 0)
                                            {
                                                route.SaveLog(LogTypeEnum.Info, $"Found {errorItems.Count} items with errors", string.Empty, userNo);

                                                // Update each error item status
                                                foreach (var errorItem in errorItems)
                                                {
                                                    string errorMessage = string.Empty;
                                                    if (errorItem.ingestionErrors != null && errorItem.ingestionErrors.Count > 0)
                                                    {
                                                        errorMessage = string.Join("; ", errorItem.ingestionErrors.Select(e => $"{e.code}: {e.description}"));
                                                    }

                                                    // Update item status in SCSInventoryFeedData
                                                    UpdateItemErrorStatus(l_SourceConnector.ConnectionString, customerId, batchId, errorItem.sku, errorItem.ingestionStatus, errorMessage);

                                                    route.SaveLog(LogTypeEnum.Debug,
                                                        $"Item Error - SKU: {errorItem.sku}, Status: {errorItem.ingestionStatus}, Error: {errorMessage}",
                                                        string.Empty, userNo);
                                                }
                                            }

                                            // Get successful items
                                            var successItems = feedStatusResponse.itemDetails.itemIngestionStatus
                                                .Where(i => i.ingestionStatus == "SUCCESS")
                                                .ToList();

                                            if (successItems.Count > 0)
                                            {
                                                route.SaveLog(LogTypeEnum.Info, $"Successfully processed {successItems.Count} items", string.Empty, userNo);

                                                // Update success status for items
                                                var successSkus = successItems.Select(i => i.sku).ToList();
                                                UpdateSuccessItemsStatus(l_SourceConnector.ConnectionString, customerId, batchId, successSkus);
                                            }
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
                                        // Feed-level error
                                        l_CustomerProductCatalog.UpdateInventoryBacthwiseStatus(batchId, feedId, "Error", customerId, sourceResponse.Content);
                                        route.SaveLog(LogTypeEnum.Error, $"Feed {feedId} has error status", sourceResponse.Content, userNo);
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

        /// <summary>
        /// Update item error status in SCSInventoryFeedData table
        /// Inserts a new record with Type = 'FEED-ERROR' to track the error
        /// </summary>
        private static void UpdateItemErrorStatus(string connectionString, string customerId, string batchId, string sku, string status, string errorMessage)
        {
            try
            {
                DBConnector connection = new DBConnector(connectionString);

                // Sanitize inputs
                string safeSku = sku?.Replace("'", "''") ?? "";
                string safeStatus = status?.Replace("'", "''") ?? "";
                string safeErrorMessage = errorMessage?.Replace("'", "''") ?? "";
                string safeBatchId = batchId?.Replace("'", "''") ?? "";
                string safeCustomerId = customerId?.Replace("'", "''") ?? "";

                // Create error data JSON
                var errorData = new
                {
                    status = status,
                    errorMessage = errorMessage,
                    processedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                string errorDataJson = JsonConvert.SerializeObject(errorData).Replace("'", "''");

                // Insert error record with Type = 'FEED-ERROR'
                string insertQuery = $@"INSERT INTO SCSInventoryFeedData (CustomerId, ItemId, Type, Data, BatchID, CreatedDate, CreatedBy)
                                       VALUES ('{safeCustomerId}', '{safeSku}', 'FEED-ERROR', '{errorDataJson}', '{safeBatchId}', GETDATE(), 1)";

                connection.Execute(insertQuery);
            }
            catch (Exception)
            {
                // Log error but don't throw - continue processing other items
            }
        }

        /// <summary>
        /// Bulk update successful items status
        /// </summary>
        private static void UpdateSuccessItemsStatus(string connectionString, string customerId, string batchId, List<string> skus)
        {
            if (skus == null || skus.Count == 0) return;

            try
            {
                DBConnector connection = new DBConnector(connectionString);

                string safeBatchId = batchId?.Replace("'", "''") ?? "";
                string safeCustomerId = customerId?.Replace("'", "''") ?? "";

                // Create success data JSON
                var successData = new
                {
                    status = "SUCCESS",
                    processedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                string successDataJson = JsonConvert.SerializeObject(successData).Replace("'", "''");

                // Build bulk insert for successful items
                StringBuilder insertQuery = new StringBuilder();
                insertQuery.Append("INSERT INTO SCSInventoryFeedData (CustomerId, ItemId, Type, Data, BatchID, CreatedDate, CreatedBy) VALUES ");

                var values = skus.Select(sku =>
                {
                    string safeSku = sku?.Replace("'", "''") ?? "";
                    return $"('{safeCustomerId}', '{safeSku}', 'FEED-SUCCESS', '{successDataJson}', '{safeBatchId}', GETDATE(), 1)";
                });

                insertQuery.Append(string.Join(", ", values));

                connection.Execute(insertQuery.ToString());
            }
            catch (Exception)
            {
                // Log error but don't throw
            }
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
        public List<WalmartIngestionError> ingestionErrors { get; set; }
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
