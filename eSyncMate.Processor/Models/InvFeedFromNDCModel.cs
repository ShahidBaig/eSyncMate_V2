namespace eSyncMate.Processor.Models
{
    public class InvFeedFromNDCModel
    {

        public int Id { get; set; }
    }

    public class InvFeedFromNDCDataModel
    {
        public int Id { get; set; }
        public string UPC { get; set; }
        public string ItemID { get; set; }
        public string Description { get; set; }
        public int Qty { get; set; }
        public int ETAQty { get; set; }
        public DateTime ETADate { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
    }

    public class InvFeedFromNDCSearchModel
    {
        public string SearchOption { get; set; }
        public string SearchValue { get; set; }
    }
}
