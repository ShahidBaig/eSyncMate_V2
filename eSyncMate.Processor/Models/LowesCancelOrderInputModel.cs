namespace eSyncMate.Processor.Models
{
    public class LowesCancelOrderInputModel
    {
        public List<LowesCancelation> cancelations { get; set; }

        public LowesCancelOrderInputModel()
        {
            this.cancelations = new List<LowesCancelation>();
        }

        public class LowesCancelation
        {
            public decimal amount { get; set; }
            public string currency_iso_code { get; set; }
            public string order_line_id { get; set; }
            public int quantity { get; set; }
            public string reason_code { get; set; }
            public int shipping_amount { get; set; }
        }
    }
}
