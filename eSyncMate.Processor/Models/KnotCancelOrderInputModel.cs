namespace eSyncMate.Processor.Models
{
    public class KnotCancelOrderInputModel
    {
        public List<KnotCancelation> cancelations { get; set; }

        public KnotCancelOrderInputModel()
        {
            this.cancelations = new List<KnotCancelation>();
        }

        public class KnotCancelation
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
