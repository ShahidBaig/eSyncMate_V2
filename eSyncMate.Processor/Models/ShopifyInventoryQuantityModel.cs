namespace eSyncMate.Processor.Models
{
    // One item at one Shopify location (warehouse WHSID mapped on the Warehouse Mapping screen)
    public class ShopifyInventoryQuantityModel
    {
        public string ItemId { get; set; } = string.Empty;
        public string InventoryItemId { get; set; } = string.Empty;
        public string LocationId { get; set; } = string.Empty;
        public string WHSID { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
