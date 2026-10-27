namespace eSyncMate.Processor.Models
{
    // orders(query: updated_at ...) { legacyResourceId } and nodes(ids) { ... on Order } - ShopifyGetCancelledOrdersRoute
    public class ShopifyCancelledOrdersResponseModel : ShopifyGraphQLResponseModel
    {
        public Data data { get; set; }

        public class Data
        {
            public Orders orders { get; set; }
            public List<Order> nodes { get; set; }
        }

        public class Orders
        {
            public List<Order> nodes { get; set; }
            public ShopifyInventoryItemsResponseModel.PageInfo pageInfo { get; set; }

            public Orders()
            {
                this.nodes = new List<Order>();
            }
        }

        public class Order
        {
            public string id { get; set; }
            public string legacyResourceId { get; set; }
            public string name { get; set; }
            // kept as Shopify's text (DateParseHandling.None in ShopifyGraphQL)
            public string cancelledAt { get; set; }
            public string cancelReason { get; set; }
            public string displayFinancialStatus { get; set; }
            public string displayFulfillmentStatus { get; set; }
            public LineItems lineItems { get; set; }
        }

        public class LineItems
        {
            public List<LineItem> nodes { get; set; }
            public ShopifyInventoryItemsResponseModel.PageInfo pageInfo { get; set; }

            public LineItems()
            {
                this.nodes = new List<LineItem>();
            }
        }

        public class LineItem
        {
            public string id { get; set; }
            public int quantity { get; set; }
            public int currentQuantity { get; set; }
            public int unfulfilledQuantity { get; set; }
        }
    }
}
