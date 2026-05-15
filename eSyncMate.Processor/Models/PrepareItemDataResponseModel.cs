using System.Data;

namespace eSyncMate.Processor.Models
{
    public class PrepareItemDataResponseModel : ResponseModel
    {
        public DataTable ItemDataResponseDatatable { get; set; }
    }

    public class DeleteItemsDataRequest
    {
        public int UserID { get; set; }
        public string CustomerID { get; set; }
        public string ItemType { get; set; }
    }
}
