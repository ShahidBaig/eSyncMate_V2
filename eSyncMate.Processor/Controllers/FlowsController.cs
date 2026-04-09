using Microsoft.AspNetCore.Mvc;
using eSyncMate.DB.Entities;
using System.Reflection;
using eSyncMate.Processor.Models;
using System.Data;
using System.Linq;
using eSyncMate.DB;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using eSyncMate.Processor.Managers;

namespace eSyncMate.Processor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    public class FlowsController : ControllerBase
    {
        private readonly ILogger<FlowsController> _logger;
        private readonly IConfiguration _config;
        public FlowsController(ILogger<FlowsController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        [HttpGet("getConfiguredRouteIds")]
        public Task<ConfiguredRouteIdsResponse> GetConfiguredRouteIds([FromQuery] long? excludeFlowId)
        {
            var l_Response = new ConfiguredRouteIdsResponse();
            var l_Connection = new DBConnector(CommonUtils.ConnectionString);
            try
            {
                DataTable l_DT = new();
                string l_Query = "SELECT DISTINCT RouteId FROM FlowDetails WHERE (Status IS NULL OR Status NOT IN ('DELETED')) AND RouteId IS NOT NULL";
                if (excludeFlowId.HasValue && excludeFlowId.Value > 0)
                {
                    l_Query += $" AND FlowId != {excludeFlowId.Value}";
                }
                l_Connection.GetData(l_Query, ref l_DT);

                l_Response.RouteIds = l_DT.AsEnumerable()
                    .Select(r => Convert.ToInt32(r["RouteId"]))
                    .ToList();
                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Configured route IDs retrieved.";
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
            }
            return Task.FromResult(l_Response);
        }

        [HttpGet("GetByRouteId")]
        public Task<GetAutofillDataResponseModel> GetByRouteId(string customerId, int routeId)
        {
            var l_Response = new GetAutofillDataResponseModel();
            var l_Connection = new DBConnector(CommonUtils.ConnectionString);
            DataTable l_DT = new();
            try
            {
                string l_Query = "EXEC Sp_GetAutofillByRouteId ";
                l_Query += PublicFunctions.FieldToParam(customerId, Declarations.FieldTypes.String);
                l_Query += ", " + PublicFunctions.FieldToParam(routeId, Declarations.FieldTypes.Number);

                if (l_Connection.GetDataSP(l_Query, ref l_DT) && l_DT.Rows.Count > 0)
                {
                    var l_Row = l_DT.Rows[0];
                    l_Response.Data = new AutofillDataModel
                    {
                        FrequencyType = l_Row["FrequencyType"]?.ToString() ?? string.Empty,
                        StartDate = l_Row["StartDate"] == DBNull.Value ? string.Empty : Convert.ToDateTime(l_Row["StartDate"]).ToString("yyyy-MM-dd"),
                        EndDate = l_Row["EndDate"] == DBNull.Value ? string.Empty : Convert.ToDateTime(l_Row["EndDate"]).ToString("yyyy-MM-dd"),
                        RepeatCount = l_Row["RepeatCount"] == DBNull.Value ? string.Empty : l_Row["RepeatCount"].ToString() ?? string.Empty,
                        WeekDays = l_Row["WeekDays"]?.ToString() ?? string.Empty,
                        OnDay = l_Row["OnDay"]?.ToString() ?? string.Empty,
                        ExecutionTime = l_Row["ExecutionTime"]?.ToString() ?? string.Empty
                    };
                    l_Response.Code = (int)ResponseCodes.Success;
                    l_Response.Message = "Autofill data retrieved successfully.";
                }
                else
                {
                    l_Response.Code = (int)ResponseCodes.NotFound;
                    l_Response.Message = "No existing flow data found for this customer and route.";
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = "An error occurred while retrieving autofill data.";
                l_Response.Description = ex.Message;
            }

            return Task.FromResult(l_Response);
        }

        [HttpGet]
        [Route("getFlows")]
        public Task<FlowsResponseModel> GetFlows([FromQuery] FlowSearchModel searchModel)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            FlowsResponseModel l_Response = new();
            DataTable l_Data = new();
            string dateRange = string.Empty;
            string[] dateValues = Array.Empty<string>();
            string startDate = string.Empty;
            string endDate = string.Empty;
            UsersClaimData userData = new();
            var claimsIdentity = User.Identity as ClaimsIdentity;
            if (claimsIdentity?.Claims == null)
            {
                l_Response.Code = StatusCodes.Status401Unauthorized;
                l_Response.Message = "Invalid token: Not Authorized";
                return Task.FromResult(l_Response);
            }

            userData = FlowsManager.GetCustomerNames(claimsIdentity);

            if (searchModel.SearchOption == "Created Date" && !string.IsNullOrEmpty(searchModel.SearchValue))
            {
                dateRange = searchModel.SearchValue;
                dateValues = dateRange.Split('/');
                if (dateValues.Length == 2)
                {
                    startDate = dateValues[0].Trim() + " 00:00:00.000";
                    endDate = dateValues[1].Trim() + " 23:59:59.999";
                }
            }
            try
            {
                string l_Criteria = string.Empty;
                Flows l_Flow = new();
                l_Response.Code = (int)ResponseCodes.Error;
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");
                l_Flow.UseConnection(CommonUtils.ConnectionString);
                if (searchModel.SearchOption == "Created Date")
                {
                    l_Criteria += $" CONVERT(DATE,CreatedDate) >= '{startDate}'";
                    l_Criteria += $" AND CONVERT(DATE,CreatedDate) <= '{endDate}'";
                }
                else if (searchModel.SearchOption == "Id")
                {
                    l_Criteria = $" Id = {searchModel.SearchValue}";
                }
                else if (searchModel.SearchOption == "Title")
                {
                    l_Criteria = $" Title = '{searchModel.SearchValue}'";
                }
                else if (searchModel.SearchOption == "Customer ID")
                {
                    l_Criteria = $" CustomerID = '{searchModel.SearchValue}'";
                }

                if (string.IsNullOrEmpty(l_Criteria) &&
                    !string.IsNullOrEmpty(userData?.Flows) &&
                    !userData.IsSuperAdmin)
                {
                    l_Criteria = $" CustomerID IN ({userData?.Flows})";
                }

                if (string.IsNullOrEmpty(l_Criteria))
                {
                    l_Criteria = " ISNULL(Status, '') != 'DELETED'";
                }
                else
                {
                    l_Criteria += " AND ISNULL(Status, '') != 'DELETED'";
                }

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Starting Flow search.");
                l_Flow.GetList(l_Criteria, string.Empty, ref l_Data, "SequenceNo ASC, Id DESC");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Flows searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating Flows.");
                l_Response.Flows = new List<FlowResponseModel>();
                List<long> l_FlowIds = new();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    FlowResponseModel l_FlowRow = new();
                    DBEntity.PopulateObjectFromRow(l_FlowRow, l_Data, l_Row);
                    l_Response.Flows.Add(l_FlowRow);
                    l_FlowIds.Add(l_FlowRow.Id);
                }

                if (l_FlowIds.Count > 0)
                {
                    DataTable l_DetailData = new();
                    FlowDetails l_DetailEntity = new(l_Flow.Connection);
                    string l_Ids = string.Join(",", l_FlowIds);

                    if (l_DetailEntity.GetViewList($"FlowId IN ({l_Ids}) AND (Status IS NULL OR Status != 'DELETED')", string.Empty, ref l_DetailData))
                    {
                        var l_DetailsByFlowId = new Dictionary<long, List<FlowsDetailsearchModel>>();
                        foreach (DataRow l_DetailRow in l_DetailData.Rows)
                        {
                            FlowsDetailsearchModel l_DetailModel = new();
                            DBEntity.PopulateObjectFromRow(l_DetailModel, l_DetailData, l_DetailRow);
                            if (!l_DetailsByFlowId.ContainsKey(l_DetailModel.FlowId))
                            {
                                l_DetailsByFlowId[l_DetailModel.FlowId] = new List<FlowsDetailsearchModel>();
                            }
                            l_DetailsByFlowId[l_DetailModel.FlowId].Add(l_DetailModel);
                        }

                        foreach (var l_FlowRow in l_Response.Flows)
                        {
                            if (l_DetailsByFlowId.ContainsKey(l_FlowRow.Id))
                            {
                                l_FlowRow.FlowDetails = l_DetailsByFlowId[l_FlowRow.Id];
                            }
                        }
                    }

                    l_DetailData.Dispose();
                }
                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Flows fetched successfully!";
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Flows are ready.");
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
            return Task.FromResult(l_Response);
        }

        [HttpPost]
        [Route("createFlow")]
        public Task<FlowsResponseModel> CreateFlow([FromBody] SaveFlowDataModel flowModel)
        {
            FlowsResponseModel l_Response = new();
            Result l_Result = new();
            DBConnector l_Connection = new DBConnector(CommonUtils.ConnectionString);
            bool l_Trans = false;
            try
            {
                Flows l_Flow = new();
                l_Flow.UseConnection(string.Empty, l_Connection);

                // Check duplicate: same Title + CustomerID among non-deleted flows
                DataTable l_DupCheck = new();
                l_Flow.GetList($"Title = '{flowModel.Title.Replace("'", "''")}' AND CustomerID = '{flowModel.CustomerID.Replace("'", "''")}' AND ISNULL(Status,'') != 'DELETED'", "Id", ref l_DupCheck);
                if (l_DupCheck.Rows.Count > 0)
                {
                    l_Response.Code = (int)ResponseCodes.FlowAlreadyExists;
                    l_Response.Description = $"Flow '{flowModel.Title}' already exists for this partner!";
                    return Task.FromResult(l_Response);
                }

                l_Trans = l_Connection.BeginTransaction();

                PublicFunctions.CopyTo(flowModel, l_Flow);
                l_Flow.CreatedBy = flowModel.CreatedBy ?? 0;
                l_Flow.CreatedDate = DateTime.Now;
                l_Result = l_Flow.SaveNew();

                if (l_Flow.Id > 0)
                {
                    long l_FlowId = l_Flow.Id;

                    if (flowModel.FlowDetails != null && flowModel.FlowDetails.Count > 0)
                    {
                        DataTable l_DT = new();
                        l_DT.Columns.Add("FlowId", typeof(long));
                        l_DT.Columns.Add("RouteId", typeof(int));
                        l_DT.Columns.Add("Status", typeof(string));
                        l_DT.Columns.Add("In_Out", typeof(string));
                        l_DT.Columns.Add("FrequencyType", typeof(string));
                        l_DT.Columns.Add("StartDate", typeof(DateTime));
                        l_DT.Columns.Add("EndDate", typeof(DateTime));
                        l_DT.Columns.Add("RepeatCount", typeof(int));
                        l_DT.Columns.Add("WeekDays", typeof(string));
                        l_DT.Columns.Add("OnDay", typeof(string));
                        l_DT.Columns.Add("ExecutionTime", typeof(string));
                        l_DT.Columns.Add("CreatedDate", typeof(DateTime));
                        l_DT.Columns.Add("CreatedBy", typeof(int));
                        l_DT.Columns.Add("ModifiedDate", typeof(DateTime));
                        l_DT.Columns.Add("ModifiedBy", typeof(int));

                        foreach (var detailModel in flowModel.FlowDetails)
                        {
                            DataRow l_DR = l_DT.NewRow();
                            l_DR["FlowId"] = l_FlowId;
                            l_DR["RouteId"] = (object)detailModel.RouteId ?? DBNull.Value;
                            l_DR["Status"] = detailModel.Status;
                            l_DR["In_Out"] = detailModel.In_Out;
                            l_DR["FrequencyType"] = detailModel.FrequencyType;
                            l_DR["StartDate"] = (object)detailModel.StartDate ?? DBNull.Value;
                            l_DR["EndDate"] = detailModel.EndDate != null && detailModel.EndDate != default(DateTime) && detailModel.EndDate.Year > 1900
                                ? (object)detailModel.EndDate
                                : DateTime.Now.AddYears(5);
                            l_DR["RepeatCount"] = detailModel.RepeatCount;
                            l_DR["WeekDays"] = detailModel.WeekDays;
                            l_DR["OnDay"] = detailModel.OnDay;
                            l_DR["ExecutionTime"] = detailModel.ExecutionTime;
                            l_DR["CreatedDate"] = DateTime.Now;
                            l_DR["CreatedBy"] = flowModel.CreatedBy ?? 0;
                            l_DR["ModifiedDate"] = DBNull.Value;
                            l_DR["ModifiedBy"] = 0;
                            l_DT.Rows.Add(l_DR);
                        }
                        if (!l_Connection.CopyDataTableTrans("FlowDetails", l_DT))
                        {
                            l_Result = Result.GetFailureResult();
                            l_Result.Description = "Failed to bulk insert flow details.";
                        }
                    }
                }
                else
                {
                    l_Result = Result.GetFailureResult();
                    l_Result.Description = "Failed to retrieve the created Flow ID.";
                }

                if (l_Result.IsSuccess)
                {
                    l_Connection.CommitTransaction();
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    int detailCount = flowModel.FlowDetails?.Count ?? 0;
                    l_Response.Description = $"Flow {flowModel.Title} created! ({detailCount} detail(s) saved)";

                    // Schedule Hangfire RecurringJobs and update Routes status after commit
                    if (flowModel.FlowDetails != null)
                    {
                        RouteEngine l_Engine = new RouteEngine(this._config);
                        foreach (var detailModel in flowModel.FlowDetails)
                        {
                            try
                            {
                                if (!detailModel.RouteId.HasValue) continue;
                                var l_UpdateConn = new DBConnector(CommonUtils.ConnectionString);
                                string l_DetailStatus = (detailModel.Status ?? "").Trim();

                                if (l_DetailStatus.Equals("Active", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Load full route data for SetupRouteJob
                                    Routes l_RouteForJob = new();
                                    l_RouteForJob.UseConnection(CommonUtils.ConnectionString);
                                    l_RouteForJob.Id = detailModel.RouteId.Value;
                                    l_RouteForJob.GetObject();

                                    // Apply scheduling from flow detail
                                    l_RouteForJob.FrequencyType = detailModel.FrequencyType ?? l_RouteForJob.FrequencyType;
                                    l_RouteForJob.RepeatCount = detailModel.RepeatCount > 0 ? detailModel.RepeatCount : l_RouteForJob.RepeatCount;
                                    l_RouteForJob.WeekDays = detailModel.WeekDays ?? l_RouteForJob.WeekDays;
                                    l_RouteForJob.OnDay = detailModel.OnDay ?? l_RouteForJob.OnDay;
                                    l_RouteForJob.ExecutionTime = detailModel.ExecutionTime ?? l_RouteForJob.ExecutionTime;

                                    string l_JobId = l_Engine.SetupRouteJob(l_RouteForJob);
                                    l_UpdateConn.Execute($"UPDATE Routes SET Status = 'Active', JobID = '{l_JobId}', ModifiedDate = GETDATE() WHERE Id = {detailModel.RouteId.Value}");
                                }
                                else
                                {
                                    l_UpdateConn.Execute($"UPDATE Routes SET Status = 'In-Active', JobID = NULL, ModifiedDate = GETDATE() WHERE Id = {detailModel.RouteId.Value}");
                                }
                            }
                            catch (Exception jobEx)
                            {
                                this._logger.LogWarning($"CreateFlow: Failed to process Hangfire job for RouteId={detailModel.RouteId}: {jobEx.Message}");
                            }
                        }
                    }
                }
                else
                {
                    if (l_Trans) l_Connection.RollbackTransaction();
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = l_Result.Description ?? "Operation failed.";
                }
            }
            catch (Exception ex)
            {
                if (l_Trans) l_Connection.RollbackTransaction();
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
                l_Response.Description = ex.StackTrace;
            }
            return Task.FromResult(l_Response);
        }

        [HttpPost]
        [Route("deleteFlow/{id}")]
        public Task<FlowsResponseModel> DeleteFlow(long id)
        {
            FlowsResponseModel l_Response = new();
            DBConnector l_Connection = new DBConnector(CommonUtils.ConnectionString);
            try
            {
                // Get active details to clean up Hangfire jobs before soft-delete
                DataTable l_DetailsDT = new();
                eSyncMate.DB.Entities.FlowDetails l_DetailEntity = new();
                l_DetailEntity.UseConnection(string.Empty, l_Connection);
                l_DetailEntity.GetList($"FlowId = {id} AND (Status IS NULL OR Status != 'DELETED')", "*", ref l_DetailsDT);

                RouteEngine l_CleanupEngine = new RouteEngine(this._config);
                foreach (DataRow l_DetailRow in l_DetailsDT.Rows)
                {
                    try
                    {
                        if (l_DetailRow["RouteId"] != DBNull.Value)
                        {
                            int l_RouteId = Convert.ToInt32(l_DetailRow["RouteId"]);

                            // Get full Route data for proper Hangfire cleanup
                            Routes l_RouteEntity = new();
                            l_RouteEntity.UseConnection(string.Empty, l_Connection);
                            l_RouteEntity.Id = l_RouteId;
                            l_RouteEntity.GetObject();

                            // Remove RecurringJobs
                            try { l_CleanupEngine.RemoveRouteJob(l_RouteEntity); }
                            catch (Exception rrEx)
                            {
                                this._logger.LogWarning($"DeleteFlow: Failed to remove RecurringJob for RouteId={l_RouteId}: {rrEx.Message}");
                            }

                            // Set Route to InActive and clear JobID
                            l_Connection.Execute($"UPDATE Routes SET Status = 'In-Active', JobID = NULL, ModifiedDate = GETDATE() WHERE Id = {l_RouteId}");
                        }
                    }
                    catch (Exception detailEx)
                    {
                        this._logger.LogWarning($"DeleteFlow: Error cleaning up RouteId in detail: {detailEx.Message}");
                    }
                }

                // Soft-delete the parent Flow record
                bool l_FlowDeleted = l_Connection.Execute($"UPDATE Flows SET Status = 'DELETED', ModifiedDate = GETDATE() WHERE Id = {id}");

                if (l_FlowDeleted)
                {
                    // Soft-delete all child FlowDetails records
                    l_Connection.Execute($"UPDATE FlowDetails SET Status = 'DELETED', ModifiedDate = GETDATE() WHERE FlowId = {id}");

                    l_Response.Code = 200;
                    l_Response.Message = "Flow and its details have been deleted successfully.";
                    l_Response.Description = $"Flow {id} deleted successfully!";

                    this._logger.LogInformation($"DeleteFlow: Flow {id} soft-deleted. {l_DetailsDT.Rows.Count} detail(s) cleaned up, Hangfire jobs deleted, Routes set to InActive.");
                }
                else
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = "Delete failed. The Flow ID may not exist or was already deleted.";
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
                l_Response.Description = ex.StackTrace;
            }
            return Task.FromResult(l_Response);
        }

        [HttpPost("testRun/{routeId}")]
        public Task<FlowsResponseModel> TestRunRoute(int routeId)
        {
            FlowsResponseModel l_Response = new();
            try
            {
                int l_UserId = FlowsManager.GetUserId(User.Identity as ClaimsIdentity);

                Routes l_Route = new();
                l_Route.UseConnection(CommonUtils.ConnectionString);
                l_Route.Id = routeId;
                var l_GetResult = l_Route.GetObject();

                if (!l_GetResult.IsSuccess)
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = $"Route with ID {routeId} not found.";
                    return Task.FromResult(l_Response);
                }

                string l_RouteName = l_Route.Name ?? $"Route #{routeId}";

                // Create notification with RUNNING status
                long l_NotifId = Notifications.CreateNotification(
                    CommonUtils.ConnectionString, l_UserId, routeId, l_RouteName,
                    "TEST_RUN", "RUNNING", $"'{l_RouteName}' is running...");

                // Enqueue with notification ID so we can update it on completion
                RouteEngine l_Engine = new RouteEngine(this._config);
                BackgroundJob.Enqueue(() => l_Engine.ExecuteWithNotification(routeId, l_NotifId));

                _logger.LogInformation($"TestRun: Route [{routeId}] '{l_RouteName}' triggered by User [{l_UserId}]. NotificationId={l_NotifId}");

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = $"'{l_RouteName}' has been triggered successfully.";
                l_Response.Description = l_RouteName;
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = $"Failed to trigger route: {ex.Message}";
                _logger.LogError($"TestRun Error: RouteId={routeId}, Error={ex.Message}");
            }
            return Task.FromResult(l_Response);
        }

        // ===== NOTIFICATION ENDPOINTS =====

        [HttpGet("notifications")]
        public Task<object> GetNotifications()
        {
            try
            {
                int l_UserId = FlowsManager.GetUserId(User.Identity as ClaimsIdentity);
                var l_DT = Notifications.GetUserNotifications(CommonUtils.ConnectionString, l_UserId);
                int l_Unread = Notifications.GetUnreadCount(CommonUtils.ConnectionString, l_UserId);

                var l_List = new List<object>();
                foreach (DataRow row in l_DT.Rows)
                {
                    l_List.Add(new
                    {
                        id = Convert.ToInt64(row["Id"]),
                        routeId = row["RouteId"] != DBNull.Value ? Convert.ToInt32(row["RouteId"]) : 0,
                        routeName = row["RouteName"]?.ToString(),
                        type = row["Type"]?.ToString(),
                        status = row["Status"]?.ToString(),
                        message = row["Message"]?.ToString(),
                        isRead = Convert.ToBoolean(row["IsRead"]),
                        createdDate = row["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(row["CreatedDate"]).ToString("yyyy-MM-ddTHH:mm:ssZ") : null,
                        completedDate = row["CompletedDate"] != DBNull.Value ? Convert.ToDateTime(row["CompletedDate"]).ToString("yyyy-MM-ddTHH:mm:ssZ") : null
                    });
                }

                return Task.FromResult<object>(new { code = 200, notifications = l_List, unreadCount = l_Unread, serverTimeUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") });
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new { code = 500, message = ex.Message });
            }
        }

        [HttpPost("notifications/markRead/{id}")]
        public Task<object> MarkNotificationRead(long id)
        {
            try
            {
                int l_UserId = FlowsManager.GetUserId(User.Identity as ClaimsIdentity);
                Notifications.MarkAsRead(CommonUtils.ConnectionString, id, l_UserId);
                return Task.FromResult<object>(new { code = 200, message = "Marked as read" });
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new { code = 500, message = ex.Message });
            }
        }

        [HttpPost("notifications/markAllRead")]
        public Task<object> MarkAllNotificationsRead()
        {
            try
            {
                int l_UserId = FlowsManager.GetUserId(User.Identity as ClaimsIdentity);
                Notifications.MarkAllAsRead(CommonUtils.ConnectionString, l_UserId);
                return Task.FromResult<object>(new { code = 200, message = "All marked as read" });
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new { code = 500, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("updateFlow")]
        public Task<FlowsResponseModel> UpdateFlow([FromBody] EditFlowDataModel flowModel)
        {
            FlowsResponseModel l_Response = new();
            Result l_Result = new();
            DBConnector l_Connection = new DBConnector(CommonUtils.ConnectionString);
            bool l_Trans = false;
            try
            {
                Flows l_Flow = new();
                l_Flow.UseConnection(string.Empty, l_Connection);
                l_Trans = l_Connection.BeginTransaction();

                int l_UserIdVal = FlowsManager.GetUserId(User.Identity as ClaimsIdentity);

                if (flowModel.Id > 0)
                {
                    l_Flow.Id = flowModel.Id;
                    var l_GetResult = l_Flow.GetObject();
                    if (!l_GetResult.IsSuccess)
                    {
                        l_Result = Result.GetFailureResult();
                        l_Result.Description = $"Flow with ID {flowModel.Id} not found.";
                    }
                    else
                    {
                        PublicFunctions.CopyTo(flowModel, l_Flow);
                        l_Flow.ModifiedBy = flowModel.ModifiedBy ?? 0;
                        l_Flow.ModifiedDate = DateTime.Now;
                        l_Result = l_Flow.Modify();
                    }
                }
                else if (!l_Flow.ResolveIdByTitle(flowModel.Title))
                {
                    l_Result = Result.GetFailureResult();
                    l_Result.Description = $"Flow with Title '{flowModel.Title}' not found.";
                }
                else
                {
                    flowModel.Id = l_Flow.Id;
                    PublicFunctions.CopyTo(flowModel, l_Flow);
                    l_Flow.ModifiedBy = flowModel.ModifiedBy ?? 0;
                    l_Flow.ModifiedDate = DateTime.Now;
                    l_Result = l_Flow.Modify();
                }

                if (l_Result.IsSuccess && flowModel.FlowDetails != null)
                {
                    eSyncMate.DB.Entities.FlowDetails l_DetailEntity = new();
                    l_DetailEntity.UseConnection(string.Empty, l_Connection);
                    DataTable l_ExistingDT = new();
                    l_DetailEntity.GetList($"FlowId = {flowModel.Id} AND (Status IS NULL OR Status != 'DELETED')", "*", ref l_ExistingDT);

                    var l_ExistingRows = l_ExistingDT.AsEnumerable()
                        .Where(r => r["RouteId"] != DBNull.Value)
                        .ToDictionary(r => Convert.ToInt32(r["RouteId"]), r => r);

                    this._logger.LogInformation($"UpdateFlow [{flowModel.Id}]: Existing DB RouteIds=[{string.Join(",", l_ExistingRows.Keys)}], Incoming RouteIds=[{string.Join(",", flowModel.FlowDetails.Where(d => d.RouteId.HasValue).Select(d => d.RouteId.Value))}]");

                    foreach (var detailModel in flowModel.FlowDetails)
                    {
                        l_Result = FlowsManager.ProcessUpdateDetail(flowModel, detailModel, l_ExistingRows, l_Connection, l_UserIdVal, this._config);
                        if (!l_Result.IsSuccess) break;
                    }

                    // Soft-delete removed details: existing rows not present in the incoming payload
                    if (l_Result.IsSuccess)
                    {
                        var l_IncomingRouteIds = flowModel.FlowDetails
                            .Where(d => d.RouteId.HasValue)
                            .Select(d => d.RouteId.Value)
                            .ToHashSet();

                        var l_RemovedRouteIds = l_ExistingRows.Keys.Where(k => !l_IncomingRouteIds.Contains(k)).ToList();

                        if (l_RemovedRouteIds.Count > 0)
                        {
                            var l_RemovedDetailIds = l_RemovedRouteIds
                                .Select(k => Convert.ToInt64(l_ExistingRows[k]["Id"]))
                                .ToList();

                            string l_DetailIdsStr = string.Join(",", l_RemovedDetailIds);

                            // Remove RecurringJobs and update Routes for removed details
                            RouteEngine l_CleanupEngine = new RouteEngine(this._config);
                            foreach (int l_RmRouteId in l_RemovedRouteIds)
                            {
                                try
                                {
                                    Routes l_RmRoute = new();
                                    l_RmRoute.UseConnection(string.Empty, l_Connection);
                                    l_RmRoute.Id = l_RmRouteId;
                                    l_RmRoute.GetObject();

                                    // Remove RecurringJobs
                                    try { l_CleanupEngine.RemoveRouteJob(l_RmRoute); }
                                    catch (Exception rrEx)
                                    {
                                        this._logger.LogWarning($"UpdateFlow: Failed to remove RecurringJob for RouteId={l_RmRouteId}: {rrEx.Message}");
                                    }

                                    // Update Route: set InActive
                                    l_Connection.Execute($"UPDATE Routes SET Status = 'In-Active', JobID = NULL, ModifiedDate = GETDATE(), ModifiedBy = {l_UserIdVal} WHERE Id = {l_RmRouteId}");

                                    // Log removal to FlowHistory
                                    long l_RmDetailId = Convert.ToInt64(l_ExistingRows[l_RmRouteId]["Id"]);
                                    l_Connection.Execute($"INSERT INTO FlowHistory (FlowId, FlowDetailId, RouteId, UserId, FlowStatus, JobId, CreatedDate, CreatedBy) VALUES ({flowModel.Id}, {l_RmDetailId}, {l_RmRouteId}, {l_UserIdVal}, 'DELETED', '', GETDATE(), {l_UserIdVal})");

                                    this._logger.LogInformation($"UpdateFlow: Removed RouteId={l_RmRouteId} from Flow {flowModel.Id}. RecurringJobs removed, Route set to InActive.");
                                }
                                catch (Exception rmEx)
                                {
                                    this._logger.LogWarning($"UpdateFlow: Error cleaning up removed RouteId={l_RmRouteId}: {rmEx.Message}");
                                }
                            }

                            // Soft-delete removed flow details
                            l_Connection.Execute($"UPDATE FlowDetails SET Status = 'DELETED', ModifiedDate = GETDATE(), ModifiedBy = {l_UserIdVal} WHERE Id IN ({l_DetailIdsStr})");

                            this._logger.LogInformation($"Soft-deleted FlowDetails [{l_DetailIdsStr}] for Flow {flowModel.Id}. Removed routes: [{string.Join(",", l_RemovedRouteIds)}]");
                        }
                    }
                }

                if (l_Result.IsSuccess)
                {
                    l_Connection.CommitTransaction();
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = $"Flow {flowModel.Title} has been updated successfully!";
                }
                else
                {
                    if (l_Trans) l_Connection.RollbackTransaction();
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = l_Result.Description ?? "Update failed. Check if the Flow ID exists.";
                }
            }
            catch (Exception ex)
            {
                if (l_Trans) l_Connection.RollbackTransaction();
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
                l_Response.Description = ex.StackTrace;
            }
            return Task.FromResult(l_Response);
        }
    }
}