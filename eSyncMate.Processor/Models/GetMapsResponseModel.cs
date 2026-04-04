namespace eSyncMate.Processor.Models
{
    public class GetMapsResponseModel : ResponseModel
    {
        public List<MapDataModel> Maps { get; set; }
        public int TotalCount { get; set; }
    }
}
