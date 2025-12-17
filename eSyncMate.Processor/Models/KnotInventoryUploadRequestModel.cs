namespace eSyncMate.Processor.Models
{
    public class KnotInventoryUploadRequestModel
    {
        public List<KnotOffer> offers { get; set; }

        public KnotInventoryUploadRequestModel() 
        {
            this.offers = new List<KnotOffer>();    
        }

        public class KnotOffer
        {
            public double price { get; set; }
            //public string product_sku { get; set; }
            public string quantity { get; set; }
            public string shop_sku { get; set; }
            public string state_code { get; set; }
        }

    }
}
