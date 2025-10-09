namespace eSyncMate.Processor.Models
{
    public class OutPut
    {
        public List<TrackingInfo> TrackingInfo { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class SCSASNResponse
    {
        public OutPut OutPut { get; set; }
    }

    public class TrackingInfo
    {
        public string TrackingNo { get; set; }
        public string ItemID { get; set; }
        public string OrderLineNo { get; set; }
        public string ShippedDate { get; set; }
        public string ShippedQty { get; set; }
        public string ShippingMethod { get; set; }
        public string APIOrderLineNo { get; set; }
    }
}
