using System.ComponentModel.DataAnnotations;

namespace eSyncMate.Processor.Models
{
    public class LoginModel
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
    }
}
