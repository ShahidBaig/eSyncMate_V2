namespace eSyncMate.Processor.Models
{
    public class TargetPlusInventoryFeedWHSWiseRequestModel
    {

        public List<TargetPlusQuantity> quantities { get; set; }


        public TargetPlusInventoryFeedWHSWiseRequestModel() 
        {
            this.quantities = new List<TargetPlusQuantity>();
        }


        public class TargetPlusQuantity
        {
            public string distribution_center_id { get; set; }
            public int quantity { get; set; }
        }

    }
}
