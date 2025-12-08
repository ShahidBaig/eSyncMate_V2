namespace eSyncMate.Processor.Models
{
    public class KnotGetOrderResponseModel
    {
        public List<KnotOrder> orders { get; set; }
        public int total_count { get; set; }

        public KnotGetOrderResponseModel() 
        {
            this.orders = new List<KnotOrder>();
        }

        public class KnotOrder
        {
            public object acceptance_decision_date { get; set; }
            public bool can_cancel { get; set; }
            public bool can_shop_ship { get; set; }
            public object channel { get; set; }
            public string commercial_id { get; set; }
            public DateTime created_date { get; set; }
            public string currency_iso_code { get; set; }
            public KnotCustomer customer { get; set; }
            public object customer_debited_date { get; set; }
            public bool customer_directly_pays_seller { get; set; }
            public string customer_notification_email { get; set; }
            public object delivery_date { get; set; }
            public Fulfillment fulfillment { get; set; }
            public bool fully_refunded { get; set; }
            public bool has_customer_message { get; set; }
            public bool has_incident { get; set; }
            public bool has_invoice { get; set; }
            public DateTime last_updated_date { get; set; }
            public int leadtime_to_ship { get; set; }
            public object[] order_additional_fields { get; set; }
            public string order_id { get; set; }
            public List<KnotOrder_Lines> order_lines { get; set; }
            public object order_refunds { get; set; }
            public string order_state { get; set; }
            public object order_state_reason_code { get; set; }
            public object order_state_reason_label { get; set; }
            public string order_tax_mode { get; set; }
            public object order_taxes { get; set; }
            public object paymentType { get; set; }
            public object payment_type { get; set; }
            public string payment_workflow { get; set; }
            public float price { get; set; }
            public Promotions promotions { get; set; }
            public object quote_id { get; set; }
            public object shipping_carrier_code { get; set; }
            public object shipping_carrier_standard_code { get; set; }
            public object shipping_company { get; set; }
            public DateTime shipping_deadline { get; set; }
            public float shipping_price { get; set; }
            public object shipping_pudo_id { get; set; }
            public object shipping_tracking { get; set; }
            public object shipping_tracking_url { get; set; }
            public string shipping_type_code { get; set; }
            public string shipping_type_label { get; set; }
            public string shipping_type_standard_code { get; set; }
            public string shipping_zone_code { get; set; }
            public string shipping_zone_label { get; set; }
            public float total_commission { get; set; }
            public float total_price { get; set; }

            public KnotOrder()
            {
                this.order_lines = new List<KnotOrder_Lines>();
            }
        }

        public class KnotCustomer
        {
            public KnotBilling_Address billing_address { get; set; }
            public object civility { get; set; }
            public string customer_id { get; set; }
            public string firstname { get; set; }
            public string lastname { get; set; }
            public object locale { get; set; }
            public KnotShipping_Address? shipping_address { get; set; }
        }

        public class KnotBilling_Address
        {
            public string city { get; set; }
            public object company { get; set; }
            public object company_2 { get; set; }
            public string country { get; set; }
            public string country_iso_code { get; set; }
            public string firstname { get; set; }
            public string lastname { get; set; }
            public string state { get; set; }
            public string street_1 { get; set; }
            public object street_2 { get; set; }
            public string zip_code { get; set; }
        }

        public class KnotShipping_Address
        {
            public object additional_info { get; set; }
            public string city { get; set; }
            public object company { get; set; }
            public object company_2 { get; set; }
            public string country { get; set; }
            public string country_iso_code { get; set; }
            public string firstname { get; set; }
            public string lastname { get; set; }
            public string state { get; set; }
            public string street_1 { get; set; }
            public string street_2 { get; set; }
            public string zip_code { get; set; }
        }

        public class KnotFulfillment
        {
            public Center center { get; set; }
        }

        public class Center
        {
            public string code { get; set; }
        }

        public class Promotions
        {
            public object[] applied_promotions { get; set; }
            public int total_deduced_amount { get; set; }
        }

        public class KnotOrder_Lines
        {
            public bool can_refund { get; set; }
            public object[] cancelations { get; set; }
            public string category_code { get; set; }
            public string category_label { get; set; }
            public float commission_fee { get; set; }
            public float commission_rate_vat { get; set; }
            public Commission_Taxes[] commission_taxes { get; set; }
            public float commission_vat { get; set; }
            public DateTime created_date { get; set; }
            public object debited_date { get; set; }
            public object description { get; set; }
            public object[] fees { get; set; }
            public DateTime last_updated_date { get; set; }
            public int offer_id { get; set; }
            public string offer_sku { get; set; }
            public string offer_state_code { get; set; }
            public object[] order_line_additional_fields { get; set; }
            public string order_line_id { get; set; }
            public int order_line_index { get; set; }
            public string order_line_state { get; set; }
            public object order_line_state_reason_code { get; set; }
            public object order_line_state_reason_label { get; set; }
            public float price { get; set; }
            public object price_additional_info { get; set; }
            public float price_unit { get; set; }
            public Product_Medias[] product_medias { get; set; }
            public string product_shop_sku { get; set; }
            public string product_sku { get; set; }
            public string product_title { get; set; }
            public object[] promotions { get; set; }
            public int quantity { get; set; }
            public object received_date { get; set; }
            public object[] refunds { get; set; }
            public object shipped_date { get; set; }
            public Shipping_From shipping_from { get; set; }
            public float shipping_price { get; set; }
            public object shipping_price_additional_unit { get; set; }
            public object shipping_price_unit { get; set; }
            public object[] shipping_taxes { get; set; }
            public object[] taxes { get; set; }
            public float total_commission { get; set; }
            public float total_price { get; set; }
            public string ItemID { get; set; }

        }

        public class Shipping_From
        {
            public Address address { get; set; }
            public object warehouse { get; set; }
        }

        public class Address
        {
            public object city { get; set; }
            public string country_iso_code { get; set; }
            public object state { get; set; }
            public object street_1 { get; set; }
            public object street_2 { get; set; }
            public object zip_code { get; set; }
        }

        public class Commission_Taxes
        {
            public float amount { get; set; }
            public string code { get; set; }
            public float rate { get; set; }
        }

        public class Product_Medias
        {
            public string media_url { get; set; }
            public string mime_type { get; set; }
            public string type { get; set; }
        }

    }
}
