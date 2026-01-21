namespace eSyncMate.Processor.Models
{
    public class AmazonInventoryFeedPatchWiseModel
    {
        public Header header { get; set; }
        public List<PatchMessage> messages { get; set; }

        public AmazonInventoryFeedPatchWiseModel()
        {
            this.messages = new List<PatchMessage>();
            this.header = new Header();
        }


        public class Header
        {
            public string sellerId { get; set; }
            public string version { get; set; }
            public string issueLocale { get; set; }
        }

        public class PatchMessage
        {
            public Int64 messageId { get; set; }
            public string sku { get; set; }
            public string operationType { get; set; }
            public string productType { get; set; }
            public List<Patch> patches { get; set; }
            public PatchMessage()
            {
                this.patches = new List<Patch>();
            }   
        }

        public class Patch
        {
            public string op { get; set; }
            public string path { get; set; }
            public List<PatchValue> value { get; set; }
            public Patch()
            {
                this.value = new List<PatchValue>();
            }
        }

        public class PatchValue
        {
            public string fulfillment_channel_code { get; set; }
            public Int64 quantity { get; set; }
            public int lead_time_to_ship_max_days { get; set; }

        }


    }
}
