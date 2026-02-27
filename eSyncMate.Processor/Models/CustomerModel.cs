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
    public class FlowSearchModel
    {
        public string SearchOption { get; set; }
        public string SearchValue { get; set; }
    }
    public class CustomerSearchModel
    {
        public string SearchOption { get; set; }
        public string SearchValue { get; set; }
    }



    public class FlowsResponseModel
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public string Description { get; set; }
        public List<FlowResponseModel> Flows { get; set; } = new List<FlowResponseModel>();
        public string SearchOption { get; set; }
        public string SearchValue { get; set; }
    }

    public class FlowDataModel
    {
        public long Id { get; set; }
        public string CustomerID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }
    }

    public class SaveFlowDataModel
    {
        public string CustomerID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public List<SaveFlowDetailsDataModel> FlowDetails { get; set; } = new();
    }

    public class SaveFlowDetailsDataModel
    {
        public int? RouteId { get; set; }
        public string? Status { get; set; }
        public string? In_Out { get; set; }
        public string? FrequencyType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int RepeatCount { get; set; }
        public string? WeekDays { get; set; }
        public string? OnDay { get; set; }
        public string? ExecutionTime { get; set; }
    }
    public class FlowResponseModel
    {
        public long Id { get; set; }
        public string CustomerID { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public List<FlowsDetailsearchModel> FlowDetails { get; set; } = new();
    }
    public class EditFlowDataModel
    {
        public long Id { get; set; }
        public string CustomerID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public List<SaveFlowDetailsDataModel> FlowDetails { get; set; } = new();
    }

    public class FlowsDetailsearchModel
    {
        public long Id { get; set; }
        public long FlowId { get; set; }
        public int? RouteId { get; set; }
        public string? Status { get; set; }
        public string? In_Out { get; set; }
        public string? FrequencyType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? RepeatCount { get; set; }
        public string? WeekDays { get; set; }
        public string? OnDay { get; set; }
        public string? ExecutionTime { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public string? RouteName { get; set; }
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

    public class AutofillDataModel
    {
        public string FrequencyType { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public string RepeatCount { get; set; } = string.Empty;
        public string WeekDays { get; set; } = string.Empty;
        public string OnDay { get; set; } = string.Empty;
        public string ExecutionTime { get; set; } = string.Empty;
    }

    public class GetAutofillDataResponseModel
    {
        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public AutofillDataModel? Data { get; set; }
    }
}
