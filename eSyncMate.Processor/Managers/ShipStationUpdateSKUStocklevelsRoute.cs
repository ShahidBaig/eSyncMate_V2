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
using System.Data.SqlClient;
using System.Net.Http.Headers;

namespace eSyncMate.Processor.Managers
{

    public class ShipStationUpdateSKUStocklevelsRoute
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
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("api-key", l_SourceConnector.ConsumerKey);

            route.SaveLog(LogTypeEnum.Info, $"Started executing route [{route.Id}]", string.Empty, userNo);

            shipmentData.UseConnection(l_DestinationConnector.ConnectionString);
            shipmentData.GetShipmentDetailFromNDCShipStationStatus(ref l_Data);

            if (l_Data.Rows.Count == 0)
            {
                route.SaveLog(LogTypeEnum.Info, $"No items found in ShipmentDetailFromNDC [{route.Id}]", string.Empty, userNo);
                return;
            }

            foreach (DataRow row in l_Data.Rows)
            {
                string sku = row["ItemID"].ToString();
                string warehouseId = "se-1508413"; 
                int quantity = Convert.ToInt32(row["QTY"]);
                string lot = row["LotNumber"].ToString();
                string expDate = row["ExpirationDate"].ToString();

                await UpdateShipStationInventoryQuantity(sku, warehouseId, quantity, lot, expDate, httpClient, baseUrl, route, l_DestinationConnector.ConnectionString);
            }

            route.SaveLog(LogTypeEnum.Info, $"Completed execution of route [{route.Id}]", string.Empty, userNo);
        }

        private static async Task UpdateShipStationInventoryQuantity(string sku, string warehouseId, int quantity, string lotNumber, string expDate, HttpClient httpClient, string baseUrl, Routes route, string connectionString)
        {
            var payload = new
            {
                transaction_type = "increment",
                inventory_location_id = warehouseId,
                sku = sku,
                quantity = quantity,
                //cost = new
                //{
                //    amount = 1,
                //    currency = "USD"
                //},
                condition = "sellable",
                lot = lotNumber,
                usable_end_date = DateTime.Parse(expDate).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                effective_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                reason = "Stock sync from ERP",
                notes = "Updated Lot and Exp Date"
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(baseUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                route.SaveLog(LogTypeEnum.Error,$" Failed to update stock for SKU: {sku}, Warehouse: {warehouseId}, Status: {response.StatusCode}, Message: {await response.Content.ReadAsStringAsync()}",
                string.Empty, 1);

            }
            else
            {
                route.SaveLog(LogTypeEnum.Info, $"Stock successfully updated for SKU: {sku} in Warehouse: {warehouseId}", string.Empty, 1);
                //string dbWarehouseName = warehouseId == "SurgiMac NY - Merrick Warehouse" ? "SurgiMac NY" : warehouseId;
                string dbWarehouseName = warehouseId == "se-1508413" ? "SurgiMac NY" : warehouseId;
                MarkAsSynced(sku, dbWarehouseName ?? "SurgiMac NY", connectionString);
            }
        }

        private static void MarkAsSynced(string sku, string warehouseName, string connectionString)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                UPDATE ShipmentDetailFromNDC
                SET ShipStationStatus = 'SYNCED'
                WHERE ItemID = @sku 
                  AND WarehouseName = @warehouseName 
                  AND ShipStationStatus = 'NEW'";

                    cmd.Parameters.AddWithValue("@sku", sku);
                    cmd.Parameters.AddWithValue("@warehouseName", warehouseName);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
