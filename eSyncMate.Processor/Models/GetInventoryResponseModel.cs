using System.Data;

namespace eSyncMate.Processor.Models
{
    public class GetInventoryResponseModel : ResponseModel
    {
        //public List<InventoryDataModel> Inventory { get; set; }
        public DataTable Inventory { get; set; }
        public DataTable BatchWiseInventory { get; set; }
        public DataTable RouteType { get; set; }
    }
    public class GetInventoryFilesResponseModel : ResponseModel
    {
        public List<InventoryFileModel> Files { get; set; }
    }
}
