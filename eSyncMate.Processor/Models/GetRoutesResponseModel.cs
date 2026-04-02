namespace eSyncMate.Processor.Models
{
    public class GetRoutesResponseModel : ResponseModel
    {
        public List<RoutesDataModel> Routes { get; set; }
        public int TotalCount { get; set; }
    }
}
