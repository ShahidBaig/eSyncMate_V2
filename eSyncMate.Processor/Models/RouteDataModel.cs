using Microsoft.VisualBasic;

namespace eSyncMate.Processor.Models
{
    public class RouteDataModel
    {
        public int Id { get; set; }
        public int RouteId { get; set; }
        public string Type { get; set; }
        public int OrderId { get; set; }
        public string Data { get; set; }
        public string FileName { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }

    }





    public class SaveRouteDataModel
    {
        public int Id { get; set; }
        public int RouteId { get; set; }
        public string Type { get; set; }
        public int OrderId { get; set; }
        public string Data { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
    }

    public class UpdateRouteDataModel
    {
        public int Id { get; set; }
        public int RouteId { get; set; }
        public string Type { get; set; }
        public int OrderId { get; set; }
        public string Data { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int ModifiedBy { get; set; }
    }

    public class RouteDataSearchModel
    {
        public string SearchOption { get; set; }
        public string SearchValue { get; set; }
    }
}
