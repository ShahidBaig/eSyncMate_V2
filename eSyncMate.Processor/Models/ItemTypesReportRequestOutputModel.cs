namespace eSyncMate.Processor.Models
{
    public class ItemTypesReportRequestOutputModel
    {
        public string id { get; set; }
        public string seller_id { get; set; }
        public string type { get; set; }
        public string status { get; set; }
        public string download_url { get; set; }
        public string format { get; set; }
        public Parameters parameters { get; set; }
        public object[] feedback { get; set; }
        public DateTime created { get; set; }
        public string created_by { get; set; }
        public DateTime last_modified { get; set; }
        public string last_modified_by { get; set; }

        public class Parameters
        {
        }

    }
}
