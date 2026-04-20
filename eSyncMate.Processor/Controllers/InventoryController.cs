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

        [HttpPost]
        [Route("getInventory")]
        public async Task<GetInventoryResponseModel> GetInventory([FromBody] InventoryFilterModel filter)
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

            if (filter == null) filter = new InventoryFilterModel();

            userData = eSyncMate.Processor.Managers.CustomersManager.GetCustomerNames(claimsIdentity);

            try
            {
                Inventory l_Inventory = new Inventory();

                string itemID     = filter.ItemID     ?? string.Empty;
                string status     = filter.Status     ?? string.Empty;
                string fromDate   = filter.StartDate  ?? string.Empty;
                string toDate     = filter.FinishDate ?? string.Empty;
                string customerID = filter.CustomerID ?? string.Empty;

                // Non-super-admin users: restrict to their assigned customers when filter is empty
                if (string.IsNullOrEmpty(customerID))
                {
                    customerID = (!string.IsNullOrEmpty(userData.Customers) && !userData.IsSuperAdmin)
                        ? userData.Customers
                        : string.Empty;
                }

                int pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
                int pageSize   = filter.PageSize   < 1 ? 10 : filter.PageSize;

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Calling Sp_GetInventoryFeedSummary.");

                l_Inventory.UseConnection(CommonUtils.ConnectionString);

                int totalCount = 0;
                l_Inventory.GetInventoryFeedSummary(
                    itemID, customerID, fromDate, toDate, status,
                    pageNumber, pageSize,
                    ref l_Data, out totalCount);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - InventoryRow searched {{{l_Data.Rows.Count}}} of {totalCount} total.");

                l_Response.Inventory = l_Data;
                l_Response.TotalCount = totalCount;
                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "InventoryRow fetched successfully!";
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

        /// <summary>
        /// Returns item-level data for one or more inventory batches.
        /// - 1 BatchID  -> direct item list for that batch (upload or download).
        /// - Multiple   -> deduped merged view (latest row per ItemId across batches).
        /// </summary>
        [HttpPost]
        [Route("getBatchItems")]
        public async Task<GetInventoryResponseModel> GetBatchItems([FromBody] BatchItemsRequestModel request)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetInventoryResponseModel l_Response = new GetInventoryResponseModel();
            DataTable l_Data = new DataTable();

            if (request == null || string.IsNullOrWhiteSpace(request.BatchIDs))
            {
                l_Response.Code = (int)ResponseCodes.Error;
                l_Response.Message = "BatchIDs is required";
                return l_Response;
            }

            // Keep only valid GUIDs to prevent SQL injection through the CSV
            var validIds = request.BatchIDs.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                           .Select(s => s.Trim())
                                           .Where(s => Guid.TryParse(s, out _))
                                           .ToList();

            if (validIds.Count == 0)
            {
                l_Response.Code = (int)ResponseCodes.Error;
                l_Response.Message = "No valid batch IDs provided";
                return l_Response;
            }

            try
            {
                Inventory l_Inventory = new Inventory();
                l_Inventory.UseConnection(CommonUtils.ConnectionString);

                int pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
                int pageSize   = request.PageSize   < 1 ? 10 : request.PageSize;

                int totalCount = 0;
                l_Inventory.GetInventoryBatchItems(
                    string.Join(",", validIds),
                    request.ItemID ?? string.Empty,
                    pageNumber, pageSize,
                    ref l_Data, out totalCount);

                l_Response.BatchWiseInventory = l_Data;
                l_Response.TotalCount = totalCount;
                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Batch items fetched successfully";
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
        /// For a given UPLOAD batch, returns the consolidated view of all DOWNLOAD
        /// batches (SCSFullInventoryFeed / SCSDifferentialInventoryFeed) that occurred
        /// between the previous upload and this upload for the same customer.
        /// If this is the customer's first upload, all prior downloads are included.
        /// </summary>
        [HttpPost]
        [Route("getConsolidatedDownload")]
        public async Task<GetConsolidatedDownloadResponseModel> GetConsolidatedDownload(
            [FromBody] ConsolidatedDownloadRequestModel request)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetConsolidatedDownloadResponseModel l_Response = new GetConsolidatedDownloadResponseModel();
            DataTable l_MainRow = new DataTable();
            DataTable l_TypeBreakdown = new DataTable();

            var claimsIdentity = User.Identity as ClaimsIdentity;
            if (claimsIdentity?.Claims == null)
            {
                l_Response.Code = StatusCodes.Status401Unauthorized;
                l_Response.Message = "Invalid token: Not Authorized";
                return l_Response;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.UploadBatchID))
            {
                l_Response.Code = (int)ResponseCodes.Error;
                l_Response.Message = "UploadBatchID is required";
                return l_Response;
            }

            try
            {
                Inventory l_Inventory = new Inventory();
                l_Inventory.UseConnection(CommonUtils.ConnectionString);

                l_Inventory.GetConsolidatedDownloadBatches(
                    request.UploadBatchID,
                    request.ItemID,
                    ref l_MainRow, ref l_TypeBreakdown);

                l_Response.MainRow = l_MainRow;
                l_Response.TypeBreakdown = l_TypeBreakdown;
                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Consolidated download batches fetched successfully";
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }
            finally
            {
                l_MainRow.Dispose();
                l_TypeBreakdown.Dispose();
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
