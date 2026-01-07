using System.Collections.Generic;

namespace eSyncMate.Processor.Models
{
    public class AmazonInventoryFeedReportDownloadResponseModel
    {
        public Header header { get; set; }
        public List<Issue> issues { get; set; }
        public Summary summary { get; set; }

        public class Header
        {
            public string sellerId { get; set; }
            public string version { get; set; }
            public string feedId { get; set; }
        }

        public class Summary
        {
            public int errors { get; set; }
            public int warnings { get; set; }
            public int messagesProcessed { get; set; }
            public int messagesAccepted { get; set; }
            public int messagesInvalid { get; set; }
        }

        public class Issue
        {
            public long? messageId { get; set; }
            public string severity { get; set; }
            public string code { get; set; }
            public string message { get; set; }
            public string attributeName { get; set; }
        }
    }
}
