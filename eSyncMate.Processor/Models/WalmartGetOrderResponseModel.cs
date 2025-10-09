namespace eSyncMate.Processor.Models
{
    public class WalmartGetOrderResponseModel
    {
        public List list { get; set; }

        public class List
        {
            public Meta meta { get; set; }
            public Elements elements { get; set; }
        }

        public class Meta
        {
            public int totalCount { get; set; }
            public int limit { get; set; }
            public string nextCursor { get; set; }
        }

        public class Elements
        {
            public List<WalmartOrder> order { get; set; }

            public Elements()
            {
                this.order = new List<WalmartOrder>();
            }
        }

        public class WalmartOrder
        {
            public string purchaseOrderId { get; set; }
            public string customerOrderId { get; set; }
            public string customerEmailId { get; set; }
            public long orderDate { get; set; }
            public Shippinginfo shippingInfo { get; set; }
            public Orderlines orderLines { get; set; }
            public Shipnode shipNode { get; set; }
        }

        public class Shippinginfo
        {
            public string phone { get; set; }
            public long estimatedDeliveryDate { get; set; }
            public long estimatedShipDate { get; set; }
            public string methodCode { get; set; }
            public Postaladdress postalAddress { get; set; }
        }

        public class Postaladdress
        {
            public string name { get; set; }
            public string address1 { get; set; }
            public string address2 { get; set; }
            public string city { get; set; }
            public string state { get; set; }
            public string postalCode { get; set; }
            public string country { get; set; }
            public string addressType { get; set; }
        }

        public class Orderlines
        {
            public List<Orderline> orderLine { get; set; }

            public Orderlines()
            {
                this.orderLine = new List<Orderline>(); 
            }
        }

        public class Orderline
        {
            public string lineNumber { get; set; }
            public Item item { get; set; }
            public Charges charges { get; set; }
            public Orderlinequantity orderLineQuantity { get; set; }
            public long statusDate { get; set; }
            public Orderlinestatuses orderLineStatuses { get; set; }
            public object refund { get; set; }
            public Fulfillment fulfillment { get; set; }
        }

        public class Item
        {
            public string productName { get; set; }
            public string sku { get; set; }
            public string condition { get; set; }
        }

        public class Charges
        {
            public List<Charge> charge { get; set; }

            public Charges()
            {
                this.charge = new List<Charge>();
            }
        }

        public class Charge
        {
            public string chargeType { get; set; }
            public string chargeName { get; set; }
            public Chargeamount chargeAmount { get; set; }
            public Tax tax { get; set; }
        }

        public class Chargeamount
        {
            public string currency { get; set; }
            public float amount { get; set; }
        }

        public class Tax
        {
            public string taxName { get; set; }
            public Taxamount taxAmount { get; set; }
        }

        public class Taxamount
        {
            public string currency { get; set; }
            public float amount { get; set; }
        }

        public class Orderlinequantity
        {
            public string unitOfMeasurement { get; set; }
            public string amount { get; set; }
        }

        public class Orderlinestatuses
        {
            public List<Orderlinestatu> orderLineStatus { get; set; }

            public Orderlinestatuses()
            {
                this.orderLineStatus = new List<Orderlinestatu>();  
            }
        }

        public class Orderlinestatu
        {
            public string status { get; set; }
            public object subSellerId { get; set; }
            public Statusquantity statusQuantity { get; set; }
            public object cancellationReason { get; set; }
            public object trackingInfo { get; set; }
            public object returnCenterAddress { get; set; }
        }

        public class Statusquantity
        {
            public string unitOfMeasurement { get; set; }
            public string amount { get; set; }
        }

        public class Fulfillment
        {
            public string fulfillmentOption { get; set; }
            public string shipMethod { get; set; }
            public object storeId { get; set; }
            public long pickUpDateTime { get; set; }
            public object pickUpBy { get; set; }
            public string shippingProgramType { get; set; }
        }

        public class Shipnode
        {
            public string type { get; set; }
        }
    }
}
