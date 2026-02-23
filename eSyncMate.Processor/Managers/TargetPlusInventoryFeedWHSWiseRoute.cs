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
using static eSyncMate.Processor.Models.TargetPlusInventoryFeedWHSWiseRequestModel;

namespace eSyncMate.Processor.Managers
{
    public class TargetPlusInventoryFeedWHSWiseRoute
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
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ROUTETYPEID@", Convert.ToString(RouteTypesEnum.TargetPlusInventoryFeedWHSWise));
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@USERNO@", Convert.ToString(userNo));

                    if (l_SourceConnector.CommandType == "SP")
                    {
                        connection.GetDataSP(l_SourceConnector.Command, ref l_data);
                    }

                    route.SaveLog(LogTypeEnum.Debug, $"Source connector processing completed.", string.Empty, userNo);
                }

                // Set connection before the if block so it's available for UpdateInventoryBatchWise
                l_SCSInventoryFeed.UseConnection(l_SourceConnector.ConnectionString);

                if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString() && l_data.Rows.Count > 0)
                {
                    route.SaveLog(LogTypeEnum.Debug, $"Destination connector processing start...", string.Empty, userNo);

                    SCSInventoryFeed feed = new SCSInventoryFeed();

                    feed.UseConnection(l_SourceConnector.ConnectionString);

                    l_InventoryBatchWise.StartDate = Convert.ToDateTime(DateTime.Now);
                    l_InventoryBatchWise.Status = "Processing";
                    l_InventoryBatchWise.RouteType = RouteTypesEnum.TargetPlusInventoryFeedWHSWise.ToString();
                    l_InventoryBatchWise.CustomerID = l_SourceConnector.CustomerID;

                    l_SCSInventoryFeed.InsertInventoryBatchWise(l_InventoryBatchWise);

                    //if (l_data.Rows.Count <= 100)
                    //{
                    //    ProcessItemsWHSWiseThread itemsThread = new ProcessItemsWHSWiseThread(l_data, route, feed, l_DestinationConnector, l_SourceConnector, userNo, l_InventoryBatchWise.BatchID);

                    //    itemsThread.ProcessItems();
                    //}
                    //else
                    //{
                    //    int i = 0;
                    //    int totalThread = 300;
                    //    int chunkSize = l_data.Rows.Count / totalThread;
                    //    List<Thread> threads = new List<Thread>();

                    //    var tables = l_data.AsEnumerable().ToChunks(chunkSize)
                    //      .Select(rows => rows.CopyToDataTable()).ToList<DataTable>();

                    //    while (i < tables.Count)
                    //    {
                    //        ProcessItemsWHSWiseThread itemsThread = new ProcessItemsWHSWiseThread(tables[i], route, feed, l_DestinationConnector, l_SourceConnector, userNo, l_InventoryBatchWise.BatchID);

                    //        Thread t = new Thread(new ThreadStart(itemsThread.ProcessItems));
                    //        threads.Add(t);

                    //        i++;
                    //    }

                    //    foreach (Thread t in threads)
                    //    {
                    //        t.Start();
                    //    }
                    //    foreach (Thread t in threads)
                    //    {
                    //        t.Join();
                    //    }
                    //}

                    if (l_data.Rows.Count <= 100)
                    {
                        ProcessItemsWHSWiseThread itemsThread = new ProcessItemsWHSWiseThread(
                            l_data, route, feed, l_DestinationConnector, l_SourceConnector, userNo, l_InventoryBatchWise.BatchID);

                        itemsThread.ProcessItems();
                    }
                    else
                    {
                        int i = 0;
                        int totalThread = 50;
                        int chunkSize = l_data.Rows.Count / totalThread;
                        List<Thread> threads = new List<Thread>();

                        // Ensure chunkSize is at least 1 to avoid division issues
                        chunkSize = chunkSize == 0 ? 1 : chunkSize;

                        var tables = l_data.AsEnumerable()
                            .ToChunks(chunkSize)
                            .Where(rows => rows.Any()) // ✅ Avoid CopyToDataTable on empty chunks
                            .Select(rows => rows.CopyToDataTable())
                            .ToList();

                        while (i < tables.Count)
                        {
                            ProcessItemsWHSWiseThread itemsThread = new ProcessItemsWHSWiseThread(
                                tables[i], route, feed, l_DestinationConnector, l_SourceConnector, userNo, l_InventoryBatchWise.BatchID);

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


    public class ProcessItemsWHSWiseThread
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
        public ProcessItemsWHSWiseThread(DataTable data, Routes route, SCSInventoryFeed feed, ConnectorDataModel destinationConnector,
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
            DataTable ShipNodedataTable = new DataTable();
            try
            {
                this.feed.UseConnection(this.sourceConnector.ConnectionString);
                this.feed.TargetPlusShipNode(this.sourceConnector.CustomerID, ref ShipNodedataTable);

                foreach (DataRow row in this.data.Rows)
                {
                    this.ProcessItem(row, ShipNodedataTable);
                }
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                ShipNodedataTable.Dispose();
            }

        }

        public void ProcessItem(DataRow row, DataTable ShipNodedataTable)
        {
            RestResponse sourceResponse = new RestResponse();

            TargetPlusInventoryFeedWHSWiseRequestModel l_TargetPlusInventoryFeedWHSWiseRequestModel = new TargetPlusInventoryFeedWHSWiseRequestModel();
            try
            {
                string customerId = row["CustomerId"].ToString();
                string itemId = row["ItemId"].ToString();
                //var data = new
                //{
                //    quantity = row["Total_ATS"],
                //};


                foreach (DataRow l_Row in ShipNodedataTable.Rows)
                {
                    TargetPlusQuantity l_TargetPlusQuantity = new TargetPlusQuantity();

                    l_TargetPlusQuantity.quantity = Convert.ToInt32(row[$"ATS_{l_Row["WHSID"]}"]);
                    l_TargetPlusQuantity.distribution_center_id = l_Row["ShipNode"].ToString();

                    l_TargetPlusInventoryFeedWHSWiseRequestModel.quantities.Add(l_TargetPlusQuantity);
                }


                string Body = JsonConvert.SerializeObject(l_TargetPlusInventoryFeedWHSWiseRequestModel);

                this.route.UseConnection(this.sourceConnector.ConnectionString);
                this.feed.UseConnection(this.sourceConnector.ConnectionString);

                this.route.SaveData("JSON-SNT", 0, Body, userNo);
                this.feed.SaveData("JSON-SNT", customerId, itemId, Body, this.userNo, this.bacthID);

                //this.destinationConnector.Url = this.destinationConnector.BaseUrl + row["ProductId"] + "/quantities/" + row["ShipNode"].ToString();
                this.destinationConnector.Url = this.destinationConnector.BaseUrl + row["ProductId"] + "/quantities/bulk";

                sourceResponse = RestConnector.Execute(this.destinationConnector, Body).GetAwaiter().GetResult();

                if (!sourceResponse.IsSuccessful)
                {
                    this.route.SaveLog(LogTypeEnum.Error, $"API call failed for item [{row["ProductId"]}]. HTTP {(int)sourceResponse.StatusCode} {sourceResponse.StatusCode}.", sourceResponse.Content ?? sourceResponse.ErrorMessage, this.userNo);
                }
                else
                {
                    InventoryUpdateResponseModel reponse = null;

                    try
                    {
                        reponse = JsonConvert.DeserializeObject<InventoryUpdateResponseModel>(sourceResponse.Content);
                    }
                    catch (Exception ex)
                    {
                        this.route.SaveLog(LogTypeEnum.Error, $"Failed to parse API response for item [{row["ProductId"]}].", sourceResponse.Content, this.userNo);
                    }

                    if (reponse != null)
                    {
                        this.feed.UpdateItemStatus(itemId, customerId);
                        this.route.SaveLog(LogTypeEnum.Debug, $"TargetPlusInventoryFeedWHSWiseRoute updated for item [{row["ProductId"]}].", sourceResponse.Content, this.userNo);
                    }
                    else if (reponse != null)
                    {
                        this.route.SaveLog(LogTypeEnum.Error, $"Unable to update item [{row["ProductId"]}], Received: {reponse.quantity}.", sourceResponse.Content, this.userNo);
                    }
                }

                this.route.SaveData("JSON-RVD", 0, sourceResponse.Content, this.userNo);
                this.feed.SaveData("JSON-RVD", customerId, itemId, sourceResponse.Content, this.userNo, this.bacthID);
            }
            catch (Exception ex)
            {
                this.route.SaveLog(LogTypeEnum.Exception, $"Unable to update TargetPlusInventoryFeedWHSWiseRoute for item [{row["ProductId"]}].", ex.ToString(), this.userNo);
            }
        }
    }


}
