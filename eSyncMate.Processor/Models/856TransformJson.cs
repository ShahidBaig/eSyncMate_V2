namespace eSyncMate.Processor.Models
{
    public class _856TransformJson
    {
        public string ShipmentID { get; set; }
        public string TransactionDate { get; set; }
        public string ShippingName { get; set; }
        public string N1_ShipID { get; set; }
        public string ShippingAddress1 { get; set; }
        public string ShippingCity { get; set; }
        public string ShippingState { get; set; }
        public string ShippingZip { get; set; }
        public object ShippingCountry { get; set; }
        public string ShippingFromName { get; set; }
        public string SellerID { get; set; }
        public string ShippingFromAddress1 { get; set; }
        public object ShippingFromAddress2 { get; set; }
        public string ShippingFromCity { get; set; }
        public string ShippingFromState { get; set; }
        public string ShippingFromZip { get; set; }
        public object ShippingFromCountry { get; set; }
        public object PoNumber { get; set; }
        public string PoDate { get; set; }
        public object SCACCode { get; set; }
        public object Routing { get; set; }
        public string ShippingDate { get; set; }
        //public ASNDetail[] Detail { get; set; }

        public List<DetailItem> Detail { get; set; }
        public class DetailItem
        {
            public string BOLNo { get; set; }
            public string SSCC { get; set; }
            public string ItemID { get; set; }

            public string SKU { get; set; }

            public string QTY { get; set; }

            public string UOM { get; set; }

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
