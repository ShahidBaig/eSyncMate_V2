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
    /// Walmart Bulk Inventory Upload Route - Uses Feed API for bulk uploads
    /// This is much faster than per-item API calls
    /// Single API call can upload thousands of items
    /// Pattern: Same as LowesUpdateInventory
    /// </summary>
    public class WalmartBulkUploadInventoryRoute
    {
        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;
            DataTable l_data = new DataTable();
            InventoryBatchWise l_InventoryBatchWise = new InventoryBatchWise();
            l_InventoryBatchWise.BatchID = Guid.NewGuid().ToString();
            DB.Entities.SCSInventoryFeed l_SCSInventoryFeed = new DB.Entities.SCSInventoryFeed();

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

                // Step 1: Get inventory data from database
                if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                {
                    route.SaveLog(LogTypeEnum.Debug, $"Source connector processing start...", string.Empty, userNo);

                    DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);

                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@CUSTOMERID@", l_SourceConnector.CustomerID);
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ROUTETYPEID@", Convert.ToString(RouteTypesEnum.WalmartUploadInventory));
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@USERNO@", Convert.ToString(userNo));

                    if (l_SourceConnector.CommandType == "SP")
                    {
                        connection.GetDataSP(l_SourceConnector.Command, ref l_data);
                    }

                    route.SaveLog(LogTypeEnum.Debug, $"Source connector processing completed. Total items: {l_data.Rows.Count}", string.Empty, userNo);
                }

                // Set connection before the if block so it's available for UpdateInventoryBatchWise in catch block
                l_SCSInventoryFeed.UseConnection(l_SourceConnector.ConnectionString);

                // Step 2: Process bulk feed
                if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString() && l_data.Rows.Count > 0)
                {
                    route.SaveLog(LogTypeEnum.Debug, $"Destination connector processing start...", string.Empty, userNo);

                    SCSInventoryFeed feed = new SCSInventoryFeed();
                    feed.UseConnection(l_SourceConnector.ConnectionString);

                    // Insert batch tracking record
                    l_InventoryBatchWise.StartDate = DateTime.Now;
                    l_InventoryBatchWise.Status = "Processing";
                    l_InventoryBatchWise.RouteType = RouteTypesEnum.WalmartUploadInventory.ToString();
                    l_InventoryBatchWise.CustomerID = l_SourceConnector.CustomerID;
                    l_SCSInventoryFeed.InsertInventoryBatchWise(l_InventoryBatchWise);

                 
                    int chunkSize = 5000;

                    if (l_data.Rows.Count <= chunkSize)
                    {
                        // Small dataset - process directly
                        ProcessWalmartBulkItemsChunkThread itemsThread = new ProcessWalmartBulkItemsChunkThread(
                            l_data, route, feed, l_DestinationConnector, l_SourceConnector, userNo, l_InventoryBatchWise.BatchID
                        );
                        itemsThread.ProcessItems().GetAwaiter().GetResult();
                    }
                    else
                    {
                        // Large dataset - split into chunks of 10,000
                        var tables = l_data.AsEnumerable().ToChunks(chunkSize)
                            .Select(rows => rows.CopyToDataTable()).ToList();

                        route.SaveLog(LogTypeEnum.Info, $"Processing {l_data.Rows.Count} items in {tables.Count} chunks of {chunkSize}", string.Empty, userNo);

                        int chunkIndex = 0;
                        foreach (var table in tables)
                        {
                            chunkIndex++;
                            route.SaveLog(LogTypeEnum.Debug, $"Processing chunk {chunkIndex}/{tables.Count} with {table.Rows.Count} items", string.Empty, userNo);

                            var itemsThread = new ProcessWalmartBulkItemsChunkThread(
                                table, route, feed, l_DestinationConnector, l_SourceConnector, userNo, l_InventoryBatchWise.BatchID
                            );

                            itemsThread.ProcessItems().GetAwaiter().GetResult();

                            // 1-minute delay between chunks (except for last chunk)
                            if (chunkIndex < tables.Count)
                            {
                                Thread.Sleep(TimeSpan.FromMinutes(1));
                            }
                        }
                    }

                    l_InventoryBatchWise.Status = "Completed";
                    l_InventoryBatchWise.FinishDate = DateTime.Now;
                    l_SCSInventoryFeed.UpdateInventoryBatchWise(l_InventoryBatchWise);

                    route.SaveLog(LogTypeEnum.Debug, $"Destination connector processing completed.", string.Empty, userNo);
                }

                route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                l_InventoryBatchWise.FinishDate = DateTime.Now;
                l_InventoryBatchWise.Status = "Error";
                l_SCSInventoryFeed.UpdateInventoryBatchWise(l_InventoryBatchWise);
                route.SaveLog(LogTypeEnum.Exception, $"Error executing Walmart Bulk Inventory route [{route.Id}]", ex.ToString(), userNo);
            }
            finally
            {
                l_data.Dispose();
            }
        }
    }

    /// <summary>
    /// Process Walmart inventory items in chunks with bulk operations
    /// Pattern: Same as ProcessLowesItemsChunkThread
    /// </summary>
    public class ProcessWalmartBulkItemsChunkThread
    {
        private DataTable data;
        private Routes route;
        private SCSInventoryFeed feed;
        private ConnectorDataModel destinationConnector;
        private ConnectorDataModel sourceConnector;
        private int userNo;
        private string batchID;

        public ProcessWalmartBulkItemsChunkThread(DataTable data, Routes route, SCSInventoryFeed feed,
            ConnectorDataModel destinationConnector, ConnectorDataModel sourceConnector, int userNo, string batchID)
        {
            this.data = data;
            this.route = JsonConvert.DeserializeObject<Routes>(JsonConvert.SerializeObject(route));
            this.feed = JsonConvert.DeserializeObject<SCSInventoryFeed>(JsonConvert.SerializeObject(feed));
            this.destinationConnector = destinationConnector;
            this.sourceConnector = sourceConnector;
            this.userNo = userNo;
            this.batchID = batchID;
        }

        public async Task ProcessItems()
        {
            RestResponse sourceResponse = new RestResponse();
            DataTable bulkInsertTable = CreateBulkInsertDataTable();
            DataTable shipNodeDataTable = new DataTable();

            try
            {
                this.route.UseConnection(this.sourceConnector.ConnectionString);
                this.feed.UseConnection(this.sourceConnector.ConnectionString);

                // Get Ship Nodes
                this.feed.APIShipNode("WalmartAPI", ref shipNodeDataTable);

                // Build Walmart bulk inventory request model
                WalmartBulkInventoryFeed requestModel = new WalmartBulkInventoryFeed
                {
                    InventoryHeader = new WalmartInventoryHeader { version = "1.4" },
                    Inventory = new List<WalmartInventoryItem>()
                };

                // Build request model and bulk insert table in single loop
                foreach (DataRow row in this.data.Rows)
                {
                    string customerId = row["CustomerId"].ToString();
                    string itemId = row["ItemId"].ToString();

                    // If ship nodes exist, create entry for each ship node
                    if (shipNodeDataTable.Rows.Count > 0)
                    {
                        foreach (DataRow shipNodeRow in shipNodeDataTable.Rows)
                        {
                            string shipNode = shipNodeRow["ShipNode"]?.ToString() ?? "";
                            string whsId = shipNodeRow["WHSID"]?.ToString() ?? "";
                            int quantity = 0;

                            // Get quantity for this warehouse
                            if (this.data.Columns.Contains($"ATS_{whsId}") && row[$"ATS_{whsId}"] != DBNull.Value)
                            {
                                quantity = Convert.ToInt32(row[$"ATS_{whsId}"]);
                            }

                            var inventoryItem = new WalmartInventoryItem
                            {
                                sku = itemId,
                                shipNode = shipNode,
                                quantity = new WalmartInventoryQuantity
                                {
                                    unit = "EACH",
                                    amount = quantity
                                }
                            };

                            requestModel.Inventory.Add(inventoryItem);
                        }
                    }
                    else
                    {
                        // Single node - use Total_ATS
                        int totalQuantity = 0;
                        if (this.data.Columns.Contains("Total_ATS") && row["Total_ATS"] != DBNull.Value)
                        {
                            totalQuantity = Convert.ToInt32(row["Total_ATS"]);
                        }

                        var inventoryItem = new WalmartInventoryItem
                        {
                            sku = itemId,
                            quantity = new WalmartInventoryQuantity
                            {
                                unit = "EACH",
                                amount = totalQuantity
                            },
                            fulfillmentLagTime = 1
                        };

                        requestModel.Inventory.Add(inventoryItem);
                    }

                    // Build per-item JSON for logging - only items added for this row
                    int itemsAddedThisRow = shipNodeDataTable.Rows.Count > 0 ? shipNodeDataTable.Rows.Count : 1;
                    var perItemModel = new WalmartBulkInventoryFeed
                    {
                        InventoryHeader = new WalmartInventoryHeader { version = "1.4" },
                        Inventory = requestModel.Inventory
                            .Skip(requestModel.Inventory.Count - itemsAddedThisRow)
                            .ToList()
                    };
                    string perItemJson = JsonConvert.SerializeObject(perItemModel, Formatting.None, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });

                    DataRow bulkRow = bulkInsertTable.NewRow();
                    bulkRow["CustomerId"] = customerId;
                    bulkRow["ItemId"] = itemId;
                    bulkRow["Type"] = "JSON-SNT";
                    bulkRow["Data"] = perItemJson;
                    bulkRow["CreatedDate"] = DateTime.Now;
                    bulkRow["CreatedBy"] = this.userNo;
                    bulkRow["BatchID"] = this.batchID;
                    bulkInsertTable.Rows.Add(bulkRow);
                }

                // Bulk insert sent data (single DB call instead of N calls)
                this.feed.BulkNewInsertData(this.sourceConnector.ConnectionString, "SCSInventoryFeedData", bulkInsertTable);

                // Make single API call for this chunk using Feed API
                string body = JsonConvert.SerializeObject(requestModel, Formatting.None, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

                // Submit to Walmart Feed API
                string feedUrl = this.destinationConnector.BaseUrl.TrimEnd('/') + "/feeds?feedType=MP_INVENTORY";

                this.route.SaveLog(LogTypeEnum.Debug, $"Submitting feed to: {feedUrl}", string.Empty, this.userNo);
                this.route.SaveData("JSON-SNT", 0, body, this.userNo);

                sourceResponse = await SubmitWalmartFeed(feedUrl, body);

                if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK ||
                    sourceResponse.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    // Parse response to get feedId
                    WalmartFeedResponse feedResponse = null;
                    try
                    {
                        feedResponse = JsonConvert.DeserializeObject<WalmartFeedResponse>(sourceResponse.Content);
                    }
                    catch (Exception ex)
                    {
                        this.route.SaveLog(LogTypeEnum.Error, $"Failed to parse Walmart feed response: {sourceResponse.Content}", ex.ToString(), this.userNo);
                    }

                    string feedId = feedResponse?.feedId ?? "UNKNOWN";

                    this.route.SaveLog(LogTypeEnum.Info, $"Feed submitted successfully. FeedId: {feedId}", string.Empty, this.userNo);

                    // Bulk insert response data
                    DataTable bulkResponseTable = CreateBulkInsertDataTable();

                    foreach (DataRow row in this.data.Rows)
                    {
                        DataRow respRow = bulkResponseTable.NewRow();
                        respRow["CustomerId"] = row["CustomerId"].ToString();
                        respRow["ItemId"] = row["ItemId"].ToString();
                        respRow["Type"] = "JSON-RVD";
                        respRow["Data"] = sourceResponse.Content;
                        respRow["CreatedDate"] = DateTime.Now;
                        respRow["CreatedBy"] = this.userNo;
                        respRow["BatchID"] = this.batchID;
                        bulkResponseTable.Rows.Add(respRow);
                    }

                    // Single bulk insert for responses
                    this.feed.BulkNewInsertData(this.sourceConnector.ConnectionString, "SCSInventoryFeedData", bulkResponseTable);

                    // Bulk update item status (single DB call)
                    this.feed.BulkUpdateItemStatus(this.sourceConnector.ConnectionString, this.data);

                    // Save feed detail for tracking
                    this.feed.InsertInventoryBatchWiseFeedDetail(this.batchID, "NEW", feedId, this.sourceConnector.CustomerID);

                    this.route.SaveLog(LogTypeEnum.Debug, $"WalmartBulkUploadInventory updated for {this.data.Rows.Count} items.", string.Empty, this.userNo);
                }
                else
                {
                    this.route.SaveLog(LogTypeEnum.Error, $"Unable to update WalmartBulkUploadInventory for chunk. Status: {sourceResponse.StatusCode}", sourceResponse.Content, this.userNo);
                    this.route.SaveData("JSON-RVD", 0, sourceResponse.Content ?? "No response", this.userNo);
                }
            }
            catch (Exception ex)
            {
                this.route.SaveLog(LogTypeEnum.Exception, $"Unable to update WalmartBulkUploadInventory for chunk.", ex.ToString(), this.userNo);
            }
            finally
            {
                bulkInsertTable.Dispose();
                shipNodeDataTable.Dispose();
            }
        }

        /// <summary>
        /// Submit feed to Walmart Feed API using RestSharp
        /// </summary>
        private async Task<RestResponse> SubmitWalmartFeed(string feedUrl, string jsonPayload)
        {
            try
            {
                // Get Walmart access token using the same method as RestConnector
                WalmartConnector walmartConnector = new WalmartConnector();
                await walmartConnector.GetApiToken(
                    this.destinationConnector.BaseUrl,
                    this.destinationConnector.ConsumerKey,
                    this.destinationConnector.ConsumerSecret,
                    "", "", "");

                string accessToken = WalmartConnector.Token;

                // Walmart Feed API requires multipart/form-data with file attachment
                using var client = new RestClient();
                var request = new RestRequest(feedUrl, Method.Post);

                // Add Walmart authentication headers (same as RestConnector for WALMARTGetToken)
                request.AddHeader("WM_SEC.ACCESS_TOKEN", accessToken);
                request.AddHeader("WM_QOS.CORRELATION_ID", Guid.NewGuid().ToString());
                request.AddHeader("WM_SVC.NAME", "WalmartAPI");
                request.AddHeader("Accept", "application/json");

                // Add any custom headers from destination connector
                if (this.destinationConnector.Headers != null)
                {
                    foreach (var header in this.destinationConnector.Headers)
                    {
                        request.AddHeader(header.Name, header.Value);
                    }
                }

                // Add the JSON content as a file attachment (required for Feed API)
                byte[] fileBytes = Encoding.UTF8.GetBytes(jsonPayload);
                request.AddFile("file", fileBytes, "inventory.json", "application/json");

                // Execute request
                var response = await client.ExecuteAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                this.route.SaveLog(LogTypeEnum.Exception, $"Error submitting Walmart feed: {ex.Message}", ex.ToString(), this.userNo);
                throw;
            }
        }

        private static DataTable CreateBulkInsertDataTable()
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

    #region Walmart Bulk Feed Models

    /// <summary>
    /// Walmart Bulk Inventory Feed - JSON Structure
    /// </summary>
    public class WalmartBulkInventoryFeed
    {
        public WalmartInventoryHeader InventoryHeader { get; set; }
        public List<WalmartInventoryItem> Inventory { get; set; }
    }

    public class WalmartInventoryHeader
    {
        public string version { get; set; }
    }

    public class WalmartInventoryItem
    {
        public string sku { get; set; }
        public string shipNode { get; set; }
        public WalmartInventoryQuantity quantity { get; set; }
        public int? fulfillmentLagTime { get; set; }
    }

    public class WalmartInventoryQuantity
    {
        public string unit { get; set; }
        public int amount { get; set; }
    }

    public class WalmartFeedResponse
    {
        public string feedId { get; set; }
        public object additionalAttributes { get; set; }
        public object errors { get; set; }
    }

    #endregion
}
