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
        // Delay in ms between each API call to avoid Target rate limiting (429)
        private const int DelayBetweenCallsMs = 500;

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
                ConnectorDataModel? l_SourceConnector = ConnectorDataModel.Deserialize(route.SourceConnectorObject.Data);
                ConnectorDataModel? l_DestinationConnector = ConnectorDataModel.Deserialize(route.DestinationConnectorObject.Data);
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
                        // Declared outside the try so the catch records the failure on the same connection
                        // the rest of this item's product data rows are written on.
                        CustomerProductCatalog l_Product = new CustomerProductCatalog();

                        try
                        {
                        List<SCS_ProductCatalogStatusResponseModel> productList = new List<SCS_ProductCatalogStatusResponseModel>();

                        l_Product.UseConnection(l_SourceConnector.ConnectionString);
                        l_Product.ProductId = Convert.ToInt32(row["ProductId"].ToString());

                        l_DestinationConnector.Url = l_DestinationConnector.BaseUrl + "external_id=" + Uri.EscapeDataString(Convert.ToString(row["ItemID"]));
                        l_DestinationConnector.Method = "GET";

                        sourceResponse = RestConnector.Execute(l_DestinationConnector, Body).GetAwaiter().GetResult();

                        // A 2xx can still carry an error payload, in which case the item must not stay PENDING.
                        string l_StatusPayloadError = sourceResponse.IsSuccessStatusCode
                            ? ProductCatalog.GetResponsePayloadError(sourceResponse.Content, Convert.ToString(row["ItemID"]))
                            : string.Empty;

                        if (sourceResponse.IsSuccessStatusCode && !string.IsNullOrEmpty(l_StatusPayloadError))
                        {
                            l_Product.DeleteWithType(l_Product.ProductId, "STA-ERR");
                            l_Product.SaveData("STA-ERR", sourceResponse.Content, userNo);

                            l_CustomerProductCatalog.UpdateStatus(Convert.ToString(row["ItemID"]), Convert.ToString(row["VariationType"]), "ERROR", "", l_SourceConnector.CustomerID, 0);

                            route.SaveLog(LogTypeEnum.Error, $"Error in the response payload getting ProductCatalogStatus for [{row["ItemID"]}] ({l_StatusPayloadError}). Marked as ERROR.", sourceResponse.Content, userNo);
                        }
                        else if (sourceResponse.IsSuccessStatusCode)
                        {
                            route.SaveLog(LogTypeEnum.Debug, $"ProductCatalogStatus processed for [{row["ItemID"]}].", string.Empty, userNo);

                            l_Product.DeleteWithType(l_Product.ProductId, "STA-RSP");
                            l_Product.SaveData("STA-RSP", sourceResponse.Content, userNo);

                            productList = JsonConvert.DeserializeObject<List<SCS_ProductCatalogStatusResponseModel>>(sourceResponse.Content);

                            if (productList == null || !productList.Any())
                            {
                                // Target answers 2xx with no product at all, so the item was never created there.
                                // Without this the item stays PENDING and is polled on every run for ever.
                                if (ProductCatalog.IsRetryExhausted(row))
                                {
                                    string l_NotFoundError = JsonConvert.SerializeObject(new ProductCatalogErrorModel
                                    {
                                        message = $"Target does not return item {row["ItemID"]} so it was never created there.",
                                        errors = new[]
                                        {
                                            "The item status was requested several times and Target answered every time without a product. Upload this item again so it is created."
                                        }
                                    });

                                    l_Product.DeleteWithType(l_Product.ProductId, "STA-ERR");
                                    l_Product.SaveData("STA-ERR", l_NotFoundError, userNo);

                                    l_CustomerProductCatalog.UpdateStatus(Convert.ToString(row["ItemID"]), Convert.ToString(row["VariationType"]), "ERROR", "", l_SourceConnector.CustomerID, 0);

                                    route.SaveLog(LogTypeEnum.Error, $"Target returned no product for [{row["ItemID"]}] and the retry limit of {CommonUtils.ProductCatalogMaxRetryCount} was reached. Marked as ERROR.", sourceResponse.Content, userNo);
                                }
                                else
                                {
                                    l_CustomerProductCatalog.UpdateStatus(Convert.ToString(row["ItemID"]), Convert.ToString(row["VariationType"]), "PENDING", "", l_SourceConnector.CustomerID,
                                        row.Table.Columns.Contains("RetryCount") && row["RetryCount"] != DBNull.Value ? Convert.ToInt32(row["RetryCount"]) + 1 : 0);

                                    route.SaveLog(LogTypeEnum.Warning, $"Target returned no product for [{row["ItemID"]}]. The item stays PENDING and is checked again on the next run.", sourceResponse.Content, userNo);
                                }
                            }
                            else
                            {
                                SCS_ProductCatalogStatusResponseModel productStatus = productList[0];

                                // Carries the outcome of the product logistics call so that a Target error is not
                                // overwritten by the listing status further down.
                                string l_LogisticsError = string.Empty;
                                bool l_LogisticsRetry = false;

                                if (productStatus.product_statuses != null && productStatus.product_statuses.Any() && productStatus.product_statuses[0].listing_status == "APPROVED" && !string.IsNullOrWhiteSpace(productStatus.id) && !string.Equals(Convert.ToString(productStatus.relationship_type.ToUpper()), "VAP", StringComparison.OrdinalIgnoreCase))
                                {
                                    l_DestinationConnector.Url = $"https://api.target.com/sellers/v1/sellers/{l_DestinationConnector.Realm.ToString()}/product_logistics/{productStatus.id}";
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

                                    l_Product.DeleteWithType(l_Product.ProductId, "LOG-REQ");
                                    l_Product.SaveData("LOG-REQ", CommonUtils.DescribeRequest(l_DestinationConnector, requestBody), userNo);

                                    route.SaveData("JSON-SNT", 0, CommonUtils.DescribeRequest(l_DestinationConnector, requestBody), userNo);

                                    sourceResponse = RestConnector.Execute(l_DestinationConnector, Body).GetAwaiter().GetResult();

                                    // A 2xx logistics response can still carry an error payload.
                                    string l_LogisticsPayloadError = sourceResponse.IsSuccessStatusCode
                                        ? ProductCatalog.GetResponsePayloadError(sourceResponse.Content, Convert.ToString(row["ItemID"]))
                                        : string.Empty;

                                    if (sourceResponse.IsSuccessStatusCode && string.IsNullOrEmpty(l_LogisticsPayloadError))
                                    {
                                        l_Product.DeleteWithType(l_Product.ProductId, "LOG-RSP");
                                        l_Product.SaveData("LOG-RSP", sourceResponse.Content, userNo);
                                    }
                                    else
                                    {
                                        l_Product.DeleteWithType(l_Product.ProductId, "LOG-ERR");
                                        l_Product.SaveData("LOG-ERR", sourceResponse.Content, userNo);

                                        if (!sourceResponse.IsSuccessStatusCode && CommonUtils.IsTransientResponse(sourceResponse))
                                        {
                                            l_LogisticsRetry = true;

                                            route.SaveLog(LogTypeEnum.Warning, $"Transient error ({(sourceResponse.ResponseStatus == ResponseStatus.TimedOut ? "Timeout" : (int)sourceResponse.StatusCode + " " + sourceResponse.StatusCode)}) updating product logistics for [{row["ItemID"]}]. Item will be retried.", sourceResponse.Content, userNo);
                                        }
                                        else
                                        {
                                            l_LogisticsError = sourceResponse.IsSuccessStatusCode
                                                ? l_LogisticsPayloadError
                                                : (int)sourceResponse.StatusCode + " " + sourceResponse.StatusCode;

                                            route.SaveLog(LogTypeEnum.Error, $"Error ({l_LogisticsError}) updating product logistics for [{row["ItemID"]}]. Marked as ERROR.", sourceResponse.Content, userNo);
                                        }
                                    }
                                }

                                if (!string.IsNullOrEmpty(l_LogisticsError))
                                {
                                    // Target rejected the logistics update, so the listing status must not mask it.
                                    l_CustomerProductCatalog.UpdateStatus(Convert.ToString(row["ItemID"]), Convert.ToString(row["VariationType"]), "ERROR", productStatus.id, l_SourceConnector.CustomerID, 0);
                                }
                                else if (!l_LogisticsRetry && productStatus.product_statuses != null && productStatus.product_statuses.Any())
                                {
                                    l_CustomerProductCatalog.UpdateStatus(Convert.ToString(row["ItemID"]), Convert.ToString(row["VariationType"]), productStatus.product_statuses[0].listing_status, productStatus.id, l_SourceConnector.CustomerID, 0);
                                }
                            }
                        }
                        else
                        {
                            l_Product.DeleteWithType(l_Product.ProductId, "STA-ERR");
                            l_Product.SaveData("STA-ERR", sourceResponse.Content, userNo);

                            if (CommonUtils.IsTransientResponse(sourceResponse))
                            {
                                route.SaveLog(LogTypeEnum.Warning, $"Transient error ({(sourceResponse.ResponseStatus == ResponseStatus.TimedOut ? "Timeout" : (int)sourceResponse.StatusCode + " " + sourceResponse.StatusCode)}) getting ProductCatalogStatus for [{row["ItemID"]}]. Item will be retried.", sourceResponse.Content, userNo);
                            }
                            else
                            {
                                l_CustomerProductCatalog.UpdateStatus(Convert.ToString(row["ItemID"]), Convert.ToString(row["VariationType"]), "ERROR", "", l_SourceConnector.CustomerID, 0);

                                route.SaveLog(LogTypeEnum.Error, $"Error ({(int)sourceResponse.StatusCode} {sourceResponse.StatusCode}) getting ProductCatalogStatus for [{row["ItemID"]}]. Marked as ERROR.", sourceResponse.Content, userNo);
                            }
                        }

                        route.SaveData("JSON-RVD", 0, sourceResponse.Content, userNo);
                        }
                        catch (Exception itemEx)
                        {
                            route.SaveLog(LogTypeEnum.Exception, $"Error processing ProductCatalogStatus item [{row["ItemID"]}].", itemEx.ToString(), userNo);

                            ProductCatalog.MarkItemFailed(route, l_Product, l_SourceConnector.CustomerID, row, itemEx, userNo);
                        }

                        // Delay between API calls to avoid Target rate limiting
                        System.Threading.Thread.Sleep(DelayBetweenCallsMs);
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
