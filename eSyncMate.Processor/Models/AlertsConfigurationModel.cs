namespace eSyncMate.Processor.Models
{
    public class AlertsConfigurationModel
    {
        public int AlertID { get; set; }
    }

    public class AlertsConfigurationDataModel
    {
        public int AlertID { get; set; }
        public string EmailSubject { get; set; }
        public string Description { get; set; }
        public string AlertName { get; set; }
        public string Query { get; set; }
        public string AlertType { get; set; }

        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
    }

    public class SaveAlertsConfigurationDataModel
    {
        public int AlertID { get; set; }
        public string EmailSubject { get; set; }
        public string Description { get; set; }
        public string AlertName { get; set; }
        public string Query { get; set; }
        public string AlertType { get; set; }

        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
    }

    public class UpdateAlertsConfigurationDataModel
    {
        public int AlertID { get; set; }
        public string EmailSubject { get; set; }
        public string Description { get; set; }
        public string AlertName { get; set; }
        public string Query { get; set; }
        public string AlertType { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int ModifiedBy { get; set; }
    }

    public class AlertsConfigurationearchModel
    {
        public string SearchOption { get; set; }
        public string SearchValue { get; set; }
    }

    public class CustomerDropdownModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class GetCustomersDropdownResponseModel : ResponseModel
    {
        public List<CustomerDropdownModel> Customers { get; set; } = new();
    }
}
