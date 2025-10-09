using eSyncMate.DB.Entities;

namespace eSyncMate.Processor.Models
{
    public class GetCustomerProductCatalog_Log : ResponseModel
    {
        public List<CustomerProductCatalog_LogDataModel> CustomerProductCatalog_Log { get; set; }
    }
}
