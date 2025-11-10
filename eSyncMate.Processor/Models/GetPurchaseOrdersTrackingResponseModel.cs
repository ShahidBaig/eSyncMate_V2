using Org.BouncyCastle.Bcpg;
using System.Data;

namespace eSyncMate.Processor.Models
{
    public class GetPurchaseOrdersTrackingResponseModel : ResponseModel
    {
        //public List<InvFeedFromNDCDataModel> Inv { get; set; }

        public DataTable PurchaseOrdersTracking { get; set; }
    }
}
