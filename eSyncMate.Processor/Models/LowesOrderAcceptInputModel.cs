namespace eSyncMate.Processor.Models
{
    public class LowesOrderAcceptInputModel
    {
        public List<LowesAcceptedOrder_Lines> order_lines { get; set; }

        public LowesOrderAcceptInputModel()
        {
            this.order_lines = new List<LowesAcceptedOrder_Lines>();
        }

        public class LowesAcceptedOrder_Lines
        {
            public bool accepted { get; set; }
            public string id { get; set; }
        }
    }
}
