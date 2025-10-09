namespace eSyncMate.Processor.Models
{
    public class SCS_SAPrductModel
    {
        public string external_id { get; set; }
        public string relationship_type { get; set; }
        public string seller_id { get; set; }
        public List<Field> fields { get; set; }
        public SCS_SAPrductModel()
        {
            this.fields = new List<Field>();
        }

        public class Field
        {
            public string name { get; set; }
            public string value { get; set; }
        }

    }


    public class SCSProductsResponse
    {
        public List<ProductResult> results { get; set; }


        public SCSProductsResponse() 
        {
            this.results = new List<ProductResult>();
        }
    }

    public class ProductResult
    {
        public string external_id { get; set; }
        public int status { get; set; }
        public Product product { get; set; }
        public string reason { get; set; }
    }

    public class Product
    {
        public string id { get; set; }
        public string external_id { get; set; }
        public string relationship_type { get; set; }
        public string seller_id { get; set; }
        public List<ProductField> fields { get; set; }
        public List<Quantity> quantities { get; set; }
        public string tcin { get; set; }
        public string item_type_id { get; set; }
        public DateTime created { get; set; }
        public string created_by { get; set; }
        public DateTime last_modified { get; set; }
        public string last_modified_by { get; set; }
        public bool previously_approved { get; set; }
        public List<Product_Statuses> product_statuses { get; set; }


        public Product()
        {
            this.fields = new List<ProductField>();
            this.quantities = new List<Quantity>();
            this.product_statuses = new List<Product_Statuses>();
        }
    }

    public class ProductField
    {
        public string name { get; set; }
        public string value { get; set; }
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
        public List<Error> errors { get; set; }
        public string validation_status { get; set; }
        public bool is_changed { get; set; }
        public DateTime created { get; set; }
        public string created_by { get; set; }
        public DateTime last_modified { get; set; }
        public string last_modified_by { get; set; }

        public Product_Statuses() 
        {
            this.errors = new List<Error>();    
        }

    }

    public class Error
    {
        public string category { get; set; }
        public string reason { get; set; }
        public string type { get; set; }
        public int error_code { get; set; }
    }

}
