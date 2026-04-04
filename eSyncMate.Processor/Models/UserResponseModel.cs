namespace eSyncMate.Processor.Models
{
    public class UserResponseModel : ResponseModel
    {
        public List<UserDataModel> User { get; set; }
        public int TotalCount { get; set; }
    }
}
