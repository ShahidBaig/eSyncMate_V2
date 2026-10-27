namespace eSyncMate.Processor.Models
{
    public class ShopifyInventorySetQuantitiesResponseModel : ShopifyGraphQLResponseModel
    {
        public Data data { get; set; }

        public class Data
        {
            public InventorySetQuantities inventorySetQuantities { get; set; }
        }

        public class InventorySetQuantities
        {
            public InventoryAdjustmentGroup inventoryAdjustmentGroup { get; set; }
            public List<UserError> userErrors { get; set; }

            public InventorySetQuantities()
            {
                this.userErrors = new List<UserError>();
            }
        }

        public class InventoryAdjustmentGroup
        {
            public List<Change> changes { get; set; }

            public InventoryAdjustmentGroup()
            {
                this.changes = new List<Change>();
            }
        }

        public class Change
        {
            public string name { get; set; }
            public int delta { get; set; }
            public int? quantityAfterChange { get; set; }
            public IdNode item { get; set; }
            public IdNode location { get; set; }
        }
    }
}
