using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Connections;
using eSyncMate.Processor.Models;
using Intercom.Core;
using JUST;
using Newtonsoft.Json;
using RestSharp;
using System.Data;
using static eSyncMate.DB.Declarations;

namespace eSyncMate.Processor.Managers
{
    public class GetOrderStatusRoute
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

                if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                {
                    DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);
                    DataTable l_Data = new DataTable();

                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@CUSTOMERID@", route.SourcePartyObject.ERPCustomerID);

                    if (l_SourceConnector.CommandType == "SP")
                        connection.GetDataSP(l_SourceConnector.Command, ref l_Data);
                    else
                        connection.GetData(l_SourceConnector.Command, ref l_Data);

                    route.SaveLog(LogTypeEnum.Info, $"Source connector executed with [{l_Data.Rows.Count}] orders", string.Empty, userNo);

                    foreach (DataRow l_Row in l_Data.Rows)
                    {
                        string orderId = PublicFunctions.ConvertNullAsString(l_Row[l_SourceConnector.KeyFieldName], string.Empty);

                        try
                        {
                            sourceData = PublicFunctions.ConvertNullAsString(l_Row[l_SourceConnector.DataFieldName], string.Empty);

                            route.SaveLog(LogTypeEnum.Info, $"Processing Order [{orderId}]", string.Empty, userNo);
                            route.SaveData("SRC", Convert.ToInt32(orderId), sourceData, userNo);

                            if (route.MapId > 0)
                            {
                                transformedData = new JsonTransformer().Transform(route.MapObject.Map, sourceData);
                            }
                            else
                            {
                                transformedData = sourceData;
                            }

                            route.SaveData("DST", Convert.ToInt32(orderId), transformedData, userNo);

                            RestResponse destResponse = RestConnector.Execute(l_DestinationConnector, transformedData).GetAwaiter().GetResult();

                            if (destResponse.Content == null)
                                destResponse.Content = "";

                            destinationData = destResponse.Content = "";

                            route.SaveData("DST-RSP", Convert.ToInt32(orderId), destinationData, userNo);
                            route.SaveLog(LogTypeEnum.Info, $"Processed Order [{orderId}]", string.Empty, userNo);
                        }
                        catch (Exception ex)
                        {
                            route.SaveLog(LogTypeEnum.Info, $"Error processing Order [{orderId}]", ex.Message, userNo);
                        }
                    }

                    l_Data.Dispose();
                    route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
                }
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Info, $"Error executing the route [{route.Id}]", ex.Message, userNo);
            }
        }
    }
}
