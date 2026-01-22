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
    public class VeeqoUpdatedProductsQTYRoute
    {
        public static async Task Execute(IConfiguration config, Routes route)
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
            shipmentData.GetViewList(String.Empty, "", ref l_Data, "Id DESC");

            if (l_Data.Rows.Count == 0)
            {
                route.SaveLog(LogTypeEnum.Error, "No items found in ShipmentDetailFromNDC", string.Empty, userNo);
                return;
            }

            Dictionary<string, int> warehouseIdMap = await FetchWarehouses(httpClient, baseUrl, route);

            foreach (DataRow row in l_Data.Rows)
            {
                string itemID = row["ItemID"].ToString();
                string warehouseName = row["WarehouseName"].ToString();
                int newQuantity = Convert.ToInt32(row["QTY"]);

                if (warehouseIdMap.TryGetValue(warehouseName, out int warehouseId))
                {
                    //string productApiUrl = $"{baseUrl}/products?warehouse_id={warehouseId}&page_size=25&page=1&query={itemID}";
                    string productApiUrl = $"{baseUrl}/products?page_size=25&page=1&query={itemID}";

                    HttpResponseMessage response = await httpClient.GetAsync(productApiUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        route.SaveLog(LogTypeEnum.Error, $"Failed to fetch product for ItemID: {itemID}, Status Code: {response.StatusCode}", string.Empty, userNo);
                        continue;
                    }

                    string responseData = await response.Content.ReadAsStringAsync();
                    JArray productData = JArray.Parse(responseData);

                    foreach (var product in productData)
                    {
                        foreach (var sellable in product["sellables"])
                        {
                            if (sellable["sku_code"]?.ToString().Equals(itemID, StringComparison.OrdinalIgnoreCase) == true)
                            {
                                int sellableId = sellable.Value<int>("id");
                                await UpdateVeeqoProductQuantity(sellableId, warehouseId, warehouseName, newQuantity, httpClient, baseUrl, route);
                            }
                        }
                    }
                }
                else
                {
                    route.SaveLog(LogTypeEnum.Error, $"Warehouse name '{warehouseName}' not found in Veeqo.", string.Empty, userNo);
                }
            }

            route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
        }

        private static async Task<Dictionary<string, int>> FetchWarehouses(HttpClient httpClient, string baseUrl, Routes route)
        {
            //string warehouseApiUrl = "https://api.veeqo.com/warehouses?page_size=25&page=1";
            string warehouseApiUrl = $"{baseUrl}/warehouses?page_size=25&page=1";
            HttpResponseMessage response = await httpClient.GetAsync(warehouseApiUrl);

            if (!response.IsSuccessStatusCode)
            {
                route.SaveLog(LogTypeEnum.Error, $"Failed to fetch warehouse data, Status Code: {response.StatusCode}", string.Empty, 1);
                return new Dictionary<string, int>();
            }

            string responseData = await response.Content.ReadAsStringAsync();
            JArray warehouseData = JArray.Parse(responseData);

            return warehouseData.ToDictionary(
                warehouse => warehouse.Value<string>("name"),
                warehouse => warehouse.Value<int>("id")
            );
        }

        private static async Task UpdateVeeqoProductQuantity(int sellableId, int warehouseId, string warehouseName, int quantity, HttpClient httpClient, string baseUrl, Routes route)
        {
            string apiUrl = $"{baseUrl}/sellables/{sellableId}/warehouses/{warehouseId}/stock_entry";

            var payload = new
            {
                stock_entry = new
                {
                    physical_stock_level = quantity,
                    infinite = false,
                    location = warehouseName
                }
            };

            StringContent content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PutAsync(apiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                route.SaveLog(LogTypeEnum.Info, $"Completed to update stock for Sellable ID: {sellableId} Completed execution of route [{route.Id}]", string.Empty, 1);
            }
            else
            {
                route.SaveLog(LogTypeEnum.Info, $"Failed to update stock for Sellable ID: {sellableId} route [{route.Id}]", string.Empty, 1);
            }
        }
    }
}
