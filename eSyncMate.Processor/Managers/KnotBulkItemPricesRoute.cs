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
using static eSyncMate.Processor.Models.MacysInventoryUploadRequestModel;
using static eSyncMate.Processor.Models.LowesInventoryUploadRequestModel;
using static eSyncMate.Processor.Models.KnotInventoryUploadRequestModel;

namespace eSyncMate.Processor.Managers
{
    public class KnotBulkItemPricesRoute
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
            KnotInventoryUploadRequestModel l_KnotInventoryUploadRequestModel = new KnotInventoryUploadRequestModel();

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
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ROUTETYPEID@", Convert.ToString(RouteTypesEnum.KnotInventoryUpload));
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


                    foreach (DataRow row in l_data.Rows)
                    {
                        string customerId = row["CustomerId"].ToString();
                        string itemId = row["ItemId"].ToString();
                        KnotOffer l_offers = new KnotOffer();

                        l_offers.price = Convert.ToDouble(row["ListPrice"]);
                        l_offers.product_sku = row["CustomerItemCode"].ToString();
                        l_offers.quantity = row["Total_ATS"].ToString();
                        l_offers.shop_sku = row["ItemId"].ToString();
                        l_offers.state_code = "11";
                        l_KnotInventoryUploadRequestModel.offers.Add(l_offers);

                        Body = JsonConvert.SerializeObject(l_KnotInventoryUploadRequestModel);

                        route.SaveData("JSON-SNT", 0, Body, userNo);
                    }


                    Body = JsonConvert.SerializeObject(l_KnotInventoryUploadRequestModel);
                    route.SaveData("JSON-SNT", 0, Body, userNo);

                    l_DestinationConnector.Url = l_DestinationConnector.BaseUrl + "/api/offers/";
                    sourceResponse = RestConnector.Execute(l_DestinationConnector, Body).GetAwaiter().GetResult();

                    if (sourceResponse.StatusCode == System.Net.HttpStatusCode.Created)
                    {
                        foreach (DataRow row in l_data.Rows)
                        {
                            route.SaveLog(LogTypeEnum.Debug, $"Knot Bulk ItemPrices updated for item [{row["id"]}].", string.Empty, userNo);

                            l_CustomerProductCatalog.UseConnection(l_SourceConnector.ConnectionString);
                            l_CustomerProductCatalog.UpdateSCSProductStatus(Convert.ToString(row["ItemID"]), "", "APPROVED_PR", row["id"].ToString(), l_SourceConnector.CustomerID);
                            l_CustomerProductCatalog.CustomerProductCatalogPrices(l_DestinationConnector.CustomerID, Convert.ToString(row["ItemID"]), Convert.ToString(row["id"]), "APPROVED");

                            l_CustomerProductCatalog.DeleteSCSProductStatus(Convert.ToString(row["ItemID"]), "", l_SourceConnector.CustomerID);
                        }
                    }
                    else
                    {
                        route.SaveLog(LogTypeEnum.Error, $"Unable to update Knot Bulk ItemPrices for items.", string.Empty, userNo);
                    }

                    route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);

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

}
