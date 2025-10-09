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


namespace eSyncMate.Processor.Managers
{
    public class AmazonInventoryStatusRoute
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
            CustomerProductCatalog l_CustomerProductCatalog = new CustomerProductCatalog();
            MacysInventoryUploadRequestModel l_MacysInventoryUploadRequestModel = new MacysInventoryUploadRequestModel();

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
                        l_DestinationConnector.Url = l_DestinationConnector.BaseUrl + $"/feeds/2021-06-30/feeds/{Convert.ToString(l_data.Rows[0]["FeedDocumentID"])}";
                        route.SaveData("JSON-SNT", 0, l_DestinationConnector.Url, userNo);

                        sourceResponse = RestConnector.Execute(l_DestinationConnector, Body).GetAwaiter().GetResult();

                        if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            AmazonInventoryStatusResponseModel l_AmazonInventoryStatusResponseModel = new AmazonInventoryStatusResponseModel();

                            l_AmazonInventoryStatusResponseModel = JsonConvert.DeserializeObject<AmazonInventoryStatusResponseModel>(sourceResponse.Content);

                            route.SaveLog(LogTypeEnum.Debug, $"Amazon Inventory Status updated for FeedDocumentID [{item["FeedDocumentID"]}].", string.Empty, userNo);

                            l_CustomerProductCatalog.UseConnection(l_SourceConnector.ConnectionString);
                            l_CustomerProductCatalog.UpdateInventoryBacthwiseStatus(Convert.ToString(item["BatchID"]), Convert.ToString(item["FeedDocumentID"]), l_AmazonInventoryStatusResponseModel.processingStatus, l_SourceConnector.CustomerID);

                        }
                        else
                        {
                            route.SaveLog(LogTypeEnum.Error, $"Unable to Amazon Inventory Status for FeedDocumentID.", string.Empty, userNo);
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
    }
}
