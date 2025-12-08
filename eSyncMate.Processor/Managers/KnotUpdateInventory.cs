using eSyncMate.DB.Entities;
using eSyncMate.DB;
using eSyncMate.Processor.Models;
using Newtonsoft.Json;
using RestSharp;
using static eSyncMate.DB.Declarations;
using eSyncMate.Processor.Connections;
using System.Data;
using static eSyncMate.Processor.Models.KnotInventoryUploadRequestModel;

namespace eSyncMate.Processor.Managers
{
    public class KnotUpdateInventory
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
            KnotInventoryUploadRequestModel l_KnotInventoryUploadRequestModel = new KnotInventoryUploadRequestModel();
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
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ROUTETYPEID@", Convert.ToString(RouteTypesEnum.KnotInventoryUpload));
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
                    l_InventoryBatchWise.RouteType = RouteTypesEnum.KnotInventoryUpload.ToString();
                    l_InventoryBatchWise.CustomerID = l_SourceConnector.CustomerID;

                    l_SCSInventoryFeed.InsertInventoryBatchWise(l_InventoryBatchWise);

                    foreach (DataRow row in l_data.Rows)
                    {
                        string customerId = row["CustomerId"].ToString();
                        string itemId = row["ItemId"].ToString();
                        KnotOffer l_offers = new KnotOffer();

                        l_offers.price = Convert.ToDouble(row["ListPrice"]);
                        l_offers.product_sku = row["CustomerItemCode"].ToString();
                        l_offers.quantity = row["Total_ATS"].ToString();
                        l_offers.shop_sku = row["ItemId"].ToString();
                        l_offers.state_code = "11";
                        l_KnotInventoryUploadRequestModel.offers.Add(l_offers);

                        Body = JsonConvert.SerializeObject(l_KnotInventoryUploadRequestModel);

                        route.SaveData("JSON-SNT", 0, Body, userNo);
                        feed.SaveData("JSON-SNT", customerId, itemId, Body, userNo, l_InventoryBatchWise.BatchID);
                    }


                    Body = JsonConvert.SerializeObject(l_KnotInventoryUploadRequestModel);

                    route.UseConnection(l_SourceConnector.ConnectionString);
                    feed.UseConnection(l_SourceConnector.ConnectionString);


                    l_DestinationConnector.Url = l_DestinationConnector.BaseUrl + "/api/offers";
                    sourceResponse = RestConnector.Execute(l_DestinationConnector, Body).GetAwaiter().GetResult();

                    if (sourceResponse.StatusCode == System.Net.HttpStatusCode.Created)
                    {
                        foreach (DataRow l_row in l_data.Rows)
                        {
                            feed.UpdateItemStatus(l_row["ItemId"].ToString(), l_row["CustomerId"].ToString());
                            route.SaveLog(LogTypeEnum.Debug, $"KnotUpdateInventory updated for item [{l_row["ProductId"]}].", sourceResponse.Content, userNo);
                            feed.SaveData("JSON-RVD", l_row["CustomerId"].ToString(), l_row["ItemId"].ToString(), sourceResponse.Content, userNo, l_InventoryBatchWise.BatchID);
                        }

                        l_InventoryBatchWise.Status = "Completed";
                        l_InventoryBatchWise.FinishDate = DateTime.Now;

                        l_SCSInventoryFeed.UpdateInventoryBatchWise(l_InventoryBatchWise);

                        route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);

                        route.SaveLog(LogTypeEnum.Debug, $"Destination connector processing completed.", string.Empty, userNo);
                    }
                    else
                    {
                        route.SaveLog(LogTypeEnum.Error, $"Unable to update KnotUpdateInventory for items.", sourceResponse.Content, userNo);
                    }

                    route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);
                }
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Unable to update KnotUpdateInventory for items.", ex.ToString(), userNo);
            }

        }
    }

}

