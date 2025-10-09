using Microsoft.VisualBasic;

namespace eSyncMate.Processor.Models
{
    public class ProductUploadPricesModel
    {
        public int Id { get; set; }
    }

    public class ProductUploadPricesDataModel
    {
        public int Id { get; set; }
        public string CustomerID { get; set; }
        public string ItemID { get; set; }
        public string ListPrice { get; set; }
        public string OffPrice { get; set; }
        public string MAPPrice { get; set; }
        public DateTime PromoStartDate { get; set; }
        public DateTime PromoEndDate { get; set; }
        public string OldListPrice { get; set; }
        public string OldOffPrice { get; set; } 
        public string OldMAPPrice { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
    }

    public class SaveProductUploadPricesDataModel
    {
        public int Id { get; set; }
        public string CustomerID { get; set; }
        public string ItemID { get; set; }
        public string ListPrice { get; set; }
        public string OffPrice { get; set; }
        public DateTime PromoStartDate { get; set; }
        public DateTime PromoEndDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
    }

    public class UpdateProductUploadPricesDataModel
    {
        public int Id { get; set; }
        public string CustomerID { get; set; }
        public string ItemID { get; set; }
        public string ListPrice { get; set; }
        public string OffPrice { get; set; }
        public DateTime PromoStartDate { get; set; }
        public DateTime PromoEndDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int ModifiedBy { get; set; }
    }

    public class ProductUploadPricesSearchModel
    {
        public string SearchOption { get; set; }
        public string SearchValue { get; set; }
    }
}
