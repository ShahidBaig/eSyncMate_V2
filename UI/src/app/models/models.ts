import { Data } from "@angular/router";

export interface SideNavItem {
  title: string;
  link: string;
  visible: boolean;
}

export enum UserType {
  ADMIN,
  READER,
  WRITER,
}

export interface User {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  mobile: string;
  password: string;
  status: string;
  createdDate: string;
  userType: string;
  company: string;
  customerName: string;
  isSetupMenu: string;
  userID: string;
}
export interface RouteLog {
  Id: number,
  RouteId: number,
  Type: number,
  Status: string,
  Message: string,
  Details: string,
  CreatedDate: Date,
  ModifiedDate: Date,
  RouteExceptionDate: Date,
  TypeName: string
}
export interface Order {
  Id: number,
  Status: string,
  CustomerId: string,
  OrderDate: Date,
  OrderNumber: string,
  VendorNumber: string,
  OrderType: string,
  ReferenceNo: string,
  CustomerOrderNo: string,
  ExternalId: string,
  ShippingMethod: string,
  ShipToId: string,
  ShipToName: string,
  ShipToAddress1: string,
  ShipToAddress2: string,
  ShipToCity: string,
  ShipToState: string,
  ShipToZip: string,
  ShipToCountry: string,
  ShipToEmail: string,
  ShipToPhone: string,
  BillToId: string,
  BillToName: string,
  BillToAddress1: string,
  BillToAddress2: string,
  BillToCity: string,
  BillToState: string,
  BillToZip: string,
  BillToCountry: string,
  BillToEmail: string,
  BillToPhone: string,
  BuyerId: string,
  BuyerName: string,
  BuyerAddress1: string,
  BuyerAddress2: string,
  BuyerCity: string,
  BuyerState: string,
  BuyerZip: string,
  BuyerCountry: string,
  BuyerEmail: string,
  BuyerPhone: string,
  CreatedDate: Date,
  CreatedBy: number,
}

export interface Customer {
  Id: number,
  Name: string,
  ERPCustomerID: string,
  ISACustomerID: string,
  ISA810ReceiverId: string,
  Marketplace: string,
  CreatedDate: Date,
  CreatedBy: number
}

export interface Map {
  Id: number,
  Name: string,
  Data: string,
  TypeId: number,
  Map: string,
  CreatedDate: Date,
  CreatedBy: number,
  MapType: string
}

export interface RouteType {
  Id: number,
  Name: string,
  Description: string,
  CreatedDate: Date,
  CreatedBy: number,
  MapType: string
}

export interface Connector {
  Id: number,
  Name: string,
  Data: string,
  TypeId: number,
  Map: string,
  CreatedDate: Date,
  CreatedBy: number,
  ConnectorType: string,
  Party: string
}

export interface PartnerGroup {
  Id: number,
  Description: string,
  SourceParty: string,
  DestinationParty: string,
  CreatedDate: Date,
  CreatedBy: number
}

export interface Routes {
  Id: number,
  ERPCustomerID: string,
  Name: string,
  SourceParty: string,
  DestinationParty: string,
  SourceConnector: string,
  DestinationConnector: string,
  PartyGroup: string,
  RouteType: string,
  CreatedDate: Date,
  CreatedBy: number
}

export interface CustomerProductCatalog {
  Id: number,
  ERPCustomerID: string,
  TCIN: string,
  PartnerSKU: string,
  ProductTitle: string,
  ItemType: string,
  Relationship: string,
  PublishStatus: string,
  DataUpdatesStatus: string,
  CreatedDate: Date,
  CreatedBy: number
}

export interface HistoryCustomerProductCatalog {
  ERPCustomerID: string,
  FileName: string,
  CreatedDate: Date,
  CreatedBy: number
}

export interface CarrierLoadTender {
  Id: number,
  Status: string,
  CustomerId: string,
  InboundEDIId: number,
  DocumentDate: Date,
  Purpose: string,
  On_N1: string,
  FreightBillParty: string,
  EquipmentDetails: string,
  PurchaseOrderNumber: string,
  OrderReferenceIdentification: string,
  OrderQuantity: CarrierLoadTender,
  OrderWeightUnit: CarrierLoadTender,
  OrderWeight: CarrierLoadTender,
  OrderVolumeUnit: CarrierLoadTender,
  OrderVolume: CarrierLoadTender,
  CreatedDate: Date,
  CreatedBy: number
}

export interface ProductUploadPrices {
  Id: number,
  ERPCustomerID: string,
  ItemID: string,
  ListPrice: string,
  OffPrice: string,
  PromoStartDate: Date,
  PromoEndDate: Date,
  CreatedDate: Date,
  CreatedBy: number,
  OldListPrice: string,
  OldPromoPrice: string
}

export interface RouteData {
  Id: number,
  RouteId: number,
  Type: number,
  OrderId: string,
  Data: string,
  CreatedDate: Date,
  CreatedBy: number,
  ModifiedDate: Date,
  ModifiedBy: number
}

export interface HeaderMapping {
  [key: string]: string;
}

export interface EdiCountFile {
  customerName: string;
  docDate: string;
  counT_204: number;
  counT_214: number;
}

export interface Inventory {
  BatchID: string,
  ItemCount: number,
  StartDate: Date,
  FinishDate: Date,
  PageCount: number,
  Status: string,
  RouteType: string,
  CustomerID: string
}

export interface BatchWiseInventory {
  CustomerID: number,
  ItemId: string,
  CustomerItemCode: string,
  ETA_Date: Date,
  ETA_Qty: number,
  Total_ATS: number,
  ATS_L10: number,
  ATS_L21: number,
  ATS_L28: number,
  ATS_L30: number,
  ATS_L34: number,
  ATS_L35: number,
  ATS_L36: number,
  ATS_L37: number,
  ATS_L40: number,
  ATS_L41: number,
  ATS_L55: number,
  ATS_L60: number,
  ATS_L70: number,
  ATS_L91: number,
  Status: number,
  CreatedDate: Date,
  CreatedBy: number,
  ModifiedDate: Data,
  ModifiedBy: number
}

export interface StatesModel{
  SCS_Code: string;
  Description: string;
}

export interface PrepareItemData {
  ID: number,
  UserID: number,
  itemTypeID: string,
  status: string,
  CustomerID: number,
  CreatedDate: Date,
  ModifiedDate: Date,
  fileName:string
}

export interface InvFeedFromNDC {
  Id: number,
  sku: string,
  itemID: string,
  description: string,
  manufacturerName: string,
  uom: string,
  ndcItemID: string,
  productName: string,
  primaryCategoryName: string,
  secondaryCategoryName: string,
  qty: number,
  unitPrice: number,
  ETAQty: number,
  ETADate: Date,
  CreatedDate: Date,
  CreatedBy: number,
  SupplierName: String
}

export interface PurchaseOrder {
  Id: number,
  Status: string,
  CustomerId: string,
  OrderDate: Date,
  PONumber: string,
  SupplierID: string,
  LocationID: string,
  VExpectedDate: Date,
  ReferenceNo: string,
  CustomerOrderNo: string,
  ExternalId: string,
  ShippingMethod: string,
  ShipToId: string,
  ShipToName: string,
  ShipToAddress1: string,
  ShipToAddress2: string,
  ShipToCity: string,
  ShipToState: string,
  ShipToZip: string,
  ShipToCountry: string,
  ShipToEmail: string,
  ShipToPhone: string,
  BillToId: string,
  BillToName: string,
  BillToAddress1: string,
  BillToAddress2: string,
  BillToCity: string,
  BillToState: string,
  BillToZip: string,
  BillToCountry: string,
  BillToEmail: string,
  BillToPhone: string,
  BuyerId: string,
  BuyerName: string,
  BuyerAddress1: string,
  BuyerAddress2: string,
  BuyerCity: string,
  BuyerState: string,
  BuyerZip: string,
  BuyerCountry: string,
  BuyerEmail: string,
  BuyerPhone: string,
  CreatedDate: Date,
  CreatedBy: number,
  TotalQty: number,
}

export interface Supplier {
  Id: number,
  supplierID: string,
  name: string,
  status: string,
  CreatedDate: Date,
  CreatedBy: number
}

export interface ShipmentFromNDC {
  ID: number,
  ShipmentID: string,
  TransactionDate: Date,
  PoNumber: number,
  PoDate: Date,
  Status: string,
  SCACCode: string,
  Routing: string,
  ShippingDate: Date,
  N1_ShipID: string,
  ShippingName: string,
  ShippingAddress1: string,
  ShippingAddress2: string,
  ShippingCity: string,
  ShippingState: string,
  ShippingZip: string,
  ShippingCountry: string,
  SellerID: string,
  ShippingFromName: string,
  ShippingFromAddress1: string,
  ShippingFromAddress2: string,
  ShippingFromCity: string,
  ShippingFromState: string,
  ShippingFromCountry: string,
}

export interface SalesInvoiceNDC {
  id: number;
  invoiceNo: number;
  invoiceDate: Date;
  poNumber: string;
  status: string;
  scacCode: string;
  routing: string;
  shippingDate: Date;
  shippingName: string;
  shippingToNo: string;
  shippingAddress1: string;
  shippingAddress2: string;
  shippingCity: string;
  shippingState: string;
  shippingZip: string;
  shippingCountry: string;
  sellerID: string;
  invoiceTerms: string;
  frieght: number;
  handlingAmount: number;
  salesTax: number;
  invoiceAmount: number;
  trackingNo: string;
  createdDate: Date;
  createdBy: number;
  modifiedDate: Date;
  modifiedBy: number;
}

export interface ShipmentDetailFromNDC {
  ID: number,
  ShipmentFromNDC_ID: number,
  ShipmentID: string,
  PoNumber: number,
  Status: string,
  EDILineID: number,
  ItemID: string,
  QTY: number,
  SupplierStyle: string,
  UPC: string,
  SKU: string,
  TrackingNo: string,
  SSCC: string,
  BOLNO:string
}

export interface SalesInvoiceFromNDC {
  ID: number,
  ShipmentFromNDC_ID: number,
  ShipmentID: string,
  PoNumber: number,
  Status: string,
  InvoiceDate: number,
  TrackingNo: string,
}
