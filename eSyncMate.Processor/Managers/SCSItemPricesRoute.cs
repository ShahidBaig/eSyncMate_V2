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

namespace eSyncMate.Processor.Managers
{
    public class SCSItemPrices
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
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ROUTETYPEID@", Convert.ToString(RouteTypesEnum.SCSItemPrices));
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

                    if (l_data.Rows.Count <= 100)
                    {
                        ProcessItemPricesThread itemsThread = new ProcessItemPricesThread(l_data, route, l_DestinationConnector, l_SourceConnector, userNo);

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
                            ProcessItemPricesThread itemsThread = new ProcessItemPricesThread(tables[i], route, l_DestinationConnector, l_SourceConnector, userNo);

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

    public class ProcessItemPricesThread
    {
        // State information used in the task.
        private DataTable data;
        private Routes route;
        private SCSInventoryFeed feed;
        private ConnectorDataModel destinationConnector;
        private ConnectorDataModel sourceConnector;
        private int userNo;

        // The constructor obtains the state information.
        public ProcessItemPricesThread(DataTable data, Routes route, ConnectorDataModel destinationConnector,
                                ConnectorDataModel sourceConnector, int userNo)
        {
            this.data = data;
            this.route = JsonConvert.DeserializeObject<Routes>(JsonConvert.SerializeObject(route));
            this.destinationConnector = destinationConnector;
            this.sourceConnector = sourceConnector;
            this.userNo = userNo;
        }

        public void ProcessItems()
        {
            foreach (DataRow row in this.data.Rows)
            {
                this.ProcessItem(row);
            }
        }

        public void ProcessItem(DataRow row)
        {
            try
            {
                var data = new
                {
                    list_price = row["ListPrice"],
                    offer_price = row["OffPrice"],
                    map_price = row["MapPrice"]
                };

                this.route.UseConnection(this.sourceConnector.ConnectionString);

                string Body = JsonConvert.SerializeObject(data);
                route.SaveData("JSON-SNT", 0, Body, userNo);

                this.destinationConnector.Url = this.destinationConnector.BaseUrl + row["id"];
                RestResponse sourceResponse = RestConnector.Execute(this.destinationConnector, Body).GetAwaiter().GetResult();

                if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    route.SaveLog(LogTypeEnum.Debug, $"ItemPrices updated for item [{row["id"]}].", string.Empty, userNo);

                    CustomerProductCatalog l_CustomerProductCatalog = new CustomerProductCatalog();

                    l_CustomerProductCatalog.UseConnection(this.sourceConnector.ConnectionString);
                    l_CustomerProductCatalog.UpdateStatus(Convert.ToString(row["ItemID"]), Convert.ToString(row["VariationType"]), "APPROVED_PR", row["id"].ToString(), this.sourceConnector.CustomerID);
                    l_CustomerProductCatalog.CustomerProductCatalogPrices(this.destinationConnector.CustomerID, Convert.ToString(row["ItemID"]), Convert.ToString(row["id"]), "APPROVED");

                    l_CustomerProductCatalog.DeleteCustomerProductCatalog(Convert.ToString(row["ItemID"]), Convert.ToString(row["VariationType"]), this.sourceConnector.CustomerID);
                }
                else
                {
                    route.SaveLog(LogTypeEnum.Error, $"Unable to update ItemPrices for item [{row["id"]}].", string.Empty, userNo);
                }

                route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Error, $"{ex.Message} - Unable to update ItemPrices for item [{row["id"]}].", string.Empty, userNo);
            }
        }
    }
}
