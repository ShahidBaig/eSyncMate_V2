using eSyncMate.Processor.Models;
using System.Data;
using System.Security.Claims;
using System.Linq;

namespace eSyncMate.Processor.Managers
{
    public class FlowsManager
    {
        public static UsersClaimData GetCustomerNames(ClaimsIdentity claimsIdentity)
        {
            UsersClaimData userData = new UsersClaimData();

            var customerNameClaim = claimsIdentity.FindFirst("customerName")?.Value;

            if (!string.IsNullOrEmpty(customerNameClaim))
            {
                string[] valuesArray = customerNameClaim.Split(',').Select(id => $"'{id.Trim()}'").ToArray();
                userData.Flows = string.Join(",", valuesArray);
            }
            
            userData.UserType = claimsIdentity.FindFirst("userType")?.Value;

            return userData;
        }
    }
}
