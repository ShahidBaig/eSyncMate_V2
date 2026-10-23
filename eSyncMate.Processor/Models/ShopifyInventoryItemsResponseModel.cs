namespace eSyncMate.Processor.Models
{
    // inventoryItems(first: 250, after: ...) - every inventory item of the store: SKU -> inventory item id
    public class ShopifyInventoryItemsResponseModel : ShopifyGraphQLResponseModel
    {
        public Data data { get; set; }

        public class Data
        {
            public InventoryItems inventoryItems { get; set; }
        }

        public class InventoryItems
        {
            public List<InventoryItem> nodes { get; set; }
            public PageInfo pageInfo { get; set; }

            public InventoryItems()
            {
                this.nodes = new List<InventoryItem>();
            }
        }

        public class InventoryItem
        {
            public string id { get; set; }
            public string sku { get; set; }
        }

        public class PageInfo
        {
            public bool hasNextPage { get; set; }
            public string endCursor { get; set; }
        }
    }
}
