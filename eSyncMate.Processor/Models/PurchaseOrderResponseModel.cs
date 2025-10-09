namespace eSyncMate.Processor.Models
{
    public class PurchaseOrderResponseModel
    {

        public List<PurchasesOrders> PurchaseOrder { get; set; }

        public PurchaseOrderResponseModel()
        {
            this.PurchaseOrder = new List<PurchasesOrders>();    
        }

        public class PurchasesOrders
        {
            public int id { get; set; }
            public string number { get; set; }
            public object reference_number { get; set; }
            public object user_id { get; set; }
            public int supplier_id { get; set; }
            public int destination_warehouse_id { get; set; }
            public string state { get; set; }
            public int created_by_id { get; set; }
            public object updated_by_id { get; set; }
            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }
            public DateTime expected_date { get; set; }
            public int estimated_delivery_days { get; set; }
            public string currency_code { get; set; }
            public float currency_rate { get; set; }
            public string shipping_address_line_1 { get; set; }
            public string shipping_address_line_2 { get; set; }
            public string shipping_address_town { get; set; }
            public string shipping_address_state { get; set; }
            public string shipping_address_postcode { get; set; }
            public string shipping_address_country { get; set; }
            public string billing_address_line_1 { get; set; }
            public string billing_address_line_2 { get; set; }
            public string billing_address_town { get; set; }
            public string billing_address_state { get; set; }
            public string billing_address_postcode { get; set; }
            public string billing_address_country { get; set; }
            public int product_variants_count { get; set; }
            public int received_product_variants_count { get; set; }
            public object note { get; set; }
            public int units_ordered { get; set; }
            public int units_received { get; set; }
            public float subtotal { get; set; }
            public float total_tax { get; set; }
            public float shipping_and_handling { get; set; }
            public float total_including_tax { get; set; }
            public float total_excluding_tax { get; set; }
            public object supplier_report_format { get; set; }
            public object sent_at { get; set; }
            public object received_at { get; set; }
            public Supplier supplier { get; set; }
            public Destination_Warehouse destination_warehouse { get; set; }
            public Created_By created_by { get; set; }
            public List<Purchase_Order_Product_Variants> purchase_order_product_variants { get; set; }

            public PurchasesOrders()
            {
                this.purchase_order_product_variants = new List<Purchase_Order_Product_Variants>();
            }
        }

        public class Supplier
        {
            public int id { get; set; }
            public string name { get; set; }
            public string address_line_1 { get; set; }
            public string address_line_2 { get; set; }
            public string city { get; set; }
            public string region { get; set; }
            public string country { get; set; }
            public string post_code { get; set; }
            public string sales_contact_name { get; set; }
            public string sales_contact_email { get; set; }
            public object sales_phone_number { get; set; }
            public string accounting_contact_name { get; set; }
            public string accounting_contact_email { get; set; }
            public object accounting_phone_number { get; set; }
            public string currency_code { get; set; }
            public int created_by_id { get; set; }
            public object updated_by_id { get; set; }
            public object deleted_at { get; set; }
            public object deleted_by_id { get; set; }
            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }
            public object bank_name { get; set; }
            public object bank_account_number { get; set; }
            public object bank_sort_code { get; set; }
            public float credit_limit { get; set; }
            public int active_purchase_order_count { get; set; }
            public int completed_purchase_order_count { get; set; }
            public string purchase_order_template { get; set; }
            public string reminder_email_template { get; set; }
        }

        public class Destination_Warehouse
        {
            public string address_line_1 { get; set; }
            public object address_line_2 { get; set; }
            public string city { get; set; }
            public object click_and_collect_days { get; set; }
            public bool click_and_collect_enabled { get; set; }
            public string country { get; set; }
            public DateTime created_at { get; set; }
            public int created_by_id { get; set; }
            public int default_min_reorder { get; set; }
            public object deleted_at { get; set; }
            public object deleted_by_id { get; set; }
            public int id { get; set; }
            public string inventory_type_code { get; set; }
            public string name { get; set; }
            public string trading_name { get; set; }
            public string phone { get; set; }
            public string post_code { get; set; }
            public string region { get; set; }
            public object tax_id { get; set; }
            public object eori_id { get; set; }
            public object ioss_id { get; set; }
            public int display_position { get; set; }
            public DateTime updated_at { get; set; }
            public object updated_by_id { get; set; }
            public object requested_carrier_account { get; set; }
            public string letterbox_address_status { get; set; }
            public object cut_off_times { get; set; }
            public object cut_off_times_enabled { get; set; }
        }

        public class Created_By
        {
            public int id { get; set; }
            public string login { get; set; }
            public string email { get; set; }
        }

        public class Purchase_Order_Product_Variants
        {
            public int id { get; set; }
            public int purchase_order_id { get; set; }
            public int product_variant_id { get; set; }
            public int supplier_id { get; set; }
            public int created_by_id { get; set; }
            public object updated_by_id { get; set; }
            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }
            public int quantity { get; set; }
            public int received { get; set; }
            public object received_at { get; set; }
            public float cost { get; set; }
            public float tax_rate { get; set; }
            public float total_amount_excluding_tax { get; set; }
            public float total_amount_including_tax { get; set; }
            public bool shipped { get; set; }
            public Supplier_Product_Variant supplier_product_variant { get; set; }
        }

        public class Supplier_Product_Variant
        {
            public int id { get; set; }
            public Supplier1 supplier { get; set; }
            public float cost { get; set; }
            public string reference_number { get; set; }
            public bool default_supplier { get; set; }
            public object tax_rate { get; set; }
            public float average_cost { get; set; }
            public string title { get; set; }
            public bool has_active_purchase_order { get; set; }
            public int created_by_id { get; set; }
            public object updated_by_id { get; set; }
            public int supplier_id { get; set; }
            public int product_variant_id { get; set; }
            public int lead_time { get; set; }
            public Product_Variant product_variant { get; set; }
        }

        public class Supplier1
        {
            public int id { get; set; }
            public string name { get; set; }
            public string address_line_1 { get; set; }
            public string address_line_2 { get; set; }
            public string city { get; set; }
            public string region { get; set; }
            public string country { get; set; }
            public string post_code { get; set; }
            public string sales_contact_name { get; set; }
            public string sales_contact_email { get; set; }
            public string accounting_contact_name { get; set; }
            public string accounting_contact_email { get; set; }
            public string credit_limit { get; set; }
            public string currency_code { get; set; }
            public int created_by_id { get; set; }
            public object updated_by_id { get; set; }
            public object deleted_at { get; set; }
            public object deleted_by_id { get; set; }
            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }
            public object sales_phone_number { get; set; }
            public object accounting_phone_number { get; set; }
            public object bank_account_number { get; set; }
            public object bank_sort_code { get; set; }
            public object bank_name { get; set; }
            public string purchase_order_template { get; set; }
            public string reminder_email_template { get; set; }
            public int company_id { get; set; }
        }

        public class Product_Variant
        {
            public int awaiting_stock_orders_count { get; set; }
            public int backorder_quantity { get; set; }
            public int total_quantity_sold { get; set; }
            public bool requires_review { get; set; }
            public int allocated_stock_level_at_all_warehouses { get; set; }
            public int id { get; set; }
            public string type { get; set; }
            public string title { get; set; }
            public string sku_code { get; set; }
            public string upc_code { get; set; }
            public string model_number { get; set; }
            public decimal price { get; set; }
            public float cost_price { get; set; }
            public int min_reorder_level { get; set; }
            public int quantity_to_reorder { get; set; }
            public int created_by_id { get; set; }
            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }
            public object deleted_at { get; set; }
            public float weight_grams { get; set; }
            public string weight_unit { get; set; }
            public string product_title { get; set; }
            public string full_title { get; set; }
            public string sellable_title { get; set; }
            public float profit { get; set; }
            public float margin { get; set; }
            public float tax_rate { get; set; }
            public object estimated_delivery { get; set; }
            public object origin_country { get; set; }
            public object hs_tariff_number { get; set; }
            public object customs_description { get; set; }
            public string image_url { get; set; }
            public Product product { get; set; }
            public object[] reorders { get; set; }
            public Stock_Entries[] stock_entries { get; set; }
            public object[] variant_option_specifics { get; set; }
            public object[] variant_property_specifics { get; set; }
            public object[] images { get; set; }
            public Measurement_Attributes measurement_attributes { get; set; }
            public string main_thumbnail_url { get; set; }
            public int available_stock_level_at_all_warehouses { get; set; }
            public int stock_level_at_all_warehouses { get; set; }
            public Inventory inventory { get; set; }
            public float weight { get; set; }
            public object[] active_channels { get; set; }
            public object[] channel_sellables { get; set; }
            public float on_hand_value { get; set; }
        }

        public class Product
        {
            public int id { get; set; }
            public string title { get; set; }
            public int weight { get; set; }
            public string origin_country { get; set; }
            public string hs_tariff_number { get; set; }
            public int tax_rate { get; set; }
            public object estimated_delivery { get; set; }
            public object deleted_at { get; set; }
            public object deleted_by_id { get; set; }
            public string description { get; set; }
            public string main_image_src { get; set; }
            public int LineNo { get; set; }
        }

        public class Measurement_Attributes
        {
            public int id { get; set; }
            public float width { get; set; }
            public float height { get; set; }
            public float depth { get; set; }
            public string dimensions_unit { get; set; }
        }

        public class Inventory
        {
            public bool infinite { get; set; }
            public int physical_stock_level_at_all_warehouses { get; set; }
            public int allocated_stock_level_at_all_warehouses { get; set; }
            public int available_stock_level_at_all_warehouses { get; set; }
            public int incoming_stock_level_at_all_warehouses { get; set; }
            public int transit_outgoing_stock_level_at_all_warehouses { get; set; }
            public int transit_incoming_stock_level_at_all_warehouses { get; set; }
        }

        public class Stock_Entries
        {
            public int sellable_id { get; set; }
            public int warehouse_id { get; set; }
            public bool infinite { get; set; }
            public int allocated_stock_level { get; set; }
            public bool stock_running_low { get; set; }
            public DateTime updated_at { get; set; }
            public int incoming_stock_level { get; set; }
            public int transit_outgoing_stock_level { get; set; }
            public Warehouse warehouse { get; set; }
            public int physical_stock_level { get; set; }
            public int available_stock_level { get; set; }
            public float sellable_on_hand_value { get; set; }
            public int transit_incoming_stock_level { get; set; }
            public string location { get; set; }
        }

        public class Warehouse
        {
            public int id { get; set; }
            public string name { get; set; }
            public int display_position { get; set; }
        }

    }



    public class Download855ResponseModel
    {
        public string PONumber { get; set; }
    }
}
