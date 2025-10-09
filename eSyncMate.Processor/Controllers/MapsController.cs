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
    public class MapsController : ControllerBase
    {
        private readonly ILogger<MapsController> _logger;

        public MapsController(ILogger<MapsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("getMaps")]
        public async Task<GetMapsResponseModel> GetMaps([FromQuery] MapSearchModel searchModel)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetMapsResponseModel l_Response = new GetMapsResponseModel();
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
                DB.Entities.Maps l_Map = new DB.Entities.Maps();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_Map.UseConnection(CommonUtils.ConnectionString);

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
                else if (searchModel.SearchOption == "Map Name")
                {
                    l_Criteria = $" Name = '{searchModel.SearchValue}'";
                }
                else if (searchModel.SearchOption == "Map Type")
                {
                    l_Criteria = $" MapType = '{searchModel.SearchValue}'";
                }

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring Map search.");

                l_Map.GetViewList(l_Criteria, string.Empty, ref l_Data, "Id DESC");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Maps searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating Maps.");

                l_Response.Maps = new List<MapDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    MapDataModel l_MapRow = new MapDataModel();

                    DBEntity.PopulateObjectFromRow(l_MapRow, l_Data, l_Row);

                    l_Response.Maps.Add(l_MapRow);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Maps fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Maps are ready.");
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
        [Route("createMap")]
        public async Task<MapsResponseModel> CreateMaps([FromBody] SaveMapDataModel connectorModel)
        {
            MapsResponseModel l_Response = new MapsResponseModel();
            Result l_Result = new Result();

            try
            {
                DB.Entities.Maps l_Maps = new DB.Entities.Maps();
                l_Maps.UseConnection(CommonUtils.ConnectionString);

                if (l_Maps.GetObject("Name", connectorModel.Name).IsSuccess)
                {
                    l_Response.Code = (int)ResponseCodes.CustomerAlreadyExists;
                    l_Response.Description = $"This Maps [ {connectorModel.Name} ] is already Exists!";

                    return l_Response;
                }

                PublicFunctions.CopyTo(connectorModel, l_Maps);

                l_Maps.CreatedBy = l_Maps.CreatedBy;
                l_Maps.CreatedDate = DateTime.Now;

                l_Result = l_Maps.SaveNew();

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = $"Maps [ {connectorModel.Name} ] has been created successfully!";
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
        [Route("updateMap")]
        public async Task<MapsResponseModel> UpdateMap([FromBody] UpdateMapDataModel mapsModel)
        {
            MapsResponseModel l_Response = new MapsResponseModel();
            Result l_Result = new Result();

            try
            {
                DB.Entities.Maps l_Maps = new DB.Entities.Maps();
                l_Maps.UseConnection(CommonUtils.ConnectionString);

                PublicFunctions.CopyTo(mapsModel, l_Maps);

                l_Maps.ModifiedBy = l_Maps.CreatedBy;
                l_Maps.ModifiedDate = DateTime.Now;

                l_Result = l_Maps.Modify();

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = $"Map [ {mapsModel.Name} ] has been updated successfully!";
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
        [Route("getMapTypes")]
        public async Task<GetMapTypesResponseModel> GetMapTypes()
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetMapTypesResponseModel l_Response = new GetMapTypesResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                string l_Criteria = string.Empty;
                MapTypes l_Map = new MapTypes();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_Map.UseConnection(CommonUtils.ConnectionString);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring Map search.");

                l_Map.GetList(l_Criteria, string.Empty, ref l_Data, "Id DESC");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Maps searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating Maps.");

                l_Response.MapTypes = new List<MapTypesDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    MapTypesDataModel l_MapRow = new MapTypesDataModel();

                    DBEntity.PopulateObjectFromRow(l_MapRow, l_Data, l_Row);

                    l_Response.MapTypes.Add(l_MapRow);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Maps fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Maps are ready.");
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
