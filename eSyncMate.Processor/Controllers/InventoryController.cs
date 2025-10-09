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
        public async Task<GetInventoryResponseModel> GetInventory(string ItemID = "", string FromDate = "", string ToDate = "", string Status = "", string CustomerID = "", string RouteType = "")
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
                    CustomerID = !(string.IsNullOrEmpty(userData.Customers)) && userData.UserType?.ToUpper() != "ADMIN" ? userData.Customers : "";

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_Inventory.UseConnection(CommonUtils.ConnectionString);

                l_Criteria = $"Status <> 'DELETED'";

                if (!string.IsNullOrEmpty(ItemID))
                {
                    l_Criteria += " AND ";
                    l_Criteria += $"ItemID LIKE '%{ItemID}%'";
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

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring Inventory search.");

                l_Inventory.GetViewList(l_Criteria, string.Empty, ref l_Data, "StartDate DESC");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - InventoryRow searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating Inventory.");

                l_Response.Inventory = l_Data;

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
        public async Task<GetInventoryResponseModel> GetBatchData(string BatchID)
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

                l_Inventory.GetBatchWiseData($"BatchID = '{BatchID}'", ref l_Data);

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

        [HttpGet]
        [Route("getRouteTypes")]
        public async Task<GetInventoryResponseModel> getRouteTypes()
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
                string l_Criteria = !(string.IsNullOrEmpty(userData.Customers)) && userData.UserType?.ToUpper() != "ADMIN" ? $"CustomerID IN ({userData.Customers})" : string.Empty;
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
