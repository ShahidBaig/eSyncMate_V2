using eSyncMate.DB.Entities;


namespace eSyncMate.Processor.Models
{
    public class OrderResponseModel : ResponseModel
    {
        public Customers Customer { get; set; }
        public Orders Order { get; set; }
        public List<OrderModel> Orders { get; set; }
    }

    public class OrderStoreResponseModel : ResponseModel
    {
        public int OrderId { get; set; }
        public string Status { get; set; }
    }

    public class OrderObjectModel 
    {
        public Customers Customer { get; set; }
        public Orders Order { get; set; }
    }
}
