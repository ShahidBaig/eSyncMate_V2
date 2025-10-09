namespace eSyncMate.Processor.Models
{
    public class DCOrders
    {
        public string DC { get; set; }
        public string SONo { get; set; }
        public string CustomerPO { get; set; }
        public string ShipDate { get; set; }
        public string OrderDate { get; set; }
        public string TotalCarton { get; set; }
        public string TotalCartonWeight { get; set; }
        public string Carrier { get; set; }
        public string VendorID { get; set; }
        public string BOL { get; set; }
        public List<Store> Stores { get; set; }
    }

    public class Store
    {
        public string StoreNo { get; set; }
        public string Dept { get; set; }
        public string TotalCarton { get; set; }
        public string TotalCartonWeight { get; set; }
        public List<StoreCarton> Cartons { get; set; }
    }

    public class StoreCarton
    {
        public string TrackingNo { get; set; }
        public List<CartonItem> Items { get; set; }
    }

    public class CartonItem
    {
        public string VendorStyle { get; set; }
        public string UPC { get; set; }
        public string Qty { get; set; }
    }
}
