using Org.BouncyCastle.Bcpg;
using System.Data;

namespace eSyncMate.Processor.Models
{
    public class GetPurchaseOrderResponseModel : ResponseModel
    {
        //public List<InvFeedFromNDCDataModel> Inv { get; set; }

        public DataTable PurchaseOrders { get; set; }
        public DataTable SupplierData { get; set; }
        public DataTable ItemsData { get; set; }
        public DataTable SuppliersItemsData { get; set; }
        public DataTable DetailData { get; set; }


    }
}
