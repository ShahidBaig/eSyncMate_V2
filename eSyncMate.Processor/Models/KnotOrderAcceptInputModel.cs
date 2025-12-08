namespace eSyncMate.Processor.Models
{
    public class KnotOrderAcceptInputModel
    {
        public List<KnotAcceptedOrder_Lines> order_lines { get; set; }

        public KnotOrderAcceptInputModel()
        {
            this.order_lines = new List<KnotAcceptedOrder_Lines>();
        }

        public class KnotAcceptedOrder_Lines
        {
            public bool accepted { get; set; }
            public string id { get; set; }
        }
    }
}
