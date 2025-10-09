using System.Data;

namespace eSyncMate.Processor.Models
{
    public class GetRouteLogResponseModel : ResponseModel
    {
        public List<RouteLogDataModel> RouteLog { get; set; }

        public DataTable RouteLogData { get; set; }

        public DataTable VeeqoSaleOrdersDetailData { get; set; }

    }

    public class GetinvFeedFromNDCResponseModel : ResponseModel
    {
        public List<GetinvFeedFromNDCDataModel> invFeedFromNDCLog { get; set; }

        public DataTable invFeedFromNDCLogData { get; set; }


        public DataTable VeeqoSaleOrdersDetailData { get; set; }

    }
}
