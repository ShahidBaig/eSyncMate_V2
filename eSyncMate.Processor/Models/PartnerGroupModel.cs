namespace eSyncMate.Processor.Models
{
    public class PartnerGroupModel
    {
        public int Id { get; set; }
    }

    public class PartnerGroupDataModel
    {
        public int Id { get; set; }
        public int SourcePartyId { get; set; }
        public int DestinationPartyId { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }

        public string SourceParty { get;set; }
        public string DestinationParty { get; set; }
    }

    public class SavePartnerGroupDataModel
    {
        public int Id { get; set; }
        public int SourcePartyId { get; set; }
        public int DestinationPartyId { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
    }

    public class UpdatePartnerGroupDataModel
    {
        public int Id { get; set; }
        public int SourcePartyId { get; set; }
        public int DestinationPartyId { get; set; }
        public string Description { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int ModifiedBy { get; set; }
    }

    public class PartnerGroupSearchModel
    {
        public string SearchOption { get; set; }
        public string SearchValue { get; set; }
    }
}
