using eSyncMate.DB.Entities;
using eSyncMate.DB;
using eSyncMate.Processor.Models;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Reflection;
using Intercom.Core;
using Intercom.Data;
using Newtonsoft.Json;
using System.Security.Claims;
using Renci.SshNet.Common;
using System.Linq;

namespace eSyncMate.Processor.Controllers
{
    [ApiController]
    [Route("api/v1/inventory")]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    public class InventoryController : ControllerBase
    {
        private readonly ILogger<InventoryController> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IRecurringJobManager _recurringJobManager;

        public InventoryController(ILogger<InventoryController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("getInventory/{ItemID}/{FromDate}/{ToDate}/{Status}/{CustomerID}/{RouteType}")]
        public async Task<GetInventoryResponseModel> GetInventory(string ItemID = "", string FromDate = "", string ToDate = "", string Status = "", string CustomerID = "", string RouteType = "", [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetInventoryResponseModel l_Response = new GetInventoryResponseModel();
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
                string l_Criteria = string.Empty;
                Inventory l_Inventory = new Inventory();

                if (FromDate == "1999-01-01")
                {
                    FromDate = string.Empty;
                }
                if (ToDate == "1999-01-01")
                {
                    ToDate = string.Empty;
                }
                if (ItemID == "EMPTY")
                {
                    ItemID = string.Empty;
                }
                if (Status == "EMPTY")
                {
                    Status = string.Empty;
                }

                if (RouteType == "EMPTY")
                {
                    RouteType = string.Empty;
                }

                if (CustomerID == "EMPTY")
                    CustomerID = !(string.IsNullOrEmpty(userData.Customers)) && !userData.IsSuperAdmin ? userData.Customers : "";

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_Inventory.UseConnection(CommonUtils.ConnectionString);

                l_Criteria = $"Status <> 'DELETED'";

                if (!string.IsNullOrEmpty(ItemID))
                {
                    // Return distinct batches that contain the item — avoid VW_Inventory_ItemWise
                    // duplication when a wildcard matches multiple items within the same batch.
                    // Lowercase [ItemId] keeps the literal "ItemID" out of the criteria so the
                    // DB layer's auto-switch to VW_Inventory_ItemWise is not triggered.
                    l_Criteria += $" AND EXISTS (SELECT 1 FROM SCSInventoryFeedData d WITH (NOLOCK) WHERE d.BatchID = [VW_Inventory].BatchID AND d.[ItemId] LIKE '%{ItemID}%')";
                }

                if (!string.IsNullOrEmpty(Status))
                {
                    l_Criteria += " AND ";
                    l_Criteria += $"Status = '{Status}'";
                }

                if (!string.IsNullOrEmpty(CustomerID))
                {
                    CustomerID = !CustomerID.StartsWith("'") ? $"'{CustomerID}'" : CustomerID;
                    l_Criteria += " AND ";
                    l_Criteria += $"CustomerID IN ({CustomerID})";
                }

                if (!string.IsNullOrEmpty(FromDate))
                {
                    l_Criteria += $" AND CONVERT(DATE,ISNULL(StartDate,FinishDate)) >= '{FromDate}'";
                }

                if (!string.IsNullOrEmpty(ToDate))
                {
                    l_Criteria += $" AND CONVERT(DATE,ISNULL(StartDate,FinishDate)) <= '{ToDate}'";
                }

                if (!string.IsNullOrEmpty(RouteType))
                {
                    l_Criteria += $" AND RouteType = '{RouteType}'";
                }
                else
                {
                    // Default: show only Upload route types in main grid
                    l_Criteria += " AND OrignalRouteType IN ('WalmartUploadInventory','TargetPlusInventoryFeedWHSWise','AmazonInventoryUpload','LowesWHSWInventoryUpload','MacysInventoryUpload','MichealInventoryUpload','KnotInventoryUpload','KnotWHSWInventoryUpload','AmazonWHSWInventoryUpload')";
                }

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring Inventory search.");

                int totalCount = 0;
                l_Inventory.GetViewListPaged(l_Criteria, string.Empty, ref l_Data, "StartDate DESC", pageNumber, pageSize, out totalCount);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - InventoryRow searched {{{l_Data.Rows.Count}}} of {totalCount} total.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating Inventory.");

                l_Response.Inventory = l_Data;
                l_Response.TotalCount = totalCount;

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "InventoryRow fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - InventoryRow are ready.");
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
        [Route("getInventoryFiles/{CustomerId}/{ItemId}/{BatchID}")]
        public async Task<GetInventoryFilesResponseModel> GetInventoryFiles(string CustomerId, String ItemId, string BatchID)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetInventoryFilesResponseModel l_Response = new GetInventoryFilesResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                string l_Criteria = string.Empty;
                SCSInventoryFeedData l_InventoryData = new SCSInventoryFeedData();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_InventoryData.UseConnection(CommonUtils.ConnectionString);

                l_Criteria = $"CustomerId = '{CustomerId}'";

                if (!string.IsNullOrEmpty(ItemId))
                {
                    l_Criteria += $" AND ItemId = '{ItemId}'";
                }

                if (!string.IsNullOrEmpty(BatchID))
                {
                    l_Criteria += $" AND BatchID = '{BatchID}'";
                }

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring inventory files search.");

                l_InventoryData.GetViewList(l_Criteria, string.Empty, ref l_Data, "Id");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Inventory files searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating order files.");

                l_Response.Files = new List<InventoryFileModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    InventoryFileModel l_InventoryFile = new InventoryFileModel();

                    DBEntity.PopulateObjectFromRow(l_InventoryFile, l_Data, l_Row);

                    l_Response.Files.Add(l_InventoryFile);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Inventory Files fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Inventory Files are ready.");
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
        [Route("getBatchData/{BatchID}")]
        public async Task<GetInventoryResponseModel> GetBatchData(string BatchID, [FromQuery] string itemID = "", [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetInventoryResponseModel l_Response = new GetInventoryResponseModel();
            DataTable l_Data = new DataTable();
            Inventory l_Inventory = new Inventory();

            try
            {
                string l_Criteria = string.Empty;
                SCSInventoryFeedData l_InventoryData = new SCSInventoryFeedData();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_Inventory.UseConnection(CommonUtils.ConnectionString);

                string l_BWCriteria = $"BatchID = '{BatchID}'";
                if (!string.IsNullOrEmpty(itemID))
                {
                    l_BWCriteria += $" AND ItemId LIKE '%{itemID}%'";
                }

                int totalCount = 0;
                l_Inventory.GetBatchWiseDataPaged(l_BWCriteria, ref l_Data, pageNumber, pageSize, out totalCount);

                l_Response.BatchWiseInventory = l_Data;
                l_Response.TotalCount = totalCount;
                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Batch Wise Inventory fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Batch Wise Inventory are ready.");
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
        [Route("getBatchWiseItemID")]
        public async Task<GetInventoryResponseModel> getBatchWiseItemID(string ItemID, string BatchID)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetInventoryResponseModel l_Response = new GetInventoryResponseModel();
            DataTable l_Data = new DataTable();
            Inventory l_Inventory = new Inventory();

            try
            {
                string l_Criteria = string.Empty;
                SCSInventoryFeedData l_InventoryData = new SCSInventoryFeedData();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_Inventory.UseConnection(CommonUtils.ConnectionString);

                l_Inventory.GetBatchWiseData($"BatchID = '{BatchID}' AND ItemID LIKE '%{ItemID}%'", ref l_Data);

                l_Response.BatchWiseInventory = l_Data;
                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Batch Wise Inventory fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Batch Wise Inventory are ready.");
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

        /// <summary>
        /// Returns the StartDate of the most recent upload batch (any upload route type)
        /// that started BEFORE the given beforeDate for the given customer. Used by the
        /// expandable row in the Inventory Feed Summary when the previous upload row is
        /// not available on the current page (e.g. the last row of a page).
        /// </summary>
        [HttpGet]
        [Route("getPreviousUploadDate")]
        public GetInventoryResponseModel GetPreviousUploadDate(
            [FromQuery] string customerID = "",
            [FromQuery] string beforeDate = "")
        {
            GetInventoryResponseModel l_Response = new GetInventoryResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                var claimsIdentity = User.Identity as ClaimsIdentity;
                if (claimsIdentity?.Claims == null)
                {
                    l_Response.Code = StatusCodes.Status401Unauthorized;
                    l_Response.Message = "Not Authorized";
                    return l_Response;
                }

                Inventory l_Inventory = new Inventory();
                l_Inventory.UseConnection(CommonUtils.ConnectionString);

                string l_Criteria = "Status <> 'DELETED'"
                    + " AND OrignalRouteType IN ('WalmartUploadInventory','TargetPlusInventoryFeedWHSWise','AmazonInventoryUpload','LowesWHSWInventoryUpload','MacysInventoryUpload','MichealInventoryUpload','KnotInventoryUpload','KnotWHSWInventoryUpload','AmazonWHSWInventoryUpload')";

                if (!string.IsNullOrEmpty(customerID))
                    l_Criteria += $" AND CustomerID = '{customerID}'";

                if (!string.IsNullOrEmpty(beforeDate))
                    l_Criteria += $" AND ISNULL(StartDate, FinishDate) < '{beforeDate}'";

                int totalCount = 0;
                l_Inventory.GetViewListPaged(l_Criteria, string.Empty, ref l_Data, "StartDate DESC", 1, 1, out totalCount);

                l_Response.Inventory = l_Data;
                l_Response.TotalCount = totalCount;
                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Previous upload date fetched";
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
            }
            finally
            {
                l_Data.Dispose();
            }

            return l_Response;
        }

        /// <summary>
        /// Get download batches between two upload dates for a customer.
        /// Used by expandable upload rows in the Inventory Feed Summary.
        /// </summary>
        [HttpGet]
        [Route("getDownloadBatches")]
        public async Task<GetInventoryResponseModel> GetDownloadBatches(
            [FromQuery] string customerID = "",
            [FromQuery] string fromDate = "",
            [FromQuery] string toDate = "")
        {
            GetInventoryResponseModel l_Response = new GetInventoryResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                var claimsIdentity = User.Identity as ClaimsIdentity;
                if (claimsIdentity?.Claims == null)
                {
                    l_Response.Code = StatusCodes.Status401Unauthorized;
                    l_Response.Message = "Not Authorized";
                    return l_Response;
                }

                Inventory l_Inventory = new Inventory();
                l_Inventory.UseConnection(CommonUtils.ConnectionString);

                // Download/Feed route types (not upload)
                string downloadFilter = "(RouteType LIKE '%Full%' OR RouteType LIKE '%Differential%' OR RouteType LIKE '%feed%' OR RouteType LIKE '%Portal%')";
                string uploadFilter = "(RouteType NOT LIKE '%Upload%' AND RouteType NOT LIKE '%WHSW%')";

                string l_Criteria = $"Status <> 'DELETED' AND {downloadFilter} AND {uploadFilter}";

                if (!string.IsNullOrEmpty(customerID))
                    l_Criteria += $" AND CustomerID = '{customerID}'";

                if (!string.IsNullOrEmpty(fromDate))
                    l_Criteria += $" AND ISNULL(StartDate, FinishDate) >= '{fromDate}'";

                if (!string.IsNullOrEmpty(toDate))
                    l_Criteria += $" AND ISNULL(StartDate, FinishDate) <= '{toDate}'";

                l_Inventory.GetViewList(l_Criteria, string.Empty, ref l_Data, "StartDate ASC");

                l_Response.Inventory = l_Data;
                l_Response.TotalCount = l_Data.Rows.Count;
                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Download batches fetched successfully";
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
            }
            finally
            {
                l_Data.Dispose();
            }

            return l_Response;
        }

        /// <summary>
        /// Returns merged item-level data across multiple download batches.
        /// One row per ItemID — latest value (last write wins).
        /// </summary>
        [HttpGet]
        [Route("getMergedDownloadItems")]
        public async Task<GetInventoryResponseModel> GetMergedDownloadItems(
            [FromQuery] string batchIDs = "",
            [FromQuery] string itemID = "",
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            GetInventoryResponseModel l_Response = new GetInventoryResponseModel();
            DataTable l_Data = new DataTable();
            Inventory l_Inventory = new Inventory();

            try
            {
                if (string.IsNullOrWhiteSpace(batchIDs))
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = "batchIDs is required";
                    return l_Response;
                }

                // Sanitize: only valid GUIDs allowed (prevents SQL injection)
                var validIds = batchIDs.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                       .Select(s => s.Trim())
                                       .Where(s => Guid.TryParse(s, out _))
                                       .Select(s => $"'{s}'")
                                       .ToList();

                if (validIds.Count == 0)
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = "No valid batch IDs provided";
                    return l_Response;
                }

                string batchIdsCsv = string.Join(",", validIds);

                l_Inventory.UseConnection(CommonUtils.ConnectionString);

                int totalCount = 0;
                l_Inventory.GetMergedDownloadItemsPaged(batchIdsCsv, itemID, ref l_Data, pageNumber, pageSize, out totalCount);

                l_Response.BatchWiseInventory = l_Data;
                l_Response.TotalCount = totalCount;
                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Merged download items fetched successfully";
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
                _logger.LogCritical($"[GetMergedDownloadItems] - {ex}");
            }
            finally
            {
                l_Data.Dispose();
            }

            return l_Response;
        }

        [HttpGet]
        [Route("getRouteTypes")]
        public async Task<GetInventoryResponseModel> getRouteTypes([FromQuery] string customerID = "")
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetInventoryResponseModel l_Response = new GetInventoryResponseModel();
            DataTable l_Data = new DataTable();
            Inventory l_Inventory = new Inventory();
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
                string l_Criteria = !(string.IsNullOrEmpty(userData.Customers)) && !userData.IsSuperAdmin ? $"CustomerID IN ({userData.Customers})" : string.Empty;

                // Filter by specific customer if provided
                if (!string.IsNullOrEmpty(customerID))
                {
                    l_Criteria = string.IsNullOrEmpty(l_Criteria)
                        ? $"CustomerID = '{customerID}'"
                        : $"{l_Criteria} AND CustomerID = '{customerID}'";
                }

                // Only include inventory-related route types, exclude price routes
                string l_InventoryFilter = "RouteType LIKE '%inventory%' OR RouteType LIKE '%feed%' OR RouteType LIKE '%Full%' OR RouteType LIKE '%Differential%' OR RouteType LIKE '%Portal%'";
                l_Criteria = string.IsNullOrEmpty(l_Criteria)
                    ? $"({l_InventoryFilter})"
                    : $"{l_Criteria} AND ({l_InventoryFilter})";

                SCSInventoryFeedData l_InventoryData = new SCSInventoryFeedData();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_Inventory.UseConnection(CommonUtils.ConnectionString);

                l_Inventory.GetViewList(l_Criteria, "RouteType", ref l_Data);

                l_Response.RouteType = l_Data.AsEnumerable().Distinct(DataRowComparer.Default).CopyToDataTable();
                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Routes type fetched successfully for Customers!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Routes type are ready.");
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
