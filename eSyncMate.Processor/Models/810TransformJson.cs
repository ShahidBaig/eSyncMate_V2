namespace eSyncMate.Processor.Models
{
    public class _810TransformJson
    {
        public string InvoiceNo { get; set; }
        public string InvoiceDate { get; set; }
        public string ShippingName { get; set; }
        public string ShippingToNo { get; set; }
        public string ShippingAddress1 { get; set; }
        public string ShippingCity { get; set; }
        public string ShippingState { get; set; }
        public string ShippingZip { get; set; }
        public string ShippingCountry { get; set; }
        public string ShippingFromName { get; set; }
        public string SellerID { get; set; }
        public string ShippingFromAddress1 { get; set; }
        public string ShippingFromAddress2 { get; set; }
        public string ShippingFromCity { get; set; }
        public string ShippingFromState { get; set; }
        public string ShippingFromZip { get; set; }
        public string ShippingFromCountry { get; set; }
        public string PoNumber { get; set; }
        public string SCACCode { get; set; }
        public string Routing { get; set; }
        public string ShippingDate { get; set; }
        public string InvoiceAmount { get; set; }
        public string InvoiceTerms { get; set; }
        public string Frieght { get; set; }
        public string HandlingAmount { get; set; }
        public string SalesTax { get; set; }
        public string TrackingNo { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedDate { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedDate { get; set; }

        //public ASNDetail[] Detail { get; set; }

        public List<DetailItem> Detail { get; set; }
        public class DetailItem
        {
            public string UnitPrice { get; set; }
            public string SSCC { get; set; }
            public string ItemID { get; set; }
            public string SKU { get; set; }
            public string QTY { get; set; }
            public string UOM { get; set; }
            public string EDILineID { get; set; }
            public string Description { get; set; }
            public string SalesInvoice_ID { get; set; }
            public string Status { get; set; }
            public string SupplierStyle { get; set; }
            public string UPC { get; set; }
            public string WarehouseName { get; set; }

        }

        public class ASNDetail
        {
            public string Type { get; set; }
            public string Name { get; set; }
            public int HL01_HierarchicalIdNumber { get; set; }
            public int HL02_HierarchicalParentIdNumber { get; set; }
            public NewContent[] Content { get; set; }
        }

        public class NewContent
        {
            public string Type { get; set; }
            public string Name { get; set; }
            public Content1[] Content { get; set; }
            public int HL01_HierarchicalIdNumber { get; set; }
            public int HL02_HierarchicalParentIdNumber { get; set; }
        }

        public class Content1
        {
            public string E { get; set; }
            public string Type { get; set; }
            public string Name { get; set; }
            public Content2[] Content { get; set; }
            public int HL01_HierarchicalIdNumber { get; set; }
            public int HL02_HierarchicalParentIdNumber { get; set; }
        }

        public class Content2
        {
            public string E { get; set; }
            public string Type { get; set; }
            public string Name { get; set; }
            public Content3[] Content { get; set; }
            public int HL01_HierarchicalIdNumber { get; set; }
            public int HL02_HierarchicalParentIdNumber { get; set; }
        }

        public class Content3
        {
            public string E { get; set; }
            public string Type { get; set; }
            public string Name { get; set; }
            public Content4[] Content { get; set; }
        }

        public class Content4
        {
            public string E { get; set; }
        }
    }
}
