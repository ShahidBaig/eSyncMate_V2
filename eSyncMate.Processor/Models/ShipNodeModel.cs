namespace eSyncMate.Processor.Models
{
    public class ShipNodeDataModel
    {
        public int ID { get; set; }
        public string WHSID { get; set; }
        public string ShipNode { get; set; }
        public string CustomerID { get; set; }
        public string APIName { get; set; }

        /// <summary>Which table the row lives in — TARGETPLUS or WALMART.</summary>
        public string Source { get; set; }
    }

    // Optional inputs are nullable on purpose: with nullable reference types enabled, a
    // non-nullable string is treated as [Required] and an empty query value binds to null,
    // which would fail model validation with a 400 before the action ever runs.

    public class SaveShipNodeDataModel
    {
        /// <summary>TARGETPLUS (default) or WALMART — selects the underlying table.</summary>
        public string? Source { get; set; }
        public string? WHSID { get; set; }
        public string? ShipNode { get; set; }
        public string? CustomerID { get; set; }
        public string? APIName { get; set; }
    }

    public class UpdateShipNodeDataModel
    {
        public string? Source { get; set; }
        public int ID { get; set; }
        public string? WHSID { get; set; }
        public string? ShipNode { get; set; }
        public string? CustomerID { get; set; }
        public string? APIName { get; set; }
    }

    public class ShipNodeSearchModel
    {
        /// <summary>Empty/absent = both tables listed together.</summary>
        public string? Source { get; set; }
        public string? CustomerID { get; set; }
        public string? SearchValue { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
