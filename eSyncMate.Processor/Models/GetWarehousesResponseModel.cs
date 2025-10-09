using System.Data;

namespace eSyncMate.Processor.Models
{
    public class GetWarehousesResponseModel : ResponseModel
    {
        //public List<WarehouseDataModel> Warehouses { get; set; }
        public DataTable Warehouses { get; set; }
    }
}
