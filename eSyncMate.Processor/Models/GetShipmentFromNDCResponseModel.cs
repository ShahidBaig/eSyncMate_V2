using System.Data;

namespace eSyncMate.Processor.Models
{
    public class GetShipmentFromNDCResponseModel : ResponseModel
    {
        //public List<InventoryDataModel> Inventory { get; set; }
        public DataTable ShipmentFromNDC { get; set; }
        public DataTable ShipmentDetailFromNDC { get; set; }
    }
    //public class GetInventoryFilesResponseModel : ResponseModel
    //{
    //    public List<InventoryFileModel> Files { get; set; }
    //}
}
