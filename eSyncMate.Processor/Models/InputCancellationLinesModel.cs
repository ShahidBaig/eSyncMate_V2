namespace eSyncMate.Processor.Models
{
    public class InputCancellationLinesModel
    {
        public string cancellation_reason { get; set; }
        public List<Order_Line_Statuses> order_line_statuses { get; set; }

        public InputCancellationLinesModel() 
        { 
            this.order_line_statuses = new List<Order_Line_Statuses>();
        }
        public class Order_Line_Statuses
        {
            public int quantity { get; set; }
            public string status { get; set; }
        }

    }
}
