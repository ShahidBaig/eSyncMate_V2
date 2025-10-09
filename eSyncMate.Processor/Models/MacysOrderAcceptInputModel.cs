namespace eSyncMate.Processor.Models
{
    public class MacysOrderAcceptInputModel
    {
        public List<MacysAcceptedOrder_Lines> order_lines { get; set; }

        public MacysOrderAcceptInputModel()
        {
            this.order_lines = new List<MacysAcceptedOrder_Lines>();
        }

        public class MacysAcceptedOrder_Lines
        {
            public bool accepted { get; set; }
            public string id { get; set; }
        }
    }
}
