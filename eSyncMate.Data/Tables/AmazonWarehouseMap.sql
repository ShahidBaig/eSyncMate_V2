
CREATE TABLE [dbo].[AmazonWarehouseMap](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[WHSID] [varchar](10) NULL,
	[ShipFromLocationName] [nvarchar](500) NULL,
	[CustomerID] [varchar](50) NOT NULL CONSTRAINT [DF_AmazonWarehouseMap_CustomerID] DEFAULT ('AMA1005')
PRIMARY KEY CLUSTERED
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO



 INSERT INTO [dbo].[AmazonWarehouseMap] (WHSID, ShipFromLocationName, CustomerID)
  VALUES ('L10', 'L10 Port Washington', 'AMA1005'),
         ('L21', 'L21 Easton, PA',      'AMA1005'),
         ('L28', 'L28 Easton, PA',      'AMA1005'),
         ('L29', 'L29 Carlisle, PA',    'AMA1005'),
         ('L35', 'L35 Savannah, GA',    'AMA1005'),
         ('L36', 'L36 Savannah 2, GA',  'AMA1005'),
         ('L37', 'L37 Savannah GA',     'AMA1005'),
         ('L40', 'L40 Lebanon NJ',      'AMA1005'),
         ('L41', 'L41 Flemington, NJ',  'AMA1005'),
         ('L55', 'L55 Whitestown IN',   'AMA1005'),
         ('L56', 'L56 Whitestown IN',   'AMA1005'),
         ('L57', 'L57 Joliet IL',       'AMA1005'),
         ('L60', 'L60 Riverside CA',    'AMA1005'),
         ('L65', 'L65 Patterson CA',    'AMA1005'),
         ('L70', 'L70 Baytown TX',      'AMA1005');
  GO



  {
  "order": {
    "header": {
      "CustomerID": "@CUSTOMERID@",
      "CustomerPO": "#valueof($.AmazonOrderId)",
      "AddressName": "#valueof($.OrderAddress.payload.ShippingAddress.Name)",
      "CompanyName": "#valueof($.OrderAddress.payload.ShippingAddress.Name)",
      "ShipToCode": "",
      "Address1": "#valueof($.OrderAddress.payload.ShippingAddress.AddressLine1)",
      "Address2": "#valueof($.OrderAddress.payload.ShippingAddress.AddressLine2)",
      "City": "#valueof($.OrderAddress.payload.ShippingAddress.City)",
      "State": "#valueof($.OrderAddress.payload.ShippingAddress.StateOrRegion)",
      "Zip": "#valueof($.OrderAddress.payload.ShippingAddress.PostalCode)",
      "Country": "#valueof($.OrderAddress.payload.ShippingAddress.CountryCode)",
      "Phone": "",
      "OrderTakenBy": "API",
      "Instructions": "",
      "ShipViaCode": "FEDX",
      "OrderDate": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.formatDate,#valueof($.PurchaseDate),MM/dd/yyyy)",
      "ShipDate": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.formatDate,#valueof($.LatestShipDate),MM/dd/yyyy)",
      "CancelDate": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.formatDate,#valueof($.LatestShipDate),MM/dd/yyyy)",
      "ExternalID": "#valueof($.AmazonOrderId)"
    },
    "detail": {
      "#loop($.OrderDetail.payload.OrderItems)": {
        "ItemID": "#currentvalueatpath($.ItemID)",
        "OrderQty": "#currentvalueatpath($.QuantityOrdered)",
        "UnitPrice": "#ifcondition(#currentvalueatpath($.QuantityOrdered),0,0,#round(#divide(#currentvalueatpath($.ItemPrice.Amount),#currentvalueatpath($.QuantityOrdered)),2))",
        "Discount": "",
        "WHSID": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.TransAmazonWarehouseID,#valueof($.DefaultShipFromLocationAddress.Name),@CUSTOMERID@)",
        "Remarks": "",
        "ETA_Date": "",
        "TaxAmount": "",
        "APIOrderLineNo": "#currentvalueatpath($.LineNo)"
      }
    }
  }
}