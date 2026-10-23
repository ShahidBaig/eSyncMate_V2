namespace eSyncMate.Processor.Models
{
    // orderEditBegin / orderEditSetQuantity / orderEditCommit - one call fills one of the three
    public class ShopifyOrderEditResponseModel : ShopifyGraphQLResponseModel
    {
        public Data data { get; set; }

        public class Data
        {
            public OrderEdit orderEditBegin { get; set; }
            public OrderEdit orderEditSetQuantity { get; set; }
            public OrderEditCommit orderEditCommit { get; set; }
        }

        public class OrderEdit
        {
            public CalculatedOrder calculatedOrder { get; set; }
            public List<UserError> userErrors { get; set; }

            public OrderEdit()
            {
                this.userErrors = new List<UserError>();
            }
        }

        public class OrderEditCommit
        {
            public ShopifyGraphQLResponseModel.IdNode order { get; set; }
            public List<UserError> userErrors { get; set; }

            public OrderEditCommit()
            {
                this.userErrors = new List<UserError>();
            }
        }

        public class CalculatedOrder
        {
            public string id { get; set; }
            public CalculatedLineItems lineItems { get; set; }
        }

        public class CalculatedLineItems
        {
            public List<CalculatedLineItem> nodes { get; set; }

            public CalculatedLineItems()
            {
                this.nodes = new List<CalculatedLineItem>();
            }
        }

        public class CalculatedLineItem
        {
            public string id { get; set; }
            public int quantity { get; set; }
            public int editableQuantity { get; set; }
        }
    }
}
