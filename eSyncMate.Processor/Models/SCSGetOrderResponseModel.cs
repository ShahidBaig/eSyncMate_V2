namespace eSyncMate.Processor.Models
{
    public class SCSGetOrderResponseModel
    {
        public string id { get; set; }
        public string order_number { get; set; }
        public string ship_advice_number { get; set; }
        public string vmm_vendor_id { get; set; }
        public string seller_id { get; set; }
        public string distribution_center_id { get; set; }
        public string ship_node_id { get; set; }
        public DateTime order_date { get; set; }
        public DateTime requested_shipment_date { get; set; }
        public DateTime requested_delivery_date { get; set; }
        public string order_status { get; set; }
        public string currency { get; set; }
        public List<OtherInfoModel> other_info { get; set; }
        public List<OrderLineModel> order_lines { get; set; }
        public DateTime created { get; set; }
        public string created_by { get; set; }
        public DateTime last_modified { get; set; }
        public string last_modified_by { get; set; }
        public Addresses addresses { get; set; }

        public SCSGetOrderResponseModel()
        {
            this.other_info = new List<OtherInfoModel>();
            this.order_lines = new List<OrderLineModel>();
        }

        public class OtherInfoModel
        {
            public string name { get; set; }
            public string value { get; set; }
        }

        public class OrderLineModel
        {
            public string order_line_number { get; set; }
            public string tcin { get; set; }
            public decimal unit_price { get; set; }
            public int quantity { get; set; }
            public string routing { get; set; }
            public decimal total_shipping_price { get; set; }
            public decimal total_handling_price { get; set; }
            public decimal total_price { get; set; }
            public decimal other_fees { get; set; }
            public decimal total_item_discount { get; set; }
            public decimal total_item_discount_percentage { get; set; }
            public decimal total_shipping_discount { get; set; }
            public decimal total_gift_option_price { get; set; }
            public List<OtherInfoModel> other_info { get; set; }
            public bool is_two_day_ship { get; set; }
            public string external_id { get; set; }
            public List<OrderLineStatusModel> order_line_statuses { get; set; }
            public bool is_registry_item { get; set; }

            public OrderLineModel()
            {
                this.order_line_statuses = new List<OrderLineStatusModel>();
                this.other_info = new List<OtherInfoModel>();
            }
        }

        public class OrderLineStatusModel
        {
            public string status { get; set; }
            public int quantity { get; set; }
        }

        public class Addresses
        {
            public string id { get; set; }
            public Shipping_Address shipping_address { get; set; }
            public Billing_Address billing_address { get; set; }
        }


        public class Shipping_Address
        {
            public string first_name { get; set; }
            public string last_name { get; set; }
            public string email { get; set; }
            public Phone_Numbers[] phone_numbers { get; set; }
            public string address1 { get; set; }
            public string address2 { get; set; }
            public string city { get; set; }
            public string state { get; set; }
            public string postal_code { get; set; }
            public string country_code { get; set; }
        }

        public class Billing_Address
        {
            public string first_name { get; set; }
            public string last_name { get; set; }
            public string email { get; set; }
            public Phone_Numbers[] phone_numbers { get; set; }
            public string address1 { get; set; }
            public string address2 { get; set; }
            public string city { get; set; }
            public string state { get; set; }
            public string postal_code { get; set; }
            public string country_code { get; set; }
        }

        public class Phone_Numbers
        {
            public string number { get; set; }
            public string type { get; set; }
        }

    }
}
