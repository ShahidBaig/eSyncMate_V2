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
        public static void Execute(IConfiguration config, Routes route)
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
                ConnectorDataModel? l_SourceConnector = ConnectorDataModel.Deserialize(route.SourceConnectorObject.Data);
                ConnectorDataModel? l_DestinationConnector = ConnectorDataModel.Deserialize(route.DestinationConnectorObject.Data);

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
                                try
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

                                    l_CustomerProductCatalog.DeleteWithType(l_CustomerProductCatalog.ProductId, "PRD-REQ");
                                    l_CustomerProductCatalog.SaveData("PRD-REQ", CommonUtils.DescribeRequest(l_DestinationConnector, l_SCS_SAPrductModel), userNo);

                                    string l_UnnamedFieldError = GetUnnamedFieldError(l_SCS_SAPrductModel.fields.Select(f => (f.name, f.value)), l_SCS_SAPrductModel.external_id);

                                    if (!string.IsNullOrEmpty(l_UnnamedFieldError))
                                    {
                                        l_CustomerProductCatalog.DeleteWithType(l_CustomerProductCatalog.ProductId, "PRD-ERR");
                                        l_CustomerProductCatalog.SaveData("PRD-ERR", l_UnnamedFieldError, userNo);

                                        l_CustomerProductCatalog.UpdateStatus(l_SCS_SAPrductModel.external_id, l_SCS_SAPrductModel.relationship_type, "ERROR", "", l_SourceConnector.CustomerID, Convert.ToInt32(itemsSA["RetryCount"] == DBNull.Value ? 0 : itemsSA["RetryCount"]) + 1);

                                        route.SaveLog(LogTypeEnum.Error, $"SA item {l_SCS_SAPrductModel.external_id} [{itemsSA["ItemTypeName"]}] has a column without an attribute name so Target would reject the whole item. Marked as ERROR and not sent.", l_UnnamedFieldError, userNo);

                                        continue;
                                    }

                                    sourceResponse = RestConnector.Execute(l_DestinationConnector, Body).GetAwaiter().GetResult();

                                    string l_Status = "PENDING";
                                    string l_PayloadError = sourceResponse.IsSuccessStatusCode
                                        ? GetResponsePayloadError(sourceResponse.Content, l_SCS_SAPrductModel.external_id)
                                        : string.Empty;

                                    if (sourceResponse.IsSuccessStatusCode && string.IsNullOrEmpty(l_PayloadError))
                                    {
                                        route.SaveLog(LogTypeEnum.Debug, $"Item Sync request is accepted for {l_SCS_SAPrductModel.external_id}", string.Empty, userNo);
                                        l_CustomerProductCatalog.DeleteWithType(l_CustomerProductCatalog.ProductId, "PRD-RSP");
                                        l_CustomerProductCatalog.SaveData("PRD-RSP", sourceResponse.Content, userNo);
                                    }
                                    else if (sourceResponse.IsSuccessStatusCode)
                                    {
                                        l_Status = "ERROR";

                                        route.SaveLog(LogTypeEnum.Error, $"Error in the response payload from SA Item Sync for item {l_SCS_SAPrductModel.external_id} ({l_PayloadError}). Marked as ERROR.", sourceResponse.Content, userNo);

                                        l_CustomerProductCatalog.DeleteWithType(l_CustomerProductCatalog.ProductId, "PRD-ERR");
                                        l_CustomerProductCatalog.SaveData("PRD-ERR", sourceResponse.Content, userNo);
                                    }
                                    else
                                    {
                                        if (CommonUtils.IsTransientResponse(sourceResponse) && !IsRetryExhausted(itemsSA))
                                        {
                                            l_Status = "PENDING";

                                            route.SaveLog(LogTypeEnum.Warning, $"Transient error ({(sourceResponse.ResponseStatus == ResponseStatus.TimedOut ? "Timeout" : (int)sourceResponse.StatusCode + " " + sourceResponse.StatusCode)}) from SA Item Sync for item {l_SCS_SAPrductModel.external_id}. Item will be retried.", sourceResponse.Content, userNo);
                                        }
                                        else if (CommonUtils.IsTransientResponse(sourceResponse))
                                        {
                                            l_Status = "ERROR";

                                            route.SaveLog(LogTypeEnum.Error, $"SA Item Sync for item {l_SCS_SAPrductModel.external_id} kept failing with a transient error and reached the retry limit of {CommonUtils.ProductCatalogMaxRetryCount}. Marked as ERROR.", sourceResponse.Content, userNo);
                                        }
                                        else
                                        {
                                            l_Status = "ERROR";

                                            route.SaveLog(LogTypeEnum.Error, $"Error ({(int)sourceResponse.StatusCode} {sourceResponse.StatusCode}) from SA Item Sync for item {l_SCS_SAPrductModel.external_id}. Marked as ERROR.", sourceResponse.Content, userNo);
                                        }

                                        l_CustomerProductCatalog.DeleteWithType(l_CustomerProductCatalog.ProductId, "PRD-ERR");
                                        l_CustomerProductCatalog.SaveData("PRD-ERR", sourceResponse.Content, userNo);
                                    }

                                    l_CustomerProductCatalog.UpdateStatus(l_SCS_SAPrductModel.external_id, l_SCS_SAPrductModel.relationship_type, l_Status, "", l_SourceConnector.CustomerID, Convert.ToInt32(itemsSA["RetryCount"] == DBNull.Value ? 0 : itemsSA["RetryCount"]) + 1);
                                    //l_CustomerProductCatalog.DeleteProductCatalogDiscrepencies(l_SCS_VAPProductCatalogModel.parent.external_id);

                                    route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);
                                }
                                catch (Exception itemEx)
                                {
                                    route.SaveLog(LogTypeEnum.Exception, $"Error processing SA item [{itemsSA["ItemID"]}].", itemEx.ToString(), userNo);

                                    MarkItemFailed(route, l_CustomerProductCatalog, l_SourceConnector.CustomerID, itemsSA, itemEx, userNo);
                                }
                            }
                        }

                        if (filteredVAPVCItems.Any())
                        {
                            foreach (var itemVAP in filteredVAPVCItems)
                            {
                                // Set once the group has been posted, so a crash afterwards does not push
                                // children that were already answered for back to ERROR.
                                bool l_GroupSent = false;

                                try
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

                                        l_Product.UseConnection(l_SourceConnector.ConnectionString);

                                        string l_ParentUnnamedFieldError = GetUnnamedFieldError(l_SCS_VAPProductCatalogModel.parent.fields.Select(f => (f.name, f.value)), l_SCS_VAPProductCatalogModel.parent.external_id);

                                        if (!string.IsNullOrEmpty(l_ParentUnnamedFieldError))
                                        {
                                            // The parent fails the whole request, so the group is not sent at all.
                                            l_Product.ProductId = Convert.ToInt32(itemVAP["ProductId"].ToString());
                                            l_Product.DeleteWithType(l_Product.ProductId, "PRD-ERR");
                                            l_Product.SaveData("PRD-ERR", l_ParentUnnamedFieldError, userNo);

                                            l_CustomerProductCatalog.UpdateStatus(l_SCS_VAPProductCatalogModel.parent.external_id, l_SCS_VAPProductCatalogModel.parent.relationship_type, "ERROR", "", l_SourceConnector.CustomerID, Convert.ToInt32(itemVAP["RetryCount"] == DBNull.Value ? 0 : itemVAP["RetryCount"]) + 1);

                                            route.SaveLog(LogTypeEnum.Error, $"VAP item {l_SCS_VAPProductCatalogModel.parent.external_id} [{itemVAP["ItemTypeName"]}] has a column without an attribute name so Target would reject the whole group. Marked as ERROR and not sent.", l_ParentUnnamedFieldError, userNo);

                                            MarkChildrenFailed(route, l_Product, l_SourceConnector.CustomerID,
                                                filteredVAPVCItems.Where(row => row.Field<string>("VariationType") == "VC"
                                                                             && row.Field<string>("ParentID") == l_SCS_VAPProductCatalogModel.parent.external_id),
                                                l_SCS_VAPProductCatalogModel.parent.external_id, userNo);

                                            continue;
                                        }

                                        var filteredVCItems = filteredVAPVCItems.Where(row => row.Field<string>("VariationType") == "VC"
                                                                    && row.Field<string>("ParentID") == l_SCS_VAPProductCatalogModel.parent.external_id);

                                        // Children left out of the payload because of an unnamed field, so the
                                        // response loop below does not overwrite the ERROR set here.
                                        HashSet<string> l_SkippedVCItems = new HashSet<string>();

                                        if (filteredVCItems.Any())
                                        {
                                            foreach (var itemVC in filteredVCItems)
                                            {
                                                VCChild l_VCChild = new VCChild();

                                                l_VCChild = JsonConvert.DeserializeObject<VCChild>(itemVC["JsonData"].ToString());
                                                l_VCChild.external_id = itemVC["ItemID"].ToString();
                                                l_VCChild.relationship_type = itemVC["VariationType"].ToString();

                                                string l_ChildUnnamedFieldError = GetUnnamedFieldError(l_VCChild.fields.Select(f => (f.name, f.value)), l_VCChild.external_id);

                                                if (!string.IsNullOrEmpty(l_ChildUnnamedFieldError))
                                                {
                                                    l_Product.ProductId = Convert.ToInt32(itemVC["ProductId"].ToString());
                                                    l_Product.DeleteWithType(l_Product.ProductId, "PRD-ERR");
                                                    l_Product.SaveData("PRD-ERR", l_ChildUnnamedFieldError, userNo);

                                                    l_CustomerProductCatalog.UpdateStatus(l_VCChild.external_id, l_VCChild.relationship_type, "ERROR", "", l_SourceConnector.CustomerID, Convert.ToInt32(itemVC["RetryCount"] == DBNull.Value ? 0 : itemVC["RetryCount"]) + 1);

                                                    route.SaveLog(LogTypeEnum.Error, $"VC item {l_VCChild.external_id} [{itemVC["ItemTypeName"]}] has a column without an attribute name so Target would reject the whole group. Marked as ERROR and left out of the payload.", l_ChildUnnamedFieldError, userNo);

                                                    l_SkippedVCItems.Add(l_VCChild.external_id);

                                                    continue;
                                                }

                                                l_SCS_VAPProductCatalogModel.children.Add(l_VCChild);
                                            }
                                        }

                                        Body = JsonConvert.SerializeObject(l_SCS_VAPProductCatalogModel);
                                        route.SaveData("JSON-SNT", 0, Body, userNo);

                                        // Set before the request is logged so the saved copy names the endpoint it went to.
                                        l_DestinationConnector.Url = l_DestinationConnector.BaseUrl + "product_variation_update";
                                        l_DestinationConnector.Method = "POST";

                                        l_CustomerProductCatalog.ProductId = Convert.ToInt32(Convert.ToInt32(itemVAP["ProductId"].ToString()));
                                        l_CustomerProductCatalog.DeleteWithType(l_CustomerProductCatalog.ProductId, "PRD-REQ");
                                        l_CustomerProductCatalog.SaveData("PRD-REQ", CommonUtils.DescribeRequest(l_DestinationConnector, l_SCS_VAPProductCatalogModel), userNo);

                                        sourceResponse = RestConnector.Execute(l_DestinationConnector, Body).GetAwaiter().GetResult();

                                        l_GroupSent = true;

                                        l_Product.UseConnection(l_SourceConnector.ConnectionString);
                                        l_Product.ProductId = Convert.ToInt32(itemVAP["ProductId"].ToString());

                                        string l_ParentPayloadError = sourceResponse.IsSuccessStatusCode
                                            ? GetResponsePayloadError(sourceResponse.Content, l_SCS_VAPProductCatalogModel.parent.external_id)
                                            : string.Empty;

                                        if (sourceResponse.IsSuccessStatusCode && string.IsNullOrEmpty(l_ParentPayloadError))
                                        {
                                            l_Product.DeleteWithType(l_Product.ProductId, "PRD-RSP");
                                            l_Product.SaveData("PRD-RSP", sourceResponse.Content, userNo);

                                            route.SaveLog(LogTypeEnum.Debug, $"Item Sync request is accepted for {l_SCS_VAPProductCatalogModel.parent.external_id}", string.Empty, userNo);
                                        }
                                        else if (sourceResponse.IsSuccessStatusCode)
                                        {
                                            l_Status = "ERROR";

                                            route.SaveLog(LogTypeEnum.Error, $"Error in the response payload from VAP Item Sync for item {l_SCS_VAPProductCatalogModel.parent.external_id} ({l_ParentPayloadError}). Marked as ERROR.", sourceResponse.Content, userNo);

                                            l_Product.DeleteWithType(l_Product.ProductId, "PRD-ERR");
                                            l_Product.SaveData("PRD-ERR", sourceResponse.Content, userNo);
                                        }
                                        else
                                        {
                                            if (CommonUtils.IsTransientResponse(sourceResponse) && !IsRetryExhausted(itemVAP))
                                            {
                                                l_Status = "PENDING";

                                                route.SaveLog(LogTypeEnum.Warning, $"Transient error ({(sourceResponse.ResponseStatus == ResponseStatus.TimedOut ? "Timeout" : (int)sourceResponse.StatusCode + " " + sourceResponse.StatusCode)}) from VAP Item Sync for item {l_SCS_VAPProductCatalogModel.parent.external_id}. Item will be retried.", sourceResponse.Content, userNo);
                                            }
                                            else if (CommonUtils.IsTransientResponse(sourceResponse))
                                            {
                                                l_Status = "ERROR";

                                                route.SaveLog(LogTypeEnum.Error, $"VAP Item Sync for item {l_SCS_VAPProductCatalogModel.parent.external_id} kept failing with a transient error and reached the retry limit of {CommonUtils.ProductCatalogMaxRetryCount}. Marked as ERROR.", sourceResponse.Content, userNo);
                                            }
                                            else
                                            {
                                                l_Status = "ERROR";

                                                route.SaveLog(LogTypeEnum.Error, $"Error ({(int)sourceResponse.StatusCode} {sourceResponse.StatusCode}) from VAP Item Sync for item {l_SCS_VAPProductCatalogModel.parent.external_id}. Marked as ERROR.", sourceResponse.Content, userNo);
                                            }

                                            l_Product.DeleteWithType(l_Product.ProductId, "PRD-ERR");
                                            l_Product.SaveData("PRD-ERR", sourceResponse.Content, userNo);
                                        }

                                        route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);
                                        l_CustomerProductCatalog.UpdateStatus(l_SCS_VAPProductCatalogModel.parent.external_id, l_SCS_VAPProductCatalogModel.parent.relationship_type, l_Status, "", l_SourceConnector.CustomerID, Convert.ToInt32(itemVAP["RetryCount"] == DBNull.Value ? 0 : itemVAP["RetryCount"]) + 1);

                                        //l_CustomerProductCatalog.DeleteProductCatalogDiscrepencies(l_SCS_VAPProductCatalogModel.parent.external_id);
                                        if (filteredVCItems.Any())
                                        {
                                            foreach (var itemVC in filteredVCItems)
                                            {
                                                if (l_SkippedVCItems.Contains(Convert.ToString(itemVC["ItemID"])))
                                                {
                                                    continue;
                                                }

                                                l_Product.UseConnection(l_SourceConnector.ConnectionString);
                                                l_Product.ProductId = Convert.ToInt32(itemVC["ProductId"].ToString());

                                                // The child can fail inside an accepted response, so it carries its own status
                                                string l_ChildStatus = l_Status;
                                                string l_ChildPayloadError = sourceResponse.IsSuccessStatusCode
                                                    ? GetResponsePayloadError(sourceResponse.Content, itemVC["ItemID"].ToString())
                                                    : string.Empty;

                                                if (!sourceResponse.IsSuccessStatusCode)
                                                {
                                                    l_Product.DeleteWithType(l_Product.ProductId, "PRD-ERR");
                                                    l_Product.SaveData("PRD-ERR", sourceResponse.Content, userNo);

                                                    route.SaveLog(LogTypeEnum.Error, $"Error ({(int)sourceResponse.StatusCode} {sourceResponse.StatusCode}) from VC Item Sync for item {itemVC["ItemID"]}.", sourceResponse.Content, userNo);
                                                }
                                                else if (!string.IsNullOrEmpty(l_ChildPayloadError))
                                                {
                                                    l_ChildStatus = "ERROR";

                                                    l_Product.DeleteWithType(l_Product.ProductId, "PRD-ERR");
                                                    l_Product.SaveData("PRD-ERR", sourceResponse.Content, userNo);

                                                    route.SaveLog(LogTypeEnum.Error, $"Error in the response payload from VC Item Sync for item {itemVC["ItemID"]} ({l_ChildPayloadError}). Marked as ERROR.", sourceResponse.Content, userNo);
                                                }
                                                else if (!String.IsNullOrEmpty(sourceResponse.Content))
                                                {
                                                    SCSProductsResponse l_SCSProductsResponse = JsonConvert.DeserializeObject<SCSProductsResponse>(sourceResponse.Content);

                                                    string l_ChildResponse = "[]";

                                                    if (l_SCSProductsResponse != null && l_SCSProductsResponse.results != null)
                                                    {
                                                        var filteredResults = l_SCSProductsResponse.results
                                                        .Where(r => r.external_id == itemVC["ItemID"].ToString())
                                                        .ToList();

                                                        l_ChildResponse = JsonConvert.SerializeObject(filteredResults);
                                                    }

                                                    l_Product.DeleteWithType(l_Product.ProductId, "PRD-RSP");
                                                    l_Product.SaveData("PRD-RSP", l_ChildResponse, userNo);
                                                }


                                                l_CustomerProductCatalog.UpdateStatus(itemVC["ItemID"].ToString(), itemVC["VariationType"].ToString(), l_ChildStatus, "", l_SourceConnector.CustomerID, Convert.ToInt32(itemVC["RetryCount"] == DBNull.Value ? 0 : itemVC["RetryCount"]) + 1);

                                                //l_CustomerProductCatalog.DeleteProductCatalogDiscrepencies(itemVC["ItemID"].ToString());
                                            }
                                        }
                                    }
                                }
                                catch (Exception itemEx)
                                {
                                    route.SaveLog(LogTypeEnum.Exception, $"Error processing VAP/VC item [{itemVAP["ItemID"]}].", itemEx.ToString(), userNo);

                                    MarkItemFailed(route, l_CustomerProductCatalog, l_SourceConnector.CustomerID, itemVAP, itemEx, userNo);

                                    if (!l_GroupSent)
                                    {
                                        MarkChildrenFailed(route, l_CustomerProductCatalog, l_SourceConnector.CustomerID,
                                            filteredVAPVCItems.Where(row => row.Field<string>("VariationType") == "VC"
                                                                         && row.Field<string>("ParentID") == Convert.ToString(itemVAP["ItemID"])),
                                            Convert.ToString(itemVAP["ItemID"]), userNo);
                                    }
                                }
                            }
                        }

                        if (filteredUnlistedItems.Any())
                        {
                            foreach (var unlistedItems in filteredUnlistedItems)
                            {
                                try
                                {
                                    if (string.IsNullOrEmpty(unlistedItems.Field<string>("id")))
                                        continue;

                                    string destUrl = l_DestinationConnector.BaseUrl + "products/" + unlistedItems.Field<string>("id");

                                    l_DestinationConnector.Url = destUrl;
                                    l_DestinationConnector.Method = "GET";
                                    l_ProductCatalogErrorModel = new ProductCatalogErrorModel();

                                    route.SaveData("JSON-SNT", 0, l_DestinationConnector.Url, userNo);
                                    l_CustomerProductCatalog.ProductId = Convert.ToInt32(unlistedItems["ProductId"]);

                                    l_CustomerProductCatalog.DeleteWithType(l_CustomerProductCatalog.ProductId, "UNL-REQ");
                                    l_CustomerProductCatalog.SaveData("UNL-REQ", CommonUtils.DescribeRequest(l_DestinationConnector, null), userNo);

                                    sourceResponse = RestConnector.Execute(l_DestinationConnector, "").GetAwaiter().GetResult();

                                    SCS_ProductCatalogStatusResponseModel response = new SCS_ProductCatalogStatusResponseModel();

                                    if (sourceResponse.IsSuccessStatusCode)
                                    {
                                        route.SaveLog(LogTypeEnum.Debug, $"Get Product request is accepted for {response.external_id}", string.Empty, userNo);
                                        l_CustomerProductCatalog.DeleteWithType(l_CustomerProductCatalog.ProductId, "UNL-RSP");
                                        l_CustomerProductCatalog.SaveData("UNL-RSP", sourceResponse.Content, userNo);

                                        response = JsonConvert.DeserializeObject<SCS_ProductCatalogStatusResponseModel>(sourceResponse.Content);

                                        if (response != null && response.product_statuses != null && response.product_statuses.Any())
                                        {
                                            destUrl = l_DestinationConnector.BaseUrl + "products/" + unlistedItems.Field<string>("id") + "/statuses/" + response.product_statuses[0].id;

                                            l_DestinationConnector.Url = destUrl;
                                            l_DestinationConnector.Method = "PUT";

                                            var data = new
                                            {
                                                listing_status = "UNLISTED"
                                            };

                                            Body = string.Empty;

                                            Body = JsonConvert.SerializeObject(data);

                                            route.SaveData("JSON-SNT", 0, Body, userNo);
                                            l_CustomerProductCatalog.DeleteWithType(l_CustomerProductCatalog.ProductId, "UNL-REQ");
                                            l_CustomerProductCatalog.SaveData("UNL-REQ", CommonUtils.DescribeRequest(l_DestinationConnector, data), userNo);

                                            sourceResponse = RestConnector.Execute(l_DestinationConnector, Body).GetAwaiter().GetResult();

                                            string l_Status = "APPROVED";
                                            bool l_Retryable = false;
                                            string l_UnlistPayloadError = sourceResponse.IsSuccessStatusCode
                                                ? GetResponsePayloadError(sourceResponse.Content, response.external_id)
                                                : string.Empty;

                                            if (sourceResponse.IsSuccessStatusCode && string.IsNullOrEmpty(l_UnlistPayloadError))
                                            {
                                                route.SaveLog(LogTypeEnum.Debug, $"Unlist Item Sync request is accepted for {response.external_id}", string.Empty, userNo);
                                                l_CustomerProductCatalog.DeleteWithType(l_CustomerProductCatalog.ProductId, "UNL-RSP");
                                                l_CustomerProductCatalog.SaveData("UNL-RSP", sourceResponse.Content, userNo);
                                            }
                                            else if (sourceResponse.IsSuccessStatusCode)
                                            {
                                                l_Status = "ERROR";

                                                route.SaveLog(LogTypeEnum.Error, $"Error in the response payload from Unlist Item Sync for item {response.external_id} ({l_UnlistPayloadError}). Marked as ERROR.", sourceResponse.Content, userNo);

                                                l_CustomerProductCatalog.DeleteWithType(l_CustomerProductCatalog.ProductId, "UNL-ERR");
                                                l_CustomerProductCatalog.SaveData("UNL-ERR", sourceResponse.Content, userNo);
                                            }
                                            else
                                            {
                                                if (CommonUtils.IsTransientResponse(sourceResponse) && !IsRetryExhausted(unlistedItems))
                                                {
                                                    l_Retryable = true;

                                                    route.SaveLog(LogTypeEnum.Warning, $"Transient error ({(sourceResponse.ResponseStatus == ResponseStatus.TimedOut ? "Timeout" : (int)sourceResponse.StatusCode + " " + sourceResponse.StatusCode)}) from Unlist Item Sync for item {response.external_id}. Item will be retried.", sourceResponse.Content, userNo);
                                                }
                                                else if (CommonUtils.IsTransientResponse(sourceResponse))
                                                {
                                                    l_Status = "ERROR";

                                                    route.SaveLog(LogTypeEnum.Error, $"Unlist Item Sync for item {response.external_id} kept failing with a transient error and reached the retry limit of {CommonUtils.ProductCatalogMaxRetryCount}. Marked as ERROR.", sourceResponse.Content, userNo);
                                                }
                                                else
                                                {
                                                    l_Status = "ERROR";

                                                    route.SaveLog(LogTypeEnum.Error, $"Error ({(int)sourceResponse.StatusCode} {sourceResponse.StatusCode}) from Unlist Item Sync for item {response.external_id}. Marked as ERROR.", sourceResponse.Content, userNo);
                                                }

                                                l_CustomerProductCatalog.DeleteWithType(l_CustomerProductCatalog.ProductId, "UNL-ERR");
                                                l_CustomerProductCatalog.SaveData("UNL-ERR", sourceResponse.Content, userNo);
                                            }

                                            if (!l_Retryable)
                                            {
                                                l_CustomerProductCatalog.UpdateStatus(response.external_id, response.relationship_type, l_Status, "", l_SourceConnector.CustomerID, Convert.ToInt32(unlistedItems["RetryCount"] == DBNull.Value ? 0 : unlistedItems["RetryCount"]) + 1);
                                                l_CustomerProductCatalog.DeleteProductCatalogDiscrepencies(response.external_id);
                                            }

                                            route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);
                                        }
                                    }
                                    else
                                    {
                                        l_CustomerProductCatalog.DeleteWithType(l_CustomerProductCatalog.ProductId, "UNL-ERR");
                                        l_CustomerProductCatalog.SaveData("UNL-ERR", sourceResponse.Content, userNo);

                                        if (CommonUtils.IsTransientResponse(sourceResponse) && !IsRetryExhausted(unlistedItems))
                                        {
                                            route.SaveLog(LogTypeEnum.Warning, $"Transient error ({(sourceResponse.ResponseStatus == ResponseStatus.TimedOut ? "Timeout" : (int)sourceResponse.StatusCode + " " + sourceResponse.StatusCode)}) getting product for unlist item id [{unlistedItems.Field<string>("id")}]. Item will be retried.", sourceResponse.Content, userNo);
                                        }
                                        else
                                        {
                                            // A definite error (or the retry limit) must not leave the item unlisting for ever.
                                            l_CustomerProductCatalog.UpdateStatus(Convert.ToString(unlistedItems["ItemID"]), Convert.ToString(unlistedItems["VariationType"]), "ERROR", "", l_SourceConnector.CustomerID, Convert.ToInt32(unlistedItems["RetryCount"] == DBNull.Value ? 0 : unlistedItems["RetryCount"]) + 1);

                                            route.SaveLog(LogTypeEnum.Error, $"Error ({(int)sourceResponse.StatusCode} {sourceResponse.StatusCode}) getting product for unlist item id [{unlistedItems.Field<string>("id")}]. Marked as ERROR.", sourceResponse.Content, userNo);
                                        }
                                    }
                                }
                                catch (Exception itemEx)
                                {
                                    route.SaveLog(LogTypeEnum.Exception, $"Error processing unlist item id [{unlistedItems.Field<string>("id")}].", itemEx.ToString(), userNo);

                                    MarkItemFailed(route, l_CustomerProductCatalog, l_SourceConnector.CustomerID, unlistedItems, itemEx, userNo);
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

        /// <summary>
        /// Target answers 2xx even when the payload itself carries the failure, either as a top level
        /// { "message": "Conflict", "errors": [ ... ] } or as a per item result whose status is not 2xx.
        /// Returns the readable reason, or an empty string when the response really did succeed.
        /// Pass p_ExternalId to only look at that item inside a multi item response.
        /// Listing errors under product.product_statuses are deliberately ignored: those describe the
        /// listing being validated by the marketplace, not the acceptance of this request, and they are
        /// handled by ProductCatalogStatusRoute.
        /// </summary>
        /// <summary>
        /// Target rejects a field that carries no name ("fields[n].name must not be empty") and fails the
        /// whole request, so such an item is marked ERROR instead of being sent. The message is written in
        /// the shape the rejected items export reads, and names the position and the value of every unnamed
        /// field so the column can be found in the uploaded file. Empty string when every field is named.
        /// </summary>
        private static string GetUnnamedFieldError(IEnumerable<(string Name, string Value)> p_Fields, string p_ExternalId)
        {
            if (p_Fields == null)
            {
                return string.Empty;
            }

            List<(string Name, string Value)> l_Fields = p_Fields.ToList();
            List<string> l_Errors = new List<string>();

            for (int l_Index = 0; l_Index < l_Fields.Count; l_Index++)
            {
                if (!string.IsNullOrWhiteSpace(l_Fields[l_Index].Name))
                {
                    continue;
                }

                l_Errors.Add($"Column {l_Index + 1} of {l_Fields.Count} was sent without an attribute name. "
                           + $"The value it carried is '{GetShortValue(l_Fields[l_Index].Value)}'. "
                           + "This column has no attribute mapping for this item type. "
                           + "Add the mapping in the item type attribute setup and upload the catalog file again.");
            }

            if (l_Errors.Count == 0)
            {
                return string.Empty;
            }

            return JsonConvert.SerializeObject(new ProductCatalogErrorModel
            {
                message = $"Item {p_ExternalId} was not sent to Target because {l_Errors.Count} column(s) have no attribute name. Target rejects the whole item when a field has no name.",
                errors = l_Errors.ToArray()
            });
        }

        /// <summary>
        /// Keeps a value readable inside the rejected items CSV, which is not quoted, so separators
        /// and line breaks would otherwise shift the columns.
        /// </summary>
        private static string GetShortValue(string p_Value, int p_MaxLength = 40)
        {
            if (string.IsNullOrEmpty(p_Value))
            {
                return string.Empty;
            }

            string l_Value = p_Value.Replace(',', ' ').Replace(';', ' ').Replace('\r', ' ').Replace('\n', ' ').Trim();

            return l_Value.Length <= p_MaxLength ? l_Value : l_Value.Substring(0, p_MaxLength) + "…";
        }

        /// <summary>
        /// True when an item has already been retried as often as CommonUtils.ProductCatalogMaxRetryCount
        /// allows, so it must stop being retried and be marked ERROR instead. A transient failure that never
        /// clears would otherwise keep the item in the queue for ever.
        /// </summary>
        internal static bool IsRetryExhausted(DataRow p_Row)
        {
            if (CommonUtils.ProductCatalogMaxRetryCount <= 0)
            {
                return false;
            }

            int l_RetryCount = p_Row.Table.Columns.Contains("RetryCount") && p_Row["RetryCount"] != DBNull.Value
                             ? Convert.ToInt32(p_Row["RetryCount"]) : 0;

            return l_RetryCount + 1 >= CommonUtils.ProductCatalogMaxRetryCount;
        }

        /// <summary>
        /// True when the failure comes from the platform (database or network) rather than from the item's
        /// own data. Such an item must keep its current status so it is retried once the platform recovers,
        /// otherwise a short outage would permanently mark every item in the run as ERROR.
        /// </summary>
        private static bool IsInfrastructureException(Exception p_Exception)
        {
            for (Exception? l_Exception = p_Exception; l_Exception != null; l_Exception = l_Exception.InnerException)
            {
                if (l_Exception is System.Data.Common.DbException
                    || l_Exception is TimeoutException
                    || l_Exception is System.Net.Http.HttpRequestException
                    || l_Exception is TaskCanceledException
                    || l_Exception is System.Net.Sockets.SocketException
                    || l_Exception is OutOfMemoryException)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// A crash while preparing or saving an item used to leave it on its old status, so it was retried on
        /// every run and never reached the rejected items list. A data problem is permanent, so the item is
        /// marked ERROR with a readable reason; a platform problem is left untouched to be retried.
        /// Never throws: failing to record the failure must not stop the rest of the run.
        /// </summary>
        internal static void MarkItemFailed(Routes route, CustomerProductCatalog p_Catalog, string p_CustomerID, DataRow p_Row, Exception p_Exception, int userNo)
        {
            try
            {
                string l_ItemID = Convert.ToString(p_Row["ItemID"]);

                if (IsInfrastructureException(p_Exception))
                {
                    route.SaveLog(LogTypeEnum.Warning, $"Item {l_ItemID} could not be processed because of a database or network failure. Status left unchanged so the item is retried on the next run.", p_Exception.Message, userNo);

                    return;
                }

                string l_VariationType = p_Row.Table.Columns.Contains("VariationType") ? Convert.ToString(p_Row["VariationType"]) : string.Empty;
                int l_RetryCount = p_Row.Table.Columns.Contains("RetryCount") && p_Row["RetryCount"] != DBNull.Value
                                 ? Convert.ToInt32(p_Row["RetryCount"]) : 0;

                string l_Error = JsonConvert.SerializeObject(new ProductCatalogErrorModel
                {
                    message = $"Item {l_ItemID} could not be prepared for Target because its saved product data could not be read.",
                    errors = new[]
                    {
                        $"The item failed with '{GetShortValue(p_Exception.Message, 200)}'.",
                        "This item is not sent to Target until its data is corrected. Upload the catalog file for this item again."
                    }
                });

                p_Catalog.ProductId = Convert.ToInt32(p_Row["ProductId"]);
                p_Catalog.DeleteWithType(p_Catalog.ProductId, "PRD-ERR");
                p_Catalog.SaveData("PRD-ERR", l_Error, userNo);

                p_Catalog.UpdateStatus(l_ItemID, l_VariationType, "ERROR", "", p_CustomerID, l_RetryCount + 1);

                route.SaveLog(LogTypeEnum.Error, $"Item {l_ItemID} could not be processed because of its own data. Marked as ERROR.", l_Error, userNo);
            }
            catch (Exception l_MarkException)
            {
                route.SaveLog(LogTypeEnum.Exception, "Failed to record the item failure.", l_MarkException.ToString(), userNo);
            }
        }

        /// <summary>
        /// A VC child is only ever sent together with its VAP parent, so when the parent never leaves the
        /// route its children would sit at PENDING for ever and be polled on every run. They are marked
        /// ERROR with the parent as the reason. Never throws.
        /// </summary>
        internal static void MarkChildrenFailed(Routes route, CustomerProductCatalog p_Catalog, string p_CustomerID, IEnumerable<DataRow> p_Children, string p_ParentItemID, int userNo)
        {
            foreach (DataRow l_Child in p_Children)
            {
                try
                {
                    string l_ChildItemID = Convert.ToString(l_Child["ItemID"]);

                    string l_Error = JsonConvert.SerializeObject(new ProductCatalogErrorModel
                    {
                        message = $"Item {l_ChildItemID} was not sent to Target because its parent item {p_ParentItemID} failed.",
                        errors = new[]
                        {
                            $"A variation is only sent together with its parent. Correct parent item {p_ParentItemID} first and this item goes out with it."
                        }
                    });

                    p_Catalog.ProductId = Convert.ToInt32(l_Child["ProductId"]);
                    p_Catalog.DeleteWithType(p_Catalog.ProductId, "PRD-ERR");
                    p_Catalog.SaveData("PRD-ERR", l_Error, userNo);

                    p_Catalog.UpdateStatus(l_ChildItemID, Convert.ToString(l_Child["VariationType"]), "ERROR", "", p_CustomerID,
                        Convert.ToInt32(l_Child["RetryCount"] == DBNull.Value ? 0 : l_Child["RetryCount"]) + 1);

                    route.SaveLog(LogTypeEnum.Error, $"VC item {l_ChildItemID} marked as ERROR because its parent {p_ParentItemID} failed.", l_Error, userNo);
                }
                catch (Exception l_MarkException)
                {
                    route.SaveLog(LogTypeEnum.Exception, "Failed to record the child item failure.", l_MarkException.ToString(), userNo);
                }
            }
        }

        internal static string GetResponsePayloadError(string p_Content, string p_ExternalId = "")
        {
            if (string.IsNullOrWhiteSpace(p_Content))
            {
                return string.Empty;
            }

            string l_Trimmed = p_Content.TrimStart();

            if (l_Trimmed.StartsWith("{"))
            {
                try
                {
                    ProductCatalogErrorModel l_Error = JsonConvert.DeserializeObject<ProductCatalogErrorModel>(p_Content);

                    if (l_Error != null && l_Error.errors != null && l_Error.errors.Length > 0)
                    {
                        return (string.IsNullOrEmpty(l_Error.message) ? string.Empty : l_Error.message + ": ") + string.Join(" | ", l_Error.errors);
                    }
                }
                catch
                {
                    // Not the error shape, fall through to the results shape below
                }
            }

            List<ProductResult> l_Results = null;

            try
            {
                if (l_Trimmed.StartsWith("["))
                {
                    l_Results = JsonConvert.DeserializeObject<List<ProductResult>>(p_Content);
                }
                else if (l_Trimmed.StartsWith("{"))
                {
                    SCSProductsResponse l_Response = JsonConvert.DeserializeObject<SCSProductsResponse>(p_Content);
                    l_Results = l_Response == null ? null : l_Response.results;
                }
            }
            catch
            {
                // Unparsable payload is left to the caller's existing handling
                return string.Empty;
            }

            if (l_Results == null || l_Results.Count == 0)
            {
                return string.Empty;
            }

            // status 0 means the payload carried no per item status at all, which is not a failure
            var l_Failed = l_Results
                .Where(r => r != null
                            && (string.IsNullOrEmpty(p_ExternalId) || r.external_id == p_ExternalId)
                            && r.status != 0
                            && (r.status < 200 || r.status > 299))
                .Select(r => $"{r.external_id} ({r.status})" + (string.IsNullOrEmpty(r.reason) ? string.Empty : ": " + r.reason))
                .ToList();

            return l_Failed.Count == 0 ? string.Empty : string.Join(" | ", l_Failed);
        }
    }
}
