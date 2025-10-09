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

namespace eSyncMate.Processor.Managers
{
    public class SCSInvoiceRoute
    {
        public static void Execute(IConfiguration config, ILogger logger, Routes route)
        {
            int userNo = 1;
            string destinationData = string.Empty;
            string sourceData = string.Empty;
            string Body = string.Empty;
            int l_ID = 0;
            DataTable l_dataTable = new DataTable();
            RestResponse sourceResponse = new RestResponse();
            SCSPlaceOrderResponse l_SCSPlaceOrderResponse = new SCSPlaceOrderResponse();

            try
            {
                ConnectorDataModel? l_SourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
                ConnectorDataModel? l_DestinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);

                route.SaveLog(LogTypeEnum.Info, $"Started executing route [{route.Id}]", string.Empty, userNo);

                if (l_SourceConnector == null)
                {
                    logger.LogError("Source Connector is not setup properly");
                    route.SaveLog(LogTypeEnum.Info, "Source Connector is not setup properly", string.Empty, userNo);
                    return;
                }

                if (l_DestinationConnector == null)
                {
                    logger.LogError("Destination Connector is not setup properly");
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

                if (l_SourceConnector.ConnectivityType == ConnectorTypesEnum.SqlServer.ToString())
                {
                    DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);
                    DataTable l_Data = new DataTable();

                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@CUSTOMERID@", l_SourceConnector.CustomerID);

                    if (l_SourceConnector.CommandType == "SP")
                    {
                       connection.GetDataSP(l_SourceConnector.Command,ref l_dataTable);
                    }
                }

                if (l_dataTable.Rows.Count > 0)
                {
                    if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString())
                    {
                        foreach (DataRow l_Row in l_dataTable.Rows)
                        {
                            var data = new
                            {
                                Input = new
                                {
                                    OrderNo = l_Row["ExternalId"],
                                    CustomerPO = "",
                                    ExternalID = ""
                                }
                            };

                            Body = JsonConvert.SerializeObject(data);

                            sourceResponse = RestConnector.Execute(l_DestinationConnector, Body).GetAwaiter().GetResult();

                            route.SaveData("JSON", 0, sourceResponse.Content, userNo);

                            ///// 1: Convert into Maps for TargetPlus sourceResponse.Content
                            /// route.SaveData("DST-RSP", 0, sourceResponse.Content, userNo);

                            DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);
                            DataTable l_Data = new DataTable();
                            string Command = string.Empty;

                            Command = "EXEC SP_UpdateOrderStatus @p_CustomerID =" + l_SourceConnector.CustomerID + ", @p_RouteType = " + RouteTypesEnum.SCSASN + ", @p_ExternalId = " + l_Row["ExternalId"];

                            connection.Execute(Command);

                            //route.SaveData("DST-RSP", 0, sourceResponse.Content, userNo);
                        }
                    }
                }
               
                route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Info, $"Error executing the route [{route.Id}]", ex.Message, userNo);
            }
            finally 
            {
                l_dataTable.Dispose();
            }
        }
    }
}
