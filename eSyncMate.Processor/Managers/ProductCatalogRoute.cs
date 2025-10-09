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
using Hangfire.Storage;
using System.Net.NetworkInformation;
using System.Text.Json;


namespace eSyncMate.Processor.Managers
{
    public class ProductCatalog
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
            SCS_SAPrductModel l_SCS_SAPrductModel = new SCS_SAPrductModel();
            SCS_VAPProductCatalogModel l_SCS_VAPProductCatalogModel = new SCS_VAPProductCatalogModel();
            CustomerProductCatalog l_CustomerProductCatalog = new CustomerProductCatalog();
            ProductCatalogErrorModel l_ProductCatalogErrorModel = new ProductCatalogErrorModel();
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
                    route.SaveLog(LogTypeEnum.Debug, "Source connector processing Start...", string.Empty, userNo);

                    DBConnector connection = new DBConnector(l_SourceConnector.ConnectionString);

                    l_CustomerProductCatalog.UseConnection(l_SourceConnector.ConnectionString);

                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@CUSTOMERID@", l_SourceConnector.CustomerID);
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@ROUTETYPEID@", Convert.ToString(RouteTypesEnum.ProductCatalog));
                    l_SourceConnector.Command = l_SourceConnector.Command.Replace("@USERNO@", Convert.ToString(userNo));

                    if (l_SourceConnector.CommandType == "SP")
                    {
                        connection.GetDataSP(l_SourceConnector.Command, ref l_data);
                    }

                    route.SaveLog(LogTypeEnum.Debug, "Source connector processing completed.", string.Empty, userNo);
                }

                if (l_data.Rows.Count > 0)
                {
                    if (l_DestinationConnector.ConnectivityType == ConnectorTypesEnum.Rest.ToString())
                    {
                        route.SaveLog(LogTypeEnum.Debug, "Destination connector processing Start...", string.Empty, userNo);

                        string[] syncStatus = { "NEW", "UPDATED", "PENDING" };
                        string[] VariationType = { "VAP", "VC" };

                        var filteredSAItems = l_data.AsEnumerable().Where(row => (new string[] { "SA", "STANDALONE" }).Select(v => v.ToUpper()).Contains(row.Field<string>("VariationType").ToUpper())
                                                             && syncStatus.Contains(row.Field<string>("SyncStatus")));

                        var filteredVAPVCItems = l_data.AsEnumerable().Where(row => VariationType.Contains(row.Field<string>("VariationType"))
                                                            && syncStatus.Contains(row.Field<string>("SyncStatus")));

                        var filteredUnlistedItems = l_data.AsEnumerable().Where(row => row.Field<bool>("UnListed") == true);

                        if (filteredSAItems.Any())
                        {
                            string destUrl = l_DestinationConnector.BaseUrl + "products";

                            foreach (var itemsSA in filteredSAItems)
                            {
                                l_DestinationConnector.Url = destUrl;
                                l_DestinationConnector.Method = "POST";
                                l_ProductCatalogErrorModel = new ProductCatalogErrorModel();

                                if (itemsSA.Field<string>("SyncStatus").Equals("UPDATED"))
                                {
                                    l_DestinationConnector.Method = "PUT";

                                    if (string.IsNullOrEmpty(itemsSA.Field<string>("id")))
                                        continue;

                                    l_DestinationConnector.Url = l_DestinationConnector.Url + "/" + itemsSA.Field<string>("id");
                                }

                                l_SCS_SAPrductModel = new SCS_SAPrductModel();

                                l_SCS_SAPrductModel = JsonConvert.DeserializeObject<SCS_SAPrductModel>(itemsSA["JsonData"].ToString());
                                l_SCS_SAPrductModel.external_id = itemsSA["ItemID"].ToString();

                                if (itemsSA["VariationType"].ToString().ToUpper() == "STANDALONE")
                                {
                                    l_SCS_SAPrductModel.relationship_type = "SA";
                                }
                                else
                                {
                                    l_SCS_SAPrductModel.relationship_type = itemsSA["VariationType"].ToString();
                                }

                                l_SCS_SAPrductModel.seller_id = l_DestinationConnector.Realm;

                                Body = JsonConvert.SerializeObject(l_SCS_SAPrductModel);

                                route.SaveData("JSON-SNT", 0, Body, userNo);
                                l_CustomerProductCatalog.ProductId = Convert.ToInt32(itemsSA["ProductId"]);

                                l_CustomerProductCatalog.SaveData("REQ-SNT", Body, userNo);

                                sourceResponse = RestConnector.Execute(l_DestinationConnector, Body).GetAwaiter().GetResult();

                                string l_Status = "PENDING";

                                if (sourceResponse.StatusCode == System.Net.HttpStatusCode.BadRequest)
                                {
                                    //l_Status = "ERROR";

                                    if (!string.IsNullOrEmpty(sourceResponse.Content))
                                    {
                                        l_ProductCatalogErrorModel = JsonConvert.DeserializeObject<ProductCatalogErrorModel>(sourceResponse.Content);
                                       
                                        if (!l_ProductCatalogErrorModel.errors.Any() == true && Convert.ToInt32(itemsSA["RetryCount"] == DBNull.Value ? 0 : itemsSA["RetryCount"]) >= 3)
                                        {
                                            l_Status = "ERROR";
                                        }

                                        if (l_ProductCatalogErrorModel.errors.Any() == true)
                                        {
                                            l_Status = "ERROR";
                                        }
                                    }


                                    route.SaveLog(LogTypeEnum.Error, $"BadRequest received from SA Item Sync for item {l_SCS_SAPrductModel.external_id}.", sourceResponse.Content, userNo);

                                    l_CustomerProductCatalog.DeleteWithType(l_CustomerProductCatalog.ProductId, "REQ-ERR");
                                    l_CustomerProductCatalog.SaveData("REQ-ERR", sourceResponse.Content, userNo);
                                }
                                else
                                {
                                    route.SaveLog(LogTypeEnum.Debug, $"Item Sync request is accepted for {l_SCS_SAPrductModel.external_id}", string.Empty, userNo);
                                    l_CustomerProductCatalog.SaveData("REQ-JSON", sourceResponse.Content, userNo);
                                }

                                l_CustomerProductCatalog.UpdateStatus(l_SCS_SAPrductModel.external_id, l_SCS_SAPrductModel.relationship_type, l_Status,"", l_SourceConnector.CustomerID, Convert.ToInt32(itemsSA["RetryCount"] == DBNull.Value ? 0 : itemsSA["RetryCount"]) + 1);
                                //l_CustomerProductCatalog.DeleteProductCatalogDiscrepencies(l_SCS_VAPProductCatalogModel.parent.external_id);

                                route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);
                            }
                        }

                        if (filteredVAPVCItems.Any())
                        {
                            foreach (var itemVAP in filteredVAPVCItems)
                            {
                                if (itemVAP.Field<string>("VariationType").Equals("VAP"))
                                {

                                    string l_Status = "PENDING";
                                    CustomerProductCatalog l_Product = new CustomerProductCatalog();
                                    l_ProductCatalogErrorModel = new ProductCatalogErrorModel();

                                    l_SCS_VAPProductCatalogModel = new SCS_VAPProductCatalogModel();
                                    l_SCS_VAPProductCatalogModel.parent = JsonConvert.DeserializeObject<SCS_VAPProductCatalogModel.Parent>(itemVAP["JsonData"].ToString());

                                    l_SCS_VAPProductCatalogModel.parent.external_id = itemVAP["ItemID"].ToString();
                                    l_SCS_VAPProductCatalogModel.parent.relationship_type = itemVAP["VariationType"].ToString();

                                    var filteredVCItems = filteredVAPVCItems.Where(row => row.Field<string>("VariationType") == "VC"
                                                                && row.Field<string>("ParentID") == l_SCS_VAPProductCatalogModel.parent.external_id);

                                    if (filteredVCItems.Any())
                                    {
                                        foreach (var itemVC in filteredVCItems)
                                        {
                                            VCChild l_VCChild = new VCChild();

                                            l_VCChild = JsonConvert.DeserializeObject<VCChild>(itemVC["JsonData"].ToString());
                                            l_VCChild.external_id = itemVC["ItemID"].ToString();
                                            l_VCChild.relationship_type = itemVC["VariationType"].ToString();

                                            l_SCS_VAPProductCatalogModel.children.Add(l_VCChild);
                                        }
                                    }

                                    Body = JsonConvert.SerializeObject(l_SCS_VAPProductCatalogModel);
                                    route.SaveData("JSON-SNT", 0, Body, userNo);

                                    l_CustomerProductCatalog.ProductId = Convert.ToInt32(Convert.ToInt32(itemVAP["ProductId"].ToString()));
                                    l_CustomerProductCatalog.SaveData("REQ-SNT", Body, userNo);

                                    l_DestinationConnector.Url = l_DestinationConnector.BaseUrl + "product_variation_update";
                                    l_DestinationConnector.Method = "POST";

                                    sourceResponse = RestConnector.Execute(l_DestinationConnector, Body).GetAwaiter().GetResult();

                                    l_Product.UseConnection(l_SourceConnector.ConnectionString);
                                    l_Product.ProductId = Convert.ToInt32(itemVAP["ProductId"].ToString());

                                    if (sourceResponse.StatusCode == System.Net.HttpStatusCode.BadRequest)
                                    {
                                        //l_Status = "ERROR";

                                        if (!string.IsNullOrEmpty(sourceResponse.Content))
                                        {
                                            l_ProductCatalogErrorModel =  JsonConvert.DeserializeObject<ProductCatalogErrorModel>(sourceResponse.Content);

                                            if (!l_ProductCatalogErrorModel.errors.Any() == true && Convert.ToInt32(itemVAP["RetryCount"] == DBNull.Value ? 0 : itemVAP["RetryCount"]) >= 3)
                                            {
                                                l_Status = "ERROR";
                                            }

                                            if (l_ProductCatalogErrorModel.errors.Any() == true)
                                            {
                                                l_Status = "ERROR";
                                            }
                                        }

                                        l_Product.DeleteWithType(l_Product.ProductId, "REQ-ERR");
                                        l_Product.SaveData("REQ-ERR", sourceResponse.Content, userNo);

                                        route.SaveLog(LogTypeEnum.Error, $"BadRequest received from VAP Item Sync for item {l_SCS_VAPProductCatalogModel.parent.external_id}.", sourceResponse.Content, userNo);
                                    }
                                    else
                                    {
                                        l_Product.SaveData("REQ-JSON", sourceResponse.Content, userNo);

                                        route.SaveLog(LogTypeEnum.Debug, $"Item Sync request is accepted for {l_SCS_VAPProductCatalogModel.parent.external_id}", string.Empty, userNo);
                                    }

                                    route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);
                                    l_CustomerProductCatalog.UpdateStatus(l_SCS_VAPProductCatalogModel.parent.external_id, l_SCS_VAPProductCatalogModel.parent.relationship_type, l_Status,"", l_SourceConnector.CustomerID, Convert.ToInt32(itemVAP["RetryCount"] == DBNull.Value ? 0 : itemVAP["RetryCount"]) + 1);

                                    //l_CustomerProductCatalog.DeleteProductCatalogDiscrepencies(l_SCS_VAPProductCatalogModel.parent.external_id);
                                    if (filteredVCItems.Any())
                                    {
                                        foreach (var itemVC in filteredVCItems)
                                        {
                                            if (!String.IsNullOrEmpty(sourceResponse.Content))
                                            {
                                                SCSProductsResponse l_SCSProductsResponse = JsonConvert.DeserializeObject<SCSProductsResponse>(sourceResponse.Content);

                                                var filteredResults = l_SCSProductsResponse.results
                                                .Where(r => r.external_id == itemVC["ItemID"].ToString())
                                                .ToList();

                                                string l_ChildResponse = JsonConvert.SerializeObject(filteredResults);

                                                l_Product.UseConnection(l_SourceConnector.ConnectionString);

                                                l_Product.ProductId = Convert.ToInt32(itemVC["ProductId"].ToString());
                                                l_Product.SaveData("REQ-JSON", l_ChildResponse, userNo);
                                            }

                                            
                                            l_CustomerProductCatalog.UpdateStatus(itemVC["ItemID"].ToString(), itemVC["VariationType"].ToString(), l_Status, "", l_SourceConnector.CustomerID, Convert.ToInt32(itemVC["RetryCount"] == DBNull.Value ? 0 : itemVC["RetryCount"]) + 1);

                                            //l_CustomerProductCatalog.DeleteProductCatalogDiscrepencies(itemVC["ItemID"].ToString());
                                        }
                                    }
                                }
                            }
                        }

                        if (filteredUnlistedItems.Any())
                        {
                            foreach (var unlistedItems in filteredUnlistedItems)
                            {
                                if (string.IsNullOrEmpty(unlistedItems.Field<string>("id")))
                                    continue;

                                string destUrl = l_DestinationConnector.BaseUrl + "products/" + unlistedItems.Field<string>("id");

                                l_DestinationConnector.Url = destUrl;
                                l_DestinationConnector.Method = "GET";
                                l_ProductCatalogErrorModel = new ProductCatalogErrorModel();

                                route.SaveData("JSON-SNT", 0, l_DestinationConnector.Url, userNo);
                                l_CustomerProductCatalog.ProductId = Convert.ToInt32(unlistedItems["ProductId"]);

                                l_CustomerProductCatalog.SaveData("REQ-SNT", l_DestinationConnector.Url, userNo);

                                sourceResponse = RestConnector.Execute(l_DestinationConnector, "").GetAwaiter().GetResult();

                                SCS_ProductCatalogStatusResponseModel response = new SCS_ProductCatalogStatusResponseModel();

                                if (sourceResponse.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    route.SaveLog(LogTypeEnum.Debug, $"Get Product request is accepted for {response.external_id}", string.Empty, userNo);
                                    l_CustomerProductCatalog.SaveData("REQ-JSON", sourceResponse.Content, userNo);

                                    response = JsonConvert.DeserializeObject<SCS_ProductCatalogStatusResponseModel>(sourceResponse.Content);

                                    if (response != null)
                                    {
                                        destUrl = l_DestinationConnector.BaseUrl + "products/" + unlistedItems.Field<string>("id") + "/statuses/"+ response.product_statuses[0].id;

                                        l_DestinationConnector.Url = destUrl;
                                        l_DestinationConnector.Method = "PUT";

                                        var data = new
                                        {
                                            listing_status = "UNLISTED"
                                        };

                                        Body = string.Empty;

                                        Body = JsonConvert.SerializeObject(data);

                                        route.SaveData("JSON-SNT", 0, Body, userNo);
                                        l_CustomerProductCatalog.SaveData("REQ-SNT", l_DestinationConnector.Url, userNo);

                                        sourceResponse = RestConnector.Execute(l_DestinationConnector, Body).GetAwaiter().GetResult();

                                        string l_Status = "APPROVED";

                                        if (sourceResponse.StatusCode == System.Net.HttpStatusCode.BadRequest)
                                        {
                                            //l_Status = "ERROR";

                                            if (!string.IsNullOrEmpty(sourceResponse.Content))
                                            {
                                                l_ProductCatalogErrorModel = JsonConvert.DeserializeObject<ProductCatalogErrorModel>(sourceResponse.Content);

                                                if (!l_ProductCatalogErrorModel.errors.Any() == true && Convert.ToInt32(unlistedItems["RetryCount"] == DBNull.Value ? 0 : unlistedItems["RetryCount"]) >= 3)
                                                {
                                                    l_Status = "ERROR";
                                                }

                                                if (l_ProductCatalogErrorModel.errors.Any() == true)
                                                {
                                                    l_Status = "ERROR";
                                                }
                                            }

                                            route.SaveLog(LogTypeEnum.Error, $"BadRequest received from Unlist Item Sync for item {response.external_id}.", sourceResponse.Content, userNo);

                                            l_CustomerProductCatalog.DeleteWithType(l_CustomerProductCatalog.ProductId, "REQ-ERR");
                                            l_CustomerProductCatalog.SaveData("REQ-ERR", sourceResponse.Content, userNo);
                                        }
                                        else
                                        {
                                            route.SaveLog(LogTypeEnum.Debug, $"Unlist Item Sync request is accepted for {response.external_id}", string.Empty, userNo);
                                            l_CustomerProductCatalog.SaveData("REQ-JSON", sourceResponse.Content, userNo);
                                        }

                                        l_CustomerProductCatalog.UpdateStatus(response.external_id, response.relationship_type, l_Status, "", l_SourceConnector.CustomerID, Convert.ToInt32(unlistedItems["RetryCount"] == DBNull.Value ? 0 : unlistedItems["RetryCount"]) + 1);
                                        l_CustomerProductCatalog.DeleteProductCatalogDiscrepencies(response.external_id);
                                        route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);
                                    }
                                }
                            }
                        }

                        route.SaveLog(LogTypeEnum.Debug, "Destination connector processing completed.", string.Empty, userNo);
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
                l_data.Dispose();
            }
        }
    }
}
