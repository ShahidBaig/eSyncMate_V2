namespace eSyncMate.Processor.Models
{
    public class LowesInventoryUploadRequestModel
    {
        public List<LowesOffer> offers { get; set; }

        public LowesInventoryUploadRequestModel() 
        {
            this.offers = new List<LowesOffer>();    
        }

        public class LowesOffer
        {
            public double price { get; set; }
            public string product_sku { get; set; }
            public string quantity { get; set; }
            public string shop_sku { get; set; }
            public string state_code { get; set; }
        }

    }
}
