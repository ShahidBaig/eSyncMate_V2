using Microsoft.AspNetCore.Mvc;
using eSyncMate.DB.Entities;
using System.Reflection;
using eSyncMate.Processor.Models;
using System.Data;
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
        public FlowsController(ILogger<FlowsController> logger)
        {
            _logger = logger;
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
                    userData?.UserType?.ToUpper() != "ADMIN")
                {
                    l_Criteria = $" CustomerID IN ({userData?.Flows})";
                }

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Starting Flow search.");
                l_Flow.GetList(l_Criteria, string.Empty, ref l_Data, "Id DESC");
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

                    if (l_DetailEntity.GetViewList($"FlowId IN ({l_Ids})", string.Empty, ref l_DetailData))
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

                if (l_Flow.GetObject("Title", flowModel.Title).IsSuccess)
                {
                    l_Response.Code = (int)ResponseCodes.FlowAlreadyExists;
                    l_Response.Description = $"This flow {flowModel.Title} already exists!";
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
                            l_DR["EndDate"] = (object)detailModel.EndDate ?? DBNull.Value;
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
                    l_Response.Description = $"Flow {flowModel.Title} has been created successfully!";
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

        [HttpPut]
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

                if (flowModel.Id <= 0)
                {
                    if (!string.IsNullOrEmpty(flowModel.Title))
                    {
                        DataTable l_FlowDT = new();
                        l_Flow.GetList($"Title = '{flowModel.Title.Replace("'", "''")}'", "Id", ref l_FlowDT);
                        if (l_FlowDT.Rows.Count > 0)
                        {
                            flowModel.Id = Convert.ToInt64(l_FlowDT.Rows[0]["Id"]);
                            l_Flow.Id = flowModel.Id;
                        }
                    }
                }
                if (flowModel.Id <= 0)
                {
                    l_Result = Result.GetFailureResult();
                    l_Result.Description = "A valid Flow ID or Title is required for updating.";
                }
                else
                {
                    PublicFunctions.CopyTo(flowModel, l_Flow);
                    l_Flow.ModifiedBy = flowModel.ModifiedBy ?? 0;
                    l_Flow.ModifiedDate = DateTime.Now;
                    l_Result = l_Flow.Modify();
                }

                if (l_Result.IsSuccess)
                {
                    if (flowModel.FlowDetails != null)
                    {
                        eSyncMate.DB.Entities.FlowDetails l_DetailEntity = new();
                        l_DetailEntity.UseConnection(string.Empty, l_Connection);
                        DataTable l_ExistingDT = new();
                        l_DetailEntity.GetList($"FlowId = {flowModel.Id}", "*", ref l_ExistingDT);

                        var l_ExistingRows = l_ExistingDT.AsEnumerable()
                            .Where(r => r["RouteId"] != DBNull.Value)
                            .ToDictionary(r => Convert.ToInt32(r["RouteId"]), r => r);

                        var l_InputRouteIds = new HashSet<int>();

                        foreach (var detailModel in flowModel.FlowDetails)
                        {
                            if (detailModel.RouteId == null) continue;
                            int l_RouteId = detailModel.RouteId.Value;
                            l_InputRouteIds.Add(l_RouteId);
                            eSyncMate.DB.Entities.FlowDetails l_Row = new();
                            l_Row.UseConnection(string.Empty, l_Connection);

                            if (l_ExistingRows.ContainsKey(l_RouteId))
                            {
                                DataRow l_ExistingRow = l_ExistingRows[l_RouteId];
                                l_Row.Id = Convert.ToInt64(l_ExistingRow["Id"]);
                                PublicFunctions.CopyTo(detailModel, l_Row);
                                l_Row.FlowId = flowModel.Id;
                                l_Row.ModifiedDate = DateTime.Now;
                                l_Row.ModifiedBy = flowModel.ModifiedBy ?? 0;
                                l_Row.CreatedDate = Convert.ToDateTime(l_ExistingRow["CreatedDate"]);
                                l_Row.CreatedBy = Convert.ToInt32(l_ExistingRow["CreatedBy"]);

                                l_Result = l_Row.Modify();
                            }
                            else
                            {
                                PublicFunctions.CopyTo(detailModel, l_Row);
                                l_Row.FlowId = flowModel.Id;
                                l_Row.CreatedDate = DateTime.Now;
                                l_Row.CreatedBy = flowModel.ModifiedBy ?? 0;
                                l_Row.ModifiedDate = DateTime.Now;
                                l_Row.ModifiedBy = flowModel.ModifiedBy ?? 0;
                                l_Result = l_Row.SaveNew();
                            }

                            if (!l_Result.IsSuccess) break;
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