namespace eSyncMate.Processor.Models
{
    public class MapModel
    {
        public int Id { get; set; }
    }

    public class MapDataModel
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string MapType { get; set; }
        public int TypeId { get; set; }
        public string Map { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
    }

    public class SaveMapDataModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int TypeId { get; set; }
        public string Map { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
    }

    public class UpdateMapDataModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int TypeId { get; set; }
        public string Map { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int ModifiedBy { get; set; }
    }

    public class MapSearchModel
    {
        public string SearchOption { get; set; }
        public string SearchValue { get; set; }
    }
}
