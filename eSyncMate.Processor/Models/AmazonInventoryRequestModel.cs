namespace eSyncMate.Processor.Models
{
    public class AmazonInventoryRequestModel
    {
        public Header header { get; set; }
        public List<AmazonMessage> messages { get; set; }

        public AmazonInventoryRequestModel()
        {
            this.messages = new List<AmazonMessage>();
            this.header = new Header();

        }

        public class Header
        {
            public string sellerId { get; set; }
            public string version { get; set; }
            public string issueLocale { get; set; }
        }

        public class AmazonMessage
        {
            public int messageId { get; set; }
            public string sku { get; set; }
            public string operationType { get; set; }
            public string productType { get; set; }
            public AmazonAttributes attributes { get; set; }

            public AmazonMessage()
            {
                this.attributes = new AmazonAttributes();
            }
        }

        public class AmazonAttributes
        {
            public List<Fulfillment_Availability> fulfillment_availability { get; set; }

            public AmazonAttributes() 
            {
                this.fulfillment_availability = new List<Fulfillment_Availability>();
            }
        }

        public class Fulfillment_Availability
        {
            public string fulfillment_channel_code { get; set; }
            public Int64 quantity { get; set; }
            public int lead_time_to_ship_max_days { get; set; }
        }

    }
}
