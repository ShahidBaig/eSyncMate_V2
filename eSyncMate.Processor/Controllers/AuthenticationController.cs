using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Drawing;
using EdiEngine.Runtime;
using EdiEngine;
using JUST;
using eSyncMate.DB.Entities;
using System.Reflection;
using eSyncMate.Processor.Models;
using eSyncMate.Processor.Managers;
using System.Data;
using eSyncMate.DB;
using Nancy;
using Hangfire;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using AuthenticationandAuthorization.Controllers;
using System.Security.Claims;
using MySqlX.XDevAPI.Common;
using System.Diagnostics;
using Result = eSyncMate.DB.Result;
using OtpNet;
using QRCoder;

namespace eSyncMate.Processor.Controllers
{
    public class AuthenticationController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(IConfiguration config, ILogger<AuthenticationController> logger)
        {
            _config = config;
            _logger = logger;
        }

        private string GenerateJSONWebToken(LoginModel user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim("id", user.Id.ToString()),
                new Claim("firstName", user.FirstName),
                new Claim("lastName", user.LastName),
                new Claim("mobile", user.Mobile),
                new Claim("email", user.Email),
                new Claim("status", user.Status.ToString()),
                new Claim("createdDate", user.CreatedDate.ToString()),
                new Claim("userType", user.UserType.ToString()),
                new Claim("company", user.Company.ToString()),
				new Claim("customerName", user.CustomerName.ToString()),
                new Claim("isSetupAllowed", user.IsSetupAllowed.ToString()),
                new Claim("userID", user.UserID.ToString())

            };

            var token = new JwtSecurityToken(_config["Jwt:Issuer"],
                _config["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(120),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<LoginModel> AuthenticateUser(string userID, string password)
        {
            LoginModel user = null;
            var l_DataTable = new DataTable();
            Users l_users = new Users();

            l_users.UseConnection(CommonUtils.ConnectionString);

            string criteria = $"(UserID = '{userID}') AND Password = '{l_users.Encrypt(password)}' AND Status = 'ACTIVE'";

            if (l_users.GetViewList(criteria, "", ref l_DataTable))
            {
                if (l_DataTable.Rows.Count > 0)
                {
                    DataRow row = l_DataTable.Rows[0];
                    user = new LoginModel
                    {
                        Id = int.Parse(row["Id"].ToString()),
                        FirstName = row["FirstName"].ToString(),
                        LastName = row["LastName"].ToString(),
                        Mobile = row["Mobile"].ToString(),
                        Email = row["Email"].ToString(),
                        Status = row["Status"].ToString(),
                        CreatedDate = DateTime.Parse(row["createdDate"].ToString()),
                        UserType = row["UserType"].ToString(),
                        Company = row["Company"].ToString(),
                        CustomerName = row["CustomerName"] != DBNull.Value ? row["CustomerName"].ToString() : "",
                        IsSetupAllowed= row["IsSetupAllowed"] != DBNull.Value ?  Convert.ToBoolean(row["IsSetupAllowed"]): false,
                        MFAEnabled = row.Table.Columns.Contains("MFAEnabled") && row["MFAEnabled"] != DBNull.Value ? Convert.ToBoolean(row["MFAEnabled"]) : false,
                        MFASecret = row.Table.Columns.Contains("MFASecret") && row["MFASecret"] != DBNull.Value ? Convert.ToString(row["MFASecret"]) : "",
                        UserID = row["UserID"] != DBNull.Value ? Convert.ToString(row["UserID"]) : "",
                    };
                }
            }

            return user;
        }

        [HttpGet("Login")]
        public async Task<IActionResult> Login(string userID, string password)
        {
            IActionResult response = Unauthorized();
            var user = await AuthenticateUser(userID, password);
           
            if (user !=null)
            {
                if (user.MFAEnabled)
                {
                    // 2FA enabled for this user — same pattern as BizMate
                    string secretKey = user.MFASecret;
                    string qrImage = "";
                    bool requiresSetup = false;

                    if (string.IsNullOrEmpty(secretKey))
                    {
                        // First time: generate secret + QR code
                        requiresSetup = true;
                        secretKey = GenerateSecretKey();
                        qrImage = GenerateQRCode(user.Email, secretKey);

                        // Store secret in DB
                        Users l_users = new Users();
                        l_users.UseConnection(CommonUtils.ConnectionString);
                        l_users.Connection.Execute($"UPDATE Users SET MFASecret = '{secretKey}' WHERE Id = {user.Id}");
                    }

                    response = Ok(new
                    {
                        requiresMfa = true,
                        requiresSetup = requiresSetup,
                        secretKey = secretKey,
                        qrImage = qrImage,
                        userId = user.UserID,
                        userIdNum = user.Id,
                        email = user.Email,
                        message = requiresSetup ? "MFA setup required." : "MFA verification required."
                    });
                }
                else
                {
                    // No 2FA — normal login (original flow)
                    var tokenString = GenerateJSONWebToken(user);
                    Declarations.g_UserNo = user.Id;
                    var userMenus = GetUserMenuTree(user.Id, user.Company);

                    response = Ok(new { token = tokenString, message = "Success", menus = userMenus });
                }
            }
            else
            {
                response = Ok(new { token = "Invalid", message = "Either you dont have permission or Invalid Credentials!" });
            }
            return response;
        }

        // Exact BizMate AuthManager.GenerateSecretKey()
        private string GenerateSecretKey()
        {
            byte[] secretKey = KeyGeneration.GenerateRandomKey(20);
            return OtpNet.Base32Encoding.ToString(secretKey);
        }

        // Exact BizMate AuthManager.GenerateQRCode()
        private string GenerateQRCode(string email, string secret)
        {
            string issuer = "eSyncMate";
            string protocol = $"otpauth://totp/{issuer}:{email}?secret={secret}&issuer={issuer}";

            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(protocol, QRCodeGenerator.ECCLevel.Q);

            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeImage = qrCode.GetGraphic(20);
            return Convert.ToBase64String(qrCodeImage);
        }

        private GetUserMenusResponseModel GetUserMenuTree(int userId, string company)
        {
            GetUserMenusResponseModel l_Response = new GetUserMenusResponseModel();
            DataTable l_Data = new DataTable();

            try
            {
                RoleMenus l_Entity = new RoleMenus();
                l_Entity.UseConnection(CommonUtils.ConnectionString);

                // Filter by company: show menus where Company is NULL/empty (shared) or contains the user's company
                string criteria = $"UserId = {userId}";
                if (!string.IsNullOrEmpty(company))
                {
                    criteria += $" AND (Company IS NULL OR Company = '' OR Company LIKE '%{company}%')";
                }
                l_Entity.GetViewList(criteria, string.Empty, ref l_Data, "ModuleSortOrder ASC, MenuSortOrder ASC");

                var moduleDict = new Dictionary<int, UserMenuModuleModel>();

                foreach (DataRow l_Row in l_Data.Rows)
                {
                    int moduleId = Convert.ToInt32(l_Row["ModuleId"]);
                    string roleName = l_Row["RoleName"]?.ToString() ?? "";

                    if (string.IsNullOrEmpty(l_Response.RoleName))
                        l_Response.RoleName = roleName;

                    if (!moduleDict.ContainsKey(moduleId))
                    {
                        moduleDict[moduleId] = new UserMenuModuleModel
                        {
                            ModuleId = moduleId,
                            ModuleName = l_Row["ModuleName"]?.ToString() ?? "",
                            ModuleTranslationKey = l_Row["ModuleTranslationKey"]?.ToString() ?? "",
                            ModuleIcon = l_Row["ModuleIcon"]?.ToString() ?? "",
                            ModuleSortOrder = Convert.ToInt32(l_Row["ModuleSortOrder"]),
                            MenuItems = new List<UserMenuItemModel>()
                        };
                    }

                    moduleDict[moduleId].MenuItems.Add(new UserMenuItemModel
                    {
                        MenuId = Convert.ToInt32(l_Row["MenuId"]),
                        MenuName = l_Row["MenuName"]?.ToString() ?? "",
                        MenuTranslationKey = l_Row["MenuTranslationKey"]?.ToString() ?? "",
                        Route = l_Row["Route"]?.ToString() ?? "",
                        MenuIcon = l_Row["MenuIcon"]?.ToString() ?? "",
                        IsExternalLink = Convert.ToBoolean(l_Row["IsExternalLink"]),
                        ExternalUrl = l_Row["ExternalUrl"] != DBNull.Value ? l_Row["ExternalUrl"]?.ToString() ?? "" : "",
                        MenuSortOrder = Convert.ToInt32(l_Row["MenuSortOrder"]),
                        CanView = Convert.ToBoolean(l_Row["CanView"]),
                        CanAdd = Convert.ToBoolean(l_Row["CanAdd"]),
                        CanEdit = Convert.ToBoolean(l_Row["CanEdit"]),
                        CanDelete = Convert.ToBoolean(l_Row["CanDelete"])
                    });
                }

                l_Response.Modules = moduleDict.Values.OrderBy(m => m.ModuleSortOrder).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AuthenticationController.GetUserMenuTree] userId={userId}, company={company} - {ex}");
            }
            finally
            {
                l_Data.Dispose();
            }

            return l_Response;
        }

        [HttpPost]
        [Route("Get")]
        public async Task<IEnumerable<string>> Get()
        {
            var accessToken = await HttpContext.GetTokenAsync("access_token");

            return new string[] { accessToken };
        }

        [HttpPost]
        [Route("registerUser")]
        public async Task<GetUserResponseModel> RegisterUser([FromBody] LoginModel User)
        {
            GetUserResponseModel l_Response = new GetUserResponseModel();
            Result l_Result = new Result();
            Users l_Users = new Users();
            DataTable l_Data =new DataTable();
            try
            {
                l_Users.UseConnection(CommonUtils.ConnectionString);

                l_Users.FirstName = User.FirstName;
                l_Users.LastName =  User.LastName;
                l_Users.Password = l_Users.Encrypt(User.Password) ;
                l_Users.Mobile = User.Mobile;
                l_Users.Status = User.Status;
                l_Users.UserType = User.UserType;
                l_Users.Email = User.Email;
                l_Users.CreatedBy = Declarations.g_UserNo;
                l_Users.CreatedDate = DateTime.Now;
                l_Users.Company = CommonUtils.Company;
                l_Users.CustomerName = User.CustomerName;
                l_Users.IsSetupAllowed = User.IsSetupAllowed;
                l_Users.UserID = User.UserID;

                l_Users.GetViewList("(UserID = '" + l_Users.UserID+"')" +"OR" + "(Email = '" + l_Users.Email + "')", "",ref l_Data);

                if (l_Data.Rows.Count > 0)
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = "";
                    l_Response.Description = $"User {Convert.ToString(l_Data.Rows[0]["FirstName"])} already registered.";
                }
                else
                {
					l_Result = l_Users.SaveNew();

					if (l_Result.IsSuccess)
					{
						l_Response.Code = l_Result.Code;
						l_Response.Message = l_Result.Description;
						l_Response.Description = $"User {l_Users.UserID} has been register successfully!";
						l_Response.NewUserId = l_Users.Id;

						// Assign role to new user if roleId provided
						if (User.RoleId > 0)
						{
							try
							{
								UserRoles l_UserRole = new UserRoles();
								l_UserRole.UseConnection(CommonUtils.ConnectionString);
								l_UserRole.UserId = l_Users.Id;
								l_UserRole.RoleId = User.RoleId;
								l_UserRole.CreatedDate = DateTime.Now;
								l_UserRole.CreatedBy = Declarations.g_UserNo;
								l_UserRole.SaveNew();
							}
							catch (Exception ex)
							{
								_logger.LogError($"[RegisterUser] Role assignment failed for UserId={l_Users.Id}, RoleId={User.RoleId} - {ex}");
							}
						}
					}
					else
					{
						l_Response.Code = (int)ResponseCodes.Error;
						l_Response.Message = l_Result.Description;
					}
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



      /*  private async Task<LoginModel> ChangePassword(string email,string oldPassword, string password)
        {
            LoginModel user = null;
            var l_DataTable = new DataTable();
            Users l_users = new Users();

            l_users.UseConnection(CommonUtils.ConnectionString);

            string criteria = $"(Email = '{email}' OR FirstName = '{email}') AND Password = '{l_users.Encrypt(password)}' AND Status = 'ACTIVE'";

            if (l_users.GetViewList(criteria, "", ref l_DataTable))
            {
                l_users.UpdatePassword(email, password);
            }

            return user;
        }*/



        [HttpGet("UpdatePassword")]
        public async Task<IActionResult> UpdatePassword(string UserID, string oldPassword, string Password)
        {
            IActionResult response = Unauthorized();

            Result l_Result = new Result();

            Password = PublicFunctions.Encrypt(Password);

            var l_DataTable = new DataTable();
            Users l_users = new Users();

            l_users.UseConnection(CommonUtils.ConnectionString);

            string criteria = $"UserID = '{UserID}' AND Password = '{l_users.Encrypt(oldPassword)}' AND Status = 'ACTIVE'";

            if (l_users.GetViewList(criteria, "", ref l_DataTable))
            {
                l_Result = l_users.UpdatePassword(UserID, Password);
            }

            if (l_Result.IsSuccess)
            {
                response = Ok(new { Message = "Password Updated Successfuly", Code = 100 });
            }
            else
            {
                response = Ok(new { Message = "Either you dont have permission or Invalid Credentials!", Code = 400 });
            }
            return response;
        }

        // Exact BizMate VerifyOTP pattern with wider window for clock drift
        [HttpPost("VerifyMFA")]
        public async Task<IActionResult> VerifyMFA([FromBody] VerifyMFARequest request)
        {
            try
            {
                // Exact BizMate 3 lines + VerificationWindow(10,10) for clock drift
                var secretBytes = OtpNet.Base32Encoding.ToBytes(request.SecretKey);
                var totp = new Totp(secretBytes);
                bool result = totp.VerifyTotp(request.Code, out long timeStepMatched, new VerificationWindow(10, 10));

                if (result)
                {
                    // Get user data for JWT
                    Users l_users = new Users();
                    l_users.UseConnection(CommonUtils.ConnectionString);
                    DataTable userData = new DataTable();

                    if (!l_users.GetViewList($"UserID = '{request.UserID}' AND Status = 'ACTIVE'", "", ref userData) || userData.Rows.Count == 0)
                    {
                        return Ok(new { code = 900, token = "Invalid", message = "User not found." });
                    }

                    DataRow row = userData.Rows[0];
                    LoginModel user = new LoginModel
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        FirstName = row["FirstName"].ToString(),
                        LastName = row["LastName"].ToString(),
                        Mobile = row["Mobile"].ToString(),
                        Email = row["Email"].ToString(),
                        Status = row["Status"].ToString(),
                        CreatedDate = DateTime.Parse(row["createdDate"].ToString()),
                        UserType = row["UserType"].ToString(),
                        Company = row["Company"].ToString(),
                        CustomerName = row["CustomerName"] != DBNull.Value ? row["CustomerName"].ToString() : "",
                        IsSetupAllowed = row["IsSetupAllowed"] != DBNull.Value ? Convert.ToBoolean(row["IsSetupAllowed"]) : false,
                        UserID = row["UserID"] != DBNull.Value ? Convert.ToString(row["UserID"]) : "",
                    };

                    var tokenString = GenerateJSONWebToken(user);
                    Declarations.g_UserNo = user.Id;
                    var userMenus = GetUserMenuTree(user.Id, user.Company);

                    return Ok(new { code = 100, token = tokenString, message = "OTP is verified.", menus = userMenus });
                }
                else
                {
                    return Ok(new { code = 900, token = "Invalid", message = "OTP is either wrong or expired." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[VerifyMFA] Error: {ex}");
                return Ok(new { code = 900, token = "Invalid", message = "An error occurred during verification." });
            }
        }
    }

    // Simple request model for VerifyMFA
    public class VerifyMFARequest
    {
        public string SecretKey { get; set; } = "";
        public string Code { get; set; } = "";
        public string UserID { get; set; } = "";
    }
}
