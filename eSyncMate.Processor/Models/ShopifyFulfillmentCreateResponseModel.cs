namespace eSyncMate.Processor.Models
{
    // fulfillmentCreate { fulfillment { id status } userErrors }
    public class ShopifyFulfillmentCreateResponseModel : ShopifyGraphQLResponseModel
    {
        public Data data { get; set; }

        public class Data
        {
            public FulfillmentCreate fulfillmentCreate { get; set; }
        }

        public class FulfillmentCreate
        {
            public Fulfillment fulfillment { get; set; }
            public List<UserError> userErrors { get; set; }

            public FulfillmentCreate()
            {
                this.userErrors = new List<UserError>();
            }
        }

        public class Fulfillment
        {
            public string id { get; set; }
            public string status { get; set; }
        }
    }
}
