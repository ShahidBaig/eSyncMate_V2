namespace eSyncMate.Processor.Models
{
    public class GetConnectorTypesResponseModel : ResponseModel
    {
        public List<ConnectorTypesDataModel> ConnectorTypes { get; set; }
    }
}
