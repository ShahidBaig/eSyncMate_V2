using System.Diagnostics;

namespace eSyncMate.Processor.Models
{
    public class WalmartInventoryOutPutModel
    {

        public string sku { get; set; }
        public List<Node> nodes { get; set; }

        public WalmartInventoryOutPutModel()
        {
            this.nodes = new List<Node>();
        }

        public class Node
        {
            public string shipNode { get; set; }
            public string status { get; set; }
            public List<errors> errors { get; set; }

            public Node()
            {
                this.errors = new List<errors>();   
            }
        }

        public class errors
        {
            public string code { get; set; }
            public string field { get; set; }
            public string description { get; set; }
            public string info { get; set; }
            public string severity { get; set; }
            public string category { get; set; }
            public List<causes> causes { get; set; }
            public errors()
            {
                this.causes = new List<causes>();
            }
        }

        public class causes
        {
            public string code { get; set; }
            public string field { get; set; }
            public string type { get; set; }
            public string description { get; set; }
        }
    }
}
