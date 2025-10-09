namespace eSyncMate.Processor.Models
{
    public class RouteTypeModel
    {
        public int Id { get; set; }
    }

    public class RouteTypeDataModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
    }

    public class SaveRouteTypeDataModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
    }

    public class UpdateRouteTypeDataModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int ModifiedBy { get; set; }
    }

    public class RouteTypeSearchModel
    {
        public string SearchOption { get; set; }
        public string SearchValue { get; set; }
    }
}
