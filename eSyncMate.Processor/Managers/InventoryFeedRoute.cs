using eSyncMate.DB.Entities;
using eSyncMate.Processor.Connections;
using eSyncMate.Processor.Models;
using Hangfire.Storage;
using Intercom.Core;
using JUST;
using Nancy;
using Newtonsoft.Json;
using RestSharp;
using static eSyncMate.DB.Declarations;

namespace eSyncMate.Processor.Managers
{
    public static class InventoryFeedRoute
    {
        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;

            try
            {
                ConnectorDataModel? l_SourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
                ConnectorDataModel? l_DestinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);
                string destinationData = string.Empty;
                string sourceData = string.Empty;
                string transformedData = string.Empty;

                route.SaveLog(LogTypeEnum.Info, $"Started executing route [{route.Id}]", string.Empty, userNo);

                if (l_SourceConnector == null)
                {
                    
                    route.SaveLog(LogTypeEnum.Info, "Source Connector is not setup properly", string.Empty, userNo);
                    return;
                }

                if (l_DestinationConnector == null)
                {
                    
                    route.SaveLog(LogTypeEnum.Info, "Destination Connector is not setup properly", string.Empty, userNo);
                    return;
                }

                if (l_SourceConnector.Parmeters != null)
                {
                    foreach (Models.Parameter l_Parameter in l_SourceConnector.Parmeters)
                    {
                        l_Parameter.Value = l_Parameter.Value.Replace("@CUSTOMERID@", route.SourcePartyObject.ERPCustomerID);
                    }
                }

                if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString())
                {
                    RestResponse sourceResponse = RestConnector.Execute(l_SourceConnector, "").GetAwaiter().GetResult();

                    if (sourceResponse.Content == null)
                        sourceResponse.Content = "";

                    sourceData = sourceResponse.Content;

                    route.SaveData("SRC", 0, sourceData, userNo);
                    route.SaveLog(LogTypeEnum.Info, $"Feed data received!", string.Empty, userNo);
                }

                if (route.MapId > 0)
                {
                    transformedData = new JsonTransformer().Transform(route.MapObject.Map, sourceData);
                }
                else
                {
                    transformedData = sourceData;
                }

                route.SaveData("DST", 0, transformedData, userNo);
                route.SaveLog(LogTypeEnum.Info, $"Feed data tranformed for destination party!", string.Empty, userNo);

                RestResponse destResponse = RestConnector.Execute(l_DestinationConnector, transformedData).GetAwaiter().GetResult();

                if (destResponse.Content == null)
                    destResponse.Content = "";

                destinationData = destResponse.Content = "";

                route.SaveData("DST-RSP", 0, destinationData, userNo);
                route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Info, $"Error executing the route [{route.Id}]", ex.Message, userNo);
            }
        }
    }
}
