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

            if (searchModel.SearchOption == "Created Date")
            {
                dateRange = searchModel.SearchValue;
                dateValues = dateRange.Split('/');
                startDate = dateValues[0].Trim() + " 00:00:00.000";
                endDate = dateValues[1].Trim() + " 23:59:59.999";
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

                if (searchModel.SearchOption == "Created Date")
                {
                    l_Criteria += $" CONVERT(DATE,CreatedDate) >= '{startDate}'";
                }

                if (searchModel.SearchOption == "Created Date")
                {
                    l_Criteria += $" AND CONVERT(DATE,CreatedDate) <= '{endDate}'";
                }

                if (searchModel.SearchOption == "ProductId")
                {
                    l_Criteria = $" ProductId = {searchModel.SearchValue}";
                }
                else if (searchModel.SearchOption == "ERP CustomerID")
                {
                    l_Criteria = $" CustomerID = '{searchModel.SearchValue}'";
                }
                else if (searchModel.SearchOption == "ItemID")
                {
                    l_Criteria = $" ItemID LIKE '%{searchModel.SearchValue}%'";
                }
                else if (searchModel.SearchOption == "Item Type Name")
                {
                    l_Criteria = $" ItemTypeName LIKE '%{searchModel.SearchValue}%'";
                }
                else if (searchModel.SearchOption == "Parent ID")
                {
                    l_Criteria = $" ParentID LIKE '%{searchModel.SearchValue}%'";
                }
                else if (searchModel.SearchOption == "Status")
                {
                    l_Criteria = $" SyncStatus LIKE '%{searchModel.SearchValue}%'";
                }

                if (string.IsNullOrEmpty(l_Criteria) && userData.UserType?.ToUpper() != "ADMIN")
                {
                    l_Criteria = $" CustomerID IN ({userData.Customers})";
                }


                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring PartnerGroup search.");

                l_CustomerProductCatalog.GetViewList(l_Criteria, string.Empty, ref l_Data, "Id DESC");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - CustomerProductCatalog searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating CustomerProductCatalog.");

                l_Response.CustomerProductCatalogDatatable = l_Data;

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

            if (searchModel.SearchOption == "Created Date")
            {
                dateRange = searchModel.SearchValue;
                dateValues = dateRange.Split('/');
                startDate = dateValues[0].Trim() + " 00:00:00.000";
                endDate = dateValues[1].Trim() + " 23:59:59.999";
            }

            try
            {
                string l_Criteria = string.Empty;
                DB.Entities.CustomerProductCatalog l_CustomerProductCatalog = new DB.Entities.CustomerProductCatalog();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_CustomerProductCatalog.UseConnection(CommonUtils.ConnectionString);

                if (searchModel.SearchOption == "Created Date")
                {
                    l_Criteria += $" CONVERT(DATE,CreatedDate) >= '{startDate}'";
                }

                if (searchModel.SearchOption == "Created Date")
                {
                    l_Criteria += $" AND CONVERT(DATE,CreatedDate) <= '{endDate}'";
                }

                if (searchModel.SearchOption == "ProductId")
                {
                    l_Criteria = $" ProductId = {searchModel.SearchValue}";
                }
                else if (searchModel.SearchOption == "ERP CustomerID")
                {
                    l_Criteria = $" CustomerID = '{searchModel.SearchValue}'";
                }
                else if (searchModel.SearchOption == "ItemID")
                {
                    l_Criteria = $" ItemID LIKE '%{searchModel.SearchValue}%'";
                }

                else if (searchModel.SearchOption == "Status")
                {
                    l_Criteria = $" SyncStatus LIKE '%{searchModel.SearchValue}%'";
                }

                if (string.IsNullOrEmpty(l_Criteria) && userData.UserType?.ToUpper() != "ADMIN")
                {
                    l_Criteria = $" CustomerID IN ({userData.Customers})";
                }

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring ProductPrices search.");

                l_CustomerProductCatalog.GetProductViewList(l_Criteria, string.Empty, ref l_Data, "Id DESC");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - ProductPrices searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating ProductPrices.");

                l_Response.CustomerProductCatalogDatatable = l_Data;

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
                string l_Criteria = !(string.IsNullOrEmpty(userData.Customers)) && userData.UserType?.ToUpper() != "ADMIN" ? $"ERPCustomerID IN ({userData.Customers})" : string.Empty;
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
                            var productStatus = JsonConvert.DeserializeObject<ProductCreateResponse>(jsonData);
                            string errorsString = string.Join(";", productStatus.errors);

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


        [HttpGet]
        [Route("deleteItemsData")]
        public async Task<PrepareItemDataResponseModel> DeleteItemsData(int UserID, string CustomerID, string ItemType)
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
