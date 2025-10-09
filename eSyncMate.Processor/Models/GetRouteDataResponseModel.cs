using System.Data;

namespace eSyncMate.Processor.Models
{
    public class GetRouteDataResponseModel : ResponseModel
    {
        public List<RouteDataModel> RouteData { get; set; }

        public DataTable RouteDatatable { get; set; }
    }

    public class GetinvFeedModel : ResponseModel
    {
        public List<RouteDataModel> RouteData { get; set; }

        public DataTable GetinvFeedDatatable { get; set; }
    }
}
