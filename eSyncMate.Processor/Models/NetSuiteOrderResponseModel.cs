using Newtonsoft.Json;

namespace eSyncMate.Processor.Models
{
    public class NetSuiteResponseModel
    {
        public string status { get; set; }
        public string message { get; set; }
        public string entity { get; set; }
        public NetSuiteError error { get; set; }
    }

    public class NetSuiteOrderResponseModel : NetSuiteResponseModel
    {
        public NetSuiteOrderData data { get; set; }
        public List<NetSuiteOrderData> orders { get; set; }
    }

    public class NetSuite855ResponseModel : NetSuiteResponseModel
    {
        public List<NetSuiteOrderStatusData> data { get; set; }
    }

    public class NetSuiteASNResponseModel : NetSuiteResponseModel
    {
        public List<NetSuiteASNData> data { get; set; }
    }

    public class NetSuiteCreateInvoiceResponseModel : NetSuiteResponseModel
    {
        public string invoiceId { get; set; }
        public string invoiceNo { get; set; }
        public string salesOrder { get; set; }
    }

    public class NetSuiteMarkASNResponseModel : List<NetSuiteOrderResponseModel>
    {
    }

    public class NetSuiteError
    {
        public string code { get; set; }
        public string message { get; set; }

    }

    public class NetSuiteOrderData
    {
        public string orderDate { get; set; }
        public string shipDate { get; set; }
        public string orderNumber { get; set; }
        public string customerName { get; set; }
        public string vendorNumber { get; set; }
        public string label { get; set; }
        public string storeNo { get; set; }
        public string storeDC { get; set; }
        public string status { get; set; }
        public string marketplace { get; set; }
        public string country { get; set; } = string.Empty;
        public string poType { get; set; }
        public string deptNo { get; set; }
        public string refNo { get; set; }
        public string customerOrderNo { get; set; }
        public string externalid { get; set; }
        public double shippingPrice { get; set; }
        public double shippingTax { get; set; }
        public string shippingMethod { get; set; }

        public List<NetSuiteTaxDataModel> shippingTaxes { get; set; }

        public string paymentType { get; set; }
        public double discount { get; set; }

        public NetSuiteAddressDataModel shipToCustomer { get; set; }

        public NetSuiteAddressDataModel billing { get; set; }

        public List<NetSuiteItemsDataModel> items { get; set; }

        public List<NetSuiteTaxDataModel> taxes { get; set; }        
    }

    public class NetSuiteTaxDataModel
    {
        public string name { get; set; }
        public string rate { get; set; }
        public string value { get; set; }
    }

    public class NetSuiteStoreDataModel
    {
        public string locationNumber { get; set; }
        public string quantity { get; set; }
    }

    public class NetSuiteAddressDataModel
    {
        public string id { get; set; }
        public string locationCode { get; set; }
        public string name { get; set; }
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string zip { get; set; }
        public string country { get; set; }
        public string phone { get; set; }
        public string email { get; set; }
    }

    public class NetSuiteItemsDataModel
    {
        public string lineNo { get; set; }
        public string partNo { get; set; }
        public string ediLineId { get; set; }
        public string upc { get; set; }
        public string description { get; set; }
        public int quantity { get; set; }
        public double price { get; set; }
        public string uom { get; set; }
        public double discount { get; set; }
        public List<NetSuiteStoreDataModel> locations { get; set; } = new List<NetSuiteStoreDataModel>();
        public List<NetSuiteTaxDataModel> taxes { get; set; } = new List<NetSuiteTaxDataModel>();
    }

    public class NetSuiteASNData
    {
        public string id { get; set; }
        public string name { get; set; }
        public string number { get; set; }
        public string docNo { get; set; }
        public string soNos { get; set; }
        public string ifNos { get; set; }
        public string customerPO { get; set; }
        public string orderDC { get; set; }
        public string orderDate { get; set; }
        public string plannedDeliveryDate { get; set; }
        public string customerOrderNo { get; set; }
        public string orderRefNo { get; set; }
        public Buyer buyer { get; set; }
        public List<Fulfillment> fulfillments { get; set; }
        public int totalPackages { get; set; }
        public double totalWeight { get; set; }
    }

    public class NetSuiteOrderStatusData
    {
        public string docNo { get; set; }
        public string customerPO { get; set; }
        public string orderDC { get; set; }
        public string orderDate { get; set; }
        public string customerOrderNo { get; set; }
        public string orderRefNo { get; set; }
        public string deptNo { get; set; }
        public List<NetsuitItem> items { get; set; }
    }

    public class Buyer
    {
        public string name { get; set; }
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string zip { get; set; }
        public string country { get; set; }
    }

    public class Fulfillment
    {
        public string id { get; set; }
        public string soId { get; set; }
        public string date { get; set; }
        public string name { get; set; }
        public string docNo { get; set; }
        public string soDocNo { get; set; }
        public string storeNo { get; set; }
        public Shipment shipment { get; set; }
        public ShipTo shipTo { get; set; }
        public ShipFrom shipFrom { get; set; }
        public List<string> trackingNos { get; set; }
        public List<NetsuitItem> items { get; set; }
        public List<Package> packages { get; set; }
    }

    public class NetsuitItem
    {
        public string itemId { get; set; }
        public string itemName { get; set; }
        public string itemSku { get; set; }
        public string itemUpc { get; set; }
        public int quantity { get; set; }
        public int quantityFulfilled { get; set; }
        public int quantityCommitted { get; set; }
        public string sku { get; set; }
        public string upc { get; set; }
        public string ediLineId { get; set; }
        public string vendorNo { get; set; }
        public string partNo { get; set; }
        public decimal amount { get; set; }
    }

    public class Package
    {
        public string cartonSSCC { get; set; }
        public double height { get; set; }
        public double length { get; set; }
        public string name { get; set; }
        public string palletSSCC { get; set; }
        public string trackingNo { get; set; }
        public double weight { get; set; }
        public double width { get; set; }
        public string id { get; set; }
        public List<NetsuitItem> items { get; set; }
    }

    public class PBM013515
    {
        public string upc { get; set; }
        public string name { get; set; }
        public string id { get; set; }
        public string ediLineId { get; set; }
        public string vendorNo { get; set; }
    }

    public class ShipFrom
    {
        public string name { get; set; }
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string country { get; set; }
        public string zip { get; set; }
    }

    public class Shipment
    {
        public string carrier { get; set; }
        public string amount { get; set; }
        public string service { get; set; }
        public string idier { get; set; }
        public string price { get; set; }
    }

    public class ShipTo
    {
        public string name { get; set; }
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string country { get; set; }
        public string zip { get; set; }
    }
}
