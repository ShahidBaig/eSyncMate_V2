namespace eSyncMate.Processor.Models
{
    public class CustomerModel
    {
        public int Id { get; set; }
    }

    public class CustomerDataModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ERPCustomerID { get; set; }
        public string ISACustomerID { get; set; }
        public string ISA810ReceiverId { get; set; }
        public string ISA856ReceiverId { get; set; }
        public string Marketplace { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int ModifiedBy { get; set; }

    }


    public class SaveCustomerDataModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ERPCustomerID { get; set; }
        public string ISACustomerID { get; set; }
        public string ISA810ReceiverId { get; set; }
        public string ISA856ReceiverId { get; set; }
        public string Marketplace { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
    }

    public class EditCustomerDataModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ERPCustomerID { get; set; }
        public string ISACustomerID { get; set; }
        public string ISA810ReceiverId { get; set; }
        public string ISA856ReceiverId { get; set; }
        public string Marketplace { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int ModifiedBy { get; set; }

    }

    public class CustomerSearchModel
    {
        public string SearchOption { get; set; }
        public string SearchValue { get; set; }
    }

    public class CustomersListModel
    {
        public int ID { get; set; }
        public string Emails { get; set; }
    }


    public class CustomerAlertConfigModel
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int AlertId { get; set; }
        public string AlertName { get; set; } = string.Empty;      // join from AlertConfiguration table
        //public string Status { get; set; } = string.Empty;         // Active / Inactive
        public string FrequencyType { get; set; } = string.Empty;
        public int RepeatCount { get; set; }
        public string ExecutionTime { get; set; }
        public string WeekDays { get; set; }
        public string DayOfMonth { get; set; }
        public string Emails { get; set; }
        public string EmailSubject { get; set; }
        public string EmailBody { get; set; }
    }

    public class GetCustomerAlertsResponseModel
    {
        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public List<CustomerAlertConfigModel> Alerts { get; set; } = new();
    }

    public class SaveCustomerAlertRequestModel
    {
        public int Id { get; set; }              // 0 = new, >0 = update
        public int CustomerId { get; set; }
        public int AlertId { get; set; }
        //public string Status { get; set; } = string.Empty;
        public string FrequencyType { get; set; } = string.Empty;
        public int RepeatCount { get; set; }
        public string ExecutionTime { get; set; }
        public string WeekDays { get; set; }
        public string DayOfMonth { get; set; }
        public string Emails { get; set; }
        public string EmailSubject { get; set; }
        public string EmailBody { get; set; }
    }

    public class DeleteCustomerAlertRequestModel
    {
        public int Id { get; set; }              // 0 = new, >0 = update
        public int CustomerId { get; set; }
        public int AlertId { get; set; }
        //public string Status { get; set; } = string.Empty;
        public string FrequencyType { get; set; } = string.Empty;
        public int RepeatCount { get; set; }
        public string ExecutionTime { get; set; }
        public string WeekDays { get; set; }
        public string DayOfMonth { get; set; }
        public string Emails { get; set; }
        public string EmailSubject { get; set; }
        public string EmailBody { get; set; }
    }
}
