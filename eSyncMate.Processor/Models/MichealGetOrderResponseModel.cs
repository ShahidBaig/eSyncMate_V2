namespace eSyncMate.Processor.Models
{
    public class MichealGetOrderResponseModel
    {
        public string code { get; set; }
        public string message { get; set; }
        public Data data { get; set; }

        public class Data
        {
            public int pageNum { get; set; }
            public int pageSize { get; set; }
            public int totalElements { get; set; }
            public int totalPages { get; set; }
            public List<Pagedata> pageData { get; set; }

            public Data()
            {
               this.pageData = new List<Pagedata>();
            }
        }

        public class Pagedata
        {
            public List<Orderline> orderLines { get; set; } =  new List<Orderline>();
            public List<object> shipments { get; set; } = new List<object>();
            public object orderCoupons { get; set; } = new object();
            public string orderNumber { get; set; }
            public string cancelReason { get; set; }
            public string userId { get; set; }
            public string firstName { get; set; }
            public string lastName { get; set; }
            public string currency { get; set; }
            public float estimatedTax { get; set; }
            public float itemsSubtotal { get; set; }
            public float shippingHandlingCharge { get; set; }
            public float partialRefundTotal { get; set; }
            public float grandTotalCollected { get; set; }
            public float totalDiscount { get; set; }
            public string status { get; set; }
            public float refundedAmount { get; set; }
            public string phone { get; set; }
            public string createdTime { get; set; } = string.Empty;
            public string updatedTime { get; set; }
            public string postBox { get; set; }
            public string suite { get; set; }
            public string address1 { get; set; }
            public string address2 { get; set; }
            public string city { get; set; }
            public string state { get; set; }
            public string countryCode { get; set; }
            public string zipCode { get; set; }
            public object taxRate { get; set; } = new object();
            public object taxState { get; set; } = new object();
            public string promiseShipDate { get; set; }
            public object promiseDeliveryDate { get; set; } = new object();
            public float shippingHandlingFeeReturnValue { get; set; }
        }

        public class Orderline
        {
            public string orderItemId { get; set; }
            public string userId { get; set; }
            public string skuNumber { get; set; }
            public string sellerSkuNumber { get; set; }
            public object cancelReason { get; set; } = new object();
            public string releaseId { get; set; }
            public string releaseLineId { get; set; }
            public int orderLineId { get; set; }
            public string adjust { get; set; }
            public string itemDescription { get; set; }
            public string thumbnail { get; set; }
            public string productName { get; set; }
            public float rating { get; set; }
            public int returnAvailableQuantity { get; set; }
            public int quantity { get; set; }
            public int fulfilledQuantity { get; set; }
            public bool hazmat { get; set; }
            public int originalPrice { get; set; }
            public int shippingHandlingCharge { get; set; }
            public int refundedAmount { get; set; }
            public int refundedTax { get; set; }
            public int itemSubtotal { get; set; }
            public float estimatedTax { get; set; }
            public int totalDiscount { get; set; }
            public int price { get; set; }
            public object taxRate { get; set; } = new object();
            public int shippingLabelCost { get; set; }
            public int returnValue { get; set; }
            public string taxation { get; set; }
            public string refundable { get; set; }
            public string cancelDeadline { get; set; }
            public string returnDeadline { get; set; }
            public string status { get; set; }
            public string serviceLevel { get; set; }
            public string createdTime { get; set; }
            public string updatedTime { get; set; }
            public int LineNo { get; set; }

        }

    }
}
