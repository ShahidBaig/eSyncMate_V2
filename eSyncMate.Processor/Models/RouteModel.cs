using Microsoft.VisualBasic;

namespace eSyncMate.Processor.Models
{
    public class RouteModel
    {
        public int Id { get; set; }
    }

    public class RoutesDataModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int TypeId { get; set; }
        public string Status { get; set; }
        public int SourcePartyId { get; set; }
        public int DestinationPartyId { get; set; }
        public int SourceConnectorId { get; set; }
        public int DestinationConnectorId { get; set; }
        public int MapId { get; set; }
        public int PartnerGroupId { get; set; }
        public string FrequencyType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int RepeatCount { get; set; }
        public string WeekDays { get; set; }
        public string OnDay { get; set; }
        public string ExecutionTime { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public string SourceParty { get; set; }
        public string DestinationParty { get; set; }
        public string SourceConnector { get; set; }
        public string DestinationConnector { get; set; }
        public string Map { get; set; }
        public string PartnerGroup { get; set; }
        public string RouteType { get; set; }
        public string RouteGroup { get; set; }
        public string CustomerName { get; set; }
    }

    public class SaveRoutesDataModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int TypeId { get; set; }
        public string Status { get; set; }
        public int SourcePartyId { get; set; }
        public int DestinationPartyId { get; set; }
        public int SourceConnectorId { get; set; }
        public int DestinationConnectorId { get; set; }
        public int MapId { get; set; }
        public int PartyGroupId { get; set; }
        public string FrequencyType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int RepeatCount { get; set; }
        public string WeekDays { get; set; }
        public string OnDay { get; set; }
        public string ExecutionTime { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public string RouteGroup { get; set; }
    }

    public class UpdateRoutesDataModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int TypeId { get; set; }
        public string Status { get; set; }
        public int SourcePartyId { get; set; }
        public int DestinationPartyId { get; set; }
        public int SourceConnectorId { get; set; }
        public int DestinationConnectorId { get; set; }
        public int MapId { get; set; }
        public int PartyGroupId { get; set; }
        public string FrequencyType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int RepeatCount { get; set; }
        public string WeekDays { get; set; }
        public string OnDay { get; set; }
        public string ExecutionTime { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int ModifiedBy { get; set; }
        public string RouteGroup { get; set; }
    }

    public class RouteSearchModel
    {
        public string SearchOption { get; set; }
        public string SearchValue { get; set; }
    }
}
