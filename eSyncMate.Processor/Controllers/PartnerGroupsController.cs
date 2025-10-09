using Microsoft.AspNetCore.Mvc;
using eSyncMate.DB.Entities;
using System.Reflection;
using eSyncMate.Processor.Models;
using System.Data;
using eSyncMate.DB;
using Hangfire;
using Microsoft.AspNetCore.Authorization;

namespace eSyncMate.Processor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    public class PartnerGroupsController : ControllerBase
    {
        private readonly ILogger<PartnerGroupsController> _logger;

        public PartnerGroupsController(ILogger<PartnerGroupsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("getPartnerGroups")]
        public async Task<GetPartnerGroupsResponseModel> GetPartnerGroups([FromQuery] PartnerGroupSearchModel searchModel)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetPartnerGroupsResponseModel l_Response = new GetPartnerGroupsResponseModel();
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
                DB.Entities.PartnerGroups l_PartnerGroups = new DB.Entities.PartnerGroups();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_PartnerGroups.UseConnection(CommonUtils.ConnectionString);

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
                else if (searchModel.SearchOption == "Description")
                {
                    l_Criteria = $" Description = '{searchModel.SearchValue}'";
                }

                else if (searchModel.SearchOption == "Source Party")
                {
                    l_Criteria = $" SourceParty = '{searchModel.SearchValue}'";
                }

                else if (searchModel.SearchOption == "Destination Party")
                {
                    l_Criteria = $" DestinationParty = '{searchModel.SearchValue}'";
                }
                //else if (searchModel.SearchOption == "Map Type")
                //{
                //    l_Criteria = $" MapType = '{searchModel.SearchValue}'";
                //}

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring PartnerGroup search.");

                l_PartnerGroups.GetViewList(l_Criteria, string.Empty, ref l_Data, "Id DESC");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - PartnerGroups searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating PartnerGroups.");

                l_Response.PartnerGroup = new List<PartnerGroupDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    PartnerGroupDataModel l_PartnerGroupRow = new PartnerGroupDataModel();

                    DBEntity.PopulateObjectFromRow(l_PartnerGroupRow, l_Data, l_Row);

                    l_Response.PartnerGroup.Add(l_PartnerGroupRow);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "PartnerGroups fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - PartnerGroups are ready.");
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
        [Route("createPartnerGroup")]
        public async Task<GetPartnerGroupsResponseModel> CreatePartnerGroup([FromBody] SavePartnerGroupDataModel partnerGroupModel)
        {
            GetPartnerGroupsResponseModel l_Response = new GetPartnerGroupsResponseModel();
            Result l_Result = new Result();

            try
            {
                DB.Entities.PartnerGroups l_PartnerGroups = new DB.Entities.PartnerGroups();
                l_PartnerGroups.UseConnection(CommonUtils.ConnectionString);

                if (l_PartnerGroups.GetObject("Description", partnerGroupModel.Description).IsSuccess)
                {
                    l_Response.Code = (int)ResponseCodes.CustomerAlreadyExists;
                    l_Response.Description = $"This PartnerGroups [ {partnerGroupModel.Description} ] is already Exists!";

                    return l_Response;
                }

                PublicFunctions.CopyTo(partnerGroupModel, l_PartnerGroups);

                l_PartnerGroups.CreatedBy = l_PartnerGroups.CreatedBy;
                l_PartnerGroups.CreatedDate = DateTime.Now;

                l_Result = l_PartnerGroups.SaveNew();

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = $"PartnerGroups [ {partnerGroupModel.Id} ] has been created successfully!";
                }
                else
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = l_Result.Description;
                }

            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Result.Description = ex.Message;
            }
            finally
            {

            }

            return l_Response;
        }

        [HttpPost]
        [Route("updatePartnerGroup")]
        public async Task<GetPartnerGroupsResponseModel> UpdatePartnerGroup([FromBody] UpdatePartnerGroupDataModel partnerGroupsModel)
        {
            GetPartnerGroupsResponseModel l_Response = new GetPartnerGroupsResponseModel();
            Result l_Result = new Result();

            try
            {
                DB.Entities.PartnerGroups l_PartnerGroups = new DB.Entities.PartnerGroups();
                l_PartnerGroups.UseConnection(CommonUtils.ConnectionString);

                PublicFunctions.CopyTo(partnerGroupsModel, l_PartnerGroups);

                l_PartnerGroups.ModifiedBy = l_PartnerGroups.CreatedBy;
                l_PartnerGroups.ModifiedDate = DateTime.Now;

                l_Result = l_PartnerGroups.Modify();

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = $"Map [ {partnerGroupsModel.Id} ] has been updated successfully!";
                }
                else
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = l_Result.Description;
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Result.Description = ex.Message;
            }
            finally
            {

            }

            return l_Response;
        }

        [HttpGet]
        [Route("getCustomers")]
        public async Task<GetCustomersResponseModel> GetCustomers()
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetCustomersResponseModel l_Response = new GetCustomersResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                string l_Criteria = string.Empty;
                Customers l_Customer = new Customers();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_Customer.UseConnection(CommonUtils.ConnectionString);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring Partner Groups search.");

                l_Customer.GetList(l_Criteria, string.Empty, ref l_Data, "Id DESC");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - PartnerGroups searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating Partner Groups.");

                l_Response.Customers = new List<CustomerDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    CustomerDataModel l_CustomerRow = new CustomerDataModel();

                    DBEntity.PopulateObjectFromRow(l_CustomerRow, l_Data, l_Row);

                    l_Response.Customers.Add(l_CustomerRow);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Partner Groups fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Partner Groups are ready.");
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
