namespace eSyncMate.Processor.Models
{
    public class GetRouteTypeResponseModel : ResponseModel
    {
        public List<RouteTypeDataModel> RouteType { get; set; }
        public int TotalCount { get; set; }
    }
}
