namespace eSyncMate.Processor.Models
{
    public class SCS_ProductCatalogStatusResponseModel
    {
        public string id { get; set; }
        public string external_id { get; set; }
        public string relationship_type { get; set; }
        public string seller_id { get; set; }
        public List<Quantity> quantities { get; set; }
        public Price price { get; set; }
        public string tcin { get; set; }
        public string item_type_id { get; set; }
        public DateTime created { get; set; }
        public string created_by { get; set; }
        public DateTime last_modified { get; set; }
        public string last_modified_by { get; set; }
        public bool previously_approved { get; set; }
        public List<ProductStatus> product_statuses { get; set; }

        public class Quantity
        {
            public int quantity { get; set; }
            public string distribution_center_id { get; set; }
            public DateTime last_modified { get; set; }
            public string last_modified_by { get; set; }
        }

        public class Price
        {
            public double list_price { get; set; }
            public double offer_price { get; set; }
            public double map_price { get; set; }
            public DateTime last_modified { get; set; }
        }

        public class Error
        {
            public string category { get; set; }
            public string reason { get; set; }
            public string type { get; set; }
            public int error_code { get; set; }
            public string field_name { get; set; }
            public string error_severity { get; set; }
        }

        public class ProductStatus
        {
            public string id { get; set; }
            public int version { get; set; }
            public bool current { get; set; }
            public bool latest { get; set; }
            public string listing_status { get; set; }
            public List<Error> errors { get; set; }
            public string validation_status { get; set; }
            public bool is_changed { get; set; }
            public DateTime created { get; set; }
            public string created_by { get; set; }
            public DateTime last_modified { get; set; }
            public string last_modified_by { get; set; }
        }
    }

    public class ProductCreateResponse
    {
        public string message { get; set; }
        public string[] errors { get; set; }

    }
}
