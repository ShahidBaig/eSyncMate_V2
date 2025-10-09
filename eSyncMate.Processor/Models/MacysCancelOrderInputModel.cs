namespace eSyncMate.Processor.Models
{
    public class MacysCancelOrderInputModel
    {
        public List<Cancelation> cancelations { get; set; }

        public MacysCancelOrderInputModel()
        {
            this.cancelations = new List<Cancelation>();
        }

        public class Cancelation
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
