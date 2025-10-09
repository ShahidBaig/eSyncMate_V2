namespace eSyncMate.Processor.Models
{
    public class LowesAsnRequestModel
    {
        public List<LowesShipment> shipments { get; set; }

        public LowesAsnRequestModel() 
        { 
            this.shipments = new List<LowesShipment>();
        }

        public class LowesShipment
        {
            public string order_id { get; set; }
            public string invoice_reference { get; set; }
            public Tracking tracking { get; set; }
            public List<LowesShipment_Lines> shipment_lines { get; set; }

            public LowesShipment()
            {
                this.shipment_lines = new List<LowesShipment_Lines>();
                this.tracking = new Tracking();
            }
        }

        public class Tracking
        {
            public string carrier_code { get; set; }
            public string carrier_name { get; set; }
            public string tracking_number { get; set; }
        }

        public class Shipping_From
        {
            public Warehouse warehouse { get; set; }

            public Shipping_From()
            {
                this.warehouse = new Warehouse();
            }
        }

        public class Warehouse
        {
            public string code { get; set; }
        }

        public class LowesShipment_Lines
        {
            public string offer_sku { get; set; }
            public int quantity { get; set; }
        }

    }
}
