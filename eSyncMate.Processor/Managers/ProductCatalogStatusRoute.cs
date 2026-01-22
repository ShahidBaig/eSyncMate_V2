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
    public class ProductCatalogStatus
    {
        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;
            string destinationData = string.Empty;
            string sourceData = string.Empty;
            string Body = string.Empty;
            int l_ID = 0;
            DataTable l_data = new DataTable();
            RestResponse sourceResponse = new RestResponse();
            CustomerProductCatalog l_CustomerProductCatalog = new CustomerProductCatalog();
            SCS_ProductCatalogStatusResponseModel l_SCS_ProductCatalogStatusResponseModel = new SCS_ProductCatalogStatusResponseModel();

            try
            {
                ConnectorDataModel? l_SourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
                ConnectorDataModel? l_DestinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);
                l_CustomerProductCatalog.UseConnection(CommonUtils.ConnectionString);

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

                    DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);

                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@CUSTOMERID@", l_SourceConnector.CustomerID);
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ROUTETYPEID@", Convert.ToString(RouteTypesEnum.ProductCatalogStatus));
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@USERNO@", Convert.ToString(userNo));

                    if (l_SourceConnector.CommandType == "SP")
                    {
                        connection.GetDataSP(l_SourceConnector.Command, ref l_data);
                    }

                    route.SaveLog(LogTypeEnum.Debug, "Source connector processed.", string.Empty, userNo);
                }

                if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString() && l_data.Rows.Count > 0)
                {
                    route.SaveLog(LogTypeEnum.Debug, "Destination connector processing Start...", string.Empty, userNo);

                    foreach (DataRow row in l_data.Rows)
                    {
                        CustomerProductCatalog l_Product = new CustomerProductCatalog();
                        List<SCS_ProductCatalogStatusResponseModel> productList = new List<SCS_ProductCatalogStatusResponseModel>();

                        l_Product.UseConnection(l_SourceConnector.ConnectionString);
                        l_Product.ProductId = Convert.ToInt32(row["ProductId"].ToString());

                        l_DestinationConnector.Url = l_DestinationConnector.BaseUrl + "external_id=" + row["ItemID"];
                        l_DestinationConnector.Method = "GET";

                        sourceResponse = RestConnector.Execute(l_DestinationConnector, Body).GetAwaiter().GetResult();
                        if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            route.SaveLog(LogTypeEnum.Debug, $"ProductCatalogStatus processed for [{row["ItemID"]}].", string.Empty, userNo);
                            
                            l_Product.DeleteWithType(l_Product.ProductId, "RSP-JSON");
                            l_Product.SaveData("RSP-JSON", sourceResponse.Content, userNo);

                            productList = JsonConvert.DeserializeObject<List<SCS_ProductCatalogStatusResponseModel>>(sourceResponse.Content);

                            if (productList.Any())
                            {
                                SCS_ProductCatalogStatusResponseModel productStatus = productList[0];

                                if (productStatus.product_statuses[0].listing_status == "APPROVED")
                                {
                                    l_DestinationConnector.Url = $"https://api.target.com/sellers/v1/sellers/{l_DestinationConnector.Realm.ToString()}/product_logistics/{row["Id"].ToString()}";
                                    l_DestinationConnector.Method = "PUT";

                                    var fields = new List<Dictionary<string, string>>();

                                    if (!string.IsNullOrEmpty(row["is_add_on"]?.ToString()))
                                    {
                                        fields.Add(new Dictionary<string, string>
                                        {
                                            { "name", "fulfillment.is_add_on" },
                                            { "value", row["is_add_on"].ToString() }
                                        });
                                    }

                                    if (!string.IsNullOrEmpty(row["two_day_shipping_eligible"]?.ToString()))
                                    {
                                        fields.Add(new Dictionary<string, string>
                                        {
                                            { "name", "fulfillment.two_day_shipping_eligible" },
                                            { "value", row["two_day_shipping_eligible"].ToString() }
                                        });
                                    }

                                    if (!string.IsNullOrEmpty(row["shipping_exclusion"]?.ToString()))
                                    {
                                        fields.Add(new Dictionary<string, string>
                                        {
                                            { "name", "shipping_exclusion" },
                                            { "value", row["shipping_exclusion"].ToString() }
                                        });
                                    }

                                    if (!string.IsNullOrEmpty(row["seller_return_policy"]?.ToString()))
                                    {
                                        fields.Add(new Dictionary<string, string>
                                        {
                                            { "name", "seller_return_policy" },
                                            { "value", row["seller_return_policy"].ToString() }
                                        });
                                    }

                                    var requestBody = new { fields };
                                    
                                    Body = JsonConvert.SerializeObject(requestBody);

                                    l_Product.SaveData("REQ-JSON", Body, userNo);

                                    sourceResponse = RestConnector.Execute(l_DestinationConnector, Body).GetAwaiter().GetResult();

                                    if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK)
                                    {
                                        l_Product.SaveData("RSP-JSON", sourceResponse.Content, userNo);
                                    }
                                }

                                l_CustomerProductCatalog.UpdateStatus(Convert.ToString(row["ItemID"]), Convert.ToString(row["VariationType"]), productStatus.product_statuses[0].listing_status, productStatus.id, l_SourceConnector.CustomerID,0);
                            }
                        }
                        else
                        {
                            l_Product.DeleteWithType(l_Product.ProductId, "RSP-ERR");
                            l_Product.SaveData("RSP-ERR", sourceResponse.Content, userNo);
                            
                            route.SaveLog(LogTypeEnum.Error, $"Unable to process ProductCatalogStatus for [{row["ItemID"]}].", sourceResponse.Content, userNo);
                        }

                        route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);
                    }

                    route.SaveLog(LogTypeEnum.Debug, "Destination connector processed.", string.Empty, userNo);
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
