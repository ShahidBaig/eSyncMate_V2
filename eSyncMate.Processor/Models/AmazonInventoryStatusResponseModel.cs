namespace eSyncMate.Processor.Models
{
    public class AmazonInventoryStatusResponseModel
    {
        public DateTime processingEndTime { get; set; }
        public string processingStatus { get; set; }
        public string[] marketplaceIds { get; set; }
        public string feedId { get; set; }
        public string feedType { get; set; }
        public DateTime createdTime { get; set; }
        public DateTime processingStartTime { get; set; }
        public string resultFeedDocumentId { get; set; }

    }
}
