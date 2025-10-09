namespace eSyncMate.Processor.Models
{
    public class OrderSyncResponseModel : ResponseModel
    {
        public int OrderId { get; set; }
    }

    public class OrderStoreSyncResponseModel : ResponseModel
    {
        public int OrderId { get; set; }
        public string Status { get; set; }
    }
}
