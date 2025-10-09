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

namespace eSyncMate.Processor.Managers
{
    public class ItemTypesReportRequest
    {
        public static void Execute(IConfiguration config, ILogger logger, Routes route)
        {
            int userNo = 1;
            string destinationData = string.Empty;
            string sourceData = string.Empty;
            string Body = string.Empty;
            int l_ID = 0;
            RestResponse sourceResponse = new RestResponse();
            ItemTypesReportRequestOutputModel l_ItemTypesReportRequestOutputModel = new ItemTypesReportRequestOutputModel();

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

                DBConnector checkConnection = new DBConnector(l_DestinationConnector.ConnectionString);
                DataTable l_ItemTypesReportData = new DataTable();

                if(checkConnection.GetData($"SELECT * FROM SCS_ItemsTypeReport WHERE Status = 'PENDING' AND CustomerID = '{l_SourceConnector.CustomerID}' ", ref l_ItemTypesReportData))
                {
                    l_ItemTypesReportData.Dispose();
                    route.SaveLog(LogTypeEnum.Info, "Already a report request is pending!", string.Empty, userNo);

                    return;
                }

                l_ItemTypesReportData.Dispose();

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

                    sourceResponse = RestConnector.Execute(l_SourceConnector, Body).GetAwaiter().GetResult();

                    if (sourceResponse.StatusCode == System.Net.HttpStatusCode.Created || sourceResponse.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        l_ItemTypesReportRequestOutputModel = JsonConvert.DeserializeObject<ItemTypesReportRequestOutputModel>(sourceResponse.Content);

                        route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);
                        route.SaveLog(LogTypeEnum.Debug, "Items Types response received.", sourceResponse.Content, userNo);

                        if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                        {
                            route.SaveLog(LogTypeEnum.Debug, "Destination connector processing start..", string.Empty, userNo);

                            DBConnector connection = new DBConnector(l_DestinationConnector.ConnectionString);

                            l_DestinationConnector.Command = l_DestinationConnector.Command.Replace("@CUSTOMERID@", l_DestinationConnector.CustomerID);
                            l_DestinationConnector.Command = l_DestinationConnector.Command.Replace("@STATUS@", l_ItemTypesReportRequestOutputModel.status);
                            l_DestinationConnector.Command = l_DestinationConnector.Command.Replace("@REPORTID@", "'" + l_ItemTypesReportRequestOutputModel.id + "'");
                            l_DestinationConnector.Command = l_DestinationConnector.Command.Replace("@USERNO@", Convert.ToString(userNo));

                            if (l_DestinationConnector.CommandType == "SP")
                            {
                                connection.Execute(l_DestinationConnector.Command);
                            }

                            route.SaveLog(LogTypeEnum.Debug, "Destination connector processed.", string.Empty, userNo);
                        }
                    }
                    else
                    {
                        route.SaveLog(LogTypeEnum.Error, "Unable to receive item types.", sourceResponse.Content, userNo);
                    }
                }
               
                route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
            }
        }
    }
}
