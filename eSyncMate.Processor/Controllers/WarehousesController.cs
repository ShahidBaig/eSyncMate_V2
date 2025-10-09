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
    public class WarehousesController : ControllerBase
    {
        private readonly ILogger<WarehousesController> _logger;

        public WarehousesController(ILogger<WarehousesController> logger)
        {
            _logger = logger;
        }


        [HttpGet]
        [Route("getWarehouses")]
        public async Task<GetWarehousesResponseModel> GetWarehouses()
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetWarehousesResponseModel l_Response = new GetWarehousesResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                string l_Criteria = string.Empty;
                Warehouses l_Warehouses = new Warehouses();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_Warehouses.UseConnection(CommonUtils.ConnectionString);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring Routes search.");

                l_Warehouses.GetList(l_Criteria, string.Empty, ref l_Data, "Id DESC");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Routes searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating Partner Groups.");

                l_Response.Warehouses = l_Data; //new List<WarehouseDataModel>();
                //foreach (DataRow l_Row in l_Data.Rows)
                //{
                //    WarehouseDataModel l_ConnectorRow = new WarehouseDataModel();

                //    DBEntity.PopulateObjectFromRow(l_ConnectorRow, l_Data, l_Row);

                //    l_Response.Warehouses.Add(l_ConnectorRow);
                //}

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Warehouses fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Warehouses are ready.");
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
