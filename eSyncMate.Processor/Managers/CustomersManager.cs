using eSyncMate.Processor.Models;
using System.Data;
using System.Security.Claims;

namespace eSyncMate.Processor.Managers
{
    public class CustomersManager
    {
        public static UsersClaimData GetCustomerNames(ClaimsIdentity claimsIdentity)
        {
            UsersClaimData userData = new UsersClaimData();

            var customerNameClaim = claimsIdentity.FindFirst("customerName")?.Value;

            string[] valuesArray = customerNameClaim.Split(',').Select(id => $"'{id.Trim()}'").ToArray();
            userData.Customers = string.Join(",", valuesArray);
            userData.UserType = claimsIdentity.FindFirst("userType")?.Value;

            return userData;
        }
    }
}
