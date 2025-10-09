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
using Microsoft.SqlServer.Server;
using Nancy;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using MySqlX.XDevAPI.Relational;
using static eSyncMate.Processor.Models.WalmartInventoryInputModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static eSyncMate.Processor.Models.ProductCatalogModel;
using System.Data.SqlClient;
using System.Text.Json.Serialization;
using System.Text.Json;
using static eSyncMate.Processor.Models.WalmartGetOrderResponseModel;
using CsvHelper;
using Microsoft.VisualBasic;
using Nancy.Routing;
using ClosedXML.Excel;
using DevLab.JmesPath.Functions;
using Microsoft.IdentityModel.Tokens;

namespace eSyncMate.Processor.Managers
{
    public class DownloadTargetItemsRoute
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
            InventoryBatchWise l_InventoryBatchWise = new InventoryBatchWise();
            l_InventoryBatchWise.BatchID = Guid.NewGuid().ToString();
            DB.Entities.CustomerProductCatalog l_CustomerProductCatalog = new DB.Entities.CustomerProductCatalog();
            DB.Entities.SCS_ItemsType l_SCS_ItemsType = new DB.Entities.SCS_ItemsType();
            DataTable dataTable = null;
            DataTable l_PrepareData = new DataTable();
            DataTable l_SCS_ItemTypeAttributeDataTable = new DataTable ();
            StringBuilder csvContent = new StringBuilder();
            DataTable l_CustomerProductCatalogPricesDT = new DataTable();


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

                    l_CustomerProductCatalog.UseConnection(l_SourceConnector.ConnectionString);
                    
                    l_CustomerProductCatalog.GetPrepareItemDataNewStatus(l_SourceConnector.CustomerID, ref l_data);
                    l_CustomerProductCatalog.GetCustomerProductCatalogPrices(l_SourceConnector.CustomerID, ref l_CustomerProductCatalogPricesDT);

                    route.SaveLog(LogTypeEnum.Debug, $"Source connector processing completed.", string.Empty, userNo);
                }

                if (l_data.Rows.Count > 0)
                {
                    route.SaveLog(LogTypeEnum.Debug, $"Destination connector processing start...", string.Empty, userNo);

                    dataTable = new DataTable();

                    GetAlItems("", route.Id,  ref dataTable,ref l_CustomerProductCatalogPricesDT, l_SourceConnector.CustomerID);

                    foreach (DataRow row in l_data.Rows)
                    {

                        if (dataTable.Rows.Count > 0)
                        {
                            csvContent = new StringBuilder();

                            if (l_PrepareData != null)
                            {
                                l_PrepareData.Dispose();
                            }

                            dataTable.DefaultView.RowFilter = "ItemTypeName = " + row["ItemTypeID"].ToString();
                            l_PrepareData = dataTable.DefaultView.ToTable();

                            l_SCS_ItemsType.UseConnection(l_SourceConnector.ConnectionString);

                            if(l_SCS_ItemTypeAttributeDataTable != null)
                            {
                                l_SCS_ItemTypeAttributeDataTable.Dispose();
                                l_SCS_ItemTypeAttributeDataTable = new DataTable();
                            }

                            l_SCS_ItemsType.GetItemTypeAttribute(row["ItemTypeID"].ToString(), l_SourceConnector.CustomerID, ref l_SCS_ItemTypeAttributeDataTable);

                            if (l_PrepareData.Rows.Count > 0)
                            {
                                var columnNames = l_PrepareData.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToList();
                                columnNames.Remove("JsonFields"); 

                                DataTable l_File = new DataTable();
                                foreach (var columnName in columnNames)
                                {
                                    l_File.Columns.Add(columnName);
                                }

                                foreach(DataRow colRow in l_SCS_ItemTypeAttributeDataTable.Rows)
                                {
                                    string name = (Convert.ToBoolean(colRow["Required"].ToString()) ? "*" : "") + colRow["Name"].ToString();

                                    if (!l_File.Columns.Contains(name))
                                    {
                                        l_File.Columns.Add(name);
                                    }
                                }

                                foreach (DataRow l_Row in l_PrepareData.Rows)
                                {
                                    DataRow line = l_File.NewRow();

                                    foreach (var columnName in columnNames)
                                    {
                                        line[columnName] = l_Row[columnName].ToString();
                                    }

                                    if (l_Row["JsonFields"] != DBNull.Value)
                                    {
                                        var jsonString = l_Row["JsonFields"]?.ToString();
                                        
                                        if (!string.IsNullOrEmpty(jsonString))
                                        {
                                            var jsonFields = JsonConvert.DeserializeObject<List<ProductCatalogModel.Field>>(jsonString);
                                            foreach (var field in jsonFields)
                                            {
                                                string name = GetItemTypeMappedName(field.name, l_SCS_ItemTypeAttributeDataTable);
                                                if (l_File.Columns.Contains(name))
                                                {
                                                    line[name] =  $"{(string.IsNullOrEmpty(line[name].ToString()) ? field.value : line[name] + "|" + field.value)}" ;
                                                    
                                                }

                                            }
                                        }
                                    }

                                    l_File.Rows.Add(line);
                                }

                                var first12ColumnNames = l_File.Columns.Cast<DataColumn>()
                                .Select(column => column.ColumnName)
                                .Take(16)
                                .ToList();

                                var remainingSortedColumnNames = l_File.Columns.Cast<DataColumn>()
                                .Select(column => column.ColumnName)
                                .Skip(16)
                                .OrderBy(name => name.TrimStart('*')) 
                                .ThenBy(name => name) 
                                .ToList();

                                var finalColumnOrder = first12ColumnNames.Concat(remainingSortedColumnNames).ToList();

                                DataTable sortedDataTable = new DataTable();

                                foreach (var columnName in finalColumnOrder)
                                {
                                    sortedDataTable.Columns.Add(columnName);
                                }

                                foreach (DataRow l_Row in l_File.Rows)
                                {
                                    DataRow sortedRow = sortedDataTable.NewRow();
                                    foreach (var columnName in finalColumnOrder)
                                    {
                                        sortedRow[columnName] = l_Row[columnName];
                                    }
                                    sortedDataTable.Rows.Add(sortedRow);
                                }


                                string base64String;

                                using (var wb = new XLWorkbook())
                                {
                                    var sheet = wb.AddWorksheet(sortedDataTable, "Items Data");

                                    using (var ms = new MemoryStream())
                                    {
                                        wb.SaveAs(ms);

                                        base64String = Convert.ToBase64String(ms.ToArray());
                                    }
                                }

                                l_CustomerProductCatalog.UpdateItemsDataStatus(row["ItemTypeID"].ToString(), l_SourceConnector.CustomerID, $"{l_SourceConnector.CustomerID}-{row["ItemTypeID"].ToString()}.xlsx", base64String);
                                l_SCS_ItemTypeAttributeDataTable.Dispose();
                                l_SCS_ItemTypeAttributeDataTable = null;
                                l_PrepareData.Dispose();
                                l_PrepareData = null;
                            }
                        }

                        dataTable.Dispose();
                    }

                    route.SaveLog(LogTypeEnum.Debug, $"Destination connector processing completed.", string.Empty, userNo);
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

                if(dataTable != null)
                    dataTable.Dispose();
                if (l_PrepareData != null)
                    l_PrepareData.Dispose();
                if(l_SCS_ItemTypeAttributeDataTable != null)
                    l_SCS_ItemTypeAttributeDataTable.Dispose();
                if (l_CustomerProductCatalogPricesDT != null)
                    l_CustomerProductCatalogPricesDT.Dispose();
            }
        }

        static DataTable GetAlItems(string ItemTypeID,int Id,ref DataTable l_data,ref DataTable l_ProductCatalogData, string CustomerID)
        {
            string after_id = string.Empty;
            StringBuilder data = new StringBuilder();
            DataTable dt = new DataTable();
            
            AddStaticColumns(ref l_data);

            do
            {
                int retryCount = 0;

            retryGetItems:
                string json = GetItems(after_id, ItemTypeID, Id, CustomerID);

                if (!string.IsNullOrEmpty(json))
                {
                    TargetItems[] items = JsonConvert.DeserializeObject<TargetItems[]>(json);

                    if (items.Count() > 0)
                    {
                        foreach (TargetItems item in items)
                        {
                            AddToDataTable(l_data, item, Id, l_ProductCatalogData ,CustomerID);
                        }
                    }

                    after_id = string.Empty;
                    if (items.Length > 0)
                        after_id = items[items.Length - 1].id;
                }
                else
                {
                    if (retryCount == 0)
                    {
                        retryCount++;
                        goto retryGetItems;
                    }

                    retryCount = 0;
                }
            }
            while (!string.IsNullOrEmpty(after_id));

            return l_data;
        }

        static string GetItems(string after_id,string ItemTypeID,int id, string CustomerID)
        {
            RestClient client;
            RestRequest request;
            RestResponse response;
            DB.Entities.Routes l_route = new DB.Entities.Routes();
            RestClientOptions options = new RestClientOptions("https://api.target.com/sellers/v1/")
            {
                MaxTimeout = -1,
            };

            client = new RestClient(options);

            if (CustomerID == "TAR6266P")
            {
                if (string.IsNullOrEmpty(after_id))
                    request = new RestRequest($"sellers/5d949496fcd4b70097dfad5e/products_catalog?per_page=1000&expand=fields", Method.Get);

                else
                    request = new RestRequest($"sellers/5d949496fcd4b70097dfad5e/products_catalog?per_page=1000&expand=fields&after_id={after_id}", Method.Get);

                request.AddHeader("x-api-key", "64dd4d52f0e4a4ffa1c25cbdca78d33906cc3af8");
                request.AddHeader("x-seller-token", "0902d4a0688a4cdeaaee926fc1f70155");
                request.AddHeader("x-seller-id", "5d949496fcd4b70097dfad5e");
            }

            if (string.IsNullOrEmpty(after_id))
                request = new RestRequest($"sellers/6802accd146a9b60e3850f70/products_catalog?per_page=1000&expand=fields", Method.Get);

            else
                request = new RestRequest($"sellers/6802accd146a9b60e3850f70/products_catalog?per_page=1000&expand=fields&after_id={after_id}", Method.Get);


            request.AddHeader("x-api-key", "80951e9d352afdd7725961817c62a51baf637658");
            request.AddHeader("x-seller-token", "d061a03b9bbc48c48c63b93559bd48a8");
            request.AddHeader("x-seller-id", "6802accd146a9b60e3850f70");

            response = client.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                l_route.Id  = id;
                l_route.UseConnection(CommonUtils.ConnectionString);
                l_route.SaveData("JSON-RVD", 0, response.Content, 1);
            }

            return response.Content.ToString();
        }

        static void AddStaticColumns(ref DataTable l_data)
        {
            l_data.Columns.Add("Brand", typeof(string));
            l_data.Columns.Add("ItemID", typeof(string));
            l_data.Columns.Add("UPC", typeof(string));
            l_data.Columns.Add("ItemTypeName", typeof(string));
            l_data.Columns.Add("ProductRelation", typeof(string));
            l_data.Columns.Add("ParentID", typeof(string)); 
            l_data.Columns.Add("ListPrice", typeof(decimal)); 
            l_data.Columns.Add("MapPrice", typeof(decimal));
            l_data.Columns.Add("OffPrice", typeof(decimal)); 
            l_data.Columns.Add("Type", typeof(string));
            l_data.Columns.Add("VariationType", typeof(string));
            l_data.Columns.Add("UnListed", typeof(string));
            l_data.Columns.Add("is_add_on", typeof(string));
            l_data.Columns.Add("two_day_shipping_eligible", typeof(string));
            l_data.Columns.Add("shipping_exclusion", typeof(string));
            l_data.Columns.Add("seller_return_policy", typeof(string));
            l_data.Columns.Add("JsonFields", typeof(string));

        }

        static void AddDynamicColumns(ref DataTable l_data, Field[] fields)
        {
            foreach(Field field in fields)
            {
                if(!l_data.Columns.Contains(field.name))
                    l_data.Columns.Add(field.name, typeof(string));
            }
        }

        static void AddToDataTable(DataTable table, TargetItems item, int Id,DataTable l_DT, string CustomerID)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            DataRow row = table.NewRow();

            var brand = "Safavieh";
            if (CustomerID == "TAR6266PAH")
            {
                brand = "SEI";
            }

            row["Brand"] = brand;
            row["ItemID"] = item.external_id ;
            row["UPC"] = "";
            row["ItemTypeName"] = item.item_type_id;
            row["ProductRelation"] = item.relationship_type;

            if (!string.IsNullOrEmpty(item.parent_id))
            {
                DataRow[] filteredRows = l_DT.Select($"ProductID = '{item.parent_id}'");

                if (filteredRows.Length > 0)
                {
                    row["ParentID"] = filteredRows[0]["ItemId"];
                }
                else
                {
                    row["ParentID"] = DBNull.Value;
                }
            }
            else
            {
                row["ParentID"] = DBNull.Value;
            }

            if (item.price != null)
            {
                row["ListPrice"] = item.price.list_price;
                row["OffPrice"] = item.price.offer_price;
                row["MapPrice"] = DBNull.Value;

            }
            else
            {
                row["ListPrice"] = DBNull.Value;
                row["OffPrice"] = DBNull.Value;
                row["MapPrice"] = DBNull.Value;
            }

            row["Type"] = item.relationship_type;
            row["VariationType"] = item.relationship_type;

            string jsonString = System.Text.Json.JsonSerializer.Serialize(item.fields);
            row["JsonFields"] = jsonString;

            table.Rows.Add(row);
        }

        static void BulkInsertIntoSqlServer(DataTable table,string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = "SCS_ItemData";

                    bulkCopy.ColumnMappings.Add("PortalID", "PortalID");
                    bulkCopy.ColumnMappings.Add("external_id", "external_id");
                    bulkCopy.ColumnMappings.Add("relationship_type", "relationship_type");
                    bulkCopy.ColumnMappings.Add("parent_id", "parent_id");
                    bulkCopy.ColumnMappings.Add("seller_id", "seller_id");
                    bulkCopy.ColumnMappings.Add("quantity", "quantity");
                    bulkCopy.ColumnMappings.Add("distribution_center_id", "distribution_center_id");
                    bulkCopy.ColumnMappings.Add("list_price", "list_price");
                    bulkCopy.ColumnMappings.Add("offer_price", "offer_price");
                    bulkCopy.ColumnMappings.Add("tcin", "tcin");
                    bulkCopy.ColumnMappings.Add("item_type_id", "item_type_id");
                    bulkCopy.ColumnMappings.Add("JsonFields", "JsonFields");

                    bulkCopy.WriteToServer(table);
                }
                connection.Close();
            }
        }

        public static string GetItemTypeMappedName(string p_Name, DataTable p_ItemTypeAttributeDataTable)
        {
            var Name = (from row in p_ItemTypeAttributeDataTable.AsEnumerable()
                        where row.Field<string>("Mapped_Property") == p_Name
                        let isRequired = bool.TryParse(row.Field<string>("Required"), out bool required) && required
                        select (isRequired ? "*" : "") + row.Field<string>("Name")).FirstOrDefault();

            return Name ?? p_Name;
        }
    }
}
