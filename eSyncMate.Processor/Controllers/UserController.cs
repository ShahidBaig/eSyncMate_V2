using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;
using Hangfire;
using Intercom.Data;
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
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;

        public UserController(ILogger<UserController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("getUser")]
        public async Task<UserResponseModel> GetUser([FromQuery] UserSearchModel searchModel)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            UserResponseModel l_Response = new UserResponseModel();
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
                DB.Entities.Users l_User = new DB.Entities.Users();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_User.UseConnection(CommonUtils.ConnectionString);

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
                else if (searchModel.SearchOption == "First Name")
                {
                    l_Criteria = $" FirstName LIKE '%{searchModel.SearchValue}%'";
                }
                else if (searchModel.SearchOption == "Email")
                {
                    l_Criteria = $" Email LIKE '%{searchModel.SearchValue}%'";
                }
                else if (searchModel.SearchOption == "Status")
                {
                    l_Criteria = $" Status LIKE '%{searchModel.SearchValue}%'";
                }
                else if (searchModel.SearchOption == "UserType")
                {
                    l_Criteria = $" UserType LIKE '%{searchModel.SearchValue}%'";
                }
                else if (searchModel.SearchOption == "User ID")
                {
                    l_Criteria = $" USERID LIKE '%{searchModel.SearchValue}%'";
                }

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring User search.");

                int totalCount = 0;
                l_User.GetViewListPaged(l_Criteria, string.Empty, ref l_Data, "Id DESC", searchModel.PageNumber, searchModel.PageSize, out totalCount);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - User searched {{{l_Data.Rows.Count}}} of {totalCount} total.");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Populating User.");

                l_Response.User = new List<UserDataModel>();
                foreach (DataRow l_Row in l_Data.Rows)
                {
                    UserDataModel l_UserRow = new UserDataModel();

                    DBEntity.PopulateObjectFromRow(l_UserRow, l_Data, l_Row);

                    l_UserRow.Password = PublicFunctions.Encrypt(l_UserRow.Password);

                    l_Response.User.Add(l_UserRow);
                }

                l_Response.TotalCount = totalCount;
                l_Response.Code = (int)ResponseCodes.Success;
                l_Response.Message = "User fetched successfully!";

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - User are ready.");
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
        [Route("updateUser")]
        public async Task<ResponseModel> UpdateUser([FromBody] UpdateUserDataModel UserModel)
        {
            ResponseModel l_Response = new ResponseModel();
            Result l_Result = new Result();

            try
            {
                DB.Entities.Users l_User = new DB.Entities.Users();
                l_User.UseConnection(CommonUtils.ConnectionString);

                // Load existing user to preserve MFASecret
                l_User.Id = UserModel.Id;
                l_User.GetObject();
                string existingMFASecret = l_User.MFASecret;

                PublicFunctions.CopyTo(UserModel, l_User);

                l_User.Password =  PublicFunctions.Encrypt(l_User.Password);

                // Preserve MFASecret — only resetMFA endpoint should clear it
                l_User.MFASecret = existingMFASecret;

                l_Result = l_User.Modify();

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = l_Result.Code;
                    l_Response.Message = l_Result.Description;
                    l_Response.Description = $"User [ {UserModel.Id} ] has been updated successfully!";
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
        [Route("resetMFA/{userId}")]
        public async Task<ResponseModel> ResetMFA(int userId)
        {
            ResponseModel l_Response = new ResponseModel();

            try
            {
                DBConnector l_Conn = new DBConnector(CommonUtils.ConnectionString);
                bool result = l_Conn.Execute($"UPDATE Users SET MFASecret = '' WHERE Id = {userId}");

                if (result)
                {
                    l_Response.Code = (int)ResponseCodes.Success;
                    l_Response.Message = "MFA has been reset successfully. User will need to scan QR code on next login.";
                    l_Response.Description = $"MFA reset for User [{userId}]";
                }
                else
                {
                    l_Response.Code = (int)ResponseCodes.Error;
                    l_Response.Message = "Failed to reset MFA.";
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                l_Response.Message = ex.Message;
            }

            return l_Response;
        }

       /* [HttpPost]
        [Route("updatePassword")]
        public async Task<ResponseModel> setUpdatePassword(string EmailID, string Password, string OldPassword)
        {
            MethodBase l_Me = MethodBase.GetCurrentMethod();
            ResponseModel l_Response = new ResponseModel();
            DataTable l_Data = new DataTable();
            eSyncMate.DB.Entities.Users user = new eSyncMate.DB.Entities.Users();

            try
            {
                string l_Criteria = string.Empty;

                l_Response.Code = (int)ResponseCodes.Error;

                user.UseConnection(CommonUtils.ConnectionString);

                OldPassword = PublicFunctions.Encrypt(OldPassword);

                Password = PublicFunctions.Encrypt(Password);

                Result l_Result = user.UpdatePassword(EmailID, OldPassword, Password);

                if (l_Result.IsSuccess)
                {
                    l_Response.Code = (int)ResponseCodes.Success;
                    l_Response.Message = "Password updated successfully!";
                }
            }
            catch (Exception ex)
            {
                l_Response.Code = (int)ResponseCodes.Exception;
                this._logger.LogCritical($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - {ex}");
            }

            return l_Response;
        }*/

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
                eSyncMate.DB.Entities.Users l_User = new eSyncMate.DB.Entities.Users();

                l_Response.Code = (int)ResponseCodes.Error;

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Building search criteria.");

                l_User.UseConnection(CommonUtils.ConnectionString);

                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Search criteria ready ({l_Criteria}).");
                this._logger.LogDebug($"[{l_Me.ReflectedType.Name}.{l_Me.Name}] - Staring Route Types search.");

                l_User.GetList(l_Criteria, string.Empty, ref l_Data, "Id DESC");

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
