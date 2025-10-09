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

namespace eSyncMate.Processor.Controllers
{
    public class AuthenticationController : Controller
    {
        private readonly IConfiguration _config;

        public AuthenticationController(IConfiguration config)
        {
            _config = config;
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
                var tokenString = GenerateJSONWebToken(user);
                Declarations.g_UserNo = user.Id;
                response = Ok(new { Token = tokenString, Message = "Success" });
            }
            else
            {
                response = Ok(new { Token = "Invalid", Message = "Either you dont have permission or Invalid Credentials!" });
            }
            return response;
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

    }
}
