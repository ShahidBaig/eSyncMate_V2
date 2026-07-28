namespace eSyncMate.Processor.Models
{
    public class ShipCodeDataModel
    {
        public int Id { get; set; }
        public string CustomerID { get; set; }
        public string SourceMethod { get; set; }
        public string MatchType { get; set; }
        public string LevelOfService { get; set; }
        public string ShippingMethod { get; set; }
        public bool IsDefault { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; }
    }

    // Optional inputs are nullable on purpose: with nullable reference types enabled a
    // non-nullable string binds a missing value to null and fails model validation with a 400.

    public class SaveShipCodeDataModel
    {
        public string? CustomerID { get; set; }
        public string? SourceMethod { get; set; }
        public string? MatchType { get; set; }
        public string? LevelOfService { get; set; }
        public string? ShippingMethod { get; set; }
        public bool IsDefault { get; set; }
        public int Priority { get; set; } = 1;
        public bool IsActive { get; set; } = true;
    }

    public class UpdateShipCodeDataModel
    {
        public int Id { get; set; }
        public string? CustomerID { get; set; }
        public string? SourceMethod { get; set; }
        public string? MatchType { get; set; }
        public string? LevelOfService { get; set; }
        public string? ShippingMethod { get; set; }
        public bool IsDefault { get; set; }
        public int Priority { get; set; } = 1;
        public bool IsActive { get; set; } = true;
    }

    public class ShipCodeSearchModel
    {
        public string? CustomerID { get; set; }
        public string? SearchValue { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetShipCodeResponseModel : ResponseModel
    {
        public List<ShipCodeDataModel> ShipCodes { get; set; }
        public int TotalCount { get; set; }
    }

    public class ShipCodesResponseModel : ResponseModel
    {
    }
}
