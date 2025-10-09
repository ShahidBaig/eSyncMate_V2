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
using Hangfire;

namespace eSyncMate.Processor.Managers
{
    public class ProductTypeAttributes
    {
        public static void Execute(IConfiguration config, ILogger logger, Routes route)
        {
            int userNo = 1;
            DataTable l_ItemTypedt = new DataTable();
            DataTable l_Attributedt = new DataTable();

            RestResponse sourceResponse = new RestResponse();
            SCS_ItemsType l_SCS_ItemsType = new SCS_ItemsType();
            SCS_ProductTypeAttributeReponseModel l_SCS_ProductTypeAttribute = new SCS_ProductTypeAttributeReponseModel();
            
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

                l_SCS_ItemsType.UseConnection(l_DestinationConnector.ConnectionString);
                l_SCS_ItemsType.GetViewList($"CustomerId = '{l_SourceConnector.CustomerID}'", "",ref l_ItemTypedt);

                if (l_ItemTypedt.Rows.Count > 0)
                {
                    foreach (DataRow row in l_ItemTypedt.Rows)
                    {
                        string itemTypeId = PublicFunctions.ConvertNullAsString(row["Item_Type_Id"], string.Empty);

                        if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString())
                        {
                            var taxonomyParam = l_SourceConnector.Parmeters.FirstOrDefault(param => param.Name == "taxonomy_id");

                            route.SaveLog(LogTypeEnum.Debug, $"Source connector processing start for {itemTypeId}.", string.Empty, userNo);

                            l_Attributedt.Dispose();
                            l_Attributedt = new DataTable();

                            ProductTypeAttributes.PrepareDataTableColumn(ref l_Attributedt);

                            if (taxonomyParam != null)
                            {
                                taxonomyParam.Value = itemTypeId;
                            }

                            sourceResponse = RestConnector.Execute(l_SourceConnector, "").GetAwaiter().GetResult();

                            if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                route.SaveLog(LogTypeEnum.Debug, $"Received Item Type [{itemTypeId}] attributes.", sourceResponse.Content, userNo);
                                route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);

                                var productAttributes = JsonConvert.DeserializeObject<SCS_ProductTypeAttributeReponseModel[]>(sourceResponse.Content);

                                foreach (var item in productAttributes)
                                {
                                    DataRow l_row = l_Attributedt.NewRow();

                                    l_row["ID"] = item.attribute.id;
                                    l_row["Name"] = item.attribute.name;
                                    l_row["Mapped_Property"] = item.attribute.mapped_property;
                                    l_row["Type"] = item.attribute.type;
                                    l_row["Item_Type_Id"] = Convert.ToString(PublicFunctions.ConvertNull(row["Item_Type_Id"], 0));
                                    l_row["Required"] = item.required;
                                    l_row["CustomerID"] = l_DestinationConnector.CustomerID;


                                    l_Attributedt.Rows.Add(l_row);
                                }
                            }
                            else
                            {
                                route.SaveLog(LogTypeEnum.Error, "Unable to receive respone for Item Type Attributes.", sourceResponse.Content, userNo);
                            }

                            route.SaveLog(LogTypeEnum.Debug, $"Source connector processed for {itemTypeId}.", string.Empty, userNo);
                        }

                        if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                        {
                            route.SaveLog(LogTypeEnum.Debug, $"Destination connector processing start for {itemTypeId}.", string.Empty, userNo);

                            PublicFunctions.BulkInsert(l_DestinationConnector.ConnectionString, "Temp_SCS_ItemTypeAttribute", l_Attributedt);

                            DBConnector connection = new DBConnector(l_DestinationConnector.ConnectionString);

                            string command = l_DestinationConnector.Command;

                            command = command.Replace("@ITEMTYPEID@", itemTypeId).Replace("@USERNO@", Convert.ToString(userNo)).Replace("@CUSTOMERID@", Convert.ToString(l_DestinationConnector.CustomerID));

                            if (l_DestinationConnector.CommandType == "SP")
                            {
                                connection.Execute(command);
                            }

                            route.SaveLog(LogTypeEnum.Debug, $"Destination connector processed for {itemTypeId}.", string.Empty, userNo);
                        }
                    }
                }

                route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
            }
            finally 
            {
                l_ItemTypedt.Dispose();
                l_Attributedt.Dispose();
            }
        }

        public static DataTable PrepareDataTableColumn(ref DataTable p_DataTable)
        {
            p_DataTable.Columns.Add("ID", typeof(string));
            p_DataTable.Columns.Add("Name", typeof(string));
            p_DataTable.Columns.Add("Mapped_Property", typeof(string));
            p_DataTable.Columns.Add("Type", typeof(string));
            p_DataTable.Columns.Add("Item_Type_Id", typeof(string));
            p_DataTable.Columns.Add("Required", typeof(string));
            p_DataTable.Columns.Add("CustomerID", typeof(string));


            return p_DataTable;
        }
    }
}
