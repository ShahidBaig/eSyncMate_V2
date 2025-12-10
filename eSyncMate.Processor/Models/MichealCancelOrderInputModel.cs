namespace eSyncMate.Processor.Models
{
    public class MichealCancelOrderInputModel
    {
        public string cancelReason { get; set; }
        public string orderNumber { get; set; }
        public List<MichealCancelorderline> cancelOrderLines { get; set; }

        public MichealCancelOrderInputModel()
        {
            this.cancelOrderLines   = new List<MichealCancelorderline>();  
        }

        public class MichealCancelorderline
        {
            public string orderItemId { get; set; }
            public string orderItemCancelReason { get; set; }
        }

    }
}
