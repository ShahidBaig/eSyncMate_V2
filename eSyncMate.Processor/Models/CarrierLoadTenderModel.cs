namespace eSyncMate.Processor.Models
{
    public class CarrierLoadTenderModel
    {
        public int CarrierLoadTenderId { get; set; }
    }

    public class CarrierLoadTenderDataModel
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public string AckStatus { get; set; }
        public string TrackStatus { get; set; }
        public DateTime TrackStatus_Updated { get; set; }
        public int CustomerId { get; set; }
        public int InboundEDIId { get; set; }
        public DateTime DocumentDate { get; set; }
        public string CarrierCode { get; set; }
        public string ShipmentId { get; set; }
        public string Purpose { get; set; }
        public string ReferenceNo { get; set; }
        public string BillToParty { get; set; }
        public string Weight { get; set; }
        public string WeightUnitCode { get; set; }
        public string TotalUnits { get; set; }
        public string TotalUnitsCode { get; set; }
        public string ShipperNo { get; set; }
        public string PickupDate { get; set; }
        public string PickupTime { get; set; }
        public string DeliverDate { get; set; }
        public string DeliverTime { get; set; }
        public string ShipFromName { get; set; }
        public string ShipFromCode { get; set; }
        public string ShipFromAddress { get; set; }
        public string ShipFromCity { get; set; }
        public string ShipFromState { get; set; }
        public string ShipFromZip { get; set; }
        public string ShipFromCountry { get; set; }
        public string ConsigneeName { get; set; }
        public string ConsigneeCode { get; set; }
        public string ConsigneeAddress { get; set; }
        public string ConsigneeAddressMutiple { get; set; }
        public string ConsigneeCity { get; set; }
        public string ConsigneeState { get; set; }
        public string ConsigneeZip { get; set; }
        public string ConsigneeCountry { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public string CustomerName { get; set; }
        public string EquipmentNo { get; set; }
        public string ManualEquipmentNo { get; set; }
        public DateTime CompletionDate { get; set; }

    }

    public class CarrierLoadTenderFileModel
    {
        public int Id { get; set; }
        public string Data { get; set; }
        public string Type { get; set; }
        public string FileName { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class CarrierLoadTenderPackage
    {
        public string LineNo { get; set; }
        public string LadingNo { get; set; }
        public string Weight { get; set; }
        public string WeightUnitCode { get; set; }
        public string TotalUnits { get; set; }
        public string ReleaseNumber { get; set; }
    }

    public class CarrierLoadTenderReceived
    {
        public string CarrierCode { get; set; }
        public string ShipmentId { get; set; }
        public string Purpose { get; set; }
        public string ReferenceNo { get; set; }
        public string DocumentDate { get; set; }
        public string BillToParty { get; set; }
        public string EquipmentNo { get; set; }
        public List<StopOff> StopOffs { get; set; } = new List<StopOff>();
    }

    public class StopOff
    {
        public string LineNo { get; set; }
        public string ReasonCode { get; set; }
        public string Weight { get; set; }
        public string WeightUnitCode { get; set; }
        public string TotalUnits { get; set; }
        public string TotalUnitsCode { get; set; }
        public List<string> ShipperNo { get; set; }
        public string PickupDate { get; set; }
        public string PickupTime { get; set; }
        public string DeliverDate { get; set; }
        public string DeliverTime { get; set; }
        public string ShipFromName { get; set; }
        public string ShipFromCode { get; set; }
        public string ShipFromAddress { get; set; }
        public string ShipFromCity { get; set; }
        public string ShipFromState { get; set; }
        public string ShipFromZip { get; set; }
        public string ShipFromCountry { get; set; }
        public string ConsigneeName { get; set; }
        public string ConsigneeCode { get; set; }
        public string ConsigneeAddress { get; set; }
        public string ConsigneeCity { get; set; }
        public string ConsigneeState { get; set; }
        public string ConsigneeZip { get; set; }
        public string ConsigneeCountry { get; set; }
        public bool processed { get; set; }
        public int processedCount { get; set; }
        public List<CarrierLoadTenderPackage> Packages { get; set; }
    }

    public class CarrierLoadTenderViewModel
    {
        public string CarrierCode { get; set; }
        public string ShipmentId { get; set; }
        public string Purpose { get; set; }
        public string ReferenceNo { get; set; }
        public string DocumentDate { get; set; }
        public string BillToParty { get; set; }
        public string EquipmentNo { get; set; }
        public string Weight { get; set; }
        public string WeightUnitCode { get; set; }
        public string TotalUnits { get; set; }
        public string TotalUnitsCode { get; set; }
        public string ShipperNo { get; set; }
        public string PickupDate { get; set; }
        public string PickupTime { get; set; }
        public string DeliverDate { get; set; }
        public string DeliverTime { get; set; }
        public string ShipFromName { get; set; }
        public string ShipFromCode { get; set; }
        public string ShipFromAddress { get; set; }
        public string ShipFromCity { get; set; }
        public string ShipFromState { get; set; }
        public string ShipFromZip { get; set; }
        public string ShipFromCountry { get; set; }
        public string ConsigneeName { get; set; }
        public string ConsigneeCode { get; set; }
        public string ConsigneeAddress { get; set; }
        public string ConsigneeAddressMutiple { get; set; }
        public string ConsigneeCity { get; set; }
        public string ConsigneeState { get; set; }
        public string ConsigneeZip { get; set; }
        public string ConsigneeCountry { get; set; }
        public List<CarrierLoadTenderPackage> Packages { get; set; }

        public CarrierLoadTenderViewModel()
        {
            this.Packages = new List<CarrierLoadTenderPackage>();
        }
    }

    public class CarrierLoadTenderAckDataModel
    {
        public string Id { get; set; }
        public string Description { get; set; }
    }

    public class EdiFilesCounterDataModel
    {
        public DateTime DocDate { get; set; }
        public int COUNT_204 { get; set; }
        public int COUNT_214 { get; set; }
        public string CustomerName { get; set; }
        public string ShipmentId { get; set; }
        public string ShipperNo { get; set; }

    }

    public class StatesDataModel
    {
        public string SCS_Code { get; set; }
        public string Description { get; set; }
    }

    public class CustomerWiseShipmentIDModel
    {
        public Int32 CustomerId { get; set; }
        public string ShipmentId { get; set; }
        public string ShipperNo { get; set; }
        
    }

}
