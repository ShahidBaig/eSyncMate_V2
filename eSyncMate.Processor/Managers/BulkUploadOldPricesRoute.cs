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
using System;

namespace eSyncMate.Processor.Managers
{
    public class BulkUploadOldPricesRoute
    {
        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;
            string destinationData = string.Empty;
            string sourceData = string.Empty;
            string Body = string.Empty;
            int l_ID = 0;
            DataTable l_Sourcedata = new DataTable();
            RestResponse sourceResponse = new RestResponse();
            ProductUploadPrices l_ProductUploadPrices = new ProductUploadPrices();

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
                    route.SaveLog(LogTypeEnum.Debug, "Source connector processing start...", string.Empty, userNo);
                    
                    l_ProductUploadPrices.UseConnection(l_SourceConnector.ConnectionString);

                    if (l_SourceConnector.CommandType.ToUpper() == "QUERY")
                    {
                        l_ProductUploadPrices.GetProductID($"Status = 'SYNCED' AND PromoEndDate <= GETDATE() AND CustomerID = '{l_SourceConnector.CustomerID}'", ref l_Sourcedata);
                    }

                    route.SaveLog(LogTypeEnum.Debug, "Source connector processed.", string.Empty, userNo);
                }

                if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString() && l_Sourcedata.Rows.Count > 0)
                {
                    route.SaveLog(LogTypeEnum.Debug, "Destination connector processing start.", string.Empty, userNo);

                    foreach (DataRow l_Row in l_Sourcedata.Rows)
                    {
                        var l_newPrices = new
                        {
                            list_price = l_Row["OldListPrice"],
                            offer_price = l_Row["OldOffPrice"],
                        };

                        Body = JsonConvert.SerializeObject(l_newPrices);

                        l_DestinationConnector.Method = "PUT";
                        l_DestinationConnector.Url = l_DestinationConnector.BaseUrl + l_Row["ProductID"];

                        route.SaveData("JSON-SNT", 0, Body, userNo);
                        sourceResponse = RestConnector.Execute(l_DestinationConnector, Body).GetAwaiter().GetResult();

                        route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);

                        if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            route.SaveLog(LogTypeEnum.Info, $"UploadPrices has been processed for [{l_Row["ProductID"]}].", string.Empty, userNo);
                            l_ProductUploadPrices.UpdateStatus(Convert.ToString(l_Row["ItemId"]), "FINISHED");
                            l_ProductUploadPrices.DeletePriceDescripencies(Convert.ToString(l_Row["ItemId"]));

                        }
                        else
                        {
                            route.SaveLog(LogTypeEnum.Error, $"UploadPrices failed for [{l_Row["ProductID"]}].", string.Empty, userNo);
                        }
                    }

                    route.SaveLog(LogTypeEnum.Debug, "Destination connector processing completed.", string.Empty, userNo);
                }

                route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"Error executing the route [{route.Id}]", ex.ToString(), userNo);
            }
            finally 
            {
                l_Sourcedata.Dispose();
            }
        }
    }
}
