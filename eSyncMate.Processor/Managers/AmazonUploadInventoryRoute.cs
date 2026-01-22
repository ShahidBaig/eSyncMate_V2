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
using Microsoft.SqlServer.Server;
using Nancy;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Security.Policy;
using Newtonsoft.Json.Linq;
using Nancy.Responses;
using System.Net.Http.Headers;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using static eSyncMate.Processor.Models.AmazonInventoryRequestModel;
using System.IO.Compression;

namespace eSyncMate.Processor.Managers
{
    public class AmazonUploadInventoryRoute
    {
        public static void Execute(IConfiguration config, Routes route)
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

                if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                {
                    route.SaveLog(LogTypeEnum.Debug, $"Source connector processing start...", string.Empty, userNo);

                    DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);

                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@CUSTOMERID@", l_SourceConnector.CustomerID);
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ROUTETYPEID@", Convert.ToString(RouteTypesEnum.AmazonInventoryUpload));
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
                    l_InventoryBatchWise.RouteType = RouteTypesEnum.AmazonInventoryUpload.ToString();
                    l_InventoryBatchWise.CustomerID = l_SourceConnector.CustomerID;

                    l_SCSInventoryFeed.InsertInventoryBatchWise(l_InventoryBatchWise);


                    int i = 0;
                    int totalThread = CommonUtils.UploadInventoryTotalThread;
                    int chunkSize = l_data.Rows.Count / totalThread;
                    List<Task> tasks = new List<Task>();

                    var tables = l_data.AsEnumerable().ToChunks(chunkSize)
                        .Select(rows => rows.CopyToDataTable()).ToList();


                    foreach (var table in tables)
                    {
                        var itemsThread = new AmazonProcessItemsThread(
                           table, route, feed, l_DestinationConnector, l_SourceConnector, userNo, l_InventoryBatchWise.BatchID
                       );

                        itemsThread.ProcessItems().GetAwaiter().GetResult();

                        Thread.Sleep(TimeSpan.FromMinutes(1));
                    }



                    //if (l_data.Rows.Count <= 100)
                    //{
                    //    AmazonProcessItemsThread itemsThread = new AmazonProcessItemsThread(l_data, route, feed, l_DestinationConnector, l_SourceConnector, userNo, l_InventoryBatchWise.BatchID);

                    //    // Run ProcessItems async
                    //    Task.Run(() => itemsThread.ProcessItems());
                    //}
                    //else
                    //{
                    //    int i = 0;
                    //    int totalThread = CommonUtils.UploadInventoryTotalThread;
                    //    int chunkSize = l_data.Rows.Count / totalThread;
                    //    List<Task> tasks = new List<Task>();

                    //    var tables = l_data.AsEnumerable().ToChunks(chunkSize)
                    //        .Select(rows => rows.CopyToDataTable()).ToList();

                    //    //while (i < tables.Count)
                    //    //{
                    //    //    AmazonProcessItemsThread itemsThread = new AmazonProcessItemsThread(tables[i], route, feed, l_DestinationConnector, l_SourceConnector, userNo, l_InventoryBatchWise.BatchID);

                    //    //    itemsThread.ProcessItems().GetAwaiter().GetResult();

                    //    //    //// Run tasks for each chunk of data asynchronously
                    //    //    ////tasks.Add(Task.Run(() => itemsThread.ProcessItems()));
                    //    //    //Task t = Task.Run(() => itemsThread.ProcessItems());
                    //    //    //Task.WhenAll(t).GetAwaiter().GetResult(); 
                    //    //    i++;
                    //    //    Thread.Sleep(5000);
                    //    //}


                    //    foreach (var table in tables)
                    //    {
                    //        var itemsThread = new AmazonProcessItemsThread(
                    //           table, route, feed, l_DestinationConnector, l_SourceConnector, userNo, l_InventoryBatchWise.BatchID
                    //       );

                    //        itemsThread.ProcessItems().GetAwaiter().GetResult();

                    //        Thread.Sleep(TimeSpan.FromMinutes(1));
                    //    }



                    //    //int feedIndex = 0;
                    //    //foreach (var table in DataTableChunker.Split(l_data, DataTableChunker.ChunkSize))
                    //    //{
                    //    //    feedIndex++;
                    //    //    route.SaveLog(LogTypeEnum.Info, $"Preparing feed {feedIndex} with {table.Rows.Count} items.", string.Empty, userNo);

                    //    //    var itemsThread = new AmazonProcessItemsThread(
                    //    //        table, route, feed, l_DestinationConnector, l_SourceConnector, userNo, l_InventoryBatchWise.BatchID
                    //    //    );

                    //    //    // strictly sequential
                    //    //    itemsThread.ProcessItems().GetAwaiter().GetResult();

                    //    //    // optional small pause
                    //    //    Thread.Sleep(2000);
                    //    //}


                    //    // Wait for all tasks to complete
                    //    //Task.WhenAll(tasks).GetAwaiter().GetResult();
                    //}

                    route.SaveLog(LogTypeEnum.Debug, $"Destination connector processing completed.", string.Empty, userNo);
                }

                l_InventoryBatchWise.Status = "Completed";
                l_InventoryBatchWise.FinishDate = DateTime.Now;

                l_SCSInventoryFeed.UpdateInventoryBatchWise(l_InventoryBatchWise);

                route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
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

    public class AmazonProcessItemsThread
    {
        // State information used in the task.
        private DataTable data;
        private Routes route;
        private SCSInventoryFeed feed;
        private ConnectorDataModel destinationConnector;
        private ConnectorDataModel sourceConnector;
        private int userNo;
        private string bacthID;

        // The constructor obtains the state information.
        public AmazonProcessItemsThread(DataTable data, Routes route, SCSInventoryFeed feed, ConnectorDataModel destinationConnector,
                                ConnectorDataModel sourceConnector, int userNo, string batchID)
        {
            this.data = data;
            this.route = JsonConvert.DeserializeObject<Routes>(JsonConvert.SerializeObject(route));
            this.feed = JsonConvert.DeserializeObject<SCSInventoryFeed>(JsonConvert.SerializeObject(feed));
            this.destinationConnector = destinationConnector;
            this.sourceConnector = sourceConnector;
            this.userNo = userNo;
            this.bacthID = batchID;
        }

        public async Task ProcessItems()
        {
            AmazonCreateDocumentResponseModel l_AmazonCreateDocumentResponseModel = new AmazonCreateDocumentResponseModel();
            RestResponse sourceResponse = new RestResponse();

            try
            {

                string token = GetToken(this.destinationConnector.Realm, this.destinationConnector.ConsumerKey, this.destinationConnector.ConsumerSecret, this.destinationConnector.TokenSecret).GetAwaiter().GetResult();

                this.route.UseConnection(this.sourceConnector.ConnectionString);
                this.feed.UseConnection(this.sourceConnector.ConnectionString);

                this.route.SaveData("JSON-SNT", 0, $@"{{ ""RequestURL"": ""{this.destinationConnector.BaseUrl}"" }}", userNo);

                JObject createDocumentResponse = CreateDocument(token, this.destinationConnector.BaseUrl).GetAwaiter().GetResult();

                string documentCreateResponse = createDocumentResponse.ToString(Newtonsoft.Json.Formatting.Indented);

                this.route.SaveData("JSON-RVD", 0, documentCreateResponse, userNo);

                string feedDocumentId = createDocumentResponse["feedDocumentId"].ToString();
                string feedUrl = createDocumentResponse["url"].ToString();

                string jsonrequest = GenerateInventoryFeedXml(this.data, this.feed, this.sourceConnector.ConnectionString, this.bacthID);

                if (!string.IsNullOrWhiteSpace(jsonrequest))
                {
                    this.route.SaveData("JSON-SNT", 0, $@"{{ ""FeedURL"": ""{feedUrl}"" }}", userNo);

                    var createUploadDocument = UploadDocument(feedUrl, jsonrequest).GetAwaiter().GetResult();

                    this.route.SaveData("JSON-RVD", 0, createUploadDocument.ToString(), userNo);

                    this.route.SaveData("JSON-SNT", 0, jsonrequest, userNo);

                    //Thread.Sleep(1000);

                    JObject l_responseSubmitFeed = await SubmitFeed(feedDocumentId, token, this.destinationConnector.BaseUrl);
                    string submitFeedResponse = l_responseSubmitFeed.ToString(Newtonsoft.Json.Formatting.Indented);

                    string feedId = l_responseSubmitFeed["feedId"].ToString();

                    this.route.SaveData("JSON-RVD", 0, submitFeedResponse, this.userNo);

                    DataTable bulkInsertTable = CreateBulkInsertDataTable();

                    foreach (DataRow row in this.data.Rows)
                    {
                        DataRow bulkRow = bulkInsertTable.NewRow();
                        bulkRow["CustomerId"] = row["CustomerId"];
                        bulkRow["ItemId"] = row["ItemId"];
                        bulkRow["Type"] = "JSON-RVD"; 
                        bulkRow["Data"] = submitFeedResponse;
                        bulkRow["CreatedDate"] = DateTime.Now;
                        bulkRow["CreatedBy"] = 1;
                        bulkRow["BatchID"] = this.bacthID;
                        bulkInsertTable.Rows.Add(bulkRow);

                        feed.UpdateItemStatus(row["ItemId"].ToString(), row["CustomerId"].ToString());
                    }
                    
                    feed.BulkNewInsertData(this.sourceConnector.ConnectionString, "SCSInventoryFeedData", bulkInsertTable);
                    this.feed.InsertInventoryBatchWiseFeedDetail(this.bacthID, "NEW", feedId, this.destinationConnector.CustomerID);
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }

            

        }

        //public void ProcessItem(DataRow row)
        //{
        //    RestResponse sourceResponse = new RestResponse();
        //    string customerId = row["CustomerId"].ToString();
        //    string itemId = row["ItemId"].ToString();
        //    var data = new
        //    {
        //        quantity = row["Total_ATS"],
        //    };

        //    try
        //    {
        //        string Body = JsonConvert.SerializeObject(data);

        //        this.route.UseConnection(this.sourceConnector.ConnectionString);
        //        this.feed.UseConnection(this.sourceConnector.ConnectionString);

        //        this.route.SaveData("JSON-SNT", 0, Body, userNo);
        //        this.feed.SaveData("JSON-SNT", customerId, itemId, Body, this.userNo, this.bacthID);

        //        this.destinationConnector.Url = this.destinationConnector.BaseUrl + row["ProductId"] + "/quantities/" + this.destinationConnector.Realm;
        //        sourceResponse = RestConnector.Execute(this.destinationConnector, Body).GetAwaiter().GetResult();

        //        InventoryUpdateResponseModel reponse = new InventoryUpdateResponseModel();

        //        try
        //        {
        //            reponse = JsonConvert.DeserializeObject<InventoryUpdateResponseModel>(sourceResponse.Content);
        //        }
        //        catch (Exception)
        //        {

        //        }

        //        if (reponse?.quantity == Convert.ToInt32(row["Total_ATS"].ToString()))
        //        {
        //            this.feed.UpdateItemStatus(itemId, customerId);
        //            this.route.SaveLog(LogTypeEnum.Debug, $"SCSUpdateInventory updated for item [{row["ProductId"]}].", sourceResponse.Content, this.userNo);
        //        }
        //        else
        //        {
        //            this.route.SaveLog(LogTypeEnum.Error, $"Unable to update SCSUpdateInventory for item [{row["ProductId"]}].", sourceResponse.Content, this.userNo);
        //        }

        //        this.route.SaveData("JSON-RVD", 0, sourceResponse.Content, this.userNo);
        //        this.feed.SaveData("JSON-RVD", customerId, itemId, sourceResponse.Content, this.userNo, this.bacthID);
        //    }
        //    catch (Exception ex)
        //    {
        //        this.route.SaveLog(LogTypeEnum.Exception, $"Unable to update SCSUpdateInventory for item [{row["ProductId"]}].", ex.ToString(), this.userNo);
        //    }
        //}

        private static string GenerateInventoryFeedXml(DataTable inventoryData, SCSInventoryFeed feed,string ConnectionString , string batchID)
        {
            StringBuilder xml = new StringBuilder();
            feed.UseConnection(ConnectionString);

            try
            {
                DataTable bulkInsertTable = CreateBulkInsertDataTable();
                AmazonInventoryRequestModel l_AmazonInventoryRequestModel = new AmazonInventoryRequestModel();

                l_AmazonInventoryRequestModel.header.sellerId = "A3NX96R6O9JSG4";
                l_AmazonInventoryRequestModel.header.version = "2.0";
                l_AmazonInventoryRequestModel.header.issueLocale = "en_US";
                int messageId = 1;

                foreach (DataRow row in inventoryData.Rows)
                {
                    var l_Message = new AmazonMessage
                    {
                        messageId = messageId,
                        sku = row["CustomerItemCode"]?.ToString(),         
                        operationType = "PARTIAL_UPDATE",
                        productType = "PRODUCT"
                    };

                    var l_Fulfillment_Availability = new Fulfillment_Availability
                    {
                        fulfillment_channel_code = "DEFAULT",
                        quantity = Convert.ToInt64(row["Total_ATS"]),
                        lead_time_to_ship_max_days = 1
                    };

                    l_Message.attributes.fulfillment_availability
                             .Add(l_Fulfillment_Availability);

                    
                    l_AmazonInventoryRequestModel.messages.Add(l_Message);
                    var perItemPayload = new
                    {
                        header = l_AmazonInventoryRequestModel.header,
                        messages = new List<AmazonMessage> { l_Message }
                    };

                    string perItemJson = JsonConvert.SerializeObject(
                        perItemPayload,
                        Formatting.None
                    );

                    DataRow bulkRow = bulkInsertTable.NewRow();
                    bulkRow["CustomerId"] = row["CustomerId"];
                    bulkRow["ItemId"] = row["ItemId"];
                    bulkRow["Type"] = "JSON-SNT"; 
                    bulkRow["Data"] = perItemJson; 
                    bulkRow["CreatedDate"] = DateTime.Now;
                    bulkRow["CreatedBy"] = 1; 
                    bulkRow["BatchID"] = batchID;

                    bulkInsertTable.Rows.Add(bulkRow);

                    messageId++;
                }


                string fullFeedJson = JsonConvert.SerializeObject(
                        l_AmazonInventoryRequestModel,
                        Formatting.None
                    );

                feed.BulkNewInsertData(ConnectionString, "SCSInventoryFeedData", bulkInsertTable);

                return fullFeedJson;
            }
            catch (Exception  ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }


            
        }

        public static DataTable CreateBulkInsertDataTable()
        {
            // Create a new DataTable to hold the necessary columns
            DataTable bulkInsertTable = new DataTable();

            // Add the required columns
            bulkInsertTable.Columns.Add("CustomerId", typeof(string));
            bulkInsertTable.Columns.Add("ItemId", typeof(string));
            bulkInsertTable.Columns.Add("Type", typeof(string));
            bulkInsertTable.Columns.Add("Data", typeof(string));
            bulkInsertTable.Columns.Add("CreatedDate", typeof(DateTime));
            bulkInsertTable.Columns.Add("CreatedBy", typeof(int));
            bulkInsertTable.Columns.Add("BatchID", typeof(string));

            return bulkInsertTable;
        }

        async static Task<string> GetToken(string l_applicationID,string l_client_id,string l_client_secret,string l_refresh_token)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.amazon.com/auth/o2/token?application_id={l_applicationID}&client_id={l_client_id}&client_secret={l_client_secret}&refresh_token={l_refresh_token}&grant_type=refresh_token");

            try
            {
                request.Headers.Add("contentType", "application/x-www-form-urlencoded");
                var content = new StringContent(string.Empty);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                request.Content = content;
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                string responseContent = await response.Content.ReadAsStringAsync();
                JObject jsonResponse = JObject.Parse(responseContent);

                return jsonResponse["access_token"].ToString();

            }
            catch (Exception)
            {

                throw;
            }
        }

        //async static Task<JObject> CreateDocument(string token, string l_baseurl)
        //{
        //    var client = new HttpClient();
        //    var request = new HttpRequestMessage(HttpMethod.Post, $"{l_baseurl}/feeds/2021-06-30/documents");

        //    try
        //    {
        //        request.Headers.Add("contentType", "application/json");
        //        request.Headers.Add("x-amz-access-token", token);

        //        var body = new JObject
        //        {
        //            ["contentType"] = "application/json; charset=UTF-8"
        //        };

        //        request.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");
        //        var response = await client.SendAsync(request);
        //        response.EnsureSuccessStatusCode();

        //        string responseContent = await response.Content.ReadAsStringAsync();
        //        JObject jsonResponse = JObject.Parse(responseContent);
        //        return jsonResponse;
        //    }
        //    catch (Exception)
        //    {

        //        throw;
        //    }
        //}

        async static Task<JObject> CreateDocument(string token, string baseUrl)
        {
            using var client = new HttpClient();
            var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/feeds/2021-06-30/documents");
            req.Headers.Add("x-amz-access-token", token);

            var body = new JObject { ["contentType"] = "application/json; charset=UTF-8" };
            req.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");

            var res = await client.SendAsync(req);
            res.EnsureSuccessStatusCode();
            return JObject.Parse(await res.Content.ReadAsStringAsync());
        }
        //async static Task<JObject> CreateDocument(string token, string l_baseurl, bool useGzip = false)
        //{
        //    using var client = new HttpClient();
        //    var request = new HttpRequestMessage(HttpMethod.Post, $"{l_baseurl}/feeds/2021-06-30/documents");

        //    // LWA token
        //    request.Headers.Add("x-amz-access-token", token);

        //    var body = new JObject
        //    {
        //        ["contentType"] = "application/json; charset=UTF-8"
        //    };
        //    if (useGzip)
        //        body["compressionAlgorithm"] = "GZIP";

        //    request.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");

        //    var response = await client.SendAsync(request);
        //    response.EnsureSuccessStatusCode();

        //    string responseContent = await response.Content.ReadAsStringAsync();
        //    return JObject.Parse(responseContent);
        //}


        //async static Task<HttpStatusCode> UploadDocument( string url, string xmlrequest)
        //{
        //    using var client = new HttpClient();

        //    byte[] xmlBytes = Encoding.UTF8.GetBytes(xmlrequest);

        //    using var content = new ByteArrayContent(xmlBytes);
        //    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        //    using var request = new HttpRequestMessage(HttpMethod.Put, url)
        //    {
        //        Content = content
        //    };

        //    using var response = await client.SendAsync(request);

        //    return (HttpStatusCode)response.StatusCode;
        //}
        async static Task<HttpStatusCode> UploadDocument(string url, string json)
        {
            using var client = new HttpClient();
            using var content = new StringContent(json, Encoding.UTF8);
            content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json; charset=UTF-8");

            using var req = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };
            using var res = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            return (HttpStatusCode)res.StatusCode;
        }

        //async static Task<HttpStatusCode> UploadDocumentGzipAsync(string url, string jsonPayload)
        //{
        //    using var client = new HttpClient();

        //    byte[] raw = Encoding.UTF8.GetBytes(jsonPayload);
        //    using var ms = new MemoryStream();
        //    using (var gzip = new GZipStream(ms, CompressionLevel.Optimal, leaveOpen: true))
        //        gzip.Write(raw, 0, raw.Length);
        //    ms.Position = 0;

        //    using var content = new StreamContent(ms);
        //    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        //    content.Headers.ContentEncoding.Add("gzip");

        //    using var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };
        //    using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        //    return (HttpStatusCode)response.StatusCode;
        //}

        private static async Task<JObject> SubmitFeed(string feedDocumentId, string token,string l_baseurl)
        {
            var client = new HttpClient();
            // Set the destination URL for the Submit Feed API endpoint
            var request = new HttpRequestMessage(HttpMethod.Post, $"{l_baseurl}/feeds/2021-06-30/feeds");

            try
            {
                request.Headers.Add("contentType", "application/json");
                request.Headers.Add("x-amz-access-token", token);

                var requestBody = new
                {
                    feedType = "JSON_LISTINGS_FEED",  
                    inputFeedDocumentId = feedDocumentId,  
                    marketplaceIds = new[] { "ATVPDKIKX0DER" },
                    feedOptions = new { } 
                };

                string requestBodyString = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(requestBodyString, Encoding.UTF8, "application/json");
                
                request.Content = content;

                var response = await client.SendAsync(request);

                response.EnsureSuccessStatusCode();

                string responseContent = await response.Content.ReadAsStringAsync();

                JObject jsonResponse = JObject.Parse(responseContent);

                //return jsonResponse["feedId"].ToString();

                return jsonResponse;
            }
            catch (Exception)
            {

                throw;
            }
        }

    }

    public static class DataTableChunker
    {
        public const int ChunkSize = 25_000;

        public static IEnumerable<DataTable> Split(DataTable source, int chunkSize = ChunkSize)
        {
            if (source == null || source.Rows.Count == 0) yield break;
            int total = source.Rows.Count;
            for (int i = 0; i < total; i += chunkSize)
            {
                int take = Math.Min(chunkSize, total - i);
                var slice = source.AsEnumerable().Skip(i).Take(take);
                yield return slice.CopyToDataTable();
            }
        }
    }
}
