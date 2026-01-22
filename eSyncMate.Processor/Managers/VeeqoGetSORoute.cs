using System;
using System.Data;
using System.Data.SqlClient;
using System.Net.Http;
using System.Threading.Tasks;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.Net.WebRequestMethods;
using static eSyncMate.DB.Declarations;

namespace eSyncMate.Processor.Managers
{
    public class VeeqoGetSORoute
    {
        public static async Task Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;
            HttpClient httpClient = new HttpClient();
            DataTable dataTable = new DataTable();
            ConnectorDataModel? l_SourceConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.SourceConnectorObject.Data);
            ConnectorDataModel? l_DestinationConnector = JsonConvert.DeserializeObject<ConnectorDataModel>(route.DestinationConnectorObject.Data);
            string baseUrl = l_SourceConnector.BaseUrl.TrimEnd('/');
            httpClient.DefaultRequestHeaders.Add(l_SourceConnector.Headers[0].Name, l_SourceConnector.Headers[0].Value);
            DateTime currentDateTime = DateTime.UtcNow;
            DateTime minDateTime = currentDateTime.AddHours(-24);
            string formattedMinDateTime = minDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            dataTable.Columns.Add("OrderId", typeof(long));
            dataTable.Columns.Add("CreatedAt", typeof(DateTime));
            dataTable.Columns.Add("WarehouseId", typeof(int));
            dataTable.Columns.Add("WarehouseName", typeof(string));
            dataTable.Columns.Add("SkuCode", typeof(string));
            dataTable.Columns.Add("Title", typeof(string));
            dataTable.Columns.Add("PricePerUnit", typeof(decimal));
            dataTable.Columns.Add("Quantity", typeof(int));
            dataTable.Columns.Add("Profit", typeof(decimal));
            dataTable.Columns.Add("Margin", typeof(decimal));

            string OrdersApiUrl = $"{baseUrl}/orders?created_at_min={Uri.EscapeDataString(formattedMinDateTime)}&status=shipped";
            HttpResponseMessage response = await httpClient.GetAsync(OrdersApiUrl);
            string responseData = await response.Content.ReadAsStringAsync();
            JArray productData = JArray.Parse(responseData);

            foreach (var order in productData)
            {
                if (order["line_items"]?.Count() > 0)
                {
                    var orderId = (long)order["id"];
                    var createdAt = (DateTime)order["created_at"];
                    string warehouseId = string.Empty;
                    string warehouseName = string.Empty;

                    if (order["allocations"] != null && order["allocations"].Count() > 0)
                    {
                        var allocation = order["allocations"][0]; 
                        warehouseId = (string)allocation["warehouse"]["id"];
                        warehouseName = (string)allocation["warehouse"]["name"];
                    }

                    foreach (var lineItem in order["line_items"])
                    {
                        var pricePerUnit = (decimal)lineItem["price_per_unit"];
                        var quantity = (int)lineItem["quantity"];
                        var title = (string)lineItem["sellable"]["title"];
                        var skuCode = (string)lineItem["sellable"]["sku_code"];
                        var profit = lineItem["sellable"]["profit"]?.ToString() ?? "0";
                        var margin = lineItem["sellable"]["margin"]?.ToString() ?? "0";

                        DataRow row = dataTable.NewRow();
                        row["OrderId"] = orderId;
                        row["CreatedAt"] = createdAt;
                        row["WarehouseId"] = warehouseId;
                        row["WarehouseName"] = warehouseName;
                        row["SkuCode"] = skuCode;
                        row["Title"] = title;
                        row["PricePerUnit"] = pricePerUnit;
                        row["Quantity"] = quantity;
                        row["Profit"] = profit;
                        row["Margin"] = margin;
                        dataTable.Rows.Add(row);
                    }
                }
            }

            using (SqlConnection connection = new SqlConnection(l_DestinationConnector.ConnectionString))
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                using (SqlCommand command = new SqlCommand("sp_SaveVeeqoOrderDetails", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    SqlParameter tableParam = new SqlParameter("@OrderDetails", SqlDbType.Structured)
                    {
                        TypeName = "dbo.VeeqoOrderDetailsType", 
                        Value = dataTable
                    };
                    command.Parameters.Add(tableParam);

                    int rowsAffected = command.ExecuteNonQuery();
                }

                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }

            route.SaveLog(LogTypeEnum.Info, "Data saved to SQL using stored procedure.", string.Empty, userNo);
        }
    }
}