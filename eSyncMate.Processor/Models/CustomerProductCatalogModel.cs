using Microsoft.VisualBasic;

namespace eSyncMate.Processor.Models
{
    public class CustomerProductCatalogModel
    {
        public int ProductId { get; set; }
    }

    public class CustomerProductCatalogDataModel
    {
        public int ProductId { get; set; }
        public int Id { get; set; }
        public string CustomerID { get; set; }
        public string Brand { get; set; }
        public string ItemID { get; set; }
        public string UPC { get; set; }
        public string ItemTypeName { get; set; }
        public string ProductRelation { get; set; }
        public string ParentID { get; set; }
        public string ListPrice { get; set; }
        public string MapPrice { get; set; }
        public string OffPrice { get; set; }
        public string SyncStatus { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public string Data { get; set; }
        public string FileName { get; set; }

        public string Type { get; set; }

    }

    public class SaveCustomerProductCatalogDataModel
    {
        public int ProductId { get; set; }
        public int Id { get; set; }
        public string CustomerID { get; set; }
        public string Brand { get; set; }
        public string ItemID { get; set; }
        public string UPC { get; set; }
        public string ItemTypeName { get; set; }
        public string ProductRelation { get; set; }
        public string ParentID { get; set; }
        public string ListPrice { get; set; }
        public string MapPrice { get; set; }
        public string OffPrice { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        
    }

    public class UpdateCustomerProductCatalogDataModel
    {
        public int ProductId { get; set; }
        public string CustomerID { get; set; }
        public string Brand { get; set; }
        public string ItemID { get; set; }
        public string UPC { get; set; }
        public string ItemTypeName { get; set; }
        public string ProductRelation { get; set; }
        public string ParentID { get; set; }
        public string ListPrice { get; set; }
        public string MapPrice { get; set; }
        public string OffPrice { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int ModifiedBy { get; set; }
    }

    public class CustomerProductCatalogSearchModel
    {
        public string SearchOption { get; set; }
        public string SearchValue { get; set; }
    }

}
