namespace eSyncMate.Processor.Models
{
    public class InventoryFilterModel
    {
        public string ItemID { get; set; }
        public string CustomerID { get; set; }
        public string StartDate { get; set; }
        public string FinishDate { get; set; }
        public string Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
