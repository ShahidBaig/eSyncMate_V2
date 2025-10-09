namespace eSyncMate.Processor.Models
{
    public class SCSInventoryFeedModel
    {
        public class SCSInventory
        {
            public bool Success { get; set; }
            public int Code { get; set; }
            public string Message { get; set; }
            public int CurrentPage { get; set; }
            public int TotalPages { get; set; }
            public List<Inventoryfeed> InventoryFeed { get; set; }
            public SCSInventory() 
            { 
             this.InventoryFeed = new List<Inventoryfeed>();
            }
        }

        public class Inventoryfeed
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
            public int ATS_L29 { get; set; }
            public int ATS_L65 { get; set; }


        }

    }
}
