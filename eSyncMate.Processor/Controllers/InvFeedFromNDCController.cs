using eSyncMate.DB.Entities;
using eSyncMate.DB;
using eSyncMate.Processor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Reflection;
using OfficeOpenXml;
using System.Data.SqlClient;
using ExcelDataReader;

namespace eSyncMate.Processor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    public class InvFeedFromNDCController : ControllerBase
    {
        private readonly ILogger<MapsController> _logger;

        public InvFeedFromNDCController(ILogger<MapsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("getInvFeedFromNDC")]
        public async Task<GetInvFeedFromNDCResponseModel> GetInvFeedFromNDC([FromQuery] InvFeedFromNDCSearchModel searchModel)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetInvFeedFromNDCResponseModel l_Response = new GetInvFeedFromNDCResponseModel();
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

            try
            {
                string l_Criteria = string.Empty;
                DB.Entities.InvFeedFromNDC l_InvFeedFromNDC = new DB.Entities.InvFeedFromNDC();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_InvFeedFromNDC.UseConnection(CommonUtils.ConnectionString);

                if (searchModel.SearchOption == "Created Date")
                {
                    l_Criteria += $" CONVERT(DATE,CreatedDate) >= '{startDate}'";
                }

                if (searchModel.SearchOption == "Created Date")
                {
                    l_Criteria += $" AND CONVERT(DATE,CreatedDate) <= '{endDate}'";
                }

                if (searchModel.SearchOption == "ItemId")
                {
                    l_Criteria = $" ItemID LIKE '%{searchModel.SearchValue}%'";
                }
                else if (searchModel.SearchOption == "UPC")
                {
                    l_Criteria = $" UPC = '{searchModel.SearchValue}'";
                }

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring InvFeedFromNDC search.");

                l_InvFeedFromNDC.GetViewList(l_Criteria, string.Empty, ref l_Data, "Id ASC");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - InvFeedFromNDC searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating InvFeedFromNDC.");

                l_Response.Inv = l_Data;
                //foreach (DataRow l_Row in l_Data.Rows)
                //{
                //    InvFeedFromNDCDataModel l_InvRow = new InvFeedFromNDCDataModel();

                //    DBEntity.PopulateObjectFromRow(l_InvRow, l_Data, l_Row);

                //    l_Response.Inv.Add(l_InvRow);
                //}

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "InvFeedFromNDC fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - InvFeedFromNDC are ready.");
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
        [Route("processInvFeedExcelFile")]
        public async Task<IActionResult> ProcessInvFeedExcelFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            try
            {
                // Ensure ExcelDataReader supports encoding
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0; // Reset the stream position to the beginning

                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                        {
                            ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                            {
                                UseHeaderRow = true // Treat the first row as headers
                            }
                        });

                        // Assuming the first sheet
                        DataTable table = result.Tables[0];

                        // Add columns for hardcoded values
                        table.Columns.Add("Qty", typeof(int));
                        table.Columns.Add("ETAQty", typeof(int));
                        table.Columns.Add("ETADate", typeof(DateTime));
                        table.Columns.Add("CreatedDate", typeof(DateTime));
                        table.Columns.Add("CreatedBy", typeof(int));
                        table.Columns.Add("SupplierName", typeof(string));

                        // Populate hardcoded values for new columns
                        foreach (DataRow row in table.Rows)
                        {
                            row["Qty"] = 0;
                            row["ETAQty"] = 0;
                            row["ETADate"] = DateTime.Now;
                            row["CreatedDate"] = DateTime.Now;
                            row["CreatedBy"] = 1; // Example hardcoded user ID
                            row["SupplierName"] = "NDC MedPlus";
                        }

                        using (var connection = new SqlConnection(CommonUtils.ConnectionString))
                        {
                            connection.Open();

                            using (var transaction = connection.BeginTransaction())
                            {
                                try
                                {
                                    // Command to delete all data from InvFeedFromNDC
                                    var deleteCommand = new SqlCommand("DELETE FROM InvFeedFromNDC", connection, transaction);
                                    deleteCommand.ExecuteNonQuery();

                                    // Batch insert using SqlBulkCopy
                                    using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
                                    {
                                        bulkCopy.DestinationTableName = "InvFeedFromNDC";
                                        bulkCopy.BulkCopyTimeout = 300; // Set timeout to handle large data

                                        // Map columns using consistent column names
                                        bulkCopy.ColumnMappings.Add("NDCSKU", "NDCItemID");
                                        bulkCopy.ColumnMappings.Add("SKU", "SKU");
                                        bulkCopy.ColumnMappings.Add("SKU", "ItemID");
                                        bulkCopy.ColumnMappings.Add("ProductName", "ProductName");
                                        bulkCopy.ColumnMappings.Add("ItemDescription", "Description");
                                        bulkCopy.ColumnMappings.Add("UnitPrice", "UnitPrice");
                                        bulkCopy.ColumnMappings.Add("ManufacturerName", "ManufacturerName");
                                        bulkCopy.ColumnMappings.Add("Qty", "Qty");
                                        bulkCopy.ColumnMappings.Add("ETAQty", "ETAQty");
                                        bulkCopy.ColumnMappings.Add("ETADate", "ETADate");
                                        bulkCopy.ColumnMappings.Add("CreatedDate", "CreatedDate");
                                        bulkCopy.ColumnMappings.Add("CreatedBy", "CreatedBy");
                                        bulkCopy.ColumnMappings.Add("SupplierName", "SupplierName");

                                        // Write data to the database
                                        bulkCopy.WriteToServer(table);
                                    }

                                    // Update query to clean up the description column
                                    var updateCommand = new SqlCommand(@"
                                UPDATE InvFeedFromNDC
                                SET description = REPLACE(
                                                    REPLACE(
                                                        REPLACE(
                                                            REPLACE(
                                                                REPLACE(description, '~', ''), 
                                                            '#', ''), 
                                                        '*', ''), 
                                                    ':', ''), 
                                                '>', '');", connection, transaction);
                                    updateCommand.ExecuteNonQuery();

                                    transaction.Commit();
                                }
                                catch (Exception ex)
                                {
                                    transaction.Rollback();
                                    return StatusCode(500, $"Internal server error: {ex.Message}");
                                }
                            }
                        }
                    }
                }

                return Ok(new { message = "File processed and data saved successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
