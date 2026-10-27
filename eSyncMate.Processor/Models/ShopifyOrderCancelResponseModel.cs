namespace eSyncMate.Processor.Models
{
    // orderCancel { job { id done } orderCancelUserErrors } - Shopify cancels the order in the background (job)
    public class ShopifyOrderCancelResponseModel : ShopifyGraphQLResponseModel
    {
        public Data data { get; set; }

        public class Data
        {
            public OrderCancel orderCancel { get; set; }
            // job(id) { done } - checking whether the background cancel has finished
            public Job job { get; set; }
        }

        public class OrderCancel
        {
            public Job job { get; set; }
            public List<UserError> orderCancelUserErrors { get; set; }

            public OrderCancel()
            {
                this.orderCancelUserErrors = new List<UserError>();
            }
        }

        public class Job
        {
            public string id { get; set; }
            public bool done { get; set; }
        }
    }
}
