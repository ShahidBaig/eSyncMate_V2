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
    public class AlertsConfigurationController : ControllerBase
    {
        private readonly ILogger<AlertsConfigurationController> _logger;

        public AlertsConfigurationController(ILogger<AlertsConfigurationController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("getAlertsConfiguration")]
        public async Task<GetAlertsConfigurationResponseModel> GetAlertsConfiguration([FromQuery] AlertsConfigurationearchModel searchModel)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetAlertsConfigurationResponseModel l_Response = new GetAlertsConfigurationResponseModel();
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
                AlertsConfiguration l_Connector = new AlertsConfiguration();

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

                if (searchModel.SearchOption == "AlertID")
                {
                    l_Criteria = $" AlertID = {searchModel.SearchValue}";
                }
                else if (searchModel.SearchOption == "Alert Name")
                {
                    l_Criteria = $" AlertName LIKE '%{searchModel.SearchValue}%'";
                }
                //else if (searchModel.SearchOption == "Connector Type")
                //{
                //    l_Criteria = $" ConnectorType LIKE '%{searchModel.SearchValue}%'";
                //}

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring Connector search.");

                l_Connector.GetViewList(l_Criteria, string.Empty, ref l_Data, "AlertID DESC");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - AlertsConfiguration searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating AlertsConfiguration.");

                l_Response.AlertsConfiguration = new List<AlertsConfigurationDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    AlertsConfigurationDataModel l_ConnectorRow = new AlertsConfigurationDataModel();

                    DBEntity.PopulateObjectFromRow(l_ConnectorRow, l_Data, l_Row);

                    l_Response.AlertsConfiguration.Add(l_ConnectorRow);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "AlertsConfiguration fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - AlertsConfiguration are ready.");
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
        [Route("createAlertsConfiguration")]
        public async Task<AlertsConfigurationResponseModel> CreateAlertsConfiguration([FromBody] SaveAlertsConfigurationDataModel connectorModel)
        {
            AlertsConfigurationResponseModel l_Response = new AlertsConfigurationResponseModel();
            Result l_Result = new Result();

            try
            {
                AlertsConfiguration l_AlertsConfiguration = new AlertsConfiguration();
                l_AlertsConfiguration.UseConnection(CommonUtils.ConnectionString);

                if (l_AlertsConfiguration.GetObject("AlertName", connectorModel.AlertName).IsSuccess)
                {
                    l_Response.Code = (int)ResponseCodes.CustomerAlreadyExists;
                    l_Response.Description = $"This AlertsConfiguration [ {connectorModel.AlertName} ] is already Exists!";

                    return l_Response;
                }

                PublicFunctions.CopyTo(connectorModel, l_AlertsConfiguration);

                l_AlertsConfiguration.CreatedBy = l_AlertsConfiguration.CreatedBy;
                l_AlertsConfiguration.CreatedDate = DateTime.Now;

                l_Result = l_AlertsConfiguration.SaveNew();

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = $"AlertsConfiguration [ {connectorModel.AlertName} ] has been created successfully!";
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
        [Route("updateAlertsConfiguration")]
        public async Task<AlertsConfigurationResponseModel> UpdateConnector([FromBody] UpdateAlertsConfigurationDataModel AlertsConfigurationModel)
        {
            AlertsConfigurationResponseModel l_Response = new AlertsConfigurationResponseModel();
            Result l_Result = new Result();

            try
            {
                AlertsConfiguration l_AlertsConfiguration = new AlertsConfiguration();
                l_AlertsConfiguration.UseConnection(CommonUtils.ConnectionString);

                PublicFunctions.CopyTo(AlertsConfigurationModel, l_AlertsConfiguration);

                l_AlertsConfiguration.ModifiedBy = l_AlertsConfiguration.CreatedBy;
                l_AlertsConfiguration.ModifiedDate = DateTime.Now;

                l_Result = l_AlertsConfiguration.Modify();

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = $"Alerts Configuration [ {AlertsConfigurationModel.AlertName} ] has been updated successfully!";
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
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring Alerts Configuration search.");

                l_Connector.GetList(l_Criteria, string.Empty, ref l_Data, "Id DESC");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - AlertsConfiguration searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating AlertsConfiguration.");

                l_Response.ConnectorTypes = new List<ConnectorTypesDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    ConnectorTypesDataModel l_ConnectorRow = new ConnectorTypesDataModel();

                    DBEntity.PopulateObjectFromRow(l_ConnectorRow, l_Data, l_Row);

                    l_Response.ConnectorTypes.Add(l_ConnectorRow);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "AlertsConfiguration fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - AlertsConfiguration are ready.");
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
