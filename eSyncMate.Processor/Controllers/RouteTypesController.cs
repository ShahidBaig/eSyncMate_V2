using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Reflection;

namespace eSyncMate.Processor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    public class RouteTypesController : ControllerBase
    {
        private readonly ILogger<RouteTypesController> _logger;

        public RouteTypesController(ILogger<RouteTypesController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("getRouteTypes")]
        public async Task<GetRouteTypeResponseModel> GetRouteTypes([FromQuery] RouteTypeSearchModel searchModel)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetRouteTypeResponseModel l_Response = new GetRouteTypeResponseModel();
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
                DB.Entities.RouteTypes l_RouteTypes = new DB.Entities.RouteTypes();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_RouteTypes.UseConnection(CommonUtils.ConnectionString);

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
                else if (searchModel.SearchOption == "Route Types Name")
                {
                    l_Criteria = $" Name = '{searchModel.SearchValue}'";
                }
                else if (searchModel.SearchOption == "Description")
                {
                    l_Criteria = $" Description = '{searchModel.SearchValue}'";
                }

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring Route Type search.");

                l_RouteTypes.GetViewList(l_Criteria, string.Empty, ref l_Data, "Id DESC");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Route Type searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating Route Type.");

                l_Response.RouteType = new List<RouteTypeDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    RouteTypeDataModel l_RouteTypeRow = new RouteTypeDataModel();

                    DBEntity.PopulateObjectFromRow(l_RouteTypeRow, l_Data, l_Row);

                    l_Response.RouteType.Add(l_RouteTypeRow);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Route Types fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Route Types are ready.");
            }
            catch (Exception ex)
            {
                //l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }
            finally
            {
                l_Data.Dispose();
            }

            return l_Response;


        }


        [HttpPost]
        [Route("createRouteTypes")]
        public async Task<RouteTypesResponseModel> CreateRouteType([FromBody] SaveRouteTypeDataModel connectorModel)
        {
            RouteTypesResponseModel l_Response = new RouteTypesResponseModel();
            Result l_Result = new Result();

            try
            {
                DB.Entities.RouteTypes l_RouteTypes = new DB.Entities.RouteTypes();
                l_RouteTypes.UseConnection(CommonUtils.ConnectionString);

                if (l_RouteTypes.GetObject("Name", connectorModel.Name).IsSuccess)
                {
                    l_Response.Code = (int)ResponseCodes.CustomerAlreadyExists;
                    l_Response.Description = $"This Route Type [ {connectorModel.Name} ] is already Exists!";

                    return l_Response;
                }

                PublicFunctions.CopyTo(connectorModel, l_RouteTypes);

                l_RouteTypes.CreatedBy = l_RouteTypes.CreatedBy;
                l_RouteTypes.CreatedDate = DateTime.Now;

                l_Result = l_RouteTypes.SaveNew();

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = $"RouteType [ {connectorModel.Name} ] has been created successfully!";
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
        [Route("updateRouteTypes")]
        public async Task<RouteTypesResponseModel> UpdateRouteTypes([FromBody] UpdateRouteTypeDataModel routetypesModel)
        {
            RouteTypesResponseModel l_Response = new RouteTypesResponseModel();
            Result l_Result = new Result();

            try
            {
                DB.Entities.RouteTypes l_RouteTypes = new DB.Entities.RouteTypes();
                l_RouteTypes.UseConnection(CommonUtils.ConnectionString);

                PublicFunctions.CopyTo(routetypesModel, l_RouteTypes);

                l_RouteTypes.ModifiedBy = l_RouteTypes.CreatedBy;
                l_RouteTypes.ModifiedDate = DateTime.Now;

                l_Result = l_RouteTypes.Modify();

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = $"Route Types [ {routetypesModel.Name} ] has been updated successfully!";
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
        [Route("getRoutesTypes")]
        public async Task<GetRouteTypeResponseModel> GetMapTypes()
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetRouteTypeResponseModel l_Response = new GetRouteTypeResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                string l_Criteria = string.Empty;
                RouteTypes l_Map = new RouteTypes();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_Map.UseConnection(CommonUtils.ConnectionString);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring Route Types search.");

                l_Map.GetList(l_Criteria, string.Empty, ref l_Data, "Id DESC");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Route Types searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating Route Types.");

                l_Response.RouteType = new List<RouteTypeDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    RouteTypeDataModel l_MapRow = new RouteTypeDataModel();

                    DBEntity.PopulateObjectFromRow(l_MapRow, l_Data, l_Row);

                    l_Response.RouteType.Add(l_MapRow);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Route Type fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Route Type are ready.");
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
