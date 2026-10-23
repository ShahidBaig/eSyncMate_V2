namespace eSyncMate.Processor.Models
{
    // order(id) { cancelledAt lineItems { currentQuantity unfulfilledQuantity } } - what a cancellation can still change
    public class ShopifyOrderLinesResponseModel : ShopifyGraphQLResponseModel
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
            public string cancelledAt { get; set; }
            public string displayFulfillmentStatus { get; set; }
            public LineItems lineItems { get; set; }
        }

        public class LineItems
        {
            public List<LineItem> nodes { get; set; }

            public LineItems()
            {
                this.nodes = new List<LineItem>();
            }
        }

        public class LineItem
        {
            public string id { get; set; }
            public int currentQuantity { get; set; }
            public int unfulfilledQuantity { get; set; }
        }
    }
}
