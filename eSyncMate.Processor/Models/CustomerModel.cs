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
        public string Name { get; set; }
    }
}
