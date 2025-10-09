namespace eSyncMate.Processor.Models
{
    public class MacysInventoryUploadRequestModel
    {
        public List<MacysOffer> offers { get; set; }

        public MacysInventoryUploadRequestModel() 
        {
            this.offers = new List<MacysOffer>();    
        }

        public class MacysOffer
        {
            public double price { get; set; }
            public string product_id { get; set; }
            public string product_id_type { get; set; }
            public string quantity { get; set; }
            public string shop_sku { get; set; }
            public string state_code { get; set; }
        }

    }
}
