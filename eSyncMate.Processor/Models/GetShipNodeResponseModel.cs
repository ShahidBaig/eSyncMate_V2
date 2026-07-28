namespace eSyncMate.Processor.Models
{
    public class GetShipNodeResponseModel : ResponseModel
    {
        public List<ShipNodeDataModel> ShipNodes { get; set; }
        public int TotalCount { get; set; }
    }

    public class GetWarehouseListResponseModel : ResponseModel
    {
        public List<string> Warehouses { get; set; }
    }

    /// <summary>Customers that already have ship nodes — used by the list screen filter.</summary>
    public class GetShipNodeCustomersResponseModel : ResponseModel
    {
        public List<string> Customers { get; set; }
    }
}
