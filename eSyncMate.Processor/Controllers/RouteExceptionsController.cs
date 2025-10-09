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

namespace eSyncMate.Processor.Controllers
{
    [ApiController]
    [Route("api/v1/routeExceptions")]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    public class RouteExceptionsController : ControllerBase
    {
        private readonly ILogger<RouteExceptionsController> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IRecurringJobManager _recurringJobManager;

        public RouteExceptionsController(ILogger<RouteExceptionsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("getRouteExceptions/{Name}/{Message}/{FromDate}/{ToDate}/{Status}")]
        public async Task<GetRouteExceptionsResponseModel> GetRouteExceptions(string Name = "", string Message = "", string FromDate = "", string ToDate = "", string Status = "")
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetRouteExceptionsResponseModel l_Response = new GetRouteExceptionsResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                string l_Criteria = string.Empty;
                RouteLog l_RouteLog = new RouteLog();

               if (FromDate == "1999-01-01")
                {
                    FromDate = string.Empty;
                }
                if (ToDate == "1999-01-01")
                {
                    ToDate = string.Empty;
                }
                if (Message == "EMPTY")
                {
                    Message = string.Empty;
                }
                if (Status == "Select Status")
                {
                    Status = string.Empty;
                }
                if (Name == "EMPTY")
                {
                    Name = string.Empty;
                }

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_RouteLog.UseConnection(CommonUtils.ConnectionString);

                l_Criteria = $" [Type] IN (3,5) ";

                if (!string.IsNullOrEmpty(Name))
                {
                    l_Criteria += $" AND  Name = '{Name}'";
                }

                if (!string.IsNullOrEmpty(Message))
                {
                    l_Criteria += " AND ";
                    l_Criteria += $"Message LIKE '%{Message}%'";
                }

                if (!string.IsNullOrEmpty(Status))
                {
                    l_Criteria += " AND ";
                    l_Criteria += $"Status = '{Status}'";
                }

                if (!string.IsNullOrEmpty(FromDate))
                {
                    l_Criteria += $" AND CONVERT(DATE,CreatedDate) >= '{FromDate}'";
                }

                if (!string.IsNullOrEmpty(ToDate))
                {
                    l_Criteria += $" AND CONVERT(DATE,CreatedDate) <= '{ToDate}'";
                }

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring Route Exceptions search.");

                l_RouteLog.GetViewList(l_Criteria, string.Empty, ref l_Data, "Id DESC");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Route Exceptions searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating Route Exceptions.");

                l_Response.RouteExceptionsData = l_Data;
                //l_Response.RouteExceptions = new List<RouteLogDataModel>();
                //foreach (DataRow l_Row in l_Data.Rows)
                //{
                //    RouteLogDataModel l_RouteExceptionsRow = new RouteLogDataModel();

                //    DBEntity.PopulateObjectFromRow(l_RouteExceptionsRow, l_Data, l_Row);

                //    l_Response.RouteExceptions.Add(l_RouteExceptionsRow);
                //}

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Route Exceptions fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Route Exceptions are ready.");
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
