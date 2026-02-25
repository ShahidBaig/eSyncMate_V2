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
            userData = eSyncMate.Processor.Managers.FlowsManager.GetCustomerNames(claimsIdentity);
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
                }
                if (searchModel.SearchOption == "Created Date")
                {
                    l_Criteria += $" AND CONVERT(DATE,CreatedDate) <= '{endDate}'";
                }
                if (searchModel.SearchOption == "Id")
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
                if (string.IsNullOrEmpty(l_Criteria) && !string.IsNullOrEmpty(userData?.Flows) && userData?.UserType?.ToUpper() != "ADMIN")
                    l_Criteria = $" CustomerID IN ({userData?.Flows})";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring Flow search.");
                l_Flow.GetList(l_Criteria, string.Empty, ref l_Data, "Id DESC");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Flows searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating Flows.");
                l_Response.Flows = new List<FlowResponseModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    FlowResponseModel l_FlowRow = new();
                    DBEntity.PopulateObjectFromRow(l_FlowRow, l_Data, l_Row);

                    // Fetch FlowDetails
                    DataTable l_DetailData = new();
                    FlowDetails l_DetailEntity = new(l_Flow.Connection);
                    if (l_DetailEntity.GetViewList($"FlowId = {l_FlowRow.Id}", string.Empty, ref l_DetailData))
                    {
                        foreach (DataRow l_DetailRow in l_DetailData.Rows)
                        {
                            FlowsDetailsearchModel l_DetailModel = new();
                            DBEntity.PopulateObjectFromRow(l_DetailModel, l_DetailData, l_DetailRow);
                            l_FlowRow.FlowDetails.Add(l_DetailModel);
                        }
                    }
                    l_DetailData.Dispose();

                    l_Response.Flows.Add(l_FlowRow);
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

                if (l_Result.IsSuccess)
                {
                    if (l_Flow.GetObject("Title", flowModel.Title).IsSuccess)
                    {
                        long l_FlowId = l_Flow.Id;

                        if (flowModel.FlowDetails != null && flowModel.FlowDetails.Count > 0)
                        {
                            foreach (var detailModel in flowModel.FlowDetails)
                            {
                                FlowDetails l_Detail = new(l_Connection);
                                PublicFunctions.CopyTo(detailModel, l_Detail);
                                l_Detail.FlowId = l_FlowId;
                                l_Detail.CreatedBy = flowModel.CreatedBy ?? 0;
                                l_Detail.CreatedDate = DateTime.Now;
                                l_Result = l_Detail.SaveNew();
                                if (!l_Result.IsSuccess)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        l_Result = Result.GetFailureResult();
                        l_Result.Description = "Failed to retrieve the created Flow ID.";
                    }
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
                    l_Response.Message = l_Result.Description;
                }
            }
            catch (Exception ex)
            {
                if (l_Trans) l_Connection.RollbackTransaction();
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Result.Description = ex.Message;
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

                PublicFunctions.CopyTo(flowModel, l_Flow);
                l_Flow.ModifiedBy = flowModel.ModifiedBy ?? 0;
                l_Flow.ModifiedDate = DateTime.Now;
                l_Result = l_Flow.Modify();

                if (l_Result.IsSuccess)
                {
                    // Update FlowDetails: Simplest approach is to delete and re-insert
                    // Using a custom hard delete for re-insert scenario if possible, 
                    // otherwise soft deletion might clutter the table.
                    // Given the project style, we'll try to follow the entity's lead or use direct SQL for efficiency in a transaction.
                    l_Connection.Execute($"DELETE FROM FlowDetails WHERE FlowId = {flowModel.Id}");

                    if (flowModel.FlowDetails != null && flowModel.FlowDetails.Count > 0)
                    {
                        foreach (var detailModel in flowModel.FlowDetails)
                        {
                            FlowDetails l_Detail = new(l_Connection);
                            PublicFunctions.CopyTo(detailModel, l_Detail);
                            l_Detail.FlowId = flowModel.Id;
                            l_Detail.ModifiedBy = flowModel.ModifiedBy ?? 0;
                            l_Detail.ModifiedDate = DateTime.Now;
                            l_Result = l_Detail.SaveNew();
                            if (!l_Result.IsSuccess)
                            {
                                break;
                            }
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
                    l_Response.Message = l_Result.Description;
                }
            }
            catch (Exception ex)
            {
                if (l_Trans) l_Connection.RollbackTransaction();
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Result.Description = ex.Message;
            } 
            return Task.FromResult(l_Response);
        }
    }
}
