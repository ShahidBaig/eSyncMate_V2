namespace eSyncMate.Processor.Models
{
    // Variables of the inventorySetQuantities mutation; key = the @idempotent key (required from API 2026-04)
    public class ShopifyInventorySetQuantitiesRequestModel
    {
        public Input input { get; set; }
        public string key { get; set; }

        public ShopifyInventorySetQuantitiesRequestModel()
        {
            this.input = new Input();
        }

        public class Input
        {
            public string name { get; set; } = "available";
            public string reason { get; set; } = "correction";
            public string referenceDocumentUri { get; set; }
            public List<Quantity> quantities { get; set; }

            public Input()
            {
                this.quantities = new List<Quantity>();
            }
        }

        public class Quantity
        {
            public string inventoryItemId { get; set; }
            public string locationId { get; set; }
            public int quantity { get; set; }

            // Required from 2026-04; null = set without comparing (the ERP is the source of truth)
            public int? changeFromQuantity { get; set; }
        }
    }
}
