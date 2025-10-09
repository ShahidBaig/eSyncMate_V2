using static eSyncMate.Processor.Models.ShipStation856ResponseModel;

namespace eSyncMate.Processor.Models
{
    public class ShipStation856ResponseModel
    {
        public List<Fulfillment> fulfillments { get; set; }
        public int total { get; set; }
        public int page { get; set; }
        public int pages { get; set; }

        public ShipStation856ResponseModel() 
        {
            this.fulfillments = new List<Fulfillment>();
        }

        public class Fulfillment
        {
            public int fulfillmentId { get; set; }
            public int orderId { get; set; }
            public string orderNumber { get; set; }
            public string userId { get; set; }
            public object customerEmail { get; set; }
            public string trackingNumber { get; set; }
            public DateTime createDate { get; set; }
            public DateTime shipDate { get; set; }
            public object voidDate { get; set; }
            public object deliveryDate { get; set; }
            public string carrierCode { get; set; }
            public object sellerFillProviderId { get; set; }
            public object sellerFillProviderName { get; set; }
            public object fulfillmentProviderCode { get; set; }
            public object fulfillmentServiceCode { get; set; }
            public float fulfillmentFee { get; set; }
            public bool voidRequested { get; set; }
            public bool voided { get; set; }
            public bool marketplaceNotified { get; set; }
            public object notifyErrorMessage { get; set; }
            public FullfillmentShipto shipTo { get; set; }
            public string externalFulfillmentId { get; set; }
        }

        public class FullfillmentShipto
        {
            public string name { get; set; }
            public string company { get; set; }
            public string street1 { get; set; }
            public string street2 { get; set; }
            public object street3 { get; set; }
            public string city { get; set; }
            public string state { get; set; }
            public string postalCode { get; set; }
            public string country { get; set; }
            public string phone { get; set; }
            public object residential { get; set; }
            public object addressVerified { get; set; }
        }


    }



    public class CustomerShipStationResponseModel 
    {
        public int fulfillmentId { get; set; }
        public int orderId { get; set; }
        public string orderNumber { get; set; }
        public string userId { get; set; }
        public object customerEmail { get; set; }
        public string trackingNumber { get; set; }
        public DateTime createDate { get; set; }
        public DateTime shipDate { get; set; }
        public object voidDate { get; set; }
        public object deliveryDate { get; set; }
        public string carrierCode { get; set; }
        public object sellerFillProviderId { get; set; }
        public object sellerFillProviderName { get; set; }
        public object fulfillmentProviderCode { get; set; }
        public object fulfillmentServiceCode { get; set; }
        public float fulfillmentFee { get; set; }
        public bool voidRequested { get; set; }
        public bool voided { get; set; }
        public bool marketplaceNotified { get; set; }
        public object notifyErrorMessage { get; set; }
        public FullfillmentShipto shipTo { get; set; } = new FullfillmentShipto();
        public string externalFulfillmentId { get; set; }

       public List<Fullfilment856Packages> packages { get; set; } = new List<Fullfilment856Packages>();   
    }

    public class Fullfilment856Packages
    {
        public string trackingNumber { get; set; }
        public List<Fullfilment856Items> items { get; set; }

        public Fullfilment856Packages()
        {
            this.items = new List<Fullfilment856Items>();
        }
    }

    public class Fullfilment856Items 
    {
        public string orderItemId { get; set; }
        public string lineItemKey { get; set; }
        public string sku { get; set; }
        public string name { get; set; }
        public string imageUrl { get; set; }
        public string weight { get; set; }
        public long quantity { get; set; }
        public string unitPrice { get; set; }
        public string taxAmount { get; set; }
        public string shippingAmount { get; set; }
        public string warehouseLocation { get; set; }
        public string productId { get; set; }
        public string upc { get; set; }
        public string lineNo { get; set; }

    }

}
