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
    [Route("api/v1/PurchaseOrdersTracking")]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    public class PurchaseOrdersTrackingController : ControllerBase
    {
        private readonly ILogger<PurchaseOrdersTrackingController> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IRecurringJobManager _recurringJobManager;

        public PurchaseOrdersTrackingController(ILogger<PurchaseOrdersTrackingController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("getPurchaseOrdersTracking/{PurchaseOrderNo}/{OrderDate}/{SKU}/{PoNumber}")]
        public async Task<GetPurchaseOrdersTrackingResponseModel> GetPurchaseOrdersTracking(Int64 PurchaseOrderNo = 0, string OrderDate = "", string SKU = "", string PoNumber = "")
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetPurchaseOrdersTrackingResponseModel l_Response = new GetPurchaseOrdersTrackingResponseModel();
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
                PurchaseOrdersTracking l_PurchaseOrdersTracking = new PurchaseOrdersTracking();

                if (OrderDate == "1999-01-01")
                {
                    OrderDate = string.Empty;
                }

                if (PurchaseOrderNo == 0)
                {
                    PurchaseOrderNo = 0;
                }

                if (SKU == "EMPTY")
                {
                    SKU = string.Empty;
                }

                if (PoNumber == "EMPTY")
                {
                    PoNumber = string.Empty;
                }

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_PurchaseOrdersTracking.UseConnection(CommonUtils.ConnectionString);

                if (!string.IsNullOrEmpty(OrderDate))
                {
                    l_Criteria += $"  CONVERT(DATE,OrderDate) >= '{OrderDate}'";
                }

                if (PurchaseOrderNo > 0)
                {
                    l_Criteria += $" AND PurchaseOrderNo = {PurchaseOrderNo}";
                }

                if (!string.IsNullOrEmpty(PoNumber))
                {
                    l_Criteria += $" AND PoNumber = '{PoNumber}'";
                }

                l_PurchaseOrdersTracking.GetViewList(l_Criteria, string.Empty, ref l_Data, "OrderDate DESC");

                l_Response.PurchaseOrdersTracking = l_Data;

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Purchase Orders Tracking Row fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Purchase Orders Tracking Row are ready.");
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
