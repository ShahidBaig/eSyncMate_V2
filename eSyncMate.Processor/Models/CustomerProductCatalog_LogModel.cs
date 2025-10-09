using Microsoft.VisualBasic;

namespace eSyncMate.Processor.Models
{
    public class CustomerProductCatalog_LogModel
    {
        public int ERPCustomerID { get; set; }
    }

    public class CustomerProductCatalog_LogDataModel
    {
        public string ERPCustomerID { get; set; }
        public string FileName { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
       

    }
}
