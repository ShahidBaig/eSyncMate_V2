namespace eSyncMate.Processor.Models
{
    // Parts every Shopify GraphQL Admin API response carries; each response model adds its own "data"
    public class ShopifyGraphQLResponseModel
    {
        public List<Error> errors { get; set; }
        public Extensions extensions { get; set; }

        public ShopifyGraphQLResponseModel()
        {
            this.errors = new List<Error>();
        }

        public class Error
        {
            public string message { get; set; }
            public ErrorExtensions extensions { get; set; }
        }

        public class ErrorExtensions
        {
            public string code { get; set; }
        }

        public class Extensions
        {
            public Cost cost { get; set; }
        }

        public class Cost
        {
            public double requestedQueryCost { get; set; }
            public double actualQueryCost { get; set; }
            public ThrottleStatus throttleStatus { get; set; }
        }

        public class ThrottleStatus
        {
            public double maximumAvailable { get; set; }
            public double currentlyAvailable { get; set; }
            public double restoreRate { get; set; }
        }

        public class UserError
        {
            public string code { get; set; }
            public List<string> field { get; set; }
            public string message { get; set; }
        }

        public class IdNode
        {
            public string id { get; set; }
        }
    }
}
