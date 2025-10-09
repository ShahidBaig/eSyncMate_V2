namespace eSyncMate.Processor.Models
{
    public class RepaintTransformJsonModel
    {
        public Data data { get; set; }

        public class Data
        {
            public string order_number { get; set; }
            public string shop_name { get; set; }
            public string fulfillment_status { get; set; }
            public string order_date { get; set; }
            public Shipping_Lines shipping_lines { get; set; }
            public Shipping_Address shipping_address { get; set; }
            public Billing_Address billing_address { get; set; }
            public List<Line_Items> line_items { get; set; }
            public string required_ship_date { get; set; }

            public Data()
            {
                this.line_items = new List<Line_Items>();
            }
        }

        public class Shipping_Lines
        {
            public string title { get; set; }
            public string price { get; set; }
            public string carrier { get; set; }
            public string method { get; set; }
        }

        public class Shipping_Address
        {
            public string first_name { get; set; }
            public string last_name { get; set; }
            public string company { get; set; }
            public string address1 { get; set; }
            public string address2 { get; set; }
            public string city { get; set; }
            public string state { get; set; }
            public string state_code { get; set; }
            public string zip { get; set; }
            public string country { get; set; }
            public string country_code { get; set; }
            public string email { get; set; }
            public string phone { get; set; }
        }

        public class Billing_Address
        {
            public string first_name { get; set; }
            public string last_name { get; set; }
            public string company { get; set; }
            public string address1 { get; set; }
            public string address2 { get; set; }
            public string city { get; set; }
            public string state { get; set; }
            public string state_code { get; set; }
            public string zip { get; set; }
            public string country { get; set; }
            public string country_code { get; set; }
            public string email { get; set; }
            public string phone { get; set; }
        }

        public class Line_Items
        {
            public string sku { get; set; }
            public string partner_line_item_id { get; set; }
            public int quantity { get; set; }
            public float price { get; set; }
            public string product_name { get; set; }
            public string fulfillment_status { get; set; }
            public int quantity_pending_fulfillment { get; set; }
        }

    }
}
