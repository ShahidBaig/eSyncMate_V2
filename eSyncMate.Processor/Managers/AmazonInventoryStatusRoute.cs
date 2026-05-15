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
using static eSyncMate.Processor.Models.SCSPlaceOrderResponseModel;
using Microsoft.SqlServer.Server;
using Nancy;
using static eSyncMate.Processor.Models.SCS_ProductTypeAttributeReponseModel;
using Microsoft.AspNetCore.Mvc;
using static eSyncMate.Processor.Models.SCS_VAPProductCatalogModel;
using static eSyncMate.Processor.Models.SCS_ProductCatalogStatusResponseModel;
using System.Net.Http.Json;
using static eSyncMate.Processor.Models.MacysInventoryUploadRequestModel;
using System.Text.Json;
using System.Text;
using System.IO.Compression;
using Microsoft.AspNetCore.Http.HttpResults;


namespace eSyncMate.Processor.Managers
{
    public class AmazonInventoryStatusRoute
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
            CustomerProductCatalog l_CustomerProductCatalog = new CustomerProductCatalog();
            MacysInventoryUploadRequestModel l_MacysInventoryUploadRequestModel = new MacysInventoryUploadRequestModel();

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

                if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                {
                    route.SaveLog(LogTypeEnum.Debug, $"Source connector processing start...", string.Empty, userNo);

                    DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);

                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@CUSTOMERID@", l_SourceConnector.CustomerID);
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ROUTETYPEID@", Convert.ToString(RouteTypesEnum.AmazonInventoryStatus));
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@USERNO@", Convert.ToString(userNo));

                    if (l_SourceConnector.CommandType == "SP")
                    {
                        connection.GetDataSP(l_SourceConnector.Command, ref l_data);
                    }

                    route.SaveLog(LogTypeEnum.Debug, $"Source connector processing completed", string.Empty, userNo);
                }

                if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString() && l_data.Rows.Count > 0)
                {
                    route.SaveLog(LogTypeEnum.Debug, $"Destination connector processing start...", string.Empty, userNo);


                    foreach (DataRow item in l_data.Rows)
                    {
                        string feedDocumentId = Convert.ToString(item["FeedDocumentID"]);

                        if (string.IsNullOrWhiteSpace(feedDocumentId))
                        {
                            route.SaveLog(LogTypeEnum.Error, $"Skipping status check — FeedDocumentID is empty for BatchID [{item["BatchID"]}].", string.Empty, userNo);
                            continue;
                        }

                        // Rate limit: GET /feeds/{feedId} = 2 req/sec
                        Thread.Sleep(TimeSpan.FromSeconds(2));

                        l_DestinationConnector.Url = l_DestinationConnector.BaseUrl + $"/feeds/2021-06-30/feeds/{feedDocumentId}";
                        route.SaveData("JSON-SNT", 0, l_DestinationConnector.Url, userNo);

                        sourceResponse = RestConnector.Execute(l_DestinationConnector, Body).GetAwaiter().GetResult();

                        // Handle 429 TooManyRequests — wait 60s and retry once
                        if ((int)sourceResponse.StatusCode == 429)
                        {
                            route.SaveLog(LogTypeEnum.Debug, $"Rate limited on getFeed [{feedDocumentId}] — waiting 60s before retry.", string.Empty, userNo);
                            Thread.Sleep(TimeSpan.FromSeconds(60));
                            sourceResponse = RestConnector.Execute(l_DestinationConnector, Body).GetAwaiter().GetResult();
                        }

                        if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            string l_Content = string.Empty;
                            AmazonInventoryStatusResponseModel l_AmazonInventoryStatusResponseModel = new AmazonInventoryStatusResponseModel();
                            l_AmazonInventoryStatusResponseModel = JsonConvert.DeserializeObject<AmazonInventoryStatusResponseModel>(sourceResponse.Content);

                            string processingStatus = l_AmazonInventoryStatusResponseModel.processingStatus ?? string.Empty;

                            route.SaveLog(LogTypeEnum.Debug, $"Feed [{feedDocumentId}] status: {processingStatus}.", string.Empty, userNo);

                            if (processingStatus == "IN_QUEUE" || processingStatus == "IN_PROGRESS")
                            {
                                // Still processing — skip, next scheduled run will check again
                                route.SaveLog(LogTypeEnum.Debug, $"Feed [{feedDocumentId}] still {processingStatus} — skipping, will check next run.", string.Empty, userNo);
                            }
                            else if (processingStatus == "FATAL" || processingStatus == "CANCELLED")
                            {
                                // Permanently failed — mark as Error in DB, no document to fetch
                                route.SaveLog(LogTypeEnum.Error, $"Feed [{feedDocumentId}] {processingStatus} — marking batch as Error.", string.Empty, userNo);
                                l_CustomerProductCatalog.UseConnection(l_SourceConnector.ConnectionString);
                                l_CustomerProductCatalog.UpdateInventoryBacthwiseStatus(Convert.ToString(item["BatchID"]), feedDocumentId, "Error", l_SourceConnector.CustomerID, sourceResponse.Content);
                            }
                            else if (processingStatus == "DONE" && !string.IsNullOrEmpty(l_AmazonInventoryStatusResponseModel.resultFeedDocumentId))
                            {
                                // DONE — fetch result document (rate limit: 1 per 45 sec)
                                Thread.Sleep(TimeSpan.FromSeconds(45));

                                l_DestinationConnector.Url = l_DestinationConnector.BaseUrl + $"/feeds/2021-06-30/documents/{l_AmazonInventoryStatusResponseModel.resultFeedDocumentId}";
                                route.SaveData("JSON-SNT", 0, l_DestinationConnector.Url, userNo);

                                sourceResponse = RestConnector.Execute(l_DestinationConnector, Body).GetAwaiter().GetResult();

                                // Handle 429 on getFeedDocument — wait 60s and retry once
                                if ((int)sourceResponse.StatusCode == 429)
                                {
                                    route.SaveLog(LogTypeEnum.Debug, $"Rate limited on getFeedDocument [{feedDocumentId}] — waiting 60s before retry.", string.Empty, userNo);
                                    Thread.Sleep(TimeSpan.FromSeconds(60));
                                    sourceResponse = RestConnector.Execute(l_DestinationConnector, Body).GetAwaiter().GetResult();
                                }

                                AmazonInventoryFeedDocumentResponseModel l_AmazonInventoryFeedDocumentResponseModel = new AmazonInventoryFeedDocumentResponseModel();
                                AmazonInventoryFeedReportDownloadResponseModel l_AmazonInventoryFeedReportDownloadResponseModel = new AmazonInventoryFeedReportDownloadResponseModel();

                                if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    l_AmazonInventoryFeedDocumentResponseModel = JsonConvert.DeserializeObject<AmazonInventoryFeedDocumentResponseModel>(sourceResponse.Content);

                                    if (!string.IsNullOrEmpty(l_AmazonInventoryFeedDocumentResponseModel.url))
                                    {
                                        Thread.Sleep(TimeSpan.FromSeconds(30));

                                        l_Content = ReadFeedIssuesAsync(l_AmazonInventoryFeedDocumentResponseModel.url).GetAwaiter().GetResult();

                                        l_AmazonInventoryFeedReportDownloadResponseModel = JsonConvert.DeserializeObject<AmazonInventoryFeedReportDownloadResponseModel>(l_Content);
                                        l_CustomerProductCatalog.UseConnection(l_SourceConnector.ConnectionString);

                                        if (l_AmazonInventoryFeedReportDownloadResponseModel.issues.Count > 0)
                                        {
                                            foreach (var issue in l_AmazonInventoryFeedReportDownloadResponseModel.issues)
                                            {
                                                l_CustomerProductCatalog.UpdateStatusSCSInventoryFeed(l_SourceConnector.CustomerID, item["BatchID"].ToString(), feedDocumentId, Convert.ToInt64(issue.messageId));
                                            }
                                        }
                                    }

                                    route.SaveLog(LogTypeEnum.Debug, $"Amazon Inventory Status updated for FeedDocumentID [{feedDocumentId}].", string.Empty, userNo);

                                    l_CustomerProductCatalog.UseConnection(l_SourceConnector.ConnectionString);
                                    l_CustomerProductCatalog.UpdateInventoryBacthwiseStatus(Convert.ToString(item["BatchID"]), feedDocumentId, "Completed", l_SourceConnector.CustomerID, l_Content);
                                }
                            }
                        }
                        else
                        {
                            route.SaveLog(LogTypeEnum.Error, $"Unable to get Amazon Inventory Status for FeedDocumentID [{feedDocumentId}]. HTTP {(int)sourceResponse.StatusCode} {sourceResponse.StatusCode}.", sourceResponse.Content ?? sourceResponse.ErrorMessage, userNo);
                        }

                        route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);
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


        static async Task<string> DownloadAndGunzipAsync(string url)
        {
            try
            {
                var bytes = await SharedHttpClientFactory.Amazon.GetByteArrayAsync(url);

                using var input = new MemoryStream(bytes);
                using var gzip = new GZipStream(input, CompressionMode.Decompress);
                using var reader = new StreamReader(gzip, Encoding.UTF8);

                return await reader.ReadToEndAsync();
            }
            catch (Exception)
            {
                throw;
            }
           
        }

        static async Task<string> ReadFeedIssuesAsync(string url)
        {
            try
            {
                var text = await DownloadAndGunzipAsync(url);


                return text;

            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
