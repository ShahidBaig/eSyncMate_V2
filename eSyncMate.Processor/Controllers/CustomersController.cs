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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using eSyncMate.Processor.Helpers;

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

                // Always filter by assigned customers for non-admin users
                if (!string.IsNullOrEmpty(userData?.Customers) && !userData.IsSuperAdmin)
                {
                    string customerFilter = $"ERPCustomerID IN ({userData?.Customers})";
                    l_Criteria = string.IsNullOrEmpty(l_Criteria) ? customerFilter : $"{l_Criteria} AND {customerFilter}";
                }

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring Customer search.");

                int totalCount = 0;
                l_Customer.GetListPaged(l_Criteria, string.Empty, ref l_Data, "Id DESC", searchModel.PageNumber, searchModel.PageSize, out totalCount);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Customers searched {{{l_Data.Rows.Count}}} of {totalCount} total.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating Customers.");

                l_Response.Customers = new List<CustomerDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    CustomerDataModel l_CustomerRow = new CustomerDataModel();

                    DBEntity.PopulateObjectFromRow(l_CustomerRow, l_Data, l_Row);

                    l_Response.Customers.Add(l_CustomerRow);
                }

                l_Response.TotalCount = totalCount;
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

                if (model.Id == 0)
                {
                    // Insert
                    l_CustomerAlerts.CreatedBy = l_CustomerAlerts.CreatedBy;
                    l_CustomerAlerts.CreatedDate = DateTime.Now;
                    l_Result = l_CustomerAlerts.SaveNew();
                }
                else
                {
                    // Update — remove old Hangfire jobs before modifying
                    DB.Entities.CustomerAlerts l_OldAlert = new DB.Entities.CustomerAlerts();
                    l_OldAlert.UseConnection(CommonUtils.ConnectionString);
                    l_OldAlert.Id = model.Id;
                    if (l_OldAlert.GetObject().IsSuccess)
                    {
                        this.RemoveAlertJob(l_OldAlert);
                    }

                    l_CustomerAlerts.ModifiedBy = l_CustomerAlerts.CreatedBy;
                    l_CustomerAlerts.ModifiedDate = DateTime.Now;
                    l_Result = l_CustomerAlerts.Modify();
                }

                if (l_Result.IsSuccess)
                {
                    l_JobID = this.SetupAlertJob(l_CustomerAlerts);
                    l_CustomerAlerts.JobID = l_JobID;

                    l_CustomerAlerts.UpdateAlertJobID(l_CustomerAlerts.Id, l_CustomerAlerts.JobID);

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

        #region Target Plus OAuth 2.0 (Authorization Code + rotating refresh token)

        // Default Target Plus OAuth endpoints (used if not stored per-customer).
        private const string TargetAuthUrlDefault  = "https://oauth.plus.iam.partnersonline.com/auth/oauth/v2/tgt/authorize/nla/1";
        private const string TargetTokenUrlDefault = "https://oauth.plus.iam.partnersonline.com/auth/oauth/v2/token";
        private const string TargetScope           = "openid email profile";

        // Short-lived state -> customerId map (CSRF + carries which customer to save).
        private static readonly ConcurrentDictionary<string, int> _oauthState = new ConcurrentDictionary<string, int>();

        /// <summary>
        /// Build the Target authorization URL for a customer. Frontend calls this (JWT),
        /// then window.open()s the returned authUrl. Target then redirects the browser to
        /// targetOAuthCallback below.
        /// </summary>
        [HttpGet]
        [Route("buildTargetAuthUrl/{customerId}")]
        public IActionResult BuildTargetAuthUrl(int customerId)
        {
            Customers l_Customer = new Customers();
            l_Customer.UseConnection(CommonUtils.ConnectionString);

            if (!l_Customer.LoadOAuth(customerId))
                return BadRequest(new { message = "Customer not found." });

            if (string.IsNullOrEmpty(l_Customer.OAuthClientId))
                return BadRequest(new { message = "OAuth Client ID is not configured for this customer." });

            string l_AuthUrl = string.IsNullOrEmpty(l_Customer.OAuthAuthUrl) ? TargetAuthUrlDefault : l_Customer.OAuthAuthUrl;
            string l_State = Guid.NewGuid().ToString("N");
            _oauthState[l_State] = customerId;

            string l_RedirectUri = BuildCallbackUri();

            string l_Url = l_AuthUrl +
                "?response_type=code" +
                "&client_id=" + Uri.EscapeDataString(l_Customer.OAuthClientId) +
                "&redirect_uri=" + Uri.EscapeDataString(l_RedirectUri) +
                "&scope=" + Uri.EscapeDataString(TargetScope) +
                "&state=" + l_State;

            return Ok(new { authUrl = l_Url, redirectUri = l_RedirectUri });
        }

        /// <summary>
        /// OAuth redirect target. Target sends the browser here with ?code &amp; state.
        /// Exchanges the code for tokens SERVER-SIDE (so the token binds to the server IP),
        /// saves them (encrypted) to the customer, and returns a popup page that posts the
        /// result back to the opener window and can be closed.
        /// AllowAnonymous: this is a top-level browser navigation, no JWT header present.
        /// </summary>
        [HttpGet]
        [Route("targetOAuthCallback")]
        [AllowAnonymous]
        public async Task<IActionResult> TargetOAuthCallback([FromQuery] string code, [FromQuery] string state,
            [FromQuery] string error = null, [FromQuery] string error_description = null)
        {
            if (!string.IsNullOrEmpty(error))
                return Content(BuildPopupHtml(false, $"{error}: {error_description}", null), "text/html");

            if (string.IsNullOrEmpty(state) || !_oauthState.TryRemove(state, out int l_CustomerId))
                return Content(BuildPopupHtml(false, "Invalid or expired state. Please start authorization again.", null), "text/html");

            if (string.IsNullOrEmpty(code))
                return Content(BuildPopupHtml(false, "No authorization code was returned by Target.", null), "text/html");

            Customers l_Customer = new Customers();
            l_Customer.UseConnection(CommonUtils.ConnectionString);
            if (!l_Customer.LoadOAuth(l_CustomerId))
                return Content(BuildPopupHtml(false, "Customer not found.", null), "text/html");

            string l_ClientId = l_Customer.OAuthClientId;
            string l_ClientSecret = l_Customer.OAuthClientSecret;
            string l_TokenUrl = string.IsNullOrEmpty(l_Customer.OAuthTokenUrl) ? TargetTokenUrlDefault : l_Customer.OAuthTokenUrl;
            string l_RedirectUri = BuildCallbackUri();

            try
            {
                using var l_Http = new HttpClient();
                using var l_Req = new HttpRequestMessage(HttpMethod.Post, l_TokenUrl);

                string l_Basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{l_ClientId}:{l_ClientSecret}"));
                l_Req.Headers.Authorization = new AuthenticationHeaderValue("Basic", l_Basic);
                l_Req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"]   = "authorization_code",
                    ["code"]         = code,
                    ["redirect_uri"] = l_RedirectUri
                });

                var l_Resp = await l_Http.SendAsync(l_Req);
                string l_Body = await l_Resp.Content.ReadAsStringAsync();

                if (!l_Resp.IsSuccessStatusCode)
                    return Content(BuildPopupHtml(false, $"Token exchange failed ({(int)l_Resp.StatusCode}). {l_Body}", null), "text/html");

                JObject l_Json = JObject.Parse(l_Body);
                string l_AccessToken     = l_Json.Value<string>("access_token");
                string l_RefreshToken    = l_Json.Value<string>("refresh_token");
                int    l_ExpiresIn       = l_Json.Value<int?>("expires_in") ?? 0;
                int    l_RefreshExpiresIn = l_Json.Value<int?>("refresh_token_expires_in") ?? 0;

                DateTime  l_AccessExpiry  = DateTime.UtcNow.AddSeconds(l_ExpiresIn > 60 ? l_ExpiresIn - 60 : l_ExpiresIn);
                DateTime? l_RefreshExpiry = l_RefreshExpiresIn > 0 ? DateTime.UtcNow.AddSeconds(l_RefreshExpiresIn) : (DateTime?)null;

                l_Customer.SaveOAuthTokens(l_CustomerId,
                    l_RefreshToken, l_RefreshExpiry,
                    l_AccessToken, l_AccessExpiry);

                var l_Info = new
                {
                    Customer         = l_Customer.Name ?? l_CustomerId.ToString(),
                    TokenType        = l_Json.Value<string>("token_type"),
                    Scope            = l_Json.Value<string>("scope"),
                    AccessToken      = Mask(l_AccessToken),
                    AccessExpires    = l_AccessExpiry.ToString("MM/dd/yyyy hh:mm tt") + " UTC",
                    RefreshToken     = Mask(l_RefreshToken),
                    RefreshExpires   = l_RefreshExpiry.HasValue ? l_RefreshExpiry.Value.ToString("MM/dd/yyyy") : "n/a",
                    SavedToDatabase  = DateTime.UtcNow.ToString("MM/dd/yyyy hh:mm tt") + " UTC"
                };

                return Content(BuildPopupHtml(true, "Authorized and saved to the database.", l_Info), "text/html");
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "[CustomersController.TargetOAuthCallback] - token exchange error.");
                return Content(BuildPopupHtml(false, "Exception during token exchange: " + ex.Message, null), "text/html");
            }
        }

        /// <summary>
        /// OAuth status for the Customers UI (authorized? flag on? expiries?).
        /// </summary>
        [HttpGet]
        [Route("getTargetOAuthStatus/{customerId}")]
        public IActionResult GetTargetOAuthStatus(int customerId)
        {
            Customers l_Customer = new Customers();
            l_Customer.UseConnection(CommonUtils.ConnectionString);
            if (!l_Customer.LoadOAuth(customerId))
                return BadRequest(new { message = "Customer not found." });

            bool l_Authorized = !string.IsNullOrEmpty(l_Customer.OAuthRefreshToken) &&
                                (l_Customer.OAuthRefreshTokenExpiry == null || l_Customer.OAuthRefreshTokenExpiry > DateTime.UtcNow);

            return Ok(new
            {
                useNewAuthentication = l_Customer.UseNewAuthentication,
                authorized           = l_Authorized,
                hasCredentials       = !string.IsNullOrEmpty(l_Customer.OAuthClientId),
                clientId             = l_Customer.OAuthClientId,   // non-secret — prefill in UI
                authUrl              = l_Customer.OAuthAuthUrl,
                tokenUrl             = l_Customer.OAuthTokenUrl,
                hasSecret            = !string.IsNullOrEmpty(l_Customer.OAuthClientSecret),
                refreshExpiry        = l_Customer.OAuthRefreshTokenExpiry,
                accessExpiry         = l_Customer.OAuthAccessTokenExpiry,
                lastUpdated          = l_Customer.OAuthTokenUpdatedDate
            });
        }

        /// <summary>
        /// Toggle the new-OAuth flag (POST — Kong blocks PUT).
        /// </summary>
        [HttpPost]
        [Route("setUseNewAuthentication")]
        public IActionResult SetUseNewAuthentication([FromQuery] int customerId, [FromQuery] bool enabled)
        {
            Customers l_Customer = new Customers();
            l_Customer.UseConnection(CommonUtils.ConnectionString);
            bool l_Ok = l_Customer.SetUseNewAuthentication(customerId, enabled);
            return l_Ok ? Ok(new { message = "Updated." }) : BadRequest(new { message = "Update failed." });
        }

        /// <summary>
        /// Save static OAuth credentials + endpoints for a customer (POST). Secret is encrypted.
        /// Body: { clientId, clientSecret, authUrl, tokenUrl }.
        /// </summary>
        [HttpPost]
        [Route("saveTargetOAuthCredentials/{customerId}")]
        public IActionResult SaveTargetOAuthCredentials(int customerId, [FromBody] Dictionary<string, string> body)
        {
            if (body == null) return BadRequest(new { message = "Missing body." });

            body.TryGetValue("clientId", out string l_ClientId);
            body.TryGetValue("clientSecret", out string l_ClientSecret);
            body.TryGetValue("authUrl", out string l_AuthUrl);
            body.TryGetValue("tokenUrl", out string l_TokenUrl);

            if (string.IsNullOrWhiteSpace(l_AuthUrl))  l_AuthUrl  = TargetAuthUrlDefault;
            if (string.IsNullOrWhiteSpace(l_TokenUrl)) l_TokenUrl = TargetTokenUrlDefault;

            Customers l_Customer = new Customers();
            l_Customer.UseConnection(CommonUtils.ConnectionString);
            l_Customer.LoadOAuth(customerId);   // load existing so we can preserve unspecified values

            // Keep the existing Client ID / Secret when the caller leaves them blank
            // (so credentials only need to be entered once, not on every save).
            if (string.IsNullOrWhiteSpace(l_ClientId))
                l_ClientId = l_Customer.OAuthClientId;

            string l_Secret;
            if (string.IsNullOrEmpty(l_ClientSecret))
                l_Secret = l_Customer.OAuthClientSecret;   // keep existing
            else
                l_Secret = l_ClientSecret;                 // stored plain

            bool l_Ok = l_Customer.SaveOAuthCredentials(customerId, l_ClientId, l_Secret, l_AuthUrl, l_TokenUrl);

            return l_Ok ? Ok(new { message = "Credentials saved." }) : BadRequest(new { message = "Save failed." });
        }

        // ── helpers ──────────────────────────────────────────────────────────
        private string BuildCallbackUri()
        {
            // Must EXACTLY match a Return URL registered in the Target portal.
            return $"{Request.Scheme}://{Request.Host}/api/Customers/targetOAuthCallback";
        }

        private static string Mask(string p_Value)
        {
            if (string.IsNullOrEmpty(p_Value)) return "";
            return p_Value.Length <= 12 ? "****" : p_Value.Substring(0, 6) + "…" + p_Value.Substring(p_Value.Length - 4);
        }

        private static string BuildPopupHtml(bool p_Success, string p_Message, object p_Info)
        {
            string l_Rows = string.Empty;
            if (p_Info != null)
            {
                JObject l_Jo = JObject.FromObject(p_Info);
                foreach (var p in l_Jo.Properties())
                {
                    l_Rows +=
                        "<tr><td style='padding:6px 10px;color:#64748b;border-bottom:1px solid #f1f5f9;white-space:nowrap;'>" +
                        System.Net.WebUtility.HtmlEncode(p.Name) +
                        "</td><td style='padding:6px 10px;color:#0f172a;border-bottom:1px solid #f1f5f9;font-family:Consolas,monospace;word-break:break-all;'>" +
                        System.Net.WebUtility.HtmlEncode(p.Value?.ToString()) + "</td></tr>";
                }
            }

            string l_Color = p_Success ? "#16a34a" : "#dc2626";
            string l_Title = p_Success ? "&#10003; Authorization Successful" : "&#10007; Authorization Failed";

            string l_Html = @"<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>Target OAuth</title></head>
<body style='font-family:Segoe UI,Arial,sans-serif;margin:0;background:#f1f5f9;'>
  <div style='max-width:580px;margin:32px auto;background:#fff;border:1px solid #e2e8f0;border-radius:8px;overflow:hidden;'>
    <div style='background:__COLOR__;color:#fff;padding:16px 20px;font-size:17px;font-weight:600;'>__TITLE__</div>
    <div style='padding:20px;'>
      <p style='margin:0 0 14px;color:#334155;font-size:14px;'>__MSG__</p>
      <table style='width:100%;border-collapse:collapse;font-size:13px;'>__ROWS__</table>
      <button onclick='window.close()' style='margin-top:18px;padding:8px 18px;background:#0f172a;color:#fff;border:none;border-radius:6px;cursor:pointer;font-size:13px;'>Close</button>
    </div>
  </div>
  <script>
    try { if (window.opener) window.opener.postMessage({ source:'target-oauth', success:__SUCCESS__, message:__MSGJSON__, info:__INFO__ }, '*'); } catch (e) {}
  </script>
</body></html>";

            return l_Html
                .Replace("__COLOR__", l_Color)
                .Replace("__TITLE__", l_Title)
                .Replace("__MSG__", System.Net.WebUtility.HtmlEncode(p_Message))
                .Replace("__ROWS__", l_Rows)
                .Replace("__SUCCESS__", p_Success ? "true" : "false")
                .Replace("__MSGJSON__", JsonConvert.SerializeObject(p_Message))
                .Replace("__INFO__", p_Info == null ? "null" : JsonConvert.SerializeObject(p_Info));
        }

        #endregion
    }
}
