namespace eSyncMate.Processor.Models
{
    public class WalmartInventoryInputModel
    {
        public Inventories inventories { get; set; }

        public class Inventories
        {
            public List<ShipNode> nodes { get; set; }
            public Inventories() 
            {
                this.nodes = new List<ShipNode>();
            }
        }

        public class ShipNode
        {
            //public string sku { get; set; }
            public string shipNode { get; set; }
            public Inputqty inputQty { get; set; }
        }

        public class Inputqty
        {
            public string unit { get; set; }
            public int amount { get; set; }
        }

    }
}
