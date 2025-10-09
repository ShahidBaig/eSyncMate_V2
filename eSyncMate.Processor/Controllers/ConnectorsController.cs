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
    public class ConnectorsController : ControllerBase
    {
        private readonly ILogger<ConnectorsController> _logger;

        public ConnectorsController(ILogger<ConnectorsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("getConnectors")]
        public async Task<GetConnectorsResponseModel> GetConnectors([FromQuery] ConnectorSearchModel searchModel)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetConnectorsResponseModel l_Response = new GetConnectorsResponseModel();
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
                Connectors l_Connector = new Connectors();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_Connector.UseConnection(CommonUtils.ConnectionString);

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
                else if (searchModel.SearchOption == "Connector Name")
                {
                    l_Criteria = $" Name = '{searchModel.SearchValue}'";
                }
                else if (searchModel.SearchOption == "Connector Type")
                {
                    l_Criteria = $" ConnectorType LIKE '%{searchModel.SearchValue}%'";
                }

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring Connector search.");

                l_Connector.GetViewList(l_Criteria, string.Empty, ref l_Data, "Id DESC");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Connectors searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating Connectors.");

                l_Response.Connectors = new List<ConnectorsDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    ConnectorsDataModel l_ConnectorRow = new ConnectorsDataModel();

                    DBEntity.PopulateObjectFromRow(l_ConnectorRow, l_Data, l_Row);

                    l_Response.Connectors.Add(l_ConnectorRow);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Connectors fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Connectors are ready.");
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
        [Route("createConnector")]
        public async Task<ConnectorsResponseModel> CreateConnectors([FromBody] SaveConnectorsDataModel connectorModel)
        {
            ConnectorsResponseModel l_Response = new ConnectorsResponseModel();
            Result l_Result = new Result();

            try
            {
                Connectors l_Connectors = new Connectors();
                l_Connectors.UseConnection(CommonUtils.ConnectionString);

                if (l_Connectors.GetObject("Name", connectorModel.Name).IsSuccess)
                {
                    l_Response.Code = (int)ResponseCodes.CustomerAlreadyExists;
                    l_Response.Description = $"This Connectors [ {connectorModel.Name} ] is already Exists!";

                    return l_Response;
                }

                PublicFunctions.CopyTo(connectorModel, l_Connectors);

                l_Connectors.CreatedBy = l_Connectors.CreatedBy;
                l_Connectors.CreatedDate = DateTime.Now;

                l_Result = l_Connectors.SaveNew();

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = $"Connectors [ {connectorModel.Name} ] has been created successfully!";
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
        [Route("updateConnector")]
        public async Task<ConnectorsResponseModel> UpdateConnector([FromBody] UpdateConnectorsDataModel connectorsModel)
        {
            ConnectorsResponseModel l_Response = new ConnectorsResponseModel();
            Result l_Result = new Result();

            try
            {
                Connectors l_Connectors = new Connectors();
                l_Connectors.UseConnection(CommonUtils.ConnectionString);

                PublicFunctions.CopyTo(connectorsModel, l_Connectors);

                l_Connectors.ModifiedBy = l_Connectors.CreatedBy;
                l_Connectors.ModifiedDate = DateTime.Now;

                l_Result = l_Connectors.Modify();

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = $"Connector [ {connectorsModel.Name} ] has been updated successfully!";
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
        [Route("getConnectorTypes")]
        public async Task<GetConnectorTypesResponseModel> GetConnectorTypes()
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetConnectorTypesResponseModel l_Response = new GetConnectorTypesResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                string l_Criteria = string.Empty;
                ConnectorTypes l_Connector = new ConnectorTypes();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_Connector.UseConnection(CommonUtils.ConnectionString);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring Connector search.");

                l_Connector.GetList(l_Criteria, string.Empty, ref l_Data, "Id DESC");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Connectors searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating Connectors.");

                l_Response.ConnectorTypes = new List<ConnectorTypesDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    ConnectorTypesDataModel l_ConnectorRow = new ConnectorTypesDataModel();

                    DBEntity.PopulateObjectFromRow(l_ConnectorRow, l_Data, l_Row);

                    l_Response.ConnectorTypes.Add(l_ConnectorRow);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Connectors fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Connectors are ready.");
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
