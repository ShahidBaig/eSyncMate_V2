namespace eSyncMate.Processor.Models
{
    public class WalmartAsnShipmentNotificationInputModel
    {
        public Ordershipment orderShipment { get; set; } = new Ordershipment();

        public class Ordershipment
        {
            public Orderlines orderLines { get; set; } = new Orderlines();
        }

        public class Orderlines
        {
            public List<Orderline> orderLine { get; set; } = new List<Orderline>();
        }

        public class Orderline
        {
            public string lineNumber { get; set; }
            public bool intentToCancelOverride { get; set; }
            public string sellerOrderId { get; set; }
            public Orderlinestatuses orderLineStatuses { get; set; } = new Orderlinestatuses();
        }

        public class Orderlinestatuses
        {
            public List<Orderlinestatu> orderLineStatus { get; set; } = new List<Orderlinestatu>();
        }

        public class Orderlinestatu
        {
            public string status { get; set; }
            public Statusquantity statusQuantity { get; set; } = new Statusquantity();
            public Trackinginfo trackingInfo { get; set; } = new Trackinginfo();
            //public Returncenteraddress returnCenterAddress { get; set; } = new Returncenteraddress();
        }

        public class Statusquantity
        {
            public string unitOfMeasurement { get; set; }
            public string amount { get; set; }
        }

        public class Trackinginfo
        {
            public long shipDateTime { get; set; }
            public Carriername carrierName { get; set; } = new Carriername();
            public string methodCode { get; set; }
            public string trackingNumber { get; set; }
            public string trackingURL { get; set; }
        }

        public class Carriername
        {
            public string carrier { get; set; }
        }

        //public class Returncenteraddress
        //{
        //    public string name { get; set; }
        //    public string address1 { get; set; }
        //    public string city { get; set; }
        //    public string state { get; set; }
        //    public string postalCode { get; set; }
        //    public string country { get; set; }
        //    public string dayPhone { get; set; }
        //    public string emailId { get; set; }
        //}
    }

}
