using System.Data;

namespace eSyncMate.Processor.Models
{
    public class OrderModel
    {
        public int OrderId { get; set; }
    }

    public class OrderDataModel
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderNumber { get; set; }
        public string VendorNumber { get; set; }
        public string OrderType { get; set; }
        public string ReferenceNo { get; set; }
        public string CustomerOrderNo { get; set; }
        public string ExternalId { get; set; }
        public string ShippingMethod { get; set; }
        public string ShipToId { get; set; }
        public string ShipToName { get; set; }
        public string ShipToAddress1 { get; set; }
        public string ShipToAddress2 { get; set; }
        public string ShipToCity { get; set; }
        public string ShipToState { get; set; }
        public string ShipToZip { get; set; }
        public string ShipToCountry { get; set; }
        public string ShipToEmail { get; set; }
        public string ShipToPhone { get; set; }
        public string BillToId { get; set; }
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
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
    }

    public class OrderStoreDataModel
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public string CustomerPO { get; set; }

        public int OrderId { get; set; }
    }

    public class OrderFileModel
    {
        public int Id { get; set; }
        public string Data { get; set; }
        public string Type { get; set; }
        public string FileName { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class OrderDetailResponseModel : ResponseModel
    {
        //public List<InvFeedFromNDCDataModel> Inv { get; set; }

        public DataTable DetailData { get; set; }
    }

    public class OrderStatusModel
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public string? CustomerPO { get; set; }
        public string? CustomerID { get; set; }
        public string? Status { get; set; }
        public string? ShipToAddress1 { get; set; }
        public string? ShipToAddress2 { get; set; }
        public string? ShipToCity { get; set; }
        public string? ShipToState { get; set; }
        public string? ShipToZip { get; set; }
        public string? ShipToCountry { get; set; }
        public string? ShipToName { get; set; }
        //public string? ShipViaCode { get; set; }


    }
}
