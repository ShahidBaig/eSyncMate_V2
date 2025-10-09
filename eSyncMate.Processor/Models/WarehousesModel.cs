namespace eSyncMate.Processor.Models
{
    public class WarehouseModel
    {
        public int Id { get; set; }
    }

    public class WarehouseDataModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
    }

}
