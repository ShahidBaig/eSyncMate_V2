using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Net.Http.Headers;
using System.Text;
using static eSyncMate.DB.Declarations;

namespace eSyncMate.Processor.Managers
{
    /// <summary>
    /// Lowes Inventory Upload (Mirakl)
    /// - STO01: POST /api/offers/stock/imports              (multipart CSV)
    /// - STO02: GET  /api/offers/stock/imports/{import_id}/status
    /// - STO03: GET  /api/offers/stock/imports/{import_id}/error_report
    /// </summary>
    public class LowesUploadWarehouseWiseInventoryRoute
    {
        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;
            DataTable l_data = new();
            InventoryBatchWise l_InventoryBatchWise = new();
            l_InventoryBatchWise.BatchID = Guid.NewGuid().ToString();
            DB.Entities.SCSInventoryFeed l_SCSInventoryFeed = new();
            DataTable ShipNodedataTable = new();

            try
            {
                ConnectorDataModel? l_SourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
                ConnectorDataModel? l_DestinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);

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
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ROUTETYPEID@", Convert.ToString(RouteTypesEnum.LowesWHSWInventoryUpload));
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@USERNO@", Convert.ToString(userNo));

                    if (l_SourceConnector.CommandType == "SP")
                    {
                        connection.GetDataSP(l_SourceConnector.Command, ref l_data);
                    }
                }

                l_SCSInventoryFeed.UseConnection(l_SourceConnector.ConnectionString);

                if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString() && l_data.Rows.Count > 0)
                {

                    SCSInventoryFeed feed = new SCSInventoryFeed();
                    feed.UseConnection(l_SourceConnector.ConnectionString);
                    feed.TargetPlusShipNode(l_SourceConnector.CustomerID, ref ShipNodedataTable);
                    l_InventoryBatchWise.StartDate = Convert.ToDateTime(DateTime.Now);
                    l_InventoryBatchWise.Status = "Processing";
                    l_InventoryBatchWise.RouteType = RouteTypesEnum.LowesWHSWInventoryUpload.ToString();
                    l_InventoryBatchWise.CustomerID = l_SourceConnector.CustomerID;
                    l_SCSInventoryFeed.InsertInventoryBatchWise(l_InventoryBatchWise);
                    int chunkSize = 0;
                    int totalThread = CommonUtils.UploadInventoryTotalThread;

                    if (l_data.Rows.Count >= 20000)
                    {
                        chunkSize = l_data.Rows.Count / totalThread;
                    }
                    else
                    {
                        chunkSize = l_data.Rows.Count;
                    }

                    var tables = l_data.AsEnumerable().ToChunks(chunkSize)
                        .Select(rows => rows.CopyToDataTable()).ToList();

                    foreach (var table in tables)
                    {
                        var itemsThread = new ProcessLowesStockImportChunkThread(table, route, feed, l_DestinationConnector, l_SourceConnector, userNo, l_InventoryBatchWise.BatchID, ShipNodedataTable);
                        itemsThread.ProcessItems().GetAwaiter().GetResult();
                        Thread.Sleep(TimeSpan.FromMinutes(1));
                    }
                }

                l_InventoryBatchWise.Status = "Completed";
                l_InventoryBatchWise.FinishDate = DateTime.Now;

                l_SCSInventoryFeed.UpdateInventoryBatchWise(l_InventoryBatchWise);
            }
            catch (Exception ex)
            {
                l_InventoryBatchWise.FinishDate = DateTime.Now;
                l_InventoryBatchWise.Status = "Error";
                l_SCSInventoryFeed.UpdateInventoryBatchWise(l_InventoryBatchWise);
                route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
            }
            finally
            {
                l_data.Dispose();
            }
        }
    }

    public class ProcessLowesStockImportChunkThread
    {
        private static HttpClient HttpClient => SharedHttpClientFactory.Default;

        private DataTable data;
        private Routes route;
        private SCSInventoryFeed feed;
        private ConnectorDataModel destinationConnector;
        private ConnectorDataModel sourceConnector;
        private int userNo;
        private string batchID;
        private DataTable ShipNodeData;

        public ProcessLowesStockImportChunkThread(DataTable data, Routes route, SCSInventoryFeed feed, ConnectorDataModel destinationConnector,ConnectorDataModel sourceConnector, int userNo, string batchID, DataTable shipNodeData)
        {
            this.data = data;
            this.route = JsonConvert.DeserializeObject<Routes>(JsonConvert.SerializeObject(route));
            this.feed = JsonConvert.DeserializeObject<SCSInventoryFeed>(JsonConvert.SerializeObject(feed));
            this.destinationConnector = destinationConnector;
            this.sourceConnector = sourceConnector;
            this.userNo = userNo;
            this.batchID = batchID;
            ShipNodeData = shipNodeData;
        }

        public async Task ProcessItems()
        {
            string l_Guid = Guid.NewGuid().ToString();
            try
            {
                this.route.UseConnection(this.sourceConnector.ConnectionString);
                this.feed.UseConnection(this.sourceConnector.ConnectionString);
                string csv = BuildStockImportCsv(this.data, this.feed, this.sourceConnector.ConnectionString, this.batchID, this.ShipNodeData, l_Guid, out DataTable sentTable);
                if (string.IsNullOrWhiteSpace(csv)) return;
                this.feed.BulkNewInsertData(this.sourceConnector.ConnectionString, "SCSInventoryFeedData", sentTable);
                {
                    var uploadResp = await PostStockImportCsv(this.destinationConnector, csv);
                    string uploadBody = await uploadResp.Content.ReadAsStringAsync();

                    if (!uploadResp.IsSuccessStatusCode)
                    {
                        this.route.SaveLog(LogTypeEnum.Error, "LowesUploadWarehouseWiseInventoryRoute STO01 failed.", uploadBody, this.userNo);
                        return;
                    }

                    string importId = JObject.Parse(uploadBody)["import_id"]?.ToString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(importId))
                    {
                        this.route.SaveLog(LogTypeEnum.Error, "LowesUploadWarehouseWiseInventoryRoute STO01 did not return import_id.", uploadBody, this.userNo);
                        return;
                    }

                    this.feed.UpdateSCSLowesFeedData(this.batchID, importId, l_Guid);
                    DataTable bulkInsertTable = CreateBulkInsertDataTable();

                    foreach (DataRow row in this.data.Rows)
                    {
                        DataRow bulkRow = bulkInsertTable.NewRow();
                        bulkRow["CustomerId"] = row["CustomerId"];
                        bulkRow["ItemId"] = row["ItemId"];
                        bulkRow["Type"] = "JSON-RVD";
                        bulkRow["Data"] = uploadBody;
                        bulkRow["CreatedDate"] = DateTime.Now;
                        bulkRow["CreatedBy"] = 1;
                        bulkRow["BatchID"] = this.batchID;
                        bulkInsertTable.Rows.Add(bulkRow);

                        feed.UpdateItemStatus(row["ItemId"].ToString(), row["CustomerId"].ToString());
                    }

                    feed.BulkNewInsertData(this.sourceConnector.ConnectionString, "SCSInventoryFeedData", bulkInsertTable);
                    this.feed.InsertInventoryBatchWiseFeedDetail(this.batchID, "NEW", importId, this.destinationConnector.CustomerID);
                }
            }
            catch (Exception ex)
            {
                this.route.SaveLog(LogTypeEnum.Error, ex.Message, string.Empty, userNo);
            }
        }

        private string BuildStockImportCsv(DataTable inventoryData, SCSInventoryFeed feed, string connectionString, string batchID, DataTable shipNodeData, string p_Guid, out DataTable sentTable)
        {
            feed.UseConnection(connectionString);
            sentTable = CreateBulkInsertDataTable();

            try
            {
                var sb = new StringBuilder();
                DataTable bulkSCSLowesFeedData = CreateBulkInsertSCSLowesFeedData();

                string header = feed.GetLowesStockImportHeader();
                if (header.IsNullOrEmpty())
                {
                    this.route.SaveLog(LogTypeEnum.Error, "Headers not received, SP returned empty.", string.Empty, this.userNo);
                    return String.Empty;
                }
                sb.AppendLine(header);

                foreach (DataRow row in inventoryData.Rows)
                {
                    string customerId = row["CustomerId"]?.ToString() ?? "";
                    string itemId = row["ItemId"]?.ToString() ?? "";
                    string offerSku = row["ItemId"]?.ToString()?.ToString() ?? "";
                    string updateDelete = "update";

                    if (string.IsNullOrWhiteSpace(offerSku)) continue;

                    var perItemLines = new List<object>();

                    foreach (DataRow item in shipNodeData.Rows)
                    {
                        string shipNode = item["ShipNode"]?.ToString() ?? "";
                        string whsId = item["WHSID"]?.ToString() ?? "";
                        if (string.IsNullOrWhiteSpace(shipNode) || string.IsNullOrWhiteSpace(whsId)) continue;
                        int quantity = inventoryData.Columns.Contains($"ATS_{whsId}") && row[$"ATS_{whsId}"] != DBNull.Value? Convert.ToInt32(row[$"ATS_{whsId}"]) : 0;
                        string line = $"{Csv(offerSku)};{Csv(quantity.ToString())};{Csv(shipNode)};{Csv(updateDelete)}";
                        sb.AppendLine(line);

                        perItemLines.Add(new
                        {
                            offer_sku = offerSku,
                            quantity = quantity,
                            warehouse_code = shipNode,
                            update_delete = updateDelete
                        });
                    }

                    string perItemJson = JsonConvert.SerializeObject(new { lines = perItemLines }, Formatting.None);

                    DataRow bulkRow = sentTable.NewRow();
                    bulkRow["CustomerId"] = customerId;
                    bulkRow["ItemId"] = itemId;
                    bulkRow["Type"] = "JSON-SNT";
                    bulkRow["Data"] = perItemJson;
                    bulkRow["CreatedDate"] = DateTime.Now;
                    bulkRow["CreatedBy"] = 1;
                    bulkRow["BatchID"] = batchID;
                    sentTable.Rows.Add(bulkRow);

                    // SCSLowesFeedData: store per-item with temp GUID (will be updated to real importId later)
                    DataRow lowesFeedRow = bulkSCSLowesFeedData.NewRow();
                    lowesFeedRow["BatchID"] = batchID;
                    lowesFeedRow["ItemID"] = itemId;
                    lowesFeedRow["CustomerID"] = customerId;
                    lowesFeedRow["ImportId"] = p_Guid;
                    lowesFeedRow["Data"] = perItemJson;
                    bulkSCSLowesFeedData.Rows.Add(lowesFeedRow);
                }

                feed.BulkLowesFeedData(connectionString, "SCSLowesFeedData", bulkSCSLowesFeedData);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to build stock CSV for BatchID '{batchID}'", ex);
            }
        }

        private static string Csv(string value)
        {
            value ??= "";
            value = value.Replace("\"", "\"\"");
            return $"\"{value}\"";
        }

        public static DataTable CreateBulkInsertDataTable()
        {
            DataTable bulkInsertTable = new();
            bulkInsertTable.Columns.Add("CustomerId", typeof(string));
            bulkInsertTable.Columns.Add("ItemId", typeof(string));
            bulkInsertTable.Columns.Add("Type", typeof(string));
            bulkInsertTable.Columns.Add("Data", typeof(string));
            bulkInsertTable.Columns.Add("CreatedDate", typeof(DateTime));
            bulkInsertTable.Columns.Add("CreatedBy", typeof(int));
            bulkInsertTable.Columns.Add("BatchID", typeof(string));
            return bulkInsertTable;
        }

        public static DataTable CreateBulkInsertSCSLowesFeedData()
        {
            DataTable bulkInsertTable = new();
            bulkInsertTable.Columns.Add("BatchID", typeof(string));
            bulkInsertTable.Columns.Add("ItemID", typeof(string));
            bulkInsertTable.Columns.Add("CustomerID", typeof(string));
            bulkInsertTable.Columns.Add("ImportId", typeof(string));
            bulkInsertTable.Columns.Add("Data", typeof(string));
            return bulkInsertTable;
        }

        private static readonly Random RetryRandom = new();

        private static TimeSpan GetRetryDelay(int attempt)
        {
            var baseSeconds = Math.Min(8, 2 * Math.Pow(2, attempt - 1));
            int jitterMs;
            lock (RetryRandom)
            {
                jitterMs = RetryRandom.Next(0, 1000);
            }
            return TimeSpan.FromSeconds(baseSeconds) + TimeSpan.FromMilliseconds(jitterMs);
        }

        /// <summary>
        /// STO01: POST /api/offers/stock/imports (multipart CSV upload)
        /// </summary>
        private static async Task<HttpResponseMessage> PostStockImportCsv(ConnectorDataModel connector, string csv)
        {
            var url = CombineUrl(connector.BaseUrl, "/api/offers/stock/imports");
            const int maxAttempts = 3;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                using var form = new MultipartFormDataContent();
                var bytes = Encoding.UTF8.GetBytes(csv);
                var fileContent = new ByteArrayContent(bytes);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/csv");
                form.Add(fileContent, "file", "stock_import.csv");
                using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
                ApplyConnectorHeaders(req, connector);

                try
                {
                    var res = await HttpClient.SendAsync(req);
                    if ((int)res.StatusCode >= 400 && (int)res.StatusCode < 500 && res.StatusCode != System.Net.HttpStatusCode.Unauthorized)
                        return res;

                    if (res.IsSuccessStatusCode || attempt == maxAttempts)
                        return res;

                    if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized || (int)res.StatusCode == 408 || (int)res.StatusCode == 429 || ((int)res.StatusCode >= 500 && (int)res.StatusCode <= 599))
                    {
                        await Task.Delay(GetRetryDelay(attempt));
                        continue;
                    }

                    return res;
                }
                catch (TaskCanceledException)
                {
                    if (attempt == maxAttempts) throw;
                    await Task.Delay(GetRetryDelay(attempt));
                }
                catch
                {
                    if (attempt == maxAttempts) throw;
                    await Task.Delay(GetRetryDelay(attempt));
                }
            }

            throw new HttpRequestException("Unexpected failure sending Lowes stock import request.");
        }


        private static string CombineUrl(string baseUrl, string path)
        {
            baseUrl ??= "";
            if (baseUrl.EndsWith("/")) baseUrl = baseUrl.TrimEnd('/');
            if (!path.StartsWith("/")) path = "/" + path;
            return baseUrl + path;
        }

        private static void ApplyConnectorHeaders(HttpRequestMessage req, ConnectorDataModel connector)
        {
            if (connector.Headers != null)
            {
                foreach (var h in connector.Headers)
                {
                    if (string.IsNullOrWhiteSpace(h?.Name)) continue;
                    req.Headers.TryAddWithoutValidation(h.Name, h.Value);
                }
            }
            if (!string.IsNullOrWhiteSpace(connector.Username) && connector.Password != null)
            {
                var raw = $"{connector.Username}:{connector.Password}";
                var b64 = Convert.ToBase64String(Encoding.ASCII.GetBytes(raw));
                req.Headers.Authorization = new AuthenticationHeaderValue("Basic", b64);
            }
        }
    }
}
