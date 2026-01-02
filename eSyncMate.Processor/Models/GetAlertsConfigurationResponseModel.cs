namespace eSyncMate.Processor.Models
{
    public class GetAlertsConfigurationResponseModel : ResponseModel
    {
        public List<AlertsConfigurationDataModel> AlertsConfiguration { get; set; }
    }
}
