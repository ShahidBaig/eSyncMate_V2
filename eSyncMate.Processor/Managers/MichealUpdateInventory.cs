using eSyncMate.DB.Entities;
using eSyncMate.DB;
using eSyncMate.Processor.Models;
using Newtonsoft.Json;
using RestSharp;
using static eSyncMate.DB.Declarations;
using eSyncMate.Processor.Connections;
using System.Data;
using static eSyncMate.Processor.Models.MichealInventoryUploadRequestModel;
using static eSyncMate.Processor.Models.MacysInventoryUploadRequestModel;

namespace eSyncMate.Processor.Managers
{
    public class MichealUpdateInventory
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
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ROUTETYPEID@", Convert.ToString(RouteTypesEnum.MichealInventoryUpload));
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
                    l_InventoryBatchWise.RouteType = RouteTypesEnum.MichealInventoryUpload.ToString();
                    l_InventoryBatchWise.CustomerID = l_SourceConnector.CustomerID;

                    l_SCSInventoryFeed.InsertInventoryBatchWise(l_InventoryBatchWise);

                    if (l_data.Rows.Count <= 50)
                    {
                        ProcessMichleItemPricesThread itemsThread = new ProcessMichleItemPricesThread(l_data, route, feed, l_DestinationConnector, l_SourceConnector, userNo, l_InventoryBatchWise.BatchID);

                        itemsThread.ProcessItems();
                    }
                    else
                    {
                        int i = 0;
                        int totalThread = CommonUtils.UploadInventoryTotalThread;
                        int chunkSize = l_data.Rows.Count / totalThread;
                        List<Thread> threads = new List<Thread>();

                        var tables = l_data.AsEnumerable().ToChunks(chunkSize)
                          .Select(rows => rows.CopyToDataTable()).ToList<DataTable>();

                        while (i < tables.Count)
                        {
                            ProcessMichleItemPricesThread itemsThread = new ProcessMichleItemPricesThread(tables[i], route, feed, l_DestinationConnector, l_SourceConnector, userNo, l_InventoryBatchWise.BatchID);

                            Thread t = new Thread(new ThreadStart(itemsThread.ProcessItems));
                            threads.Add(t);

                            i++;
                        }

                        foreach (Thread t in threads)
                        {
                            t.Start();
                        }
                        foreach (Thread t in threads)
                        {
                            t.Join();
                        }
                    }

                    route.SaveLog(LogTypeEnum.Debug, $"Destination connector processing completed.", string.Empty, userNo);
                }

                l_InventoryBatchWise.Status = "Completed";
                l_InventoryBatchWise.FinishDate = DateTime.Now;

                l_SCSInventoryFeed.UpdateInventoryBatchWise(l_InventoryBatchWise);

                route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);

                //if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString() && l_data.Rows.Count > 0)
                //{
                //    route.SaveLog(LogTypeEnum.Debug, $"Destination connector processing start...", string.Empty, userNo);

                //    SCSInventoryFeed feed = new SCSInventoryFeed();

                //    feed.UseConnection(l_SourceConnector.ConnectionString);
                //    l_SCSInventoryFeed.UseConnection(l_SourceConnector.ConnectionString);

                //    l_InventoryBatchWise.StartDate = Convert.ToDateTime(DateTime.Now);
                //    l_InventoryBatchWise.Status = "Processing";
                //    l_InventoryBatchWise.RouteType = RouteTypesEnum.MichealInventoryUpload.ToString();
                //    l_InventoryBatchWise.CustomerID = l_SourceConnector.CustomerID;

                //    l_SCSInventoryFeed.InsertInventoryBatchWise(l_InventoryBatchWise);

                //    foreach (DataRow row in l_data.Rows)
                //    {
                //        string customerId = row["CustomerId"].ToString();
                //        string itemId = row["ItemId"].ToString();


                //        l_MichealInventoryUploadRequestModel.Add(new MichealInventoryUploadRequestModel
                //        {
                //            skuNumber = row["ItemId"].ToString(),
                //            availableQuantity = Convert.ToInt64(row["Total_ATS"])
                //        });

                //        Body = JsonConvert.SerializeObject(l_MichealInventoryUploadRequestModel);

                //        route.SaveData("JSON-SNT", 0, Body, userNo);
                //        feed.SaveData("JSON-SNT", customerId, itemId, Body, userNo, l_InventoryBatchWise.BatchID);
                //    }


                //    Body = JsonConvert.SerializeObject(l_MichealInventoryUploadRequestModel);

                //    route.UseConnection(l_SourceConnector.ConnectionString);
                //    feed.UseConnection(l_SourceConnector.ConnectionString);


                //    l_DestinationConnector.Url = l_DestinationConnector.BaseUrl + "/listing/inventory/update-inventory";
                //    sourceResponse = RestConnector.Execute(l_DestinationConnector, Body).GetAwaiter().GetResult();

                //    if (sourceResponse.StatusCode == System.Net.HttpStatusCode.Created)
                //    {
                //        foreach (DataRow l_row in l_data.Rows)
                //        {
                //            feed.UpdateItemStatus(l_row["ItemId"].ToString(), l_row["CustomerId"].ToString());
                //            route.SaveLog(LogTypeEnum.Debug, $"MichealUpdateInventory updated for item [{l_row["ProductId"]}].", sourceResponse.Content, userNo);
                //            feed.SaveData("JSON-RVD", l_row["CustomerId"].ToString(), l_row["ItemId"].ToString(), sourceResponse.Content, userNo, l_InventoryBatchWise.BatchID);
                //        }

                //        l_InventoryBatchWise.Status = "Completed";
                //        l_InventoryBatchWise.FinishDate = DateTime.Now;

                //        l_SCSInventoryFeed.UpdateInventoryBatchWise(l_InventoryBatchWise);

                //        route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);

                //        route.SaveLog(LogTypeEnum.Debug, $"Destination connector processing completed.", string.Empty, userNo);
                //    }
                //    else
                //    {
                //        route.SaveLog(LogTypeEnum.Error, $"Unable to update MichealUpdateInventory for items.", sourceResponse.Content, userNo);
                //    }

                //    route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);
                //}
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Unable to update MichealUpdateInventory for items.", ex.ToString(), userNo);
            }

        }
    }

    public class ProcessMichleItemPricesThread
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
        public ProcessMichleItemPricesThread(DataTable data, Routes route, SCSInventoryFeed feed, ConnectorDataModel destinationConnector,
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

        public void ProcessItems()
        {
            foreach (DataRow row in this.data.Rows)
            {
                this.ProcessItem(row);

                Thread.Sleep(400);
            }
        }

        public void ProcessItem(DataRow row)
        {
            RestResponse sourceResponse = new RestResponse();
            string customerId = row["CustomerId"].ToString();
            string itemId = row["ItemId"].ToString();
            List<MichealInventoryUploadRequestModel> l_MichealInventoryUploadRequestModel = new List<MichealInventoryUploadRequestModel>();
           
            try
            {

                l_MichealInventoryUploadRequestModel.Add(new MichealInventoryUploadRequestModel
                {
                    sellerSkuNumber = row["ItemId"].ToString(),
                    availableQuantity = Convert.ToInt64(row["Total_ATS"])
                });


                string Body = JsonConvert.SerializeObject(l_MichealInventoryUploadRequestModel);

                this.route.UseConnection(this.sourceConnector.ConnectionString);
                this.feed.UseConnection(this.sourceConnector.ConnectionString);

                this.route.SaveData("JSON-SNT", 0, Body, userNo);
                this.feed.SaveData("JSON-SNT", customerId, itemId, Body, this.userNo, this.bacthID);

                this.destinationConnector.Url = this.destinationConnector.BaseUrl + "/listing/inventory/update-inventory-by-seller-sku-number";
                sourceResponse = RestConnector.Execute(this.destinationConnector, Body).GetAwaiter().GetResult();

                if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK || sourceResponse.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    this.feed.UpdateItemStatus(itemId, customerId);
                    this.route.SaveLog(LogTypeEnum.Debug, $"MichealUpdateInventory updated for item [{row["ProductId"]}].", sourceResponse.Content, this.userNo);
                }
                else
                {
                    this.route.SaveLog(LogTypeEnum.Error, $"Unable to update MichealUpdateInventory for item [{row["ProductId"]}].", sourceResponse.Content, this.userNo);

                }

                this.route.SaveData("JSON-RVD", 0, sourceResponse.Content, this.userNo);
                this.feed.SaveData("JSON-RVD", customerId, itemId, sourceResponse.Content, this.userNo, this.bacthID);
            }
            catch (Exception ex)
            {
                this.route.SaveLog(LogTypeEnum.Exception, $"Unable to update MichealUpdateInventory for item [{row["ProductId"]}].", ex.ToString(), this.userNo);
            }
        }
    }
}

