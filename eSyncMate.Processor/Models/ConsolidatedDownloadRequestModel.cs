using System.Data;

namespace eSyncMate.Processor.Models
{
    public class ConsolidatedDownloadRequestModel
    {
        public string UploadBatchID { get; set; }
        public string ItemID { get; set; }
    }

    public class BatchItemsRequestModel
    {
        public string BatchIDs { get; set; }
        public string ItemID { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetConsolidatedDownloadResponseModel : ResponseModel
    {
        public DataTable MainRow { get; set; }
        public DataTable TypeBreakdown { get; set; }
    }
}
