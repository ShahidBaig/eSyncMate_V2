namespace eSyncMate.Processor.Models
{
    public class ASNResponse
    {
        public string id { get; set; }
        public string order_id { get; set; }
        public string order_line_number { get; set; }
        public string seller_id { get; set; }
        public int quantity { get; set; }
        public DateTime shipped_date { get; set; }
        public string shipping_method { get; set; }
        public string level_of_service { get; set; }
        public string tracking_number { get; set; }
        public DateTime created { get; set; }
        public string created_by { get; set; }
        public DateTime last_modified { get; set; }
        public string last_modified_by { get; set; }
    }

}
