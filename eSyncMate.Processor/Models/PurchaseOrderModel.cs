namespace eSyncMate.Processor.Models
{
    public class PurchaseOrderModel
    {

        public int Id { get; set; }
    }

    public class PurchaseOrderDataModel
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public int CustomerId { get; set; }
        public int InboundEDIId { get; set; }
        public DateTime OrderDate { get; set; }
        public string PONumber { get; set; }
        public string SupplierID { get; set; }
        public string LocationID { get; set; }
        public string POStatus { get; set; }
        public DateTime VCreatedDate { get; set; }
        public DateTime VExpectedDate { get; set; }
        public string ShipToName { get; set; }
        public string ShipToAddress1 { get; set; }
        public string ShipToAddress2 { get; set; }
        public string ShipToCity { get; set; }
        public string ShipToState { get; set; }
        public string ShipToZip { get; set; }
        public string ShipToCountry { get; set; }
        public string ShipToEmail { get; set; }
        public string ShipToPhone { get; set; }
        public string BillToName { get; set; }
        public string BillToAddress1 { get; set; }
        public string BillToAddress2 { get; set; }
        public string BillToCity { get; set; }
        public string BillToState { get; set; }
        public string BillToZip { get; set; }
        public string BillToCountry { get; set; }
        public string BillToEmail { get; set; }
        public string BillToPhone { get; set; }
        public string BuyerId { get; set; }
        public string BuyerName { get; set; }
        public string BuyerAddress1 { get; set; }
        public string BuyerAddress2 { get; set; }
        public string BuyerCity { get; set; }
        public string BuyerState { get; set; }
        public string BuyerZip { get; set; }
        public string BuyerCountry { get; set; }
        public string BuyerEmail { get; set; }
        public string BuyerPhone { get; set; }
        public bool IsStoreOrder { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public string SupplierName { get; set; }
        public string ShipServiceCode { get; set; }
        public int WarehouseID { get; set; }
    }

    public class SavePurchaseOrderModel
    {
        public DateTime OrderDate { get; set; }
        public string PONumber { get; set; }
        public string SupplierID { get; set; }
        public string ShipServiceCode { get; set; }
        public int WarehouseID { get; set; }
        public DateTime VExpectedDate { get; set; }
        public string ShipToAddress1 { get; set; }
        public string ShipToAddress2 { get; set; }
        public string ShipToCity { get; set; }
        public string ShipToState { get; set; }
        public string ShipToZip { get; set; }
        public string ShipToCountry { get; set; }
        public string BillToAddress1 { get; set; }
        public string BillToAddress2 { get; set; }
        public string BillToCity { get; set; }
        public string BillToState { get; set; }
        public string BillToZip { get; set; }
        public string BillToCountry { get; set; }
        public string ReferenceNo { get; set; }
        public int TotalQty { get; set; }
        public decimal TotalExtendedPrice { get; set; }
        public List<PurchaseOrderDetailSaveModel> Details { get; set; } = new List<PurchaseOrderDetailSaveModel>();
    }

    public class UpdatePurchaseOrderModel
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public string PONumber { get; set; }
        public string ShipServiceCode { get; set; }
        public int WarehouseID { get; set; }
        public string SupplierID { get; set; }
        public DateTime VExpectedDate { get; set; }
        public string ShipToAddress1 { get; set; }
        public string ShipToAddress2 { get; set; }
        public string ShipToCity { get; set; }
        public string ShipToState { get; set; }
        public string ShipToZip { get; set; }
        public string ShipToCountry { get; set; }
        public string BillToAddress1 { get; set; }
        public string BillToAddress2 { get; set; }
        public string BillToCity { get; set; }
        public string BillToState { get; set; }
        public string BillToZip { get; set; }
        public string BillToCountry { get; set; }
        public string ReferenceNo { get; set; }
        public int TotalQty { get; set; }
        public decimal TotalExtendedPrice { get; set; }
        public List<PurchaseOrderDetailModel> Details { get; set; } = new List<PurchaseOrderDetailModel>();
    }

    public class PurchaseOrderSearchModel
    {
        public string SearchOption { get; set; }
        public string SearchValue { get; set; }
    }

    public class PurchaseOrderDetailModel
    {
        public string ItemID { get; set; }
        public string UPC { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public int LineNo { get; set; }
        public decimal UnitPrice { get; set; }
        public int OrderQty { get; set; } // Unit of Measure
        public Boolean isNew { get; set; }
        public string ManufacturerName { get; set; }
        public string NDCItemID { get; set; } = string.Empty;
        public string PrimaryCategoryName { get; set; }
        public string SecondaryCategoryName { get; set; }
        public string ProductName { get; set; }
        public decimal ExtendedPrice { get; set; }
    }

    public class PurchaseOrderDetailSaveModel
    {
        public string ItemID { get; set; }
        public string UPC { get; set; }
        public string Description { get; set; }
        public int LineNo { get; set; }
        public decimal UnitPrice { get; set; }
        public int OrderQty { get; set; } // Unit of Measure
        public Boolean isNew { get; set; }
        public string ManufacturerName { get; set; }
        public string NDCItemID { get; set; } = string.Empty;
        public string PrimaryCategoryName { get; set; }
        public string SecondaryCategoryName { get; set; }
        public string ProductName { get; set; }
        public decimal ExtendedPrice { get; set; }
    }

    public class DetailItem
    {
        public string ItemID { get; set; }
        public int OrderQty { get; set; }
    }

    public class UpdateQtyRequestModel
    {
        public string ItemID { get; set; }
        public int OrderQty { get; set; }
        public string Action { get; set; }
        public List<DetailItem> Details { get; set; } = new List<DetailItem>();
    }
}
