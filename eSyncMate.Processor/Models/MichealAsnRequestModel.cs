namespace eSyncMate.Processor.Models
{
    public class MichealAsnRequestModel
    {
        public string orderNumber { get; set; }
        public List<Shipmentslist> shipmentsList { get; set; }

        public MichealAsnRequestModel() 
        { 
            this.shipmentsList = new List<Shipmentslist>();    
        }

        public class Shipmentslist
        {
            public string trackingNumber { get; set; }
            public string carrier { get; set; }
            public List<Shipmentitemlist> shipmentItemList { get; set; }
            
            public Shipmentslist()
            {
                this.shipmentItemList = new List<Shipmentitemlist>();
            }
        }

        public class Shipmentitemlist
        {
            public int quantity { get; set; }
            public string orderItemId { get; set; }
        }


    }
}
