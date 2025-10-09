using System.Data;

namespace eSyncMate.Processor.Models
{
    public class SalesInvoiceNDCResponseModel : ResponseModel
    {
        //public List<InventoryDataModel> Inventory { get; set; }
        public DataTable SalesInvoiceNDC { get; set; }
        public DataTable SalesInvoiceDetailNDC { get; set; }
    }
    //public class GetInventoryFilesResponseModel : ResponseModel
    //{
    //    public List<InventoryFileModel> Files { get; set; }
    //}
}
