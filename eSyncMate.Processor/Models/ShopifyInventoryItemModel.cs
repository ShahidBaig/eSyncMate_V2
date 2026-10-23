using static eSyncMate.Processor.Models.ShopifyInventorySetQuantitiesResponseModel;

namespace eSyncMate.Processor.Models
{
    // One SCSInventoryFeed item while ShopifyInventoryUploadRoute sends it: the quantities built for it,
    // the Shopify changes that came back and any reason it failed.
    // Errors     = a problem with the item itself (SKU missing, not tracked, rejected) -> item ERROR
    // CallErrors = the call failed (token, network, HTTP, throttle) -> status left as is, sent again next run
    public class ShopifyInventoryItemModel
    {
        public string ItemId { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public List<ShopifyInventoryQuantityModel> Sent { get; set; } = new();
        public List<Change> Changes { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public List<string> CallErrors { get; set; } = new();
    }
}
