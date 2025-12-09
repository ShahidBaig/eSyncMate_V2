namespace eSyncMate.Processor.Models
{
    public class SCSGetOrderInfoModel
    {
        public Output OutPut { get; set; }

        public class Output
        {
            public Order Order { get; set; }
            public bool Success { get; set; }
            public string Message { get; set; }
        }

        public class Order
        {
            public Header Header { get; set; }
            public List<Detail> Detail { get; set; }

            public Order ()
            {
                this.Detail = new List<Detail>();   
            }
        }

        public class Header
        {
            public int OrderNo { get; set; }
            public string CustomerID { get; set; }
            public DateTime OrderDate { get; set; }
            public string CustomerPO { get; set; }
            public string Status { get; set; }
            public string BillingFirstName { get; set; }
            public string BillingLastName { get; set; }
            public string BillingAddress1 { get; set; }
            public string BillingAddress2 { get; set; }
            public string BillingCity { get; set; }
            public string BillingState { get; set; }
            public string BillingZipCode { get; set; }
            public string BillingCountry { get; set; }
            public string BillingPhone1 { get; set; }
            public string BillingPhone2 { get; set; }
            public string BillingFax { get; set; }
            public string BillingEmail { get; set; }
            public string PaymentTerm { get; set; }
            public string ShippingFirstName { get; set; }
            public string ShippingLastName { get; set; }
            public string ShippingAddress1 { get; set; }
            public string ShippingAddress2 { get; set; }
            public string ShippingState { get; set; }
            public string ShippingZipCode { get; set; }
            public string ShipViaCode { get; set; }
            public float SubTotal { get; set; }
            public float ShippingCost { get; set; }
            public float TotalAmount { get; set; }
            public string Instructions { get; set; }
            public DateTime ShippingDate { get; set; }
            public string OrderTakenBy { get; set; }
            public string ExternalID { get; set; }
        }

        public class Detail
        {
            public string ItemID { get; set; }
            public int Line_No { get; set; }
            public int OrderQty { get; set; }
            public float UnitPrice { get; set; }
            public float Discount { get; set; }
            public string Remarks { get; set; }
            public string Status { get; set; }
            public int QtyInStock { get; set; }
            public object ETA_Date { get; set; }
            public string APIOrderLineNo { get; set; }
        }

    }
}
