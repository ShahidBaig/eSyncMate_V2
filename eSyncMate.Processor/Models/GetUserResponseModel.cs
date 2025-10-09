namespace eSyncMate.Processor.Models
{
    public class GetUserResponseModel : ResponseModel
    {
        public List<LoginModel> User { get; set; }
    }
}
