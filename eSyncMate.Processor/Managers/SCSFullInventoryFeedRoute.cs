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
using static eSyncMate.Processor.Models.SCSInventoryFeedModel;

namespace eSyncMate.Processor.Managers
{
    public class SCSFullInventoryFeedRoute
    {
        public  static void Execute(IConfiguration config, ILogger logger, Routes route)
        {
            int userNo = 1;
            string destinationData = string.Empty;
            string sourceData = string.Empty;
            string transformedData = string.Empty;
            SCSInventory l_SCSInventory = new SCSInventory();
            string Body = string.Empty;
            int TotalPages = 0;
            DataTable l_dataTable = new DataTable();
            DB.Entities.SCSInventoryFeed l_SCSInventoryFeed = new DB.Entities.SCSInventoryFeed();
            DataTable l_TempData = new DataTable();
            int l_CurrentPage = 0;
            bool l_Process = false;
            InventoryBatchWise l_InventoryBatchWise = new InventoryBatchWise();
            l_InventoryBatchWise.BatchID = Guid.NewGuid().ToString();   

            try
            {
                ConnectorDataModel? l_SourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
                ConnectorDataModel? l_DestinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);

                int currentHour = DateTime.Now.Hour;

                if ((currentHour == 5 || currentHour == 6) && route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSDifferentialInventoryFeed))
                {
                    route.SaveLog(LogTypeEnum.Debug, "SPARS Full Inventory Feed is in Processing", string.Empty, userNo);
                    return;
                }

                SCSFullInventoryFeedRoute.PrepareDataTableColumn(ref l_dataTable);

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

                if (l_SourceConnector.Parmeters != null)
                {
                    foreach (Models.Parameter l_Parameter in l_SourceConnector.Parmeters)
                    {
                        l_Parameter.Value = l_Parameter.Value.Replace("@CUSTOMERID@", route.SourcePartyObject.ERPCustomerID);
                    }
                }

                l_SCSInventoryFeed.UseConnection(l_DestinationConnector.ConnectionString);

                route.SaveLog(LogTypeEnum.Debug, $"SCSInventory GetObject Processing", string.Empty, userNo);
                
                l_InventoryBatchWise.StartDate = Convert.ToDateTime(DateTime.Now);
                l_InventoryBatchWise.Status = "Processing";
                l_InventoryBatchWise.RouteType = route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSDifferentialInventoryFeed)? "SCSDifferentialInventoryFeed" : "SCSFullInventoryFeed";
                l_InventoryBatchWise.CustomerID = l_DestinationConnector.CustomerID;

                l_SCSInventoryFeed.InsertInventoryBatchWise(l_InventoryBatchWise);

                l_SCSInventoryFeed.GetViewList("CustomerID = " + PublicFunctions.FieldToParam(l_DestinationConnector.CustomerID, Declarations.FieldTypes.String), "*", ref l_TempData, "CurrentPage DESC");
                
                route.SaveLog(LogTypeEnum.Debug, $"SCSInventory GetObject Processed Successfully", string.Empty, userNo);

                if (l_TempData.Rows.Count > 0)
                {
                    l_CurrentPage = Convert.ToInt32(PublicFunctions.ConvertNull(l_TempData.Rows[0]["CurrentPage"], 0).ToString());
                    l_CurrentPage = l_CurrentPage + 1;
                    TotalPages = Convert.ToInt32(PublicFunctions.ConvertNull(l_TempData.Rows[0]["TotalPages"], 0).ToString());
                }
                else
                {
                    l_CurrentPage = 1;
                    TotalPages = 1;
                }

                l_TempData.Dispose();

                if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString())
                {
                    for (int i = l_CurrentPage; i <= TotalPages; i++)
                    {
                        int retryCount = 0;

                        getpage:
                        try
                        {
                            var data = new
                            {
                                CustomerID = l_SourceConnector.CustomerID,
                                PageNo = i
                            };

                            Body = JsonConvert.SerializeObject(data);

                            l_SCSInventory = GetSCSInventory(l_SourceConnector, route, Body, userNo);

                            if (l_SCSInventory == null)
                            {
                                Thread.Sleep(100);
                                i--;

                                continue;
                            }

                            TotalPages = l_SCSInventory.TotalPages;
                            l_dataTable.Rows.Clear();

                            if (l_SCSInventory.InventoryFeed != null)
                            {
                                foreach (var inventory in l_SCSInventory.InventoryFeed)
                                {
                                    DataRow row = l_dataTable.NewRow();

                                    row["CustomerID"] = inventory.CustomerID;
                                    row["ItemId"] = inventory.ItemId;
                                    row["CustomerItemCode"] = inventory.CustomerItemCode;
                                    row["ETA_Date"] = inventory.ETA_Date;
                                    row["ETA_Qty"] = inventory.ETA_Qty;
                                    row["Total_ATS"] = inventory.Total_ATS;
                                    row["ATS_L10"] = inventory.ATS_L10;
                                    row["ATS_L21"] = inventory.ATS_L21;
                                    row["ATS_L28"] = inventory.ATS_L28;
                                    row["ATS_L30"] = inventory.ATS_L30;
                                    row["ATS_L34"] = inventory.ATS_L34;
                                    row["ATS_L35"] = inventory.ATS_L35;
                                    row["ATS_L36"] = inventory.ATS_L36;
                                    row["ATS_L37"] = inventory.ATS_L37;
                                    row["ATS_L40"] = inventory.ATS_L40;
                                    row["ATS_L41"] = inventory.ATS_L41;
                                    row["ATS_L55"] = inventory.ATS_L55;
                                    row["ATS_L60"] = inventory.ATS_L60;
                                    row["ATS_L70"] = inventory.ATS_L70;
                                    row["ATS_L91"] = inventory.ATS_L91;
                                    row["ATS_L29"] = inventory.ATS_L29;
                                    row["ATS_L65"] = inventory.ATS_L65;

                                    row["CurrentPage"] = l_SCSInventory.CurrentPage;
                                    row["TotalPages"] = l_SCSInventory.TotalPages;

                                    l_dataTable.Rows.Add(row);
                                }

                                route.SaveLog(LogTypeEnum.Debug, $"SCSInventory Bulk Insert Processing start...", string.Empty, userNo);

                                l_SCSInventoryFeed.CustomerID = l_DestinationConnector.CustomerID;
                                SCSInventoryFeed.BulkInsert(l_DestinationConnector.ConnectionString, "Temp_", l_dataTable);

                                l_InventoryBatchWise.PageCount = i;
                                l_SCSInventoryFeed.UpdateInventoryBatchWisePageCount(l_InventoryBatchWise);

                                route.SaveLog(LogTypeEnum.Debug, $"SCSInventory Bulk Insert Processed...", string.Empty, userNo);

                                Thread.Sleep(500);
                            }
                        }
                        catch (Exception)
                        {
                            if(retryCount <= 1)
                            {
                                retryCount++;
                                goto getpage;
                            }

                            throw;
                        }
                    }
                }

                if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                {
                    DBConnector connection = new DBConnector(l_DestinationConnector.ConnectionString);
                    DataTable l_Data = new DataTable();

                    l_DestinationConnector.Command = l_DestinationConnector.Command.Replace("@CUSTOMERID@", l_DestinationConnector.CustomerID);
                    l_DestinationConnector.Command = l_DestinationConnector.Command.Replace("@BATCHID@", "'"+l_InventoryBatchWise.BatchID+"'");

                    if (l_DestinationConnector.CommandType == "SP")
                    {
                        l_Process = connection.Execute(l_DestinationConnector.Command);
                    }

                    if (l_Process)
                    {
                        route.SaveLog(LogTypeEnum.Debug, $"SCSInventoryACK Processing start...", string.Empty, userNo);

                        string sourceResponse = SendSCSInventoryACK(l_SourceConnector);
                        route.SaveData("JSON", 0, sourceResponse, userNo);

                        route.SaveLog(LogTypeEnum.Debug, $"SCSInventoryACK Processed.", string.Empty, userNo);
                    }
                }

                l_InventoryBatchWise.Status = "Completed";
                l_InventoryBatchWise.RouteType = route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSDifferentialInventoryFeed) ? "SCSDifferentialInventoryFeed" : "SCSFullInventoryFeed";
                l_InventoryBatchWise.FinishDate = DateTime.Now;

                l_SCSInventoryFeed.UpdateInventoryBatchWise(l_InventoryBatchWise);

                route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                l_InventoryBatchWise.FinishDate = DateTime.Now;
                l_InventoryBatchWise.Status = "Error";
                l_InventoryBatchWise.RouteType = route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSDifferentialInventoryFeed) ? "SCSDifferentialInventoryFeed" : "SCSFullInventoryFeed";

                l_SCSInventoryFeed.UpdateInventoryBatchWise(l_InventoryBatchWise);

                route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
            }
            finally 
            {
                l_dataTable.Dispose();
            }
        }

        private static SCSInventory GetSCSInventory(ConnectorDataModel sourceConnector, Routes route, string Body, int userNo, int retryCount = 0)
        {
            RestResponse sourceResponse = null;

            try
            {
                SCSInventory l_SCSInventory = null;
                string sourceData = string.Empty;

                route.SaveLog(LogTypeEnum.Debug, Body, string.Empty, userNo);

                sourceResponse = RestConnector.Execute(sourceConnector, Body).GetAwaiter().GetResult();

                if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK && sourceResponse.Content != null)
                {
                    sourceData = sourceResponse.Content;
                    l_SCSInventory = JsonConvert.DeserializeObject<SCSInventory>(sourceData);

                    route.SaveData("JSON", 0, sourceResponse.Content, userNo);
                }

                return l_SCSInventory;
            }
            catch (Exception ex)
            {
                if (retryCount <= 2)
                    return GetSCSInventory(sourceConnector, route, Body, userNo, retryCount++);

                throw new Exception(sourceResponse?.Content ?? "", ex);
            }
        }

        private static DataTable PrepareDataTableColumn(ref DataTable p_DataTable)
        {
            p_DataTable.Columns.Add("CustomerID", typeof(string));
            p_DataTable.Columns.Add("ItemId", typeof(string));
            p_DataTable.Columns.Add("CustomerItemCode", typeof(string));
            p_DataTable.Columns.Add("ETA_Date", typeof(string));
            p_DataTable.Columns.Add("ETA_Qty", typeof(int));
            p_DataTable.Columns.Add("Total_ATS", typeof(int));
            p_DataTable.Columns.Add("ATS_L10", typeof(int));
            p_DataTable.Columns.Add("ATS_L21", typeof(int));
            p_DataTable.Columns.Add("ATS_L28", typeof(int));
            p_DataTable.Columns.Add("ATS_L30", typeof(int));
            p_DataTable.Columns.Add("ATS_L34", typeof(int));
            p_DataTable.Columns.Add("ATS_L35", typeof(int));
            p_DataTable.Columns.Add("ATS_L36", typeof(int));
            p_DataTable.Columns.Add("ATS_L37", typeof(int));
            p_DataTable.Columns.Add("ATS_L40", typeof(int));
            p_DataTable.Columns.Add("ATS_L41", typeof(int));
            p_DataTable.Columns.Add("ATS_L55", typeof(int));
            p_DataTable.Columns.Add("ATS_L60", typeof(int));
            p_DataTable.Columns.Add("ATS_L70", typeof(int));
            p_DataTable.Columns.Add("ATS_L91", typeof(int));
            p_DataTable.Columns.Add("ATS_L29", typeof(int));
            p_DataTable.Columns.Add("ATS_L65", typeof(int));
            p_DataTable.Columns.Add("CurrentPage", typeof(int));
            p_DataTable.Columns.Add("TotalPages", typeof(int));

            return p_DataTable;
        }

        private static string SendSCSInventoryACK(ConnectorDataModel connector)
        {
            connector.Url = "inventory-feed-acknowledgement";
            connector.Method = "Post";
            connector.Parmeters = new List<Models.Parameter>();

            connector.Parmeters.Add(new Models.Parameter() { Name = "p_CustomerId", Value = connector.CustomerID });

            RestResponse sourceResponse = RestConnector.Execute(connector, "").GetAwaiter().GetResult();

            return sourceResponse.Content ?? "";
        }
    }
}
