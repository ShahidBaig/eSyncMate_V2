namespace eSyncMate.Processor.Models
{
    public class OrderTransformationResponseModel : ResponseModel
    {
        public string EDI { get; set; }
        public string JSON { get; set; }
        public string DBFields { get; set; }
        public int SystemUser { get; set; }
    }
}
