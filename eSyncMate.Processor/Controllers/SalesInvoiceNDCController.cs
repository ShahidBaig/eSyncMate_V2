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
using Microsoft.IdentityModel.Tokens;

namespace eSyncMate.Processor.Controllers
{
    [ApiController]
    [Route("api/v1/SalesInvoiceNDC")]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    public class SalesInvoiceNDCController : ControllerBase
    {
        private readonly ILogger<SalesInvoiceNDCController> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IRecurringJobManager _recurringJobManager;

        public SalesInvoiceNDCController(ILogger<SalesInvoiceNDCController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("getSalesInvoiceNDC/{InvoiceNo}/{InvoiceDate}/{Status}/{PoNumber}")]
        public async Task<SalesInvoiceNDCResponseModel> GetSalesInvoiceNDC(Int32 InvoiceNo = 0, string InvoiceDate = "", string Status = "", string PoNumber = "")
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            SalesInvoiceNDCResponseModel l_Response = new SalesInvoiceNDCResponseModel();
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
                SalesInvoiceNDC l_SalesInvoiceNDC = new SalesInvoiceNDC();

                if (InvoiceDate == "1999-01-01")
                {
                    InvoiceDate = string.Empty;
                }
                
                if (InvoiceNo == 0)
                {
                    InvoiceNo = 0;
                }
                if (Status == "EMPTY")
                {
                    Status = string.Empty;
                }

                if (PoNumber == "EMPTY")
                {
                    PoNumber = string.Empty;
                }

                if (Status == "EMPTY")
                {
                    Status = string.Empty;
                }

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_SalesInvoiceNDC.UseConnection(CommonUtils.ConnectionString);

                l_Criteria = $"Status <> 'DELETED'";

                if (InvoiceNo == 0)
                {
                    l_Criteria += " AND ";
                    l_Criteria += $"InvoiceNo = {InvoiceNo}";
                }

                if (!string.IsNullOrEmpty(Status))
                {
                    l_Criteria += " AND ";
                    l_Criteria += $"Status = '{Status}'";
                }

               
                if (!string.IsNullOrEmpty(InvoiceDate))
                {
                    l_Criteria += $" AND CONVERT(DATE,InvoiceDate) >= '{InvoiceDate}'";
                }
                

                if (!string.IsNullOrEmpty(PoNumber))
                {
                    l_Criteria += $" AND PoNumber = '{PoNumber}'";
                }

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring SalesInvoiceNDC search.");

                l_SalesInvoiceNDC.GetViewList(l_Criteria, string.Empty, ref l_Data, "Id DESC");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - SalesInvoiceNDCRow searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating SalesInvoiceNDC.");

                l_Response.SalesInvoiceNDC = l_Data;

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "SalesInvoiceNDCRow fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - SalesInvoiceNDCRow are ready.");
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
