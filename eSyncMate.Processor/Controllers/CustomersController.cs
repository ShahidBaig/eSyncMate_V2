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
    public class CustomersController : ControllerBase
    {
        private readonly ILogger<CustomersController> _logger;
        private readonly IConfiguration _config;
        public CustomersController(ILogger<CustomersController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("getCustomers")]
        public async Task<GetCustomersResponseModel> GetCustomers([FromQuery] CustomerSearchModel searchModel)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetCustomersResponseModel l_Response = new GetCustomersResponseModel();
            DataTable l_Data = new DataTable();
            string dateRange = string.Empty;
            string[] dateValues = new string[0];
            string startDate = string.Empty;
            string endDate = string.Empty;

            UsersClaimData userData = new UsersClaimData();

            var claimsIdentity = User.Identity as ClaimsIdentity;

            if (claimsIdentity?.Claims == null)
            {
                l_Response.Code = StatusCodes.Status401Unauthorized;
                l_Response.Message = "Invalid token: Not Authorized";

                return l_Response;
            }

            userData = eSyncMate.Processor.Managers.CustomersManager.GetCustomerNames(claimsIdentity);

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
                Customers l_Customer = new Customers();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_Customer.UseConnection(CommonUtils.ConnectionString);

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
                else if (searchModel.SearchOption == "Customer Name")
                {
                    l_Criteria = $" Name = '{searchModel.SearchValue}'";
                }
                else if (searchModel.SearchOption == "ERP Customer ID")
                {
                    l_Criteria = $" ERPCustomerID = '{searchModel.SearchValue}'";
                }
                else if (searchModel.SearchOption == "ISA Customer ID")
                {
                    l_Criteria = $" ISACustomerID = '{searchModel.SearchValue}'";
                }
                else if (searchModel.SearchOption == "ISA 810 Receiver ID")
                {
                    l_Criteria = $" ISA810ReceiverId = '{searchModel.SearchValue}'";
                }
                else if (searchModel.SearchOption == "Market Place")
                {
                    l_Criteria = $" Marketplace = '{searchModel.SearchValue}'";
                }

                if (string.IsNullOrEmpty(l_Criteria) && !string.IsNullOrEmpty(userData?.Customers) && userData?.UserType?.ToUpper() != "ADMIN")
                    l_Criteria = $" ERPCustomerID IN ({userData?.Customers})";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring Customer search.");

                l_Customer.GetList(l_Criteria, string.Empty, ref l_Data, "Id DESC");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Customers searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating Customers.");

                l_Response.Customers = new List<CustomerDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    CustomerDataModel l_CustomerRow = new CustomerDataModel();

                    DBEntity.PopulateObjectFromRow(l_CustomerRow, l_Data, l_Row);

                    l_Response.Customers.Add(l_CustomerRow);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Customers fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Customers are ready.");
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
        [Route("createCustomer")]
        public async Task<CustomersResponseModel> CreateCustomer([FromBody] SaveCustomerDataModel customerModel)
        {
            CustomersResponseModel l_Response = new CustomersResponseModel();
            Result l_Result = new Result();

            try
            {
                Customers l_Customer = new Customers();
                l_Customer.UseConnection(CommonUtils.ConnectionString);

                if (l_Customer.GetObject("Name", customerModel.Name).IsSuccess)
                {
                    l_Response.Code = (int)ResponseCodes.CustomerAlreadyExists;
                    l_Response.Description = $"This customer [ {customerModel.Name} ] is already Exists!";

                    return l_Response;
                }

                PublicFunctions.CopyTo(customerModel, l_Customer);

                l_Customer.CreatedBy = l_Customer.CreatedBy;
                l_Customer.CreatedDate = DateTime.Now;

                l_Result = l_Customer.SaveNew();

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = $"Customer [ {customerModel.Name} ] has been created successfully!";
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
        [Route("updateCustomer")]
        public async Task<CustomersResponseModel> UpdateCustomer([FromBody] EditCustomerDataModel customerModel)
        {
            CustomersResponseModel l_Response = new CustomersResponseModel();
            Result l_Result = new Result();

            try
            {
                Customers l_Customer = new Customers();
                l_Customer.UseConnection(CommonUtils.ConnectionString);

                PublicFunctions.CopyTo(customerModel, l_Customer);

                l_Customer.ModifiedBy = l_Customer.CreatedBy;
                l_Customer.ModifiedDate = DateTime.Now;

                l_Result = l_Customer.Modify();

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = $"Customer [ {customerModel.Name} ] has been updated successfully!";
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
        [Route("getCustomersList")]
        public async Task<CustomersResponseModel> GetCustomersData()
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            CustomersResponseModel l_Response = new CustomersResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                Customers l_Customers = new Customers();
                l_Customers.UseConnection(CommonUtils.ConnectionString);

                l_Response.Code = (int)ResponseCodes.Error;

                l_Customers.GetCustomersData(ref l_Data);

                l_Response.CustomersList = new List<CustomersListModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    CustomersListModel l_CustomersRow = new CustomersListModel();

                    DBEntity.PopulateObjectFromRow(l_CustomersRow, l_Data, l_Row);

                    l_Response.CustomersList.Add(l_CustomersRow);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "CustomersData fetched successfully!";
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

        [HttpGet]
        [Route("getCustomerAlerts")]
        public async Task<GetCustomerAlertsResponseModel> GetCustomerAlerts([FromQuery] int customerId)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetCustomerAlertsResponseModel l_Response = new GetCustomerAlertsResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                CustomerAlerts l_Entity = new CustomerAlerts();   // <-- your new Alerts entity/table
                l_Entity.UseConnection(CommonUtils.ConnectionString);

                l_Response.Code = (int)ResponseCodes.Error;

                // TODO: implement this method in CustomersAlerts entity or call your SP here
                // Example: l_Entity.GetCustomerAlerts(customerId, ref l_Data);

                l_Entity.GetList($"CustomerID = '{customerId}'", string.Empty, ref l_Data, "Id DESC");

                l_Response.Alerts = new List<CustomerAlertConfigModel>();

                foreach (DataRow row in l_Data.Rows)
                {
                    CustomerAlertConfigModel alertRow = new CustomerAlertConfigModel();
                    DBEntity.PopulateObjectFromRow(alertRow, l_Data, row);
                    l_Response.Alerts.Add(alertRow);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Customer alerts fetched successfully!";
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
                l_Response.Description = ex.ToString();
                this._logger.LogError(ex, $"[{l_Me.ReflectedType?.Name}.{l_Me.Name}] - Error while getting customer alerts.");
            }
            finally
            {
                l_Data.Dispose();
            }

            return l_Response;
        }


        [HttpGet]
        [Route("getAlertConfigurations")]
        public async Task<GetAlertsConfigurationResponseModel> GetAlertConfigurations()
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            GetAlertsConfigurationResponseModel l_Response = new GetAlertsConfigurationResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                string l_Criteria = string.Empty;
                AlertsConfiguration l_AlertsConfiguration = new AlertsConfiguration();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_AlertsConfiguration.UseConnection(CommonUtils.ConnectionString);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring Partner Group search.");

                l_AlertsConfiguration.GetList(l_Criteria, string.Empty, ref l_Data, "AlertId DESC");

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Partner Group searched {{{l_Data.Rows.Count}}}.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating Partner Group.");

                l_Response.AlertsConfiguration = new List<AlertsConfigurationDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    AlertsConfigurationDataModel l_AlertsConfigurationRow = new AlertsConfigurationDataModel();

                    DBEntity.PopulateObjectFromRow(l_AlertsConfigurationRow, l_Data, l_Row);

                    l_Response.AlertsConfiguration.Add(l_AlertsConfigurationRow);
                }

                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "Alerts Configuration fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Alert Configuration are ready.");
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
        [Route("saveCustomerAlert")]
        public async Task<CustomersResponseModel> SaveCustomerAlert([FromBody] SaveCustomerAlertRequestModel model)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            CustomersResponseModel l_Response = new CustomersResponseModel();
            Result l_Result = new Result();
            string l_JobID = string.Empty;

            try
            {
                DB.Entities.CustomerAlerts l_CustomerAlerts = new DB.Entities.CustomerAlerts();
                l_CustomerAlerts.UseConnection(CommonUtils.ConnectionString);

                PublicFunctions.CopyTo(model, l_CustomerAlerts);

                l_JobID = this.SetupAlertJob(l_CustomerAlerts);
                l_CustomerAlerts.JobID = l_JobID;

                if (model.Id == 0)
                {
                    // Insert
                    l_CustomerAlerts.CreatedBy = l_CustomerAlerts.CreatedBy;
                    l_CustomerAlerts.CreatedDate = DateTime.Now;
                    l_Result = l_CustomerAlerts.SaveNew();
                }
                else
                {
                    // Update
                    l_CustomerAlerts.ModifiedBy = l_CustomerAlerts.CreatedBy;
                    l_CustomerAlerts.ModifiedDate = DateTime.Now;
                    l_Result = l_CustomerAlerts.Modify();
                }

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = "Customer alert saved successfully.";
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
                l_Response.Message = ex.Message;
                this._logger.LogError(ex, $"[{l_Me.ReflectedType?.Name}.{l_Me.Name}] - Error while saving customer alert.");
            }
            finally
            {
                // dispose result if needed
            }

            return l_Response;
        }


        [HttpPost]
        [Route("deleteCustomerAlert")]
        public async Task<CustomersResponseModel> DeleteCustomerAlert([FromBody] DeleteCustomerAlertRequestModel model)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            CustomersResponseModel l_Response = new CustomersResponseModel();
            Result l_Result = new Result();
            string l_JobID = string.Empty;

            try
            {
                CustomerAlerts l_Entity = new CustomerAlerts();
                l_Entity.UseConnection(CommonUtils.ConnectionString);

                PublicFunctions.CopyTo(model, l_Entity);

                l_Result = l_Entity.Delete();

                if (l_Result.IsSuccess)
                {

                    this.RemoveAlertJob(l_Entity);

                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = "Customer alert Delete successfully.";
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
                l_Response.Message = ex.Message;
                this._logger.LogError(ex, $"[{l_Me.ReflectedType?.Name}.{l_Me.Name}] - Error while saving customer alert.");
            }
            finally
            {
                // dispose result if needed
            }

            return l_Response;
        }

        private string SetupAlertJob(CustomerAlerts alert)
        {
            AlertEngine l_Engine = new AlertEngine(this._config);

            return BackgroundJob.Schedule(() => l_Engine.Schedule(alert.Id), alert.StartDate - DateTime.Now);
        }

        private void RemoveAlertJob(CustomerAlerts route)
        {
            AlertEngine l_Engine = new AlertEngine(this._config);

            l_Engine.RemoveRouteJob(route);
        }
    }
}
