using System.Data;

namespace eSyncMate.Processor.Models
{
    public class GetRouteExceptionsResponseModel : ResponseModel
    {
        public List<RouteLogDataModel> RouteExceptions { get; set; }
        public DataTable RouteExceptionsData { get; set; }

    }
}
