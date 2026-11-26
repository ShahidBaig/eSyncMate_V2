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
    // Every action needs a signed-in user unless it is marked [AllowAnonymous] (Login, VerifyMFA, RefreshToken, Logout).
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    public class AuthenticationController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(IConfiguration config, ILogger<AuthenticationController> logger)
        {
            _config = config;
            _logger = logger;
        }

        // Long-lived refresh token lifetime (stored in the "jwt" cookie).
        // Access token stays short (15 min); the refresh token keeps the session
        // alive while the user is active, refreshing the access token seamlessly.
        private const int RefreshTokenMinutes = 8 * 60; // 8 hours

        private void SetRefreshCookie(string refreshToken)
        {
            Response.Cookies.Append("jwt", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = DateTimeOffset.Now.AddMinutes(RefreshTokenMinutes)
            });
        }

        private string GenerateJSONWebToken(LoginModel user, int expiresMinutes = 15)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(CommonUtils.JwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
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
                new Claim("userID", user.UserID.ToString()),
                new Claim("roleName", user.RoleName ?? ""),
                new Claim("mustChangePassword", user.MustChangePassword ? "true" : "false")
            };

            var token = new JwtSecurityToken(CommonUtils.JwtIssuer,
                CommonUtils.JwtIssuer,
                claims: claims,
                expires: DateTime.Now.AddMinutes(expiresMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Reads VW_Users with SQL parameters: typed text (User ID, passwords, e-mail) never becomes part of the SQL.
        private static DataTable QueryUsers(string p_Where, params System.Data.SqlClient.SqlParameter[] p_Params)
        {
            DataTable l_Table = new DataTable();

            using var l_Conn = new System.Data.SqlClient.SqlConnection(CommonUtils.ConnectionString);
            using var l_Cmd = new System.Data.SqlClient.SqlCommand("SELECT * FROM VW_Users WHERE " + p_Where, l_Conn);
            l_Cmd.Parameters.AddRange(p_Params);
            using var l_Adapter = new System.Data.SqlClient.SqlDataAdapter(l_Cmd);
            l_Adapter.Fill(l_Table);

            return l_Table;
        }

        private static int ExecuteUsers(string p_Sql, params System.Data.SqlClient.SqlParameter[] p_Params)
        {
            using var l_Conn = new System.Data.SqlClient.SqlConnection(CommonUtils.ConnectionString);
            using var l_Cmd = new System.Data.SqlClient.SqlCommand(p_Sql, l_Conn);
            l_Cmd.Parameters.AddRange(p_Params);
            l_Conn.Open();
            return l_Cmd.ExecuteNonQuery();
        }

        private static LoginModel ToLoginModel(DataRow row)
        {
            return new LoginModel
            {
                Id = int.Parse(row["Id"].ToString()),
                FirstName = row["FirstName"].ToString(),
                LastName = row["LastName"].ToString(),
                Mobile = row["Mobile"].ToString(),
                Email = row["Email"].ToString(),
                Status = row["Status"].ToString(),
                CreatedDate = DateTime.TryParse(row["createdDate"].ToString(), out DateTime l_Created) ? l_Created : DateTime.MinValue,
                UserType = row["UserType"].ToString(),
                Company = row["Company"].ToString(),
                CustomerName = row["CustomerName"] != DBNull.Value ? row["CustomerName"].ToString() : "",
                IsSetupAllowed = row["IsSetupAllowed"] != DBNull.Value ? Convert.ToBoolean(row["IsSetupAllowed"]) : false,
                MFAEnabled = row.Table.Columns.Contains("MFAEnabled") && row["MFAEnabled"] != DBNull.Value ? Convert.ToBoolean(row["MFAEnabled"]) : false,
                MFASecret = row.Table.Columns.Contains("MFASecret") && row["MFASecret"] != DBNull.Value ? Convert.ToString(row["MFASecret"]) : "",
                UserID = row["UserID"] != DBNull.Value ? Convert.ToString(row["UserID"]) : "",
                RoleName = row.Table.Columns.Contains("RoleName") && row["RoleName"] != DBNull.Value ? Convert.ToString(row["RoleName"]) : "",
                MustChangePassword = row.Table.Columns.Contains("MustChangePassword") && row["MustChangePassword"] != DBNull.Value && Convert.ToBoolean(row["MustChangePassword"]),
            };
        }

        // The current ACTIVE user by database id (null when the user is gone, blocked or deleted)
        private static LoginModel GetActiveUser(int p_Id)
        {
            using DataTable l_Table = QueryUsers("Id = @id AND Status = 'ACTIVE'", new System.Data.SqlClient.SqlParameter("@id", p_Id));
            return l_Table.Rows.Count > 0 ? ToLoginModel(l_Table.Rows[0]) : null;
        }

        private async Task<LoginModel> AuthenticateUser(string userID, string password)
        {
            if (string.IsNullOrEmpty(userID) || string.IsNullOrEmpty(password))
                return null;

            using DataTable l_DataTable = QueryUsers("UserID = @userID AND Password = @password AND Status = 'ACTIVE'",
                new System.Data.SqlClient.SqlParameter("@userID", userID),
                new System.Data.SqlClient.SqlParameter("@password", new Users().Encrypt(password)));

            return l_DataTable.Rows.Count > 0 ? ToLoginModel(l_DataTable.Rows[0]) : null;
        }

        // ---- 2FA: a short-lived ticket proves the password step happened ----
        // It is signed with a key derived from the JWT key, so it can never be used as an access token,
        // and it only says WHICH user passed the password step; the 2FA secret never leaves the server after setup.
        private const string MfaTicketAudience = "esyncmate-mfa-pending";
        private const int MfaTicketMinutes = 5;

        private static SymmetricSecurityKey MfaTicketKey =>
            new SymmetricSecurityKey(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(CommonUtils.JwtKey + "|mfa-pending")));

        private static string GenerateMfaTicket(int p_UserId)
        {
            var l_Token = new JwtSecurityToken(CommonUtils.JwtIssuer, MfaTicketAudience,
                claims: new[] { new Claim("mfaUserId", p_UserId.ToString()), new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) },
                expires: DateTime.UtcNow.AddMinutes(MfaTicketMinutes),
                signingCredentials: new SigningCredentials(MfaTicketKey, SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(l_Token);
        }

        private static int? ReadMfaTicket(string p_Ticket)
        {
            if (string.IsNullOrEmpty(p_Ticket))
                return null;

            try
            {
                new JwtSecurityTokenHandler().ValidateToken(p_Ticket, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = MfaTicketKey,
                    ValidateIssuer = true,
                    ValidIssuer = CommonUtils.JwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = MfaTicketAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken l_Validated);

                string l_Id = ((JwtSecurityToken)l_Validated).Claims.FirstOrDefault(c => c.Type == "mfaUserId")?.Value;
                return int.TryParse(l_Id, out int l_UserId) ? l_UserId : null;
            }
            catch
            {
                return null;
            }
        }

        // Issues the access token + refresh cookie for a user who passed every login step
        private object CompleteLogin(LoginModel p_User, string p_Message, int? p_Code = null)
        {
            string l_Token = GenerateJSONWebToken(p_User);                         // short access token (15 min)
            string l_Refresh = GenerateJSONWebToken(p_User, RefreshTokenMinutes);  // long-lived refresh token (8 h)
            Declarations.g_UserNo = p_User.Id;
            var l_Menus = GetUserMenuTree(p_User.Id, p_User.Company);

            SetRefreshCookie(l_Refresh);

            return p_Code.HasValue
                ? new { code = p_Code.Value, token = l_Token, message = p_Message, menus = l_Menus, mustChangePassword = p_User.MustChangePassword }
                : (object)new { token = l_Token, message = p_Message, menus = l_Menus, mustChangePassword = p_User.MustChangePassword };
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            IActionResult response = Unauthorized();
            var user = await AuthenticateUser(request?.UserID, request?.Password);

            if (user !=null)
            {
                if (user.MFAEnabled)
                {
                    // 2FA: the password is correct; the session is issued only after the code (VerifyMFA).
                    // The browser gets a short-lived ticket for that step, not the user's 2FA secret.
                    string secretKey = user.MFASecret;
                    string qrImage = "";
                    bool requiresSetup = false;

                    if (string.IsNullOrEmpty(secretKey))
                    {
                        // First time: generate secret + QR code (the QR is the only time the secret leaves the server)
                        requiresSetup = true;
                        secretKey = GenerateSecretKey();
                        qrImage = GenerateQRCode(user.Email, secretKey);

                        ExecuteUsers("UPDATE Users SET MFASecret = @secret WHERE Id = @id",
                            new System.Data.SqlClient.SqlParameter("@secret", secretKey),
                            new System.Data.SqlClient.SqlParameter("@id", user.Id));
                    }

                    response = Ok(new
                    {
                        requiresMfa = true,
                        requiresSetup = requiresSetup,
                        mfaToken = GenerateMfaTicket(user.Id),
                        qrImage = qrImage,
                        userId = user.UserID,
                        userIdNum = user.Id,
                        email = user.Email,
                        message = requiresSetup ? "MFA setup required." : "MFA verification required."
                    });
                }
                else
                {
                    // No 2FA — normal login
                    response = Ok(CompleteLogin(user, "Success"));
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
                        CanDelete = Convert.ToBoolean(l_Row["CanDelete"]),
                        CanResubmit = l_Row.Table.Columns.Contains("CanResubmit") && Convert.ToBoolean(l_Row["CanResubmit"]),
                        CanReTransmit = l_Row.Table.Columns.Contains("CanReTransmit") && Convert.ToBoolean(l_Row["CanReTransmit"])
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

        // A SuperAdmin, or a role that may add users on the Users screen
        private bool CanAddUsers()
        {
            if (string.Equals(User.FindFirst("roleName")?.Value, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
                return true;

            if (!int.TryParse(User.FindFirst("id")?.Value, out int l_UserId))
                return false;

            var l_Menus = GetUserMenuTree(l_UserId, User.FindFirst("company")?.Value ?? "");

            return l_Menus?.Modules?.Any(m => m.MenuItems?.Any(i => i.Route == "edi/users" && i.CanAdd) == true) == true;
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
                if (!CanAddUsers())
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = "You are not allowed to add users.";
                    l_Response.Description = "You are not allowed to add users.";
                    return l_Response;
                }

                l_Users.UseConnection(CommonUtils.ConnectionString);

                l_Users.FirstName = User.FirstName;
                l_Users.LastName =  User.LastName;
                l_Users.Password = l_Users.Encrypt(User.Password) ;
                l_Users.Mobile = User.Mobile;
                l_Users.Status = User.Status;
                l_Users.UserType = User.UserType;
                l_Users.Email = User.Email;
                l_Users.CreatedBy = int.TryParse(this.User.FindFirst("id")?.Value, out int l_CreatedBy) ? l_CreatedBy : Declarations.g_UserNo;
                l_Users.CreatedDate = DateTime.Now;
                l_Users.Company = CommonUtils.Company;
                l_Users.CustomerName = User.CustomerName;
                l_Users.IsSetupAllowed = User.IsSetupAllowed;
                l_Users.UserID = User.UserID;
                l_Users.MustChangePassword = User.MustChangePassword;

                l_Data = QueryUsers("UserID = @userID OR Email = @email",
                    new System.Data.SqlClient.SqlParameter("@userID", (object)l_Users.UserID ?? DBNull.Value),
                    new System.Data.SqlClient.SqlParameter("@email", (object)l_Users.Email ?? DBNull.Value));

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



        // The signed-in user changes their OWN password (the user comes from the token, not from the request).
        // A successful change clears "must change password" and issues a fresh session.
        [HttpPost("UpdatePassword")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request)
        {
            if (!int.TryParse(User.FindFirst("id")?.Value, out int l_UserId))
                return Unauthorized();

            if (string.IsNullOrEmpty(request?.OldPassword) || string.IsNullOrEmpty(request.Password))
                return Ok(new { Message = "Enter the old and the new password.", Code = 400 });

            if (request.Password.Length < 8 || request.Password.Length > 15)
                return Ok(new { Message = "The new password must be 8 to 15 characters.", Code = 400 });

            if (request.Password == request.OldPassword)
                return Ok(new { Message = "The new password must be different from the old one.", Code = 400 });

            Users l_users = new Users();

            using DataTable l_DataTable = QueryUsers("Id = @id AND Password = @old AND Status = 'ACTIVE'",
                new System.Data.SqlClient.SqlParameter("@id", l_UserId),
                new System.Data.SqlClient.SqlParameter("@old", l_users.Encrypt(request.OldPassword)));

            if (l_DataTable.Rows.Count == 0)
                return Ok(new { Message = "The old password is not correct.", Code = 400 });

            ExecuteUsers(@"UPDATE Users SET Password = @new WHERE Id = @id;
                           IF COL_LENGTH('dbo.Users', 'MustChangePassword') IS NOT NULL
                               EXEC sp_executesql N'UPDATE Users SET MustChangePassword = 0 WHERE Id = @uid', N'@uid INT', @uid = @id;",
                new System.Data.SqlClient.SqlParameter("@new", PublicFunctions.Encrypt(request.Password)),
                new System.Data.SqlClient.SqlParameter("@id", l_UserId));

            LoginModel l_User = GetActiveUser(l_UserId);

            if (l_User == null)
                return Ok(new { Message = "Password Updated Successfuly", Code = 100 });

            // new token without "must change password"
            var l_Session = CompleteLogin(l_User, "Password Updated Successfuly", 100);
            return Ok(l_Session);
        }

        [AllowAnonymous]
        [HttpPost("RefreshToken")]
        public IActionResult RefreshToken()
        {
            var cookieToken = Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(cookieToken))
                return Unauthorized(new { message = "No session found" });

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(CommonUtils.JwtKey);

                tokenHandler.ValidateToken(cookieToken, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = CommonUtils.JwtIssuer,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;

                // Read the user again: a user who was blocked or deleted meanwhile gets no new session,
                // and role / "must change password" are always current.
                if (!int.TryParse(jwtToken.Claims.FirstOrDefault(c => c.Type == "id")?.Value, out int l_UserId))
                    return Unauthorized(new { message = "Session expired" });

                LoginModel user = GetActiveUser(l_UserId);

                if (user == null)
                {
                    Response.Cookies.Delete("jwt");
                    return Unauthorized(new { message = "Session expired" });
                }

                return Ok(CompleteLogin(user, "Success"));
            }
            catch
            {
                return Unauthorized(new { message = "Session expired" });
            }
        }

        [AllowAnonymous]
        [HttpPost("Logout")]
        public IActionResult Logout()
        {
            try
            {
                // Manually parse token (without expiry validation so expired tokens also get blacklisted)
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                var rawToken   = authHeader?.StartsWith("Bearer ") == true
                                 ? authHeader.Substring(7)
                                 : null;

                if (!string.IsNullOrEmpty(rawToken))
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var jwtToken     = tokenHandler.ReadJwtToken(rawToken);

                    var jti       = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
                    var userIdStr = jwtToken.Claims.FirstOrDefault(c => c.Type == "id")?.Value;

                    if (!string.IsNullOrEmpty(jti) && int.TryParse(userIdStr, out int userId))
                    {
                        using var conn = new System.Data.SqlClient.SqlConnection(CommonUtils.ConnectionString);
                        conn.Open();
                        using var cmd = new System.Data.SqlClient.SqlCommand(
                            "INSERT INTO TokenBlacklist (TokenJti, UserId, ExpiresAt) VALUES (@jti, @uid, @exp)", conn);
                        cmd.Parameters.AddWithValue("@jti", jti);
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.Parameters.AddWithValue("@exp", jwtToken.ValidTo);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }

            Response.Cookies.Delete("jwt");
            return Ok(new { Message = "Logged out" });
        }

        // 2FA step 2: the code is checked against the secret STORED for the user named in the ticket that the
        // password step issued. The browser cannot choose the user or the secret.
        // VerificationWindow(10,10) as before, for clock drift between phone and server.
        [AllowAnonymous]
        [HttpPost("VerifyMFA")]
        public async Task<IActionResult> VerifyMFA([FromBody] VerifyMFARequest request)
        {
            try
            {
                int? l_UserId = ReadMfaTicket(request?.MfaToken);

                if (l_UserId == null)
                    return Ok(new { code = 900, token = "Invalid", message = "The sign-in has expired. Please sign in again." });

                LoginModel user = GetActiveUser(l_UserId.Value);

                if (user == null || !user.MFAEnabled || string.IsNullOrEmpty(user.MFASecret))
                    return Ok(new { code = 900, token = "Invalid", message = "User not found." });

                string l_Code = (request.Code ?? string.Empty).Trim();

                if (l_Code.Length != 6 || !l_Code.All(char.IsDigit))
                    return Ok(new { code = 900, token = "Invalid", message = "OTP is either wrong or expired." });

                var totp = new Totp(OtpNet.Base32Encoding.ToBytes(user.MFASecret));
                bool result = totp.VerifyTotp(l_Code, out long timeStepMatched, new VerificationWindow(10, 10));

                if (!result)
                    return Ok(new { code = 900, token = "Invalid", message = "OTP is either wrong or expired." });

                return Ok(CompleteLogin(user, "OTP is verified.", 100));
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
        public string MfaToken { get; set; } = "";   // ticket from the password step (Login)
        public string Code { get; set; } = "";
    }
}
