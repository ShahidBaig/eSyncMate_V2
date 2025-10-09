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
using static eSyncMate.Processor.Models.SCSGetOrderResponseModel;
using Namotion.Reflection;
using static eSyncMate.Processor.Models.PurchaseOrderResponseModel;
using System.Threading;
using static eSyncMate.Processor.Models.WalmartGetOrderResponseModel;
using MySqlX.XDevAPI;
using static Hangfire.Storage.JobStorageFeatures;
using System.Text;
using Renci.SshNet;
using Renci.SshNet.Common;
using System.Net.Sockets;
using FluentFTP;
using FluentFTP.Exceptions;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json.Linq;

namespace eSyncMate.Processor.Managers
{
    public class VeeqoCreateNewProductsRoute
    {
        public static async Task Execute(IConfiguration config, ILogger logger, Routes route)
        {
            int userNo = 1;
            HttpClient httpClient = new HttpClient();
            ShipmentDetailFromNDC shipmentData = new ShipmentDetailFromNDC();
            DataTable l_Data = new DataTable();
            ConnectorDataModel? l_SourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
            ConnectorDataModel? l_DestinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);
            string baseUrl = l_SourceConnector.BaseUrl.TrimEnd('/');
            httpClient.DefaultRequestHeaders.Add(l_SourceConnector.Headers[0].Name, l_SourceConnector.Headers[0].Value);
            route.SaveLog(LogTypeEnum.Info, $"Started executing route [{route.Id}]", string.Empty, userNo);

            shipmentData.UseConnection(l_DestinationConnector.ConnectionString);
            shipmentData.GetShipmentDetailFromNDCData(ref l_Data);

            if (l_Data.Rows.Count == 0)
            {
                logger.LogError("No items found in ShipmentDetailFromNDC");
                return;
            }

            HashSet<string> apiSkuCodes = await LoadApiSkuCodesAsync(httpClient, baseUrl, logger);

            List<string> unmatchedItemIDs = new List<string>();
            foreach (DataRow row in l_Data.Rows)
            {
                string itemID = row["ItemID"]?.ToString();
                if (!string.IsNullOrEmpty(itemID) && !apiSkuCodes.Contains(itemID))
                {
                    unmatchedItemIDs.Add(itemID);
                }
            }

            int batchSize = 5; 
            foreach (var batch in unmatchedItemIDs.Batch(batchSize))
            {
                var tasks = batch.Select(itemID => CreateProductAsync(itemID, httpClient, baseUrl, logger));
                await Task.WhenAll(tasks);
                await Task.Delay(1000); 
            }

            route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
        }

        private static async Task<HashSet<string>> LoadApiSkuCodesAsync(HttpClient httpClient, string baseUrl, ILogger logger)
        {
            int pageSize = 2000;
            int currentPage = 1;
            HashSet<string> apiSkuCodes = new HashSet<string>();

            while (true)
            {
                string apiUrl = $"{baseUrl}/products?page_size={pageSize}&page={currentPage}";
                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError($"Failed to fetch products, Status Code: {response.StatusCode}");
                    break;
                }

                string responseData = await response.Content.ReadAsStringAsync();
                JArray productData = JArray.Parse(responseData);

                if (productData.Count == 0)
                {
                    break;
                }

                foreach (var product in productData)
                {
                    foreach (var sellable in product["sellables"])
                    {
                        string skuCode = sellable["sku_code"]?.ToString();
                        if (!string.IsNullOrEmpty(skuCode))
                        {
                            apiSkuCodes.Add(skuCode);
                        }
                    }
                }

                currentPage++; 
            }

            return apiSkuCodes;
        }


        private static async Task CreateProductAsync(string itemID, HttpClient httpClient, string baseUrl, ILogger logger)
        {
            var productData = new
            {
                title = $"Product for {itemID}",
                description = "Automatically generated product",
                estimated_delivery = "N/A",
                notes = "Product created by system",
                product_brand_id = (string?)null,
                product_variants_attributes = new[]
                {
            new {
                title = $"Variant for {itemID}",
                price = 0,
                sku_code = itemID,
                weight_grams = 0,
                upc_code = "string",
                min_reorder_level = 0,
                quantity_to_reorder = 0,
                tax_rate = "N/A"
            }
        },
                images_attributes = new[]
                {
            new {
                src = "string",
                display_position = 0
            }
        }
            };

            string jsonPayload = JsonConvert.SerializeObject(productData);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await httpClient.PostAsync($"{baseUrl}/products", content);

            if (response.IsSuccessStatusCode)
            {
            }
            else
            {
            }
        }
    }
}
