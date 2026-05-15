using API.DataAccess;
using API.Models;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IDataAccess library;
        private readonly IConfiguration configuration;
        public UserController(IDataAccess library, IConfiguration configuration = null)
        {
            this.library = library;
            this.configuration = configuration;
        }

        [HttpPost("CreateAccount")]
        public IActionResult CreateAccount(User user)
        {
            if (!library.IsEmailAvailable(user.Email))
            {
                return Ok("Email is not available!");
            }
            user.CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            user.UserType = UserType.USER;
            library.CreateUser(user);
            return Ok("Account created successfully!");
        }

        [HttpGet("Login")]
        public IActionResult Login(string email, string password)
        {
            if (library.AuthenticateUser(email, password, out User? user))
            {
                if (user != null)
                {
                    var jwt = new Jwt(configuration["Jwt:Key"], configuration["Jwt:Duration"]);
                    var token = jwt.GenerateToken(user);
                    return Ok(token);
                }
            }
            return Ok("Invalid");
        }

        [HttpGet("GetAllUsers")]
        public IActionResult GetAllUsers()
        {
            var users = library.GetUsers();
            var result = users.Select(user => new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Mobile,
                user.Blocked,
                user.Active,
                user.CreatedOn,
                user.UserType,
                user.Fine
            });
            return Ok(result);
        }

        [HttpPost("ChangeBlockStatus")]
        public IActionResult ChangeBlockStatus([FromBody] UserStatusRequest request)
        {
            if (request.Status == 1)
            {
                library.BlockUser(request.Id);
            }
            else
            {
                library.UnblockUser(request.Id);
            }
            return Ok("success");
        }

        [HttpPost("ChangeEnableStatus")]
        public IActionResult ChangeEnableStatus([FromBody] UserStatusRequest request)
        {
            if (request.Status == 1)
            {
                library.ActivateUser(request.Id);
            }
            else
            {
                library.DeactivateUser(request.Id);
            }
            return Ok("success");
        }
    }
}
