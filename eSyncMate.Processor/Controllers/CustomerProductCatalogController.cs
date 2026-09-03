using Microsoft.AspNetCore.Mvc;
using eSyncMate.DB.Entities;
using System.Reflection;
using eSyncMate.Processor.Models;
using System.Data;
using eSyncMate.DB;
using Hangfire;
using System.Text.Json;
using ExcelDataReader;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using System.Numerics;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.Formats.Asn1;
using CsvHelper.Configuration;
using System.Net.WebSockets;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using CsvHelper;
using System.Globalization;
using System.Reflection.PortableExecutable;
using Hangfire.Storage;
using static eSyncMate.Processor.Models.SCSCancelOrderResponse;
using Intercom.Data;
using System.Security.Claims;
using eSyncMate.Processor.Managers;
using static Org.BouncyCastle.Math.EC.ECCurve;


namespace eSyncMate.Processor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    public class CustomerProductCatalogController : ControllerBase
    {
        private readonly ILogger<CustomerProductCatalogController> _logger;
        private readonly IConfiguration _config;

        public CustomerProductCatalogController(ILogger<CustomerProductCatalogController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("getCustomerProductCatalog")]
        public async Task<GetCustomerProductCatalog> GetCustomerProductCatalog([FromQuery] CustomerProductCatalogSearchModel searchModel)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetCustomerProductCatalog l_Response = new GetCustomerProductCatalog();
            DataTable l_Data = new DataTable();
            string dateRange = string.Empty;
            string[] dateValues = new string[0];
            string startDate = string.Empty;
            string endDate = string.Empty;

            // A date range only filters once both ends arrived as "from/to"; anything else is not a filter
            bool l_HasDateRange = false;

            if (searchModel.SearchOption == "Created Date" && !string.IsNullOrWhiteSpace(searchModel.SearchValue))
            {
                dateRange = searchModel.SearchValue;
                dateValues = dateRange.Split('/');

                if (dateValues.Length == 2 && !string.IsNullOrWhiteSpace(dateValues[0]) && !string.IsNullOrWhiteSpace(dateValues[1]))
                {
                    startDate = dateValues[0].Trim() + " 00:00:00.000";
                    endDate = dateValues[1].Trim() + " 23:59:59.999";
                    l_HasDateRange = true;
                }
            }

            UsersClaimData userData = new UsersClaimData();

            var claimsIdentity = User.Identity as ClaimsIdentity;

            if (claimsIdentity?.Claims == null)
            {
                l_Response.Code = StatusCodes.Status401Unauthorized;
                l_Response.Message = "Invalid token: Not Authorized";

                return l_Response;
            }

            userData = eSyncMate.Processor.Managers.CustomersManager.GetCustomerNames(claimsIdentity);

            try
            {
                string l_Criteria = string.Empty;
                DB.Entities.CustomerProductCatalog l_CustomerProductCatalog = new DB.Entities.CustomerProductCatalog();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_CustomerProductCatalog.UseConnection(CommonUtils.ConnectionString);

                // An option picked with no value is simply not a filter, so the unfiltered page is returned
                string l_SearchValue = (searchModel.SearchValue ?? string.Empty).Trim();

                if (l_HasDateRange)
                {
                    l_Criteria = $" CONVERT(DATE,CreatedDate) >= '{startDate}' AND CONVERT(DATE,CreatedDate) <= '{endDate}'";
                }
                else if (!string.IsNullOrEmpty(l_SearchValue))
                {
                    if (searchModel.SearchOption == "ProductId")
                    {
                        if (!long.TryParse(l_SearchValue, out long l_ProductId))
                        {
                            l_Response.Code = (int)ResponseCodes.Error;
                            l_Response.Message = "ProductId must be a number.";

                            return l_Response;
                        }

                        l_Criteria = $" ProductId = {l_ProductId}";
                    }
                    else if (searchModel.SearchOption == "ERP CustomerID")
                    {
                        l_Criteria = $" CustomerID = '{SqlSearchHelper.EscapeLiteral(l_SearchValue)}'";
                    }
                    else if (searchModel.SearchOption == "ItemID")
                    {
                        // Item ids such as 512N-60(3A)[7PC]BKS contain LIKE wildcards
                        l_Criteria = SqlSearchHelper.Contains("ItemID", l_SearchValue);
                    }
                    else if (searchModel.SearchOption == "Item Type Name")
                    {
                        l_Criteria = SqlSearchHelper.Contains("ItemTypeName", l_SearchValue);
                    }
                    else if (searchModel.SearchOption == "Parent ID")
                    {
                        l_Criteria = SqlSearchHelper.Contains("ParentID", l_SearchValue);
                    }
                    else if (searchModel.SearchOption == "Status")
                    {
                        l_Criteria = SqlSearchHelper.Contains("SyncStatus", l_SearchValue);
                    }
                }

                if (string.IsNullOrEmpty(l_Criteria) && !userData.IsSuperAdmin)
                {
                    l_Criteria = $" CustomerID IN ({userData.Customers})";
                }


                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring PartnerGroup search.");

                int totalCount = 0;
                l_CustomerProductCatalog.GetViewListPagedWithError(l_Criteria, ref l_Data, "Id DESC", searchModel.PageNumber, searchModel.PageSize, out totalCount);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - CustomerProductCatalog searched {{{l_Data.Rows.Count}}} of {totalCount} total.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating CustomerProductCatalog.");

                l_Response.CustomerProductCatalogDatatable = l_Data;
                l_Response.TotalCount = totalCount;

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "CustomerProductCatalog fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - CustomerProductCatalog are ready.");
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }
            finally
            {
                l_Data.Dispose();
            }

            return l_Response;
        }


        [HttpGet]
        [Route("getSCSBulkUploadPrice")]
        public async Task<GetCustomerProductCatalog> GetSCSBulkUploadPrice([FromQuery] CustomerProductCatalogSearchModel searchModel)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetCustomerProductCatalog l_Response = new GetCustomerProductCatalog();
            DataTable l_Data = new DataTable();
            string dateRange = string.Empty;
            string[] dateValues = new string[0];
            string startDate = string.Empty;
            string endDate = string.Empty;

            UsersClaimData userData = new UsersClaimData();

            var claimsIdentity = User.Identity as ClaimsIdentity;

            if (claimsIdentity?.Claims == null)
            {
                l_Response.Code = StatusCodes.Status401Unauthorized;
                l_Response.Message = "Invalid token: Not Authorized";

                return l_Response;
            }

            userData = eSyncMate.Processor.Managers.CustomersManager.GetCustomerNames(claimsIdentity);

            // A date range only filters once both ends arrived as "from/to"; anything else is not a filter
            bool l_HasDateRange = false;

            if (searchModel.SearchOption == "Created Date" && !string.IsNullOrWhiteSpace(searchModel.SearchValue))
            {
                dateRange = searchModel.SearchValue;
                dateValues = dateRange.Split('/');

                if (dateValues.Length == 2 && !string.IsNullOrWhiteSpace(dateValues[0]) && !string.IsNullOrWhiteSpace(dateValues[1]))
                {
                    startDate = dateValues[0].Trim() + " 00:00:00.000";
                    endDate = dateValues[1].Trim() + " 23:59:59.999";
                    l_HasDateRange = true;
                }
            }

            try
            {
                string l_Criteria = string.Empty;
                DB.Entities.CustomerProductCatalog l_CustomerProductCatalog = new DB.Entities.CustomerProductCatalog();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_CustomerProductCatalog.UseConnection(CommonUtils.ConnectionString);

                // An option picked with no value is simply not a filter, so the unfiltered page is returned
                string l_SearchValue = (searchModel.SearchValue ?? string.Empty).Trim();

                if (l_HasDateRange)
                {
                    l_Criteria = $" CONVERT(DATE,CreatedDate) >= '{startDate}' AND CONVERT(DATE,CreatedDate) <= '{endDate}'";
                }
                else if (!string.IsNullOrEmpty(l_SearchValue))
                {
                    if (searchModel.SearchOption == "ProductId")
                    {
                        if (!long.TryParse(l_SearchValue, out long l_ProductId))
                        {
                            l_Response.Code = (int)ResponseCodes.Error;
                            l_Response.Message = "ProductId must be a number.";

                            return l_Response;
                        }

                        l_Criteria = $" ProductId = {l_ProductId}";
                    }
                    else if (searchModel.SearchOption == "ERP CustomerID")
                    {
                        l_Criteria = $" CustomerID = '{SqlSearchHelper.EscapeLiteral(l_SearchValue)}'";
                    }
                    else if (searchModel.SearchOption == "ItemID")
                    {
                        l_Criteria = SqlSearchHelper.Contains("ItemID", l_SearchValue);
                    }
                    else if (searchModel.SearchOption == "Status")
                    {
                        l_Criteria = SqlSearchHelper.Contains("SyncStatus", l_SearchValue);
                    }
                }

                if (string.IsNullOrEmpty(l_Criteria) && !userData.IsSuperAdmin)
                {
                    l_Criteria = $" CustomerID IN ({userData.Customers})";
                }

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring ProductPrices search.");

                int totalCount = 0;
                l_CustomerProductCatalog.GetProductViewListPaged(l_Criteria, string.Empty, ref l_Data, "Id DESC", searchModel.PageNumber, searchModel.PageSize, out totalCount);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - ProductPrices searched {{{l_Data.Rows.Count}}} of {totalCount} total.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating ProductPrices.");

                l_Response.CustomerProductCatalogDatatable = l_Data;
                l_Response.TotalCount = totalCount;

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "ProductPrices fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - ProductPrices are ready.");
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }
            finally
            {
                l_Data.Dispose();
            }

            return l_Response;
        }


        [HttpPost]
        [Route("createCustomerProductCatalog")]
        public async Task<GetCustomerProductCatalog> CreateCustomerProductCatalog([FromBody] SaveCustomerProductCatalogDataModel CustomerProductCatalogModel)
        {
            GetCustomerProductCatalog l_Response = new GetCustomerProductCatalog();
            Result l_Result = new Result();

            try
            {
                DB.Entities.CustomerProductCatalog l_CustomerProductCatalog = new DB.Entities.CustomerProductCatalog();
                l_CustomerProductCatalog.UseConnection(CommonUtils.ConnectionString);

                PublicFunctions.CopyTo(CustomerProductCatalogModel, l_CustomerProductCatalog);

                l_CustomerProductCatalog.CreatedBy = l_CustomerProductCatalog.CreatedBy;
                l_CustomerProductCatalog.CreatedDate = DateTime.Now;

                l_Result = l_CustomerProductCatalog.SaveNew();

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = $"CustomerProductCatalog [ {CustomerProductCatalogModel.Id} ] has been created successfully!";
                }
                else
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = l_Result.Description;
                }

            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Result.Description = ex.Message;
            }
            finally
            {

            }

            return l_Response;
        }

        [HttpPost]
        [Route("updateCustomerProductCatalog")]
        public async Task<GetCustomerProductCatalog> UpdateCustomerProductCatalog([FromBody] UpdateCustomerProductCatalogDataModel CustomerProductCatalogModel)
        {
            GetCustomerProductCatalog l_Response = new GetCustomerProductCatalog();
            Result l_Result = new Result();

            try
            {
                DB.Entities.CustomerProductCatalog l_CustomerProductCatalog = new DB.Entities.CustomerProductCatalog();
                l_CustomerProductCatalog.UseConnection(CommonUtils.ConnectionString);

                PublicFunctions.CopyTo(CustomerProductCatalogModel, l_CustomerProductCatalog);

                l_CustomerProductCatalog.ModifiedBy = l_CustomerProductCatalog.CreatedBy;
                l_CustomerProductCatalog.ModifiedDate = DateTime.Now;

                l_Result = l_CustomerProductCatalog.Modify();

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = $"Product Catalog [ {CustomerProductCatalogModel.ProductId} ] has been updated successfully!";
                }
                else
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = l_Result.Description;
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Result.Description = ex.Message;
            }
            finally
            {

            }

            return l_Response;
        }

        [HttpPost]
        [Route("processCustomerProductCatalogFile")]
        public async Task<GetCustomerProductCatalog> ProcessCustomerProductCatalogFile(IFormFile file, string ERPCustomerID,string ItemType)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            DataTable l_Data = new DataTable();
            GetCustomerProductCatalog l_Response = new GetCustomerProductCatalog();
            Result l_Result = new Result();
            CustomerProductCatalog l_CustomerProductCatalog = new CustomerProductCatalog();
            int p_UserNo = 1;
            Boolean p_Result = false;
            DataTable dataTable = new DataTable();
            SCS_ItemsType l_SCS_ItemsType = new SCS_ItemsType();
            DataTable l_SCSItemTypeDataTable = new DataTable();
            string l_ItemType = string.Empty;
            DataTable l_SCS_ItemTypeAttributeDataTable = new DataTable();
            DataTable l_FinalData = new DataTable();


            try
            {
                Customers l_Customer = new Customers();
                string l_Criteria = string.Empty;

                l_Customer.UseConnection(CommonUtils.ConnectionString);
                l_SCS_ItemsType.UseConnection(CommonUtils.ConnectionString);

                l_Criteria += $" ERPCustomerID = '{ERPCustomerID}'";

                l_Customer.GetList(l_Criteria, string.Empty, ref l_Data, "Id DESC");

                if (l_Data.Rows.Count == 0)
                {
                    l_Response.Code = 400;
                    l_Response.Message = $"Please provide a valid ERPCustomerID!";
                    l_Response.Description = $"Please provide a valid ERPCustomerID!";

                    return l_Response;
                }

                if (file == null || file.Length <= 0)
                {
                    l_Response.Code = 400;
                    l_Response.Message = $"Please provide a product catalog data file!";
                    l_Response.Description = $"Please provide a product catalog data file!";

                    return l_Response;
                }

                l_Criteria = "";
                l_Criteria += $" Item_Type_Id = '{ItemType}'";

                l_SCS_ItemsType.GetViewList(l_Criteria, string.Empty, ref l_SCSItemTypeDataTable, "Id DESC");
                
                if (l_SCSItemTypeDataTable.Rows.Count > 0)
                {
                    l_ItemType = l_SCSItemTypeDataTable.Rows[0]["Item_Type"].ToString();
                }

                l_SCS_ItemsType.GetItemTypeAttribute(ItemType, ERPCustomerID, ref l_SCS_ItemTypeAttributeDataTable);

                string deleteQuery = "DELETE FROM [Temp_SCS_CustomerProductCatalog] WHERE CustomerID = @CustomerID";
                using (var connection = new SqlConnection(CommonUtils.ConnectionString))
                using (var command = new SqlCommand(deleteQuery, connection))
                {
                    command.Parameters.AddWithValue("@CustomerID", ERPCustomerID);
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                using (var stream = file.OpenReadStream())
                using (var reader = new StreamReader(stream))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    csv.Read(); 
                    csv.ReadHeader();
                   
                    for (int i = 0; i < 16; i++)
                    {
                        dataTable.Columns.Add(csv.HeaderRecord[i],typeof(string));
                    }

                    dataTable.Columns.Add("JsonData", typeof(string));
                    dataTable.Columns.Add("CustomerID", typeof(string));

                    while (csv.Read())
                    {
                        var jsonData = new
                        {
                            fields = csv.HeaderRecord
                            .Skip(16)
                            .SelectMany(header =>
                            {
                                var value = csv.GetField(header);
                                if (!string.IsNullOrEmpty(value))
                                {
                                    var splitValues = value.Split('|');
                                    return splitValues.Select(splitValue => new
                                    {
                                        name = GetItemTypeAttributeName(header.Replace("*", ""), l_SCS_ItemTypeAttributeDataTable),
                                        value = splitValue
                                    });
                                }
                                return Enumerable.Empty<object>();
                            })
                            .Where(field => field != null)
                            .ToList()
                        };

                        jsonData.fields.Add(new { name = "product_classification.item_type", value = l_ItemType });

                        string jsonString = System.Text.Json.JsonSerializer.Serialize(jsonData);
                        DataRow row = dataTable.NewRow();

                        for (int i = 0; i < 16; i++)
                        {
                            row[i] = csv.GetField(i);
                        }

                        row["JsonData"] = jsonString;
                        row["CustomerID"] = ERPCustomerID;

                        dataTable.Rows.Add(row);
                    }

                    PublicFunctions.BulkInsert(CommonUtils.ConnectionString, "Temp_SCS_CustomerProductCatalog", dataTable);
                }

                l_CustomerProductCatalog.UseConnection(CommonUtils.ConnectionString);
                l_CustomerProductCatalog.SaveCustomerProductCatalog(ERPCustomerID, p_UserNo, ref l_FinalData);

                if (l_FinalData.Rows.Count > 0)
                {
                    l_Response.Code = Convert.ToInt16(l_FinalData.Rows[0]["Code"]);
                    l_Response.Message = Convert.ToString(l_FinalData.Rows[0]["Message"]);
                    l_Response.Description = Convert.ToString(l_FinalData.Rows[0]["Description"]);
                }
                else
                {
                    l_Response.Code = 400;
                    l_Response.Message = $"Invalid product catalog data file!";
                    l_Response.Description = $"Invalid product catalog data file!";
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = 400;
                l_Response.Message = $"Invalid product catalog data file!";
                l_Response.Description = $"Invalid product catalog data file!";
            }
            finally
            {
                dataTable.Dispose();
                l_Data.Dispose();
                l_SCS_ItemTypeAttributeDataTable.Dispose();
            }

            return l_Response;
        }

        [HttpGet]
        [Route("getHistoryCustomerProductCatalog")]
        public async Task<GetCustomerProductCatalog_Log> GetHistoryCustomerProductCatalog(string ERPCustomerID)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetCustomerProductCatalog_Log l_Response = new GetCustomerProductCatalog_Log();
            DataTable l_Data = new DataTable();

            try
            {
                string l_Criteria = string.Empty;
                CustomerProductCatalog l_CustomerProductCatalog = new CustomerProductCatalog();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_CustomerProductCatalog.UseConnection(CommonUtils.ConnectionString);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring History Customer Product Catalog.");

                l_CustomerProductCatalog.HistoryCustomerProductCatalog(ERPCustomerID, ref l_Data);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - History Customer Product Catalog searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating History Customer Product Catalog.");

                l_Response.CustomerProductCatalog_Log = new List<CustomerProductCatalog_LogDataModel>();

                foreach (DataRow l_Row in l_Data.Rows)
                {
                    CustomerProductCatalog_LogDataModel l_CustomerProductCatalog_LogRow = new CustomerProductCatalog_LogDataModel();

                    DBEntity.PopulateObjectFromRow(l_CustomerProductCatalog_LogRow, l_Data, l_Row);

                    l_Response.CustomerProductCatalog_Log.Add(l_CustomerProductCatalog_LogRow);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "History Customer Product Catalog fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - History Customer Product Catalog are ready.");
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }
            finally
            {
                l_Data.Dispose();
            }

            return l_Response;
        }

        [HttpGet]
        [Route("getItemTypes")]
        public async Task<GetItemTypesResponseModel> GetItemTypes(string ERPCustomerID = "")
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetItemTypesResponseModel l_Response = new GetItemTypesResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                string l_Criteria = string.Empty;
                SCS_ItemsType l_ItemsType = new SCS_ItemsType();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_ItemsType.UseConnection(CommonUtils.ConnectionString);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring Item Types search.");

                l_ItemsType.GetList("CustomerID = '" + ERPCustomerID + "'", string.Empty, ref l_Data, "Item_Type ASC");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Item Types searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating Item Types.");

                l_Response.ItemTypes = new List<ItemTypesDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    ItemTypesDataModel l_ItemTypesRow = new ItemTypesDataModel();

                    DBEntity.PopulateObjectFromRow(l_ItemTypesRow, l_Data, l_Row);

                    l_Response.ItemTypes.Add(l_ItemTypesRow);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Item Types fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Item Types are ready.");
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }
            finally
            {
                l_Data.Dispose();
            }

            return l_Response;
        }

        [HttpGet]
        [Route("getERPCustomers")]
        public async Task<GetCustomersResponseModel> GetERPCustomers()
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetCustomersResponseModel l_Response = new GetCustomersResponseModel();
            DataTable l_Data = new DataTable();

            UsersClaimData userData = new UsersClaimData();

            var claimsIdentity = User.Identity as ClaimsIdentity;

            if (claimsIdentity?.Claims == null)
            {
                l_Response.Code = StatusCodes.Status401Unauthorized;
                l_Response.Message = "Invalid token: Not Authorized";

                return l_Response;
            }

            userData = eSyncMate.Processor.Managers.CustomersManager.GetCustomerNames(claimsIdentity);

            try
            {
                string l_Criteria = !(string.IsNullOrEmpty(userData.Customers)) && !userData.IsSuperAdmin ? $"ERPCustomerID IN ({userData.Customers})" : string.Empty;
                Customers l_Customers = new Customers();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_Customers.UseConnection(CommonUtils.ConnectionString);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring Customers search.");

                l_Customers.GetList(l_Criteria, string.Empty, ref l_Data, "Id DESC");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Customers searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating Customers.");

                l_Response.Customers = new List<CustomerDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    CustomerDataModel l_CustomersRow = new CustomerDataModel();

                    DBEntity.PopulateObjectFromRow(l_CustomersRow, l_Data, l_Row);

                    l_Response.Customers.Add(l_CustomersRow);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Customers fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Customers are ready.");
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }
            finally
            {
                l_Data.Dispose();
            }

            return l_Response;
        }


        [HttpGet]
        [Route("downloadSampleFile")]
        public async Task<IActionResult> DownloadSampleFile(string CustomerID = "", string ItemTypeID = "")
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            DataTable l_Data = new DataTable();
            CustomerProductCatalog l_CustomerProductCatalog = new CustomerProductCatalog();
            int p_UserNo = 1;
            string fileName = "ProductCatalog.csv";
            string l_data = string.Empty;

            try
            {
                l_CustomerProductCatalog.UseConnection(CommonUtils.ConnectionString);

                l_CustomerProductCatalog.ProductCatalogFileHeaderColumn(CustomerID, ItemTypeID, p_UserNo, ref l_Data);
                this._logger.LogDebug($"[{fileName}.{DateTime.Now}] - Customer Product Catalog.");

                if (l_Data.Rows.Count > 0)
                {
                    l_data = l_Data.Rows[0]["Name"].ToString();

                    var content = new ByteArrayContent(Encoding.ASCII.GetBytes(l_data.ToString()));
                    content.Headers.Add("x-filename", fileName);
                    content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
                    content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = fileName
                    };
                }

                this._logger.LogDebug($"[{fileName}.{DateTime.Now}] - Process Complete.");

                return new FileContentResult(Encoding.ASCII.GetBytes(l_data.ToString()), "text/csv")
                {
                    FileDownloadName = fileName
                };
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpGet]
        [Route("getProductsData")]
        public async Task<GetCustomerProductCatalog> GetProductData(int ID)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetCustomerProductCatalog l_Response = new GetCustomerProductCatalog();
            DataTable l_Data = new DataTable();
            DB.Entities.CustomerProductCatalog l_CustomerProductCatalog = new DB.Entities.CustomerProductCatalog();

            try
            {
                string l_Criteria = string.Empty;


                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_CustomerProductCatalog.UseConnection(CommonUtils.ConnectionString);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring Customer Product Catalog Data.");

                l_CustomerProductCatalog.GetProductsData(ID, ref l_Data);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Customer Product Catalog Data searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating Route Data.");

                l_Response.CustomerProductCatalog = new List<CustomerProductCatalogDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    CustomerProductCatalogDataModel l_CustomerProductCatalogDataRow = new CustomerProductCatalogDataModel();

                    DBEntity.PopulateObjectFromRow(l_CustomerProductCatalogDataRow, l_Data, l_Row);

                    l_Response.CustomerProductCatalog.Add(l_CustomerProductCatalogDataRow);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Customer Product Catalog Data fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Customer Product Catalog Data are ready.");
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }
            finally
            {
                l_Data.Dispose();
            }

            return l_Response;
        }

        [HttpGet]
        [Route("downloadRejectedCSV")]
        public async Task<IActionResult> DownloadRejectedCSV(string CustomerID = "")
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            DataTable l_Data = new DataTable();
            CustomerProductCatalog l_CustomerProductCatalog = new CustomerProductCatalog();
            int p_UserNo = 1;
            string fileName = "RejectedProductCatalog.csv";
            string l_data = string.Empty;
            StringBuilder csvContent = new StringBuilder();
            SCS_ItemsType l_SCS_ItemsType = new SCS_ItemsType();
            DataTable l_SCS_ItemTypeAttributeDataTable  = new DataTable();
            try
            {
                l_CustomerProductCatalog.UseConnection(CommonUtils.ConnectionString);
                l_SCS_ItemsType.UseConnection(CommonUtils.ConnectionString);

                l_CustomerProductCatalog.RejectedProductCatalog(CustomerID, p_UserNo, ref l_Data);

                l_SCS_ItemsType.GetItemTypeAttributeName(l_Data.Rows[0]["ItemTypeName"].ToString(), CustomerID, ref l_SCS_ItemTypeAttributeDataTable);

                this._logger.LogDebug($"[{fileName}.{DateTime.Now}] - Rejected Customer Product Catalog.");

                if (l_Data.Rows.Count > 0)
                {
                    csvContent.AppendLine("ItemID,CustomerID,Category,Reason,FieldName,ErrorSeverity,ItemType,ErrorSource");

                    foreach (DataRow l_Row in l_Data.Rows)
                    {
                        string jsonData = l_Row["Data"].ToString();

                        if (l_Row["Type"].ToString() == "RSP-JSON")
                        {
                            var productStatuses = JsonConvert.DeserializeObject<List<SCS_ProductCatalogStatusResponseModel>>(jsonData);

                            csvContent.AppendLine($"{l_Row["ItemID"]},{l_Row["CustomerID"]}," +
                                   $"{productStatuses[0].product_statuses[0].errors[0].category},{productStatuses[0].product_statuses[0].errors[0].reason},{productStatuses[0].product_statuses[0].errors[0].field_name + "-"+GetItemTypeAttributeMappedProperty(productStatuses[0].product_statuses[0].errors[0].field_name, l_SCS_ItemTypeAttributeDataTable)},{productStatuses[0].product_statuses[0].errors[0].error_severity},{l_Row["ItemTypeName"]},{l_Row["ErrorSource"]}");
                        }
                        else
                        {
                            // The stored payload is whatever the partner answered, so it does not always carry
                            // an errors array. Without these guards one such row fails the whole export.
                            ProductCreateResponse productStatus = null;

                            try
                            {
                                productStatus = JsonConvert.DeserializeObject<ProductCreateResponse>(jsonData);
                            }
                            catch
                            {
                                productStatus = null;
                            }

                            string errorsString = productStatus == null
                                ? jsonData
                                : productStatus.errors != null && productStatus.errors.Length > 0
                                    ? string.Join(";", productStatus.errors)
                                    : (productStatus.message ?? jsonData);

                            errorsString = (errorsString ?? string.Empty).Replace(',', ' ').Replace("\r", " ").Replace("\n", " ");

                            csvContent.AppendLine($"{l_Row["ItemID"]},{l_Row["CustomerID"]}," +
                                                  $"{""},{errorsString},{""},{""},{l_Row["ItemTypeName"]},{l_Row["ErrorSource"]}");
                        }
                    } 

                    var content = new ByteArrayContent(Encoding.ASCII.GetBytes(csvContent.ToString()));

                    content.Headers.Add("x-filename", fileName);
                    content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
                    content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = fileName
                    };
                }

                this._logger.LogDebug($"[{fileName}.{DateTime.Now}] - Process Complete.");

                return new FileContentResult(Encoding.ASCII.GetBytes(csvContent.ToString()), "text/csv")
                {
                    FileDownloadName = fileName
                };
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex);
            }
        }

        private string GetItemTypeAttributeName(string p_Name ,DataTable p_ItemTypeAttributeDataTable)
        {
            var Mapped_Property = (from row in p_ItemTypeAttributeDataTable.AsEnumerable()
                          where row.Field<string>("Name") == p_Name
                          select row.Field<string>("Mapped_Property")).FirstOrDefault();

            return Mapped_Property ?? "";
        }

        private string GetItemTypeMappedName(string p_Name, DataTable p_ItemTypeAttributeDataTable)
        {
            var Name = (from row in p_ItemTypeAttributeDataTable.AsEnumerable()
                                   where row.Field<string>("Mapped_Property") == p_Name
                                   select row.Field<string>("Name")).FirstOrDefault();

            return Name ?? p_Name;
        }

        private string GetItemTypeAttributeMappedProperty(string p_Mapped_Property, DataTable p_ItemTypeAttributeDataTable)
        {
            var Name = (from row in p_ItemTypeAttributeDataTable.AsEnumerable()
                                   where row.Field<string>("Mapped_Property") == p_Mapped_Property
                                   select row.Field<string>("Name")).FirstOrDefault();

            return Name ?? "";
        }


        [HttpGet]
        [Route("downloadProductPricesSampleFile")]
        public async Task<IActionResult> DownloadProductPricesSampleFile(string CustomerID = "")
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            DataTable l_Data = new DataTable();
            CustomerProductCatalog l_CustomerProductCatalog = new CustomerProductCatalog();
            int p_UserNo = 1;
            string fileName = "ProductPrices.csv";
            string l_data = string.Empty;

            try
            {
                l_CustomerProductCatalog.UseConnection(CommonUtils.ConnectionString);

                l_CustomerProductCatalog.ProductPricesFileHeaderColumn(CustomerID, p_UserNo, ref l_Data);
                this._logger.LogDebug($"[{fileName}.{DateTime.Now}] - Product Prices.");

                if (l_Data.Rows.Count > 0)
                {
                    l_data = l_Data.Rows[0]["Name"].ToString();

                    var content = new ByteArrayContent(Encoding.ASCII.GetBytes(l_data.ToString()));
                    content.Headers.Add("x-filename", fileName);
                    content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
                    content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = fileName
                    };
                }

                this._logger.LogDebug($"[{fileName}.{DateTime.Now}] - Process Complete.");

                return new FileContentResult(Encoding.ASCII.GetBytes(l_data.ToString()), "text/csv")
                {
                    FileDownloadName = fileName
                };
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPost]
        [Route("processProductPricesFile")]
        public async Task<GetCustomerProductCatalog> ProcessProductPricesFile(IFormFile file, string ERPCustomerID)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            DataTable l_Data = new DataTable();
            GetCustomerProductCatalog l_Response = new GetCustomerProductCatalog();
            Result l_Result = new Result();
            CustomerProductCatalog l_CustomerProductCatalog = new CustomerProductCatalog();
            int p_UserNo = 1;
            Boolean p_Result = false;
            DataTable dataTable = new DataTable();
            SCS_ItemsType l_SCS_ItemsType = new SCS_ItemsType();
            DataTable l_ProductData = new DataTable();
            string l_ItemType = string.Empty;

            try
            {
                Customers l_Customer = new Customers();
                string l_Criteria = string.Empty;

                l_Customer.UseConnection(CommonUtils.ConnectionString);
                l_SCS_ItemsType.UseConnection(CommonUtils.ConnectionString);

                l_Criteria += $" ERPCustomerID = '{ERPCustomerID}'";

                l_Customer.GetList(l_Criteria, string.Empty, ref l_Data, "Id DESC");

                if (l_Data.Rows.Count == 0)
                {
                    l_Response.Code = 400;
                    l_Response.Message = $"Please provide a valid ERPCustomerID!";
                    l_Response.Description = $"Please provide a valid ERPCustomerID!";

                    return l_Response;
                }

                if (file == null || file.Length <= 0)
                {
                    l_Response.Code = 400;
                    l_Response.Message = $"Please provide a product catalog data file!";
                    l_Response.Description = $"Please provide a product catalog data file!";

                    return l_Response;
                }

                using (var stream = file.OpenReadStream())
                using (var reader = new StreamReader(stream))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    csv.Read();
                    csv.ReadHeader();

                    for (int i = 0; i < csv.HeaderRecord.Length; i++)
                    {
                        dataTable.Columns.Add(csv.HeaderRecord[i], typeof(string));
                    }

                    dataTable.Columns.Add("CustomerID", typeof(string));

                    while (csv.Read())
                    {
                        DataRow row = dataTable.NewRow();

                        for (int i = 0; i < csv.HeaderRecord.Length; i++)
                        {
                            row[i] = csv.GetField(i);
                        }

                        row["CustomerID"] = ERPCustomerID;

                        dataTable.Rows.Add(row);
                    }

                    PublicFunctions.BulkInsert(CommonUtils.ConnectionString, "Temp_SCS_ProductPrices", dataTable);
                }

                l_CustomerProductCatalog.UseConnection(CommonUtils.ConnectionString);
                l_CustomerProductCatalog.SaveProductPrices(ERPCustomerID, p_UserNo,ref l_ProductData);

                if (Convert.ToBoolean(l_ProductData.Rows[0]["Success"])  == true)
                {
                    l_Response.Code = 200;
                    l_Response.Message = l_ProductData.Rows[0]["Message"].ToString()    ;
                    l_Response.Description = l_ProductData.Rows[0]["Description"].ToString();
                }
                else
                {
                    l_Response.Code = 400;
                    l_Response.Message = l_ProductData.Rows[0]["Message"].ToString();
                    l_Response.Description = l_ProductData.Rows[0]["Description"].ToString();
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = 400;
                l_Response.Message = $"Invalid product prices data file!";
                l_Response.Description = $"Invalid product prices data file!";
            }
            finally
            {
                dataTable.Dispose();
                l_Data.Dispose();
                l_ProductData.Dispose();
            }

            return l_Response;
        }

        [HttpGet]
        [Route("getPrepareItemData")]
        public async Task<PrepareItemDataResponseModel> GetPrepareItemData(int UserID,string CustomerID,string ItemType)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            PrepareItemDataResponseModel l_Response = new PrepareItemDataResponseModel();
            DataTable l_Data = new DataTable();
            DB.Entities.CustomerProductCatalog l_CustomerProductCatalog = new DB.Entities.CustomerProductCatalog();

            try
            {
                string l_Criteria = string.Empty;


                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_CustomerProductCatalog.UseConnection(CommonUtils.ConnectionString);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring Prepare Items Data.");

                l_CustomerProductCatalog.GetPrepareItemData(UserID, CustomerID, ItemType, ref l_Data);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Prepare Items Data searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating Prepare Items Data.");

                l_Response.ItemDataResponseDatatable = l_Data;

                //l_Response.CustomerProductCatalog = new List<CustomerProductCatalogDataModel>();
                //foreach (DataRow l_Row in l_Data.Rows)
                //{
                //    CustomerProductCatalogDataModel l_CustomerProductCatalogDataRow = new CustomerProductCatalogDataModel();

                //    DBEntity.PopulateObjectFromRow(l_CustomerProductCatalogDataRow, l_Data, l_Row);

                //    l_Response.CustomerProductCatalog.Add(l_CustomerProductCatalogDataRow);
                //}

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Prepare Items Data fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Prepare Items Data are ready.");
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }
            finally
            {
                l_Data.Dispose();
            }

            return l_Response;
        }

        [HttpGet]
        [Route("insertPrepareItemData")]
        public async Task<PrepareItemDataResponseModel> InsertPrepareItemData(int UserID, string CustomerID, string ItemType)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            PrepareItemDataResponseModel l_Response = new PrepareItemDataResponseModel();
            DataTable l_Data = new DataTable();
            DB.Entities.CustomerProductCatalog l_CustomerProductCatalog = new DB.Entities.CustomerProductCatalog();

            try
            {
                string l_Criteria = string.Empty;


                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_CustomerProductCatalog.UseConnection(CommonUtils.ConnectionString);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring Prepare Items Data.");

                l_CustomerProductCatalog.InsertPrepareItemData(UserID, CustomerID, ItemType);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Prepare Items Data searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating Prepare Items Data.");

                //l_Response.ItemDataResponseDatatable = l_Data;

                //l_Response.CustomerProductCatalog = new List<CustomerProductCatalogDataModel>();
                //foreach (DataRow l_Row in l_Data.Rows)
                //{
                //    CustomerProductCatalogDataModel l_CustomerProductCatalogDataRow = new CustomerProductCatalogDataModel();

                //    DBEntity.PopulateObjectFromRow(l_CustomerProductCatalogDataRow, l_Data, l_Row);

                //    l_Response.CustomerProductCatalog.Add(l_CustomerProductCatalogDataRow);
                //}

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "We are processing the items' data. Once this process is complete, you will be able to download it.!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Prepare Items Data are ready.");
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }
            finally
            {
                l_Data.Dispose();
            }

            return l_Response;
        }

        [HttpGet]
        [Route("downloadItemsDataCSV")]
        public async Task<ActionResult> DownloadItemsDataCSV(string CustomerID = "", string ItemType = "",int UserID = 0)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            DataTable l_Data = new DataTable();
            CustomerProductCatalog l_CustomerProductCatalog = new CustomerProductCatalog();
            int p_UserNo = 1;
            string fileName = $"{CustomerID}-{ItemType}.xlsx";
            StringBuilder csvContent = new StringBuilder();
            SCS_ItemsType l_SCS_ItemsType = new SCS_ItemsType();
            string base64String = string.Empty;
            try
            {
                l_CustomerProductCatalog.UseConnection(CommonUtils.ConnectionString);
                
                l_CustomerProductCatalog.GetPrepareItemData(UserID, CustomerID, ItemType, ref l_Data);

                this._logger.LogDebug($"[{fileName}.{DateTime.Now}] - Rejected Customer Product Catalog.");

                if (l_Data.Rows.Count > 0)
                {

                    base64String = l_Data.Rows[0]["CSVData"]?.ToString() ?? string.Empty;

                    //byte[] csvData = l_Data.Rows[0]["CSVData"] as byte[];

                    //csvContent.Clear(); // Clear existing content if necessary
                    //csvContent.Append(Encoding.ASCII.GetString(csvData)); // Append the string to the StringBuilder

                    //var content = new ByteArrayContent(csvData);
                    //content.Headers.Add("x-filename", fileName);
                    //content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
                    //content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    //{
                    //    FileName = fileName
                    //};

                    //this._logger.LogDebug($"[{fileName}.{DateTime.Now}] - Process Complete.");

                    //return new FileContentResult(csvData, "text/csv")
                    //{
                    //    FileDownloadName = fileName
                    //};
                }

                //return new StatusCodeResult((int)HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex);
            }

            return new CreatedResult(string.Empty, new
            {
                Code = 200,
                Status = true,
                Message = "",
                Data = base64String
            });
        }


        /// <summary>
        /// Reads item ids out of the uploaded CSV and reports what a delete would remove.
        /// Nothing is changed here — this feeds the confirmation step.
        /// </summary>
        [HttpPost]
        [Route("previewDeleteProducts")]
        public async Task<DeleteProductsPreviewResponseModel> PreviewDeleteProducts(IFormFile file)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            DeleteProductsPreviewResponseModel l_Response = new DeleteProductsPreviewResponseModel();
            DataTable l_Data = new DataTable();

            l_Response.Found = new List<DeleteProductsPreviewRow>();
            l_Response.NotFound = new List<string>();

            try
            {
                l_Response.Code = (int)ResponseCodes.Error;

                List<string> l_ItemIDs = ReadItemIDsFromCsv(file);

                l_Response.TotalItemIDs = l_ItemIDs.Count;

                if (l_ItemIDs.Count == 0)
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = "No Item IDs were found in the file. The file should have a single ItemID column.";

                    return l_Response;
                }

                DB.Entities.CustomerProductCatalog l_Catalog = new DB.Entities.CustomerProductCatalog();
                l_Catalog.UseConnection(CommonUtils.ConnectionString);
                l_Catalog.GetProductsByItemIDs(l_ItemIDs, ref l_Data);

                var l_FoundIDs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (DataRow l_Row in l_Data.Rows)
                {
                    string l_ItemID = PublicFunctions.ConvertNullAsString(l_Row["ItemID"], string.Empty);
                    l_FoundIDs.Add(l_ItemID);

                    l_Response.Found.Add(new DeleteProductsPreviewRow
                    {
                        ItemID = l_ItemID,
                        ProductCount = PublicFunctions.ConvertNullAsInteger(l_Row["ProductCount"], 0),
                        CustomerCount = PublicFunctions.ConvertNullAsInteger(l_Row["CustomerCount"], 0),
                        Customers = PublicFunctions.ConvertNullAsString(l_Row["Customers"], string.Empty)
                    });
                }

                foreach (string l_ItemID in l_ItemIDs)
                {
                    if (!l_FoundIDs.Contains(l_ItemID)) l_Response.NotFound.Add(l_ItemID);
                }

                l_Response.TotalProducts = l_Response.Found.Sum(f => f.ProductCount);
                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "File read successfully.";
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }
            finally
            {
                l_Data.Dispose();
            }

            return l_Response;
        }

        /// <summary>
        /// Permanently removes catalog rows for the given item ids across every customer,
        /// along with their detail rows.
        /// </summary>
        [HttpPost]
        [Route("deleteProducts")]
        public async Task<DeleteProductsResponseModel> DeleteProducts([FromBody] DeleteProductsRequestModel request)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            DeleteProductsResponseModel l_Response = new DeleteProductsResponseModel();

            try
            {
                l_Response.Code = (int)ResponseCodes.Error;

                List<string> l_ItemIDs = (request?.ItemIDs ?? new List<string>())
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .Select(i => i.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (l_ItemIDs.Count == 0)
                {
                    l_Response.Message = "No Item IDs were supplied.";

                    return l_Response;
                }

                DB.Entities.CustomerProductCatalog l_Catalog = new DB.Entities.CustomerProductCatalog();
                l_Catalog.UseConnection(CommonUtils.ConnectionString);

                int l_DeletedProducts = 0;
                int l_DeletedData = 0;

                Result l_Result = l_Catalog.DeleteProductsByItemIDs(l_ItemIDs, out l_DeletedProducts, out l_DeletedData);

                l_Response.DeletedProducts = l_DeletedProducts;
                l_Response.DeletedProductData = l_DeletedData;

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = (int)ResponseCodes.Success;
                    l_Response.Message = $"{l_DeletedProducts} product(s) and {l_DeletedData} data row(s) deleted.";

                    this._logger.LogWarning($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Deleted {l_DeletedProducts} catalog product(s) / {l_DeletedData} data row(s) for {l_ItemIDs.Count} item id(s): {string.Join(",", l_ItemIDs)}");
                }
                else
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = "No matching products were found to delete.";
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }

            return l_Response;
        }

        /// <summary>
        /// First CSV field of a line, honouring quotes so ids such as "512N-60(3A)[7PC]BKS"
        /// — or a quoted value that itself contains a comma — survive intact.
        /// </summary>
        private static string ReadFirstCsvField(string p_Line)
        {
            string l_Line = p_Line.TrimStart('﻿');

            if (!l_Line.StartsWith("\""))
            {
                int l_Comma = l_Line.IndexOf(',');
                return (l_Comma >= 0 ? l_Line.Substring(0, l_Comma) : l_Line).Trim();
            }

            var l_Field = new System.Text.StringBuilder();

            for (int i = 1; i < l_Line.Length; i++)
            {
                if (l_Line[i] == '"')
                {
                    // "" inside a quoted field is a literal quote
                    if (i + 1 < l_Line.Length && l_Line[i + 1] == '"')
                    {
                        l_Field.Append('"');
                        i++;
                        continue;
                    }

                    break;
                }

                l_Field.Append(l_Line[i]);
            }

            return l_Field.ToString().Trim();
        }

        /// <summary>
        /// Single-column CSV of item ids. A header row named ItemID/Item ID/SKU is skipped when present.
        /// </summary>
        private static List<string> ReadItemIDsFromCsv(IFormFile file)
        {
            var l_ItemIDs = new List<string>();
            var l_Seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (file == null || file.Length == 0)
            {
                return l_ItemIDs;
            }

            using (var l_Stream = file.OpenReadStream())
            using (var l_Reader = new StreamReader(l_Stream))
            {
                bool l_FirstLine = true;

                while (!l_Reader.EndOfStream)
                {
                    string l_Line = l_Reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(l_Line)) continue;

                    // Only the first column is used, so extra columns are simply ignored
                    string l_Value = ReadFirstCsvField(l_Line);

                    if (l_FirstLine)
                    {
                        l_FirstLine = false;

                        string l_Header = l_Value.Replace(" ", string.Empty).Replace("_", string.Empty);
                        if (l_Header.Equals("ItemID", StringComparison.OrdinalIgnoreCase)
                            || l_Header.Equals("Item", StringComparison.OrdinalIgnoreCase)
                            || l_Header.Equals("SKU", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(l_Value)) continue;

                    if (l_Seen.Add(l_Value))
                    {
                        l_ItemIDs.Add(l_Value);
                    }
                }
            }

            return l_ItemIDs;
        }

        [HttpPost]
        [Route("deleteItemsData")]
        public async Task<PrepareItemDataResponseModel> DeleteItemsData([FromBody] DeleteItemsDataRequest request)
        {
            int UserID = request.UserID;
            string CustomerID = request.CustomerID;
            string ItemType = request.ItemType;
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            PrepareItemDataResponseModel l_Response = new PrepareItemDataResponseModel();
            DataTable l_Data = new DataTable();
            DB.Entities.CustomerProductCatalog l_CustomerProductCatalog = new DB.Entities.CustomerProductCatalog();

            try
            {
                string l_Criteria = string.Empty;


                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_CustomerProductCatalog.UseConnection(CommonUtils.ConnectionString);

                l_CustomerProductCatalog.DeleteItemsData(UserID, CustomerID, ItemType);

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Opertaion Completed Successfully.";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Prepare Items Data are ready.");
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }
            finally
            {
                l_Data.Dispose();
            }

            return l_Response;
        }


        [HttpGet]
        [Route("processCustomerProductPricesData")]
        public async Task<PrepareItemDataResponseModel> processCustomerProductPricesData(int UserID, string customerID)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            PrepareItemDataResponseModel l_Response = new PrepareItemDataResponseModel();
            DataTable l_Data = new DataTable();
            DB.Entities.CustomerProductCatalog l_CustomerProductCatalog = new DB.Entities.CustomerProductCatalog();
            Int32 RouteID = 0;
            string Response = string.Empty;
            string l_SettingName = string.Empty;
            try
            {
                string l_Criteria = string.Empty;


                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                if (customerID == "TAR6266PAH")
                {
                    l_SettingName = "BulkUpdatePricesFromCustomerPortal_TAR6266PAH";
                }
                else if (customerID == "TAR6266P")
                {
                    l_SettingName = "RouteExecutePriceData";
                }

                l_Data = PublicFunctions.GetDataFromApplicationSettings(l_SettingName, CommonUtils.ConnectionString);

                if (l_Data.Rows.Count > 0)
                {
                    RouteID = Convert.ToInt32(l_Data.Rows[0]["TagValue"]);

                    RouteEngine l_Engine = new RouteEngine(this._config);

                    l_Engine.Execute(RouteID);


                    l_Response.Code = (int)ResponseCodes.Success;
                    l_Response.Message = "Processing the price data.";
                }
                else
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = "Processing the price data.";
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }
            finally
            {
                l_Data.Dispose();
            }

            return l_Response;
        }


        [HttpGet]
        [Route("processResolveError")]
        public async Task<PrepareItemDataResponseModel> processResolveError(int UserID)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            PrepareItemDataResponseModel l_Response = new PrepareItemDataResponseModel();
            DataTable l_Data = new DataTable();
            DB.Entities.CustomerProductCatalog l_CustomerProductCatalog = new DB.Entities.CustomerProductCatalog();
            bool Response = false;
            try
            {
                string l_Criteria = string.Empty;


                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_CustomerProductCatalog.UseConnection(CommonUtils.ConnectionString);

                Response = l_CustomerProductCatalog.UpdateErrorResolveDate();

                if (Response == true)
                {
                    l_Response.Code = (int)ResponseCodes.Success;
                    l_Response.Message = "Products' error resolved successfully.";
                }
                else
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = "Something went wrong. Please contact the administrator.";
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }
            finally
            {
                l_Data.Dispose();
            }

            return l_Response;
        }

    }
}
