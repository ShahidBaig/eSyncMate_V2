namespace eSyncMate.Processor.Models
{
    public class InventoryUpdateResponseModel
    {
        public int quantity { get; set; }
        public string distribution_center_id { get; set; }
        public DateTime last_modified { get; set; }
        public string last_modified_by { get; set; }
    }

}
