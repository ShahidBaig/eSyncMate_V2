namespace eSyncMate.Processor.Models
{
    // orders(first: 25, after: ..., query: ...) - the orders ShopifyGetOrdersRoute saves into eSyncMate
    public class ShopifyOrdersResponseModel : ShopifyGraphQLResponseModel
    {
        public Data data { get; set; }

        public class Data
        {
            public Orders orders { get; set; }
            // order(id) { lineItems(after: ...) } - the lines after the first 30
            public Order order { get; set; }
        }

        public class Orders
        {
            public List<Order> nodes { get; set; }
            public ShopifyInventoryItemsResponseModel.PageInfo pageInfo { get; set; }

            public Orders()
            {
                this.nodes = new List<Order>();
            }
        }

        public class Order
        {
            public string id { get; set; }
            public string legacyResourceId { get; set; }
            public string name { get; set; }
            // kept as Shopify's text (2026-10-23T10:31:00Z): the ERP map formats it with Transformations.formatDate
            public string createdAt { get; set; }
            public string displayFinancialStatus { get; set; }
            public string displayFulfillmentStatus { get; set; }
            public string email { get; set; }
            public string phone { get; set; }
            public string note { get; set; }
            public string currencyCode { get; set; }
            public MoneyBag totalPriceSet { get; set; }
            public Address shippingAddress { get; set; }
            public Address billingAddress { get; set; }
            public ShippingLines shippingLines { get; set; }
            public LineItems lineItems { get; set; }
        }

        public class Address
        {
            public string name { get; set; }
            public string company { get; set; }
            public string address1 { get; set; }
            public string address2 { get; set; }
            public string city { get; set; }
            public string province { get; set; }
            public string provinceCode { get; set; }
            public string zip { get; set; }
            public string country { get; set; }
            public string countryCodeV2 { get; set; }
            public string phone { get; set; }
        }

        public class ShippingLines
        {
            public List<ShippingLine> nodes { get; set; }

            public ShippingLines()
            {
                this.nodes = new List<ShippingLine>();
            }
        }

        public class ShippingLine
        {
            public string title { get; set; }
            public string code { get; set; }
            public string carrierIdentifier { get; set; }
            public MoneyBag originalPriceSet { get; set; }
        }

        public class LineItems
        {
            public List<LineItem> nodes { get; set; }
            public ShopifyInventoryItemsResponseModel.PageInfo pageInfo { get; set; }

            public LineItems()
            {
                this.nodes = new List<LineItem>();
            }
        }

        public class LineItem
        {
            public string id { get; set; }
            public string sku { get; set; }
            public string name { get; set; }
            public int quantity { get; set; }
            public int currentQuantity { get; set; }
            public bool isGiftCard { get; set; }
            public bool requiresShipping { get; set; } = true;
            public MoneyBag originalUnitPriceSet { get; set; }
            public MoneyBag discountedUnitPriceSet { get; set; }
            public Variant variant { get; set; }

            // set by ShopifyGetOrdersRoute before API-JSON is saved; the ERP map (Shopify Order Transformation) reads them
            public int lineNo { get; set; }
            public string itemId { get; set; }
        }

        public class Variant
        {
            public string id { get; set; }
            public InventoryItem inventoryItem { get; set; }
        }

        // requiresShipping = the product's own setting; the line's requiresShipping depends on how the order was made
        public class InventoryItem
        {
            public string id { get; set; }
            public bool? requiresShipping { get; set; }
        }

        public class MoneyBag
        {
            public Money shopMoney { get; set; }
        }

        public class Money
        {
            public decimal amount { get; set; }
            public string currencyCode { get; set; }
        }
    }
}
