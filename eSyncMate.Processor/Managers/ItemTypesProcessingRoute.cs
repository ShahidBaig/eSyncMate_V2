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

namespace eSyncMate.Processor.Managers
{
    public class ItemTypesProcessing
    {
        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;
            string destinationData = string.Empty;
            string sourceData = string.Empty;
            string Body = string.Empty;
            int l_ID = 0;
            DataTable l_ItemTypedt = new DataTable();
            RestResponse sourceResponse = new RestResponse();
            ItemTypesReportRequestOutputModel l_ItemTypesReportRequestOutputModel = new ItemTypesReportRequestOutputModel();
            SCS_ItemTypesReport l_ItemTypesReport = new SCS_ItemTypesReport();
            SCS_ItemTypesReponseModel l_SCS_ItemTypesReponseModel = new SCS_ItemTypesReponseModel();
            DataTable l_ItemTypesReportData = new DataTable();

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

                l_ItemTypesReport.UseConnection(l_DestinationConnector.ConnectionString);

                l_ItemTypesReport.GetViewList($"CustomerID = '{l_SourceConnector.CustomerID}' AND Status = 'PENDING' ", string.Empty, ref l_ItemTypesReportData);

                if (l_ItemTypesReportData.Rows.Count > 0)
                {
                    l_ItemTypesReport.ReportID = Convert.ToString(l_ItemTypesReportData.Rows[0]["ReportID"]);

                    if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString())
                    {
                        route.SaveLog(LogTypeEnum.Debug, "Source connector processing start...", string.Empty, userNo);

                        var data = new
                        {
                            type = "ALLOWED_ITEM_TYPES",
                            format = "JSON"
                        };

                        Body = JsonConvert.SerializeObject(data);
                        route.SaveData("JSON-SNT", 0, Body, userNo);

                        l_SourceConnector.Url = l_SourceConnector.Url + l_ItemTypesReport.ReportID;

                        sourceResponse = RestConnector.Execute(l_SourceConnector, Body).GetAwaiter().GetResult();

                        if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            route.SaveLog(LogTypeEnum.Debug, "Received response from source.", sourceResponse.Content, userNo);
                            route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);

                            l_ItemTypesReportRequestOutputModel = JsonConvert.DeserializeObject<ItemTypesReportRequestOutputModel>(sourceResponse.Content);

                            if (l_ItemTypesReportRequestOutputModel.status != "PENDING")
                            {
                                l_SourceConnector.Url = l_ItemTypesReportRequestOutputModel.download_url;
                                sourceResponse = RestConnector.Execute(l_SourceConnector, Body).GetAwaiter().GetResult();

                                if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    route.SaveLog(LogTypeEnum.Debug, "Item Types downloaded successfully.", sourceResponse.Content, userNo);
                                    route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);

                                    l_SCS_ItemTypesReponseModel = JsonConvert.DeserializeObject<SCS_ItemTypesReponseModel>(sourceResponse.Content);

                                    ItemTypesProcessing.PrepareDataTableColumn(ref l_ItemTypedt);

                                    foreach (var ItemTypes in l_SCS_ItemTypesReponseModel.sections.allowed_item_types)
                                    {
                                        DataRow row = l_ItemTypedt.NewRow();

                                        row["ReportID"] = l_ItemTypesReport.ReportID;
                                        row["Brand"] = ItemTypes.brand;
                                        row["Product_Subtype"] = ItemTypes.product_subtype;
                                        row["Item_Type"] = ItemTypes.item_type;
                                        row["Item_Type_Id"] = ItemTypes.item_type_id;
                                        row["Item_Type_Description"] = ItemTypes.item_type_description;
                                        row["CustomerID"] = l_SourceConnector.CustomerID;

                                        l_ItemTypedt.Rows.Add(row);
                                    }

                                    route.SaveLog(LogTypeEnum.Debug, "Item types processing start bulk insert.", string.Empty, userNo);

                                    PublicFunctions.BulkInsert(l_DestinationConnector.ConnectionString, "Temp_SCS_ItemsType", l_ItemTypedt);

                                    route.SaveLog(LogTypeEnum.Debug, "Item types processed bulk insert.", string.Empty, userNo);
                                    route.SaveLog(LogTypeEnum.Debug, "Source connector processed.", string.Empty, userNo);

                                    if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                                    {
                                        route.SaveLog(LogTypeEnum.Debug, "Destination connector processing start..", string.Empty, userNo);

                                        DBConnector connection = new DBConnector(l_DestinationConnector.ConnectionString);

                                        l_DestinationConnector.Command = l_DestinationConnector.Command.Replace("@REPORTID@", "'" + l_ItemTypesReport.ReportID + "'");
                                        l_DestinationConnector.Command = l_DestinationConnector.Command.Replace("@USERNO@", "'" + userNo + "'");
                                        l_DestinationConnector.Command = l_DestinationConnector.Command.Replace("@CUSTOMERID@", "'" + l_DestinationConnector.CustomerID + "'");


                                        if (l_DestinationConnector.CommandType == "SP")
                                        {
                                            connection.Execute(l_DestinationConnector.Command);
                                        }

                                        route.SaveLog(LogTypeEnum.Debug, "Destination connector processed.", string.Empty, userNo);
                                    }
                                }
                                else
                                {
                                    route.SaveLog(LogTypeEnum.Error, "Unable to download Item Types.", l_ItemTypesReportRequestOutputModel.download_url, userNo);
                                }
                            }
                            else
                            {
                                route.SaveLog(LogTypeEnum.Debug, $"Status is pending for item Types Report [{l_ItemTypesReport.ReportID}].", string.Empty, userNo);
                            }
                        }
                        else
                        {
                            route.SaveLog(LogTypeEnum.Error, "Unbale to receive response.", sourceResponse.Content, userNo);
                        }
                    }
                }
                else
                {
                    route.SaveLog(LogTypeEnum.Info, $"No pending item types found for route [{route.Id}]", string.Empty, userNo);
                }

                //if (l_ItemTypesReport.GetObject("Status", "PENDING").IsSuccess)
                //{
                //    if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString())
                //    {
                //        route.SaveLog(LogTypeEnum.Debug, "Source connector processing start...", string.Empty, userNo);

                //        var data = new
                //        {
                //            type = "ALLOWED_ITEM_TYPES",
                //            format = "JSON"
                //        };

                //        Body = JsonConvert.SerializeObject(data);
                //        route.SaveData("JSON-SNT", 0, Body, userNo);

                //        l_SourceConnector.Url = l_SourceConnector.Url + l_ItemTypesReport.ReportID;

                //        sourceResponse = RestConnector.Execute(l_SourceConnector, Body).GetAwaiter().GetResult();

                //        if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK)
                //        {
                //            route.SaveLog(LogTypeEnum.Debug, "Received response from source.", sourceResponse.Content, userNo);
                //            route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);

                //            l_ItemTypesReportRequestOutputModel = JsonConvert.DeserializeObject<ItemTypesReportRequestOutputModel>(sourceResponse.Content);

                //            if (l_ItemTypesReportRequestOutputModel.status != "PENDING")
                //            {
                //                l_SourceConnector.Url = l_ItemTypesReportRequestOutputModel.download_url;
                //                sourceResponse = RestConnector.Execute(l_SourceConnector, Body).GetAwaiter().GetResult();

                //                if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK)
                //                {
                //                    route.SaveLog(LogTypeEnum.Debug, "Item Types downloaded successfully.", sourceResponse.Content, userNo);
                //                    route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);

                //                    l_SCS_ItemTypesReponseModel = JsonConvert.DeserializeObject<SCS_ItemTypesReponseModel>(sourceResponse.Content);

                //                    ItemTypesProcessing.PrepareDataTableColumn(ref l_ItemTypedt);

                //                    foreach (var ItemTypes in l_SCS_ItemTypesReponseModel.sections.allowed_item_types)
                //                    {
                //                        DataRow row = l_ItemTypedt.NewRow();

                //                        row["ReportID"] = l_ItemTypesReport.ReportID;
                //                        row["Brand"] = ItemTypes.brand;
                //                        row["Product_Subtype"] = ItemTypes.product_subtype;
                //                        row["Item_Type"] = ItemTypes.item_type;
                //                        row["Item_Type_Id"] = ItemTypes.item_type_id;
                //                        row["Item_Type_Description"] = ItemTypes.item_type_description;
                //                        row["CustomerID"] = l_SourceConnector.CustomerID;

                //                        l_ItemTypedt.Rows.Add(row);
                //                    }

                //                    route.SaveLog(LogTypeEnum.Debug, "Item types processing start bulk insert.", string.Empty, userNo);

                //                    PublicFunctions.BulkInsert(l_DestinationConnector.ConnectionString, "Temp_SCS_ItemsType", l_ItemTypedt);

                //                    route.SaveLog(LogTypeEnum.Debug, "Item types processed bulk insert.", string.Empty, userNo);
                //                    route.SaveLog(LogTypeEnum.Debug, "Source connector processed.", string.Empty, userNo);

                //                    if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                //                    {
                //                        route.SaveLog(LogTypeEnum.Debug, "Destination connector processing start..", string.Empty, userNo);

                //                        DBConnector connection = new DBConnector(l_DestinationConnector.ConnectionString);

                //                        l_DestinationConnector.Command = l_DestinationConnector.Command.Replace("@REPORTID@", "'" + l_ItemTypesReport.ReportID + "'");
                //                        l_DestinationConnector.Command = l_DestinationConnector.Command.Replace("@USERNO@", "'" + userNo + "'");
                //                        l_DestinationConnector.Command = l_DestinationConnector.Command.Replace("@CUSTOMERID@", "'" + l_DestinationConnector.CustomerID + "'");


                //                        if (l_DestinationConnector.CommandType == "SP")
                //                        {
                //                            connection.Execute(l_DestinationConnector.Command);
                //                        }

                //                        route.SaveLog(LogTypeEnum.Debug, "Destination connector processed.", string.Empty, userNo);
                //                    }
                //                }
                //                else
                //                {
                //                    route.SaveLog(LogTypeEnum.Error, "Unable to download Item Types.", l_ItemTypesReportRequestOutputModel.download_url, userNo);
                //                }
                //            }
                //            else
                //            {
                //                route.SaveLog(LogTypeEnum.Debug, $"Status is pending for item Types Report [{l_ItemTypesReport.ReportID}].", string.Empty, userNo);
                //            }
                //        }
                //        else
                //        {
                //            route.SaveLog(LogTypeEnum.Error, "Unbale to receive response.", sourceResponse.Content, userNo);
                //        }
                //    }
                //}
                //else
                //{
                //    route.SaveLog(LogTypeEnum.Info, $"No pending item types found for route [{route.Id}]", string.Empty, userNo);
                //}

                route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
            }
            finally 
            {
                l_ItemTypedt.Dispose();
            }
        }

        public static DataTable PrepareDataTableColumn(ref DataTable p_DataTable)
        {
            p_DataTable.Columns.Add("ReportID", typeof(string));
            p_DataTable.Columns.Add("Brand", typeof(string));
            p_DataTable.Columns.Add("Product_Subtype", typeof(string));
            p_DataTable.Columns.Add("Item_Type", typeof(string));
            p_DataTable.Columns.Add("Item_Type_Id", typeof(string));
            p_DataTable.Columns.Add("Item_Type_Description", typeof(string));
            p_DataTable.Columns.Add("CustomerID", typeof(string));

            return p_DataTable;
        }
    }
}
