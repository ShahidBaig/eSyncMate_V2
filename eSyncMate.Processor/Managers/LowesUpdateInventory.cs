using eSyncMate.DB.Entities;
using eSyncMate.DB;
using eSyncMate.Processor.Models;
using Newtonsoft.Json;
using RestSharp;
using static eSyncMate.DB.Declarations;
using eSyncMate.Processor.Connections;
using System.Data;
using static eSyncMate.Processor.Models.LowesInventoryUploadRequestModel;

namespace eSyncMate.Processor.Managers
{
    public class LowesUpdateInventory
    {
        public static void Execute(IConfiguration config, ILogger logger, Routes route)
        {
            int userNo = 1;
            string destinationData = string.Empty;
            string sourceData = string.Empty;
            string Body = string.Empty;
            int l_ID = 0;
            DataTable l_data = new DataTable();
            RestResponse sourceResponse = new RestResponse();
            InventoryBatchWise l_InventoryBatchWise = new InventoryBatchWise();
            l_InventoryBatchWise.BatchID = Guid.NewGuid().ToString();
            DB.Entities.SCSInventoryFeed l_SCSInventoryFeed = new DB.Entities.SCSInventoryFeed();
            LowesInventoryUploadRequestModel l_LowesInventoryUploadRequestModel = new LowesInventoryUploadRequestModel();
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
                    route.SaveLog(LogTypeEnum.Debug, $"Source connector processing start...", string.Empty, userNo);

                    DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);

                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@CUSTOMERID@", l_SourceConnector.CustomerID);
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ROUTETYPEID@", Convert.ToString(RouteTypesEnum.LowesInventoryUpload));
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@USERNO@", Convert.ToString(userNo));

                    if (l_SourceConnector.CommandType == "SP")
                    {
                        connection.GetDataSP(l_SourceConnector.Command, ref l_data);
                    }

                    route.SaveLog(LogTypeEnum.Debug, $"Source connector processing completed.", string.Empty, userNo);
                }

                if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString() && l_data.Rows.Count > 0)
                {
                    route.SaveLog(LogTypeEnum.Debug, $"Destination connector processing start...", string.Empty, userNo);

                    SCSInventoryFeed feed = new SCSInventoryFeed();

                    feed.UseConnection(l_SourceConnector.ConnectionString);
                    l_SCSInventoryFeed.UseConnection(l_SourceConnector.ConnectionString);

                    l_InventoryBatchWise.StartDate = Convert.ToDateTime(DateTime.Now);
                    l_InventoryBatchWise.Status = "Processing";
                    l_InventoryBatchWise.RouteType = RouteTypesEnum.LowesInventoryUpload.ToString();
                    l_InventoryBatchWise.CustomerID = l_SourceConnector.CustomerID;

                    l_SCSInventoryFeed.InsertInventoryBatchWise(l_InventoryBatchWise);

                    // Process in chunks of 15,000 with 1-minute delay between chunks
                    int chunkSize = 15000;

                    if (l_data.Rows.Count <= chunkSize)
                    {
                        // Small dataset - process directly
                        ProcessLowesItemsChunkThread itemsThread = new ProcessLowesItemsChunkThread(
                            l_data, route, feed, l_DestinationConnector, l_SourceConnector, userNo, l_InventoryBatchWise.BatchID
                        );
                        itemsThread.ProcessItems().GetAwaiter().GetResult();
                    }
                    else
                    {
                        // Large dataset - split into chunks of 15,000
                        var tables = l_data.AsEnumerable().ToChunks(chunkSize)
                            .Select(rows => rows.CopyToDataTable()).ToList();

                        route.SaveLog(LogTypeEnum.Info, $"Processing {l_data.Rows.Count} items in {tables.Count} chunks of {chunkSize}", string.Empty, userNo);

                        int chunkIndex = 0;
                        foreach (var table in tables)
                        {
                            chunkIndex++;
                            route.SaveLog(LogTypeEnum.Debug, $"Processing chunk {chunkIndex}/{tables.Count} with {table.Rows.Count} items", string.Empty, userNo);

                            var itemsThread = new ProcessLowesItemsChunkThread(
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
                route.SaveLog(LogTypeEnum.Exception, $"Unable to update LowesUpdateInventory for items.", ex.ToString(), userNo);
            }
            finally
            {
                l_data.Dispose();
            }





        }
    }

    /// <summary>
    /// Process Lowes inventory items in chunks with bulk operations
    /// </summary>
    public class ProcessLowesItemsChunkThread
    {
        private DataTable data;
        private Routes route;
        private SCSInventoryFeed feed;
        private ConnectorDataModel destinationConnector;
        private ConnectorDataModel sourceConnector;
        private int userNo;
        private string batchID;

        public ProcessLowesItemsChunkThread(DataTable data, Routes route, SCSInventoryFeed feed,
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
            LowesInventoryUploadRequestModel requestModel = new LowesInventoryUploadRequestModel();
            DataTable bulkInsertTable = CreateBulkInsertDataTable();

            try
            {
                this.route.UseConnection(this.sourceConnector.ConnectionString);
                this.feed.UseConnection(this.sourceConnector.ConnectionString);

                // Build request model and bulk insert table in single loop
                foreach (DataRow row in this.data.Rows)
                {
                    string customerId = row["CustomerId"].ToString();
                    string itemId = row["ItemId"].ToString();

                    LowesOffer offer = new LowesOffer
                    {
                        price = row["ListPrice"] == DBNull.Value ? 0 : Convert.ToDouble(row["ListPrice"]),
                        product_sku = row["CustomerItemCode"].ToString(),
                        quantity = row["Total_ATS"].ToString(),
                        shop_sku = itemId,
                        state_code = "11"
                    };
                    requestModel.offers.Add(offer);

                    // Build per-item JSON for logging
                    string perItemJson = JsonConvert.SerializeObject(new LowesInventoryUploadRequestModel
                    {
                        offers = new List<LowesOffer> { offer }
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

                // Make single API call for this chunk
                string body = JsonConvert.SerializeObject(requestModel);
                this.destinationConnector.Url = this.destinationConnector.BaseUrl + "/api/offers";
                sourceResponse = RestConnector.Execute(this.destinationConnector, body).GetAwaiter().GetResult();

                if (sourceResponse.StatusCode == System.Net.HttpStatusCode.Created)
                {
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

                    this.route.SaveLog(LogTypeEnum.Debug, $"LowesUpdateInventory updated for {this.data.Rows.Count} items.", string.Empty, this.userNo);
                }
                else
                {
                    this.route.SaveLog(LogTypeEnum.Error, $"Unable to update LowesUpdateInventory for chunk.", sourceResponse.Content, this.userNo);
                }
            }
            catch (Exception ex)
            {
                this.route.SaveLog(LogTypeEnum.Exception, $"Unable to update LowesUpdateInventory for chunk.", ex.ToString(), this.userNo);
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
}

