namespace eSyncMate.Processor.Models
{
    public class _846ResponseModel
    {
        public List<Item846> Items { get; set; }

        public _846ResponseModel()
        {
            this.Items = new List<Item846>();  
        }

        public class Item846
        {
            public string SKU { get; set; }
            public string ItemID { get; set; }
            public string Description { get; set; }
            public int Qty { get; set; }
        }

    }
}
