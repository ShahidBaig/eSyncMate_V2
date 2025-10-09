namespace eSyncMate.Processor.Models
{
    public class InventoryModel
    {
        public string CustomerID { get; set; }
        public string ItemId { get; set; }
        public string CustomerItemCode { get; set; }

    }

    public class InventoryDataModel
    {
        public string CustomerID { get; set; }
        public string ItemId { get; set; }
        public string CustomerItemCode { get; set; }
        public string ETA_Date { get; set; }
        public int ETA_Qty { get; set; }
        public int Total_ATS { get; set; }
        public int ATS_L10 { get; set; }
        public int ATS_L21 { get; set; }
        public int ATS_L28 { get; set; }
        public int ATS_L30 { get; set; }
        public int ATS_L34 { get; set; }
        public int ATS_L35 { get; set; }
        public int ATS_L36 { get; set; }
        public int ATS_L37 { get; set; }
        public int ATS_L40 { get; set; }
        public int ATS_L41 { get; set; }
        public int ATS_L55 { get; set; }
        public int ATS_L60 { get; set; }
        public int ATS_L70 { get; set; }
        public int ATS_L91 { get; set; }
        public string Status { get; set; }
        public string ProductID { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int ModifiedBy { get; set; }
    }

    public class InventoryFileModel
    {
        public int Id { get; set; }
        public string CustomerId { get; set; }
        public string ItemId { get; set; }
        public string Data { get; set; }
        public string Type { get; set; }
        public string FileName { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
