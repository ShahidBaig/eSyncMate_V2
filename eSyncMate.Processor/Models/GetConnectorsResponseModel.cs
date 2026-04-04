namespace eSyncMate.Processor.Models
{
    public class GetConnectorsResponseModel : ResponseModel
    {
        public List<ConnectorsDataModel> Connectors { get; set; }
        public int TotalCount { get; set; }
    }
}
