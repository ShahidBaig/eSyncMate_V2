namespace eSyncMate.Processor.Models
{
    // order(id) { fulfillmentOrders { lineItems } } - which fulfillment order line items an order line can be shipped from
    public class ShopifyFulfillmentOrdersResponseModel : ShopifyGraphQLResponseModel
    {
        public Data data { get; set; }

        public class Data
        {
            public Order order { get; set; }
        }

        public class Order
        {
            public string id { get; set; }
            public string name { get; set; }
            public FulfillmentOrders fulfillmentOrders { get; set; }
        }

        public class FulfillmentOrders
        {
            public List<FulfillmentOrder> nodes { get; set; }

            public FulfillmentOrders()
            {
                this.nodes = new List<FulfillmentOrder>();
            }
        }

        public class FulfillmentOrder
        {
            public string id { get; set; }
            public string status { get; set; }
            public FulfillmentOrderLineItems lineItems { get; set; }
        }

        public class FulfillmentOrderLineItems
        {
            public List<FulfillmentOrderLineItem> nodes { get; set; }

            public FulfillmentOrderLineItems()
            {
                this.nodes = new List<FulfillmentOrderLineItem>();
            }
        }

        public class FulfillmentOrderLineItem
        {
            public string id { get; set; }
            public int remainingQuantity { get; set; }
            public int totalQuantity { get; set; }
            public ShopifyGraphQLResponseModel.IdNode lineItem { get; set; }
        }
    }
}
