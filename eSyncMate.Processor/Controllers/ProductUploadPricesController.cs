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
using System.Text.RegularExpressions;
using System.Security.Claims;

namespace eSyncMate.Processor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    public class ProductUploadPricesController : ControllerBase
    {
        private readonly ILogger<ProductUploadPricesController> _logger;
        public ProductUploadPricesController(ILogger<ProductUploadPricesController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("getProductUploadPrices")]
        public async Task<GetProductUploadPrices> GetProductUploadPrices([FromQuery] ProductUploadPricesSearchModel searchModel)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetProductUploadPrices l_Response = new GetProductUploadPrices();
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

            if (searchModel.SearchOption == "Created Date" || searchModel.SearchOption == "Promo StartDate" || searchModel.SearchOption == "Promo EndDate")
            {
                dateRange = searchModel.SearchValue;
                dateValues = dateRange.Split('/');
                startDate = dateValues[0].Trim() + " 00:00:00.000";
                endDate = dateValues[1].Trim() + " 23:59:59.999";
            }

            try
            {
                string l_Criteria = string.Empty;
                DB.Entities.ProductUploadPrices l_ProductUploadPrices = new DB.Entities.ProductUploadPrices();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_ProductUploadPrices.UseConnection(CommonUtils.ConnectionString);

                if (searchModel.SearchOption == "Created Date")
                {
                    l_Criteria += $" CONVERT(DATE,CreatedDate) >= '{startDate}'";
                }

                if (searchModel.SearchOption == "Created Date")
                {
                    l_Criteria += $" AND CONVERT(DATE,CreatedDate) <= '{endDate}'";
                }

                if (searchModel.SearchOption == "Promo StartDate")
                {
                    l_Criteria += $" CONVERT(DATE,PromoStartDate) >= '{startDate}'";
                }

                if (searchModel.SearchOption == "Promo StartDate")
                {
                    l_Criteria += $" AND CONVERT(DATE,PromoStartDate) <= '{endDate}'";
                }

                if (searchModel.SearchOption == "Promo EndDate")
                {
                    l_Criteria += $" CONVERT(DATE,PromoEndDate) >= '{startDate}'";
                }

                if (searchModel.SearchOption == "Promo EndDate")
                {
                    l_Criteria += $" AND CONVERT(DATE,PromoEndDate) <= '{endDate}'";
                }

                if (searchModel.SearchOption == "Id")
                {
                    l_Criteria = $" Id = {searchModel.SearchValue}";
                }
                else if (searchModel.SearchOption == "ERP CustomerID")
                {
                    l_Criteria = $" CustomerID = '{searchModel.SearchValue}'";
                }
                else if (searchModel.SearchOption == "ItemID")
                {
                    l_Criteria = $" ItemID = '{searchModel.SearchValue}'";
                }

                if (string.IsNullOrEmpty(l_Criteria) && userData.UserType?.ToUpper() != "ADMIN")
                {
                    l_Criteria = $" CustomerID IN ({userData.Customers})";
                }

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring PartnerGroup search.");

                l_ProductUploadPrices.GetViewList(l_Criteria, string.Empty, ref l_Data, "Id DESC");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - ProductUploadPrices searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating ProductUploadPrices.");

                l_Response.ProductUploadPrices = new List<ProductUploadPricesDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    ProductUploadPricesDataModel l_ProductUploadPricesRow = new ProductUploadPricesDataModel();

                    DBEntity.PopulateObjectFromRow(l_ProductUploadPricesRow, l_Data, l_Row);

                    l_Response.ProductUploadPrices.Add(l_ProductUploadPricesRow);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "ProductUploadPrices fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - ProductUploadPrices are ready.");
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
        [Route("createProductUploadPrices")]
        public async Task<GetProductUploadPrices> CreateProductUploadPrices([FromBody] SaveProductUploadPricesDataModel ProductUploadPricesModel)
        {
            GetProductUploadPrices l_Response = new GetProductUploadPrices();
            Result l_Result = new Result();

            try
            {
                DB.Entities.ProductUploadPrices l_ProductUploadPrices = new DB.Entities.ProductUploadPrices();
                l_ProductUploadPrices.UseConnection(CommonUtils.ConnectionString);

                PublicFunctions.CopyTo(ProductUploadPricesModel, l_ProductUploadPrices);

                l_ProductUploadPrices.CreatedBy = l_ProductUploadPrices.CreatedBy;
                l_ProductUploadPrices.CreatedDate = DateTime.Now;

                l_Result = l_ProductUploadPrices.SaveNew();

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = $"ProductUploadPrices [ {ProductUploadPricesModel.Id} ] has been created successfully!";
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
        [Route("updateProductUploadPrices")]
        public async Task<GetProductUploadPrices> UpdateProductUploadPrices([FromBody] UpdateProductUploadPricesDataModel ProductUploadPricesModel)
        {
            GetProductUploadPrices l_Response = new GetProductUploadPrices();
            Result l_Result = new Result();

            try
            {
                DB.Entities.ProductUploadPrices l_ProductUploadPrices = new DB.Entities.ProductUploadPrices();
                l_ProductUploadPrices.UseConnection(CommonUtils.ConnectionString);

                PublicFunctions.CopyTo(ProductUploadPricesModel, l_ProductUploadPrices);

                l_ProductUploadPrices.ModifiedBy = l_ProductUploadPrices.CreatedBy;
                l_ProductUploadPrices.ModifiedDate = DateTime.Now;

                l_Result = l_ProductUploadPrices.Modify();

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = $"Map [ {ProductUploadPricesModel.Id} ] has been updated successfully!";
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
        [Route("processProductUploadPricesFile")]
        public async Task<GetProductUploadPrices> ProcessProductUploadPricesFile(IFormFile file, string ERPCustomerID)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            DataTable l_Data = new DataTable();
            GetProductUploadPrices l_Response = new GetProductUploadPrices();
            Result l_Result = new Result();
            ProductUploadPrices l_ProductUploadPrices = new ProductUploadPrices();
            int p_UserNo = 1;
            Boolean p_Result = false;
            DataTable dataTable = new DataTable();
            DataTable l_FinalData = new DataTable();
            try
            {
                Customers l_Customer = new Customers();
                string l_Criteria = string.Empty;

                l_Customer.UseConnection(CommonUtils.ConnectionString);

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
                        dataTable.Columns.Add(csv.HeaderRecord[i].Replace("*",""), typeof(string));
                    }

                    dataTable.Columns.Add("CustomerID", typeof(string));

                    while (csv.Read())
                    {
                        DataRow row = dataTable.NewRow();

                        for (int i = 0; i < csv.HeaderRecord.Length; i++)
                        {
                            string fieldValue = csv.GetField<string>(i);
                            fieldValue = fieldValue.Replace("$", "").Replace(",", ""); // Remove dollar signs and commas

                            // Attempt to parse the cleaned value as a decimal to ensure it's a valid number
                            if (decimal.TryParse(fieldValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                            {
                                fieldValue = result.ToString(CultureInfo.InvariantCulture); // Ensures the value is in proper decimal format
                            }
                            row[i] = fieldValue;
                        }

                        row["CustomerID"] = ERPCustomerID;

                        dataTable.Rows.Add(row);
                    }

                    PublicFunctions.BulkInsert(CommonUtils.ConnectionString, "Temp_ProductUploadPrices", dataTable);
                }

                l_ProductUploadPrices.UseConnection(CommonUtils.ConnectionString);
                l_ProductUploadPrices.SaveProductUploadPrices(ERPCustomerID, p_UserNo, ref l_FinalData);

                if (l_FinalData.Rows.Count > 0)
                {
                    l_Response.Code = Convert.ToInt16(l_FinalData.Rows[0]["Code"]);
                    l_Response.Message = Convert.ToString(l_FinalData.Rows[0]["Message"]);
                    l_Response.Description = Convert.ToString(l_FinalData.Rows[0]["Description"]);
                }
                else
                {
                    l_Response.Code = 400;
                    l_Response.Message = $"Invalid product upload price data file!";
                    l_Response.Description = $"Invalid product upload price data file!";
                }

            }
            catch (Exception ex)
            {
                l_Response.Code = 400;
                l_Response.Message = $"Invalid product upload price data file!";
                l_Response.Description = $"Invalid product upload price data file!";
            }
            finally
            {
                dataTable.Dispose();
                l_Data.Dispose();
                l_FinalData.Dispose();
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
                string l_Criteria = !(string.IsNullOrEmpty(userData.Customers)) && userData.UserType.ToUpper() != "ADMIN" ? $"ERPCustomerID IN ({userData.Customers})" : string.Empty;
                
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
        public async Task<IActionResult> DownloadSampleFile()
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            DataTable l_Data = new DataTable();
            ProductUploadPrices l_ProductUploadPrices = new ProductUploadPrices();
            string fileName = "ProductPromotion.csv";
            string l_data = string.Empty;

            try
            {
                l_ProductUploadPrices.UseConnection(CommonUtils.ConnectionString);

                l_ProductUploadPrices.ProductCatalogFileHeaderColumn(ref l_Data);
                this._logger.LogDebug($"[{fileName}.{DateTime.Now}] - Product Promotion Prices.");

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
        [Route("priceDescripencies")]
        public async Task<IActionResult> PriceDescripencies(string CustomerID = "")
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            DataTable l_Data = new DataTable();
            ProductUploadPrices l_ProductUploadPrices = new ProductUploadPrices();
            int p_UserNo = 1;
            string fileName = "PriceDescripencies.csv";
            string l_data = string.Empty;
            StringBuilder csvContent = new StringBuilder();
            SCS_ItemsType l_SCS_ItemsType = new SCS_ItemsType();
            DataTable l_SCS_ItemTypeAttributeDataTable = new DataTable();
            try
            {
                l_ProductUploadPrices.UseConnection(CommonUtils.ConnectionString);
                l_SCS_ItemsType.UseConnection(CommonUtils.ConnectionString);

                l_ProductUploadPrices.DataDescripensies(CustomerID, p_UserNo, ref l_Data);


                this._logger.LogDebug($"[{fileName}.{DateTime.Now}] - Data Discrepency on Product Promo Prices.");

                if (l_Data.Rows.Count > 0)
                {
                    csvContent.AppendLine("ItemID,CustomerID,Reason");

                    foreach (DataRow l_Row in l_Data.Rows)
                    {
                        csvContent.AppendLine($"{l_Row["ItemID"]},{l_Row["CustomerID"]}," +
                                              $"{l_Row["Data"].ToString()}");
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
    }
}
