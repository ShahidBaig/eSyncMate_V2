namespace eSyncMate.Processor.Models
{
    public class MacysAsnRequestModel
    {
        public List<MacysShipment> shipments { get; set; }

        public MacysAsnRequestModel() 
        { 
            this.shipments = new List<MacysShipment>();
        }

        public class MacysShipment
        {
            public string order_id { get; set; }
            public string invoice_reference { get; set; }
            public Tracking tracking { get; set; }
            public List<MacysShipment_Lines> shipment_lines { get; set; }
            //public bool shipped { get; set; }

            public MacysShipment()
            {
                this.shipment_lines = new List<MacysShipment_Lines>();
                this.tracking = new Tracking();
            }
        }

        public class Tracking
        {
            public string carrier_code { get; set; }
            //public string carrier_standard_code { get; set; }
            public string carrier_name { get; set; }
            public string tracking_number { get; set; }
            //public string tracking_url { get; set; }
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

        public class MacysShipment_Lines
        {
            public string offer_sku { get; set; }
            //public string package_reference { get; set; }
            public int quantity { get; set; }
        }

    }
}
