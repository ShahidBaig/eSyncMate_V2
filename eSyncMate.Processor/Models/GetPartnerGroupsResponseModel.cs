namespace eSyncMate.Processor.Models
{
    public class GetPartnerGroupsResponseModel : ResponseModel
    {
        public List<PartnerGroupDataModel> PartnerGroup { get; set; }
        public int TotalCount { get; set; }
    }
}
