using System.ComponentModel.DataAnnotations;

namespace eSyncMate.Processor.Models
{
    public class UserModel
    {
        public int Id { get; set; }
    }

    public class UserDataModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public bool IsSetupAllowed { get; set; }
        public string UserID { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool MFAEnabled { get; set; }
    }

    public class UpdateUserDataModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public bool IsSetupAllowed { get; set; }
        public string UserID { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public bool MFAEnabled { get; set; }
    }

    public class UserSearchModel
    {
        public string SearchOption { get; set; }
        public string SearchValue { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class UsersClaimData
    {
        public string UserType { get; set; }
        public string Customers { get; set; }
        public string Flows { get; set; }
        public string RoleName { get; set; } = string.Empty;

        public bool IsSuperAdmin => RoleName?.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) == true;
    }
}
