namespace eSyncMate.Processor.Models
{
    public class AmazonGetOrdersResponseModel
    {
        public Payload payload { get; set; }

        public class Payload
        {
            public List<AmazonOrder> Orders { get; set; }
            public string NextToken { get; set; }
            public DateTime CreatedBefore { get; set; }
        
            public Payload()
            {
                this.Orders = new List<AmazonOrder>();    
            }
        }

        public class AmazonOrder
        {
            public Buyerinfo BuyerInfo { get; set; }
            public string AmazonOrderId { get; set; }
            public DateTime EarliestDeliveryDate { get; set; }
            public DateTime EarliestShipDate { get; set; }
            public string SalesChannel { get; set; }
            public Automatedshippingsettings AutomatedShippingSettings { get; set; }
            public string OrderStatus { get; set; }
            public int NumberOfItemsShipped { get; set; }
            public string OrderType { get; set; }
            public bool IsPremiumOrder { get; set; }
            public bool IsPrime { get; set; }
            public string FulfillmentChannel { get; set; }
            public int NumberOfItemsUnshipped { get; set; }
            public bool HasRegulatedItems { get; set; }
            public string IsReplacementOrder { get; set; }
            public bool IsSoldByAB { get; set; }
            public DateTime LatestShipDate { get; set; }
            public string ShipServiceLevel { get; set; }
            public Defaultshipfromlocationaddress DefaultShipFromLocationAddress { get; set; }
            public bool IsISPU { get; set; }
            public string MarketplaceId { get; set; }
            public DateTime LatestDeliveryDate { get; set; }
            public DateTime PurchaseDate { get; set; }
            public Shippingaddress ShippingAddress { get; set; }
            public bool IsAccessPointOrder { get; set; }
            public string PaymentMethod { get; set; }
            public bool IsBusinessOrder { get; set; }
            public Ordertotal OrderTotal { get; set; }
            public string[] PaymentMethodDetails { get; set; }
            public bool IsGlobalExpressEnabled { get; set; }
            public DateTime LastUpdateDate { get; set; }
            public string ShipmentServiceLevelCategory { get; set; }
            public string ReplacedOrderId { get; set; }

            public AmazonOrderAddressResponseModel OrderAddress { get; set; }
            public AmazonOrderDetailResponseModel OrderDetail { get; set; }

        }

        public class Buyerinfo
        {
            public string BuyerEmail { get; set; }
        }

        public class Automatedshippingsettings
        {
            public bool HasAutomatedShippingSettings { get; set; }
        }

        public class Defaultshipfromlocationaddress
        {
            public string StateOrRegion { get; set; }
            public string AddressLine1 { get; set; }
            public string PostalCode { get; set; }
            public string City { get; set; }
            public string CountryCode { get; set; }
            public string Name { get; set; }
        }

        public class Shippingaddress
        {
            public string StateOrRegion { get; set; }
            public string PostalCode { get; set; }
            public string City { get; set; }
            public string CountryCode { get; set; }
            public string CompanyName { get; set; }
        }

        public class Ordertotal
        {
            public string CurrencyCode { get; set; }
            public string Amount { get; set; }
        }
    }

    public class AmazonOrderAddressResponseModel
    {
        public AddressPayload payload { get; set; }

        public class AddressPayload
        {
            public string AmazonOrderId { get; set; }
            public Shippingaddress ShippingAddress { get; set; }
        }

        public class Shippingaddress
        {
            public string StateOrRegion { get; set; }
            public string PostalCode { get; set; }
            public string City { get; set; }
            public string CountryCode { get; set; }
            public string Name { get; set; }
            public string AddressLine1 { get; set; }
            public string AddressLine2 { get; set; }
            public string AddressLine3 { get; set; }


        }

    }


    public class AmazonOrderDetailResponseModel
    {
        public DetailPayload payload { get; set; }

        public class DetailPayload
        {
            public List<Orderitem> OrderItems { get; set; }
            public string AmazonOrderId { get; set; }

            public DetailPayload()
            {
                this.OrderItems = new List<Orderitem>();
            }
        }

        public class Orderitem
        {
            public Taxcollection TaxCollection { get; set; }
            public Productinfo ProductInfo { get; set; }
            public DetailBuyerinfo BuyerInfo { get; set; }
            public Itemtax ItemTax { get; set; }
            public int QuantityShipped { get; set; }
            public Buyerrequestedcancel BuyerRequestedCancel { get; set; }
            public Itemprice ItemPrice { get; set; }
            public string ASIN { get; set; }
            public string SellerSKU { get; set; }
            public string Title { get; set; }
            public string IsGift { get; set; }
            public string ConditionSubtypeId { get; set; }
            public bool IsTransparency { get; set; }
            public int QuantityOrdered { get; set; }
            public Promotiondiscounttax PromotionDiscountTax { get; set; }
            public string ConditionId { get; set; }
            public Promotiondiscount PromotionDiscount { get; set; }
            public string OrderItemId { get; set; }
            public Int64 LineNo { get; set; }

        }

        public class Taxcollection
        {
            public string Model { get; set; }
            public string ResponsibleParty { get; set; }
        }

        public class Productinfo
        {
            public string NumberOfItems { get; set; }
        }

        public class DetailBuyerinfo
        {
        }

        public class Itemtax
        {
            public string CurrencyCode { get; set; }
            public string Amount { get; set; }
        }

        public class Buyerrequestedcancel
        {
            public string IsBuyerRequestedCancel { get; set; }
            public string BuyerCancelReason { get; set; }
        }

        public class Itemprice
        {
            public string CurrencyCode { get; set; }
            public string Amount { get; set; }
        }

        public class Promotiondiscounttax
        {
            public string CurrencyCode { get; set; }
            public string Amount { get; set; }
        }

        public class Promotiondiscount
        {
            public string CurrencyCode { get; set; }
            public string Amount { get; set; }
        }

    }
}
