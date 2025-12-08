namespace eSyncMate.Processor.Models
{
    public class KnotAsnRequestModel
    {
        public List<KnotShipment> shipments { get; set; }

        public KnotAsnRequestModel() 
        { 
            this.shipments = new List<KnotShipment>();
        }

        public class KnotShipment
        {
            public string order_id { get; set; }
            public string invoice_reference { get; set; }
            public Tracking tracking { get; set; }
            public List<KnotShipment_Lines> shipment_lines { get; set; }

            public KnotShipment()
            {
                this.shipment_lines = new List<KnotShipment_Lines>();
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

        public class KnotShipment_Lines
        {
            public string offer_sku { get; set; }
            public int quantity { get; set; }
        }

    }
}
