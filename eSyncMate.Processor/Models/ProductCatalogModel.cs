namespace eSyncMate.Processor.Models
{
    public class ProductCatalogModel
    {
        public class AllItems
        {
            public TargetItems[] items { get; set; }
        }

        public class TargetItems
        {
            public string id { get; set; }
            public string external_id { get; set; }
            public string relationship_type { get; set; }
            public string parent_id { get; set; }
            public string seller_id { get; set; }
            public Quantity[] quantities { get; set; }
            public Price price { get; set; }
            public string tcin { get; set; }
            public string item_type_id { get; set; }
            public DateTime created { get; set; }
            public string created_by { get; set; }
            public DateTime last_modified { get; set; }
            public string last_modified_by { get; set; }
            public bool previously_approved { get; set; }
            public Product_Statuses[] product_statuses { get; set; }
            public Field[] fields { get; set; }
        }

        public class Field
        {
            public string name { get; set; }
            public string value { get; set; }
        }

        public class Price
        {
            public float list_price { get; set; }
            public float offer_price { get; set; }
            public DateTime last_modified { get; set; }
        }

        public class Quantity
        {
            public int quantity { get; set; }
            public string distribution_center_id { get; set; }
            public DateTime last_modified { get; set; }
            public string last_modified_by { get; set; }
        }

        public class Product_Statuses
        {
            public string id { get; set; }
            public int version { get; set; }
            public bool current { get; set; }
            public bool latest { get; set; }
            public string listing_status { get; set; }
            public Error[] errors { get; set; }
            public string parent_status_id { get; set; }
            public string validation_status { get; set; }
            public bool is_changed { get; set; }
            public DateTime created { get; set; }
            public string created_by { get; set; }
            public DateTime last_modified { get; set; }
            public string last_modified_by { get; set; }
        }

        public class Error
        {
            public string category { get; set; }
            public string reason { get; set; }
            public string type { get; set; }
            public int error_code { get; set; }
            public string field_name { get; set; }
            public string error_severity { get; set; }
            public string partner_action { get; set; }
        }

        public class OrderResponse
        {
            public string order_line_number { get; set; }
        }
    }
}
