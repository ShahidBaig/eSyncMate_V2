using eSyncMate.DB.Entities;
using System.Data;

namespace eSyncMate.Processor.Models
{
    public class GetCustomerProductCatalog : ResponseModel
    {
        public List<CustomerProductCatalogDataModel> CustomerProductCatalog { get; set; }

        public DataTable CustomerProductCatalogDatatable { get; set; }
    }
}
