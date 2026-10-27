namespace eSyncMate.Processor.Models
{
    // fulfillmentCreate(fulfillment: FulfillmentInput!) variables - one fulfillment per tracking number
    public class ShopifyFulfillmentCreateRequestModel
    {
        public Fulfillment fulfillment { get; set; }

        public ShopifyFulfillmentCreateRequestModel()
        {
            this.fulfillment = new Fulfillment();
        }

        public class Fulfillment
        {
            public List<LineItemsByFulfillmentOrder> lineItemsByFulfillmentOrder { get; set; }
            public TrackingInfo trackingInfo { get; set; }
            public bool notifyCustomer { get; set; }

            public Fulfillment()
            {
                this.lineItemsByFulfillmentOrder = new List<LineItemsByFulfillmentOrder>();
                this.trackingInfo = new TrackingInfo();
            }
        }

        public class LineItemsByFulfillmentOrder
        {
            public string fulfillmentOrderId { get; set; }
            public List<FulfillmentOrderLineItem> fulfillmentOrderLineItems { get; set; }

            public LineItemsByFulfillmentOrder()
            {
                this.fulfillmentOrderLineItems = new List<FulfillmentOrderLineItem>();
            }
        }

        public class FulfillmentOrderLineItem
        {
            public string id { get; set; }
            public int quantity { get; set; }
        }

        public class TrackingInfo
        {
            public string company { get; set; }
            public string number { get; set; }
        }
    }
}
