namespace eSyncMate.Processor.Models
{
    public class GetCustomersResponseModel : ResponseModel
    {
        public List<CustomerDataModel> Customers { get; set; }
        public int TotalCount { get; set; }
    }
}
