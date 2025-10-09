DELETE FROM Maps
GO

INSERT INTO Maps (id, Name, TypeId, Map, CreatedBy)
VALUES(1, '850', 1, '{
  "orderDate": "#customfunction(EDI.Processor,EDI.Maps.Transformations.formatDate,#valueof($.Content[?(@.Name==''BEG'')].Content[4].E),MM/dd/yyyy)",
  "shipDate": "#customfunction(EDI.Processor,EDI.Maps.Transformations.formatDate,#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''DTM'')]),0,830,1),MM/dd/yyyy)",
  "orderNumber": "#valueof($.Content[?(@.Name==''BEG'')].Content[2].E)",
  "customerName": "@@CUSTOMER@@",
  "vendorNumber": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''REF'')]),0,IA,1)",
  "label": "#valueof($.Content[?(@.Name==''BEG'')].Content[2].E)",
  "status": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findMapTagValue, #valueof($.Content[?(@.Name==''BEG'')].Content[0].E),purposeMap)",
  "marketplace": "@@MARKETPLACE@@",
  "poType": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findMapTagValue, #valueof($.Content[?(@.Name==''BEG'')].Content[1].E),orderTypeMap)",
  "plannedDeliveryDate": "#customfunction(EDI.Processor,EDI.Maps.Transformations.formatDate,#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''DTM'')]),0,038,1),MM/dd/yyyy)",
  "brand": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''REF'')]),0,2P,1)",
  "terms": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''TD5'')]),3,ZZ,4)",
  "deptNo": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''REF'')]),0,DP,1)",
  "refNo": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''REF'')]),0,AN,1)",
  "customerOrderNo": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''REF'')]),0,CO,1)",
  "externalid": "",
  "shippingPrice": 0,
  "shippingTax": 0,
  "shippingTaxes": [],
  "shippingMethod": "#concat(#customfunction(EDI.Processor,EDI.Maps.Transformations.findFirstLoopTagValue,#valueof($.Content[?(@.Name==''L_PO1'')]),TD5,1,ZZ,2), #customfunction(EDI.Processor,EDI.Maps.Transformations.findFirstLoopTagValue,#valueof($.Content[?(@.Name==''L_PO1'')]),TD5,1,ZZ,11))",
  "carrier": {
    "name": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findFirstLoopTagValue,#valueof($.Content[?(@.Name==''L_PO1'')]),TD5,1,ZZ,2)",
    "service": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findFirstLoopTagValue,#valueof($.Content[?(@.Name==''L_PO1'')]),TD5,1,ZZ,11)",
  },
  "paymentType": "",
  "discount": 0,
  "department": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''REF'')]),0,DP,1)",
  "division": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''REF'')]),0,19,1)",
  "merchandiseType": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''REF'')]),0,MR,1)",
  "deliveryReference": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''REF'')]),0,KK,1)",
  "placementMethod": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''REF'')]),0,EVI,1)",
  "shipping": {
    "value": 0,
    "tax": 0,
    "taxes": []
  },
  "shipNotBefore": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''DTM'')]),0,037,1)",
  "cancelDate": "#customfunction(EDI.Processor,EDI.Maps.Transformations.formatDate,#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''DTM'')]),0,001,1),MM/dd/yyyy)",
  "shipmentDate": "#customfunction(EDI.Processor,EDI.Maps.Transformations.formatDate,#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''DTM'')]),0,10,1),MM/dd/yyyy)",
  "shipAccountNumber": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N9'')]),N9,N9,0,TH,1)",
  "packingSlipUrl": "#valueof($.Content[?(@.Name==''MSG'')].Content[0].E)",
  "priceIdentifier": {
    "code": "#valueof($.Content[?(@.Name==''CTP'')].Content[1].E)",
    "name": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findMapTagValue, #valueof($.Content[?(@.Name==''CTP'')].Content[1].E),priceIdentifierMap)"
  },
  "paymentMode": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findMapTagValue, #valueof($.Content[?(@.Name==''FOB'')].Content[0].E),paymentMethodMap)",
  "allowanceOrCharge": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findMapTagValue, #valueof($.Content[?(@.Name==''SAC'')].Content[0].E),allowanceChargeMap)",
  "grossAmount": "#valueof($.Content[?(@.Name==''AMT'')].Content[1].E)",
  "supplier": {
    "id": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,SU,3)",
    "name": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,SU,1)",
    "code": '''',
    "warehouseZip": "",
    "phone": '''',
    "email": '''',
    "fax": ''''
  },
  "shipToCustomer": {
    "id": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,ST,3)",
    "locationCode": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,ST,3)",
    "name": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,ST,1)",
    "address1": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N3,0,ST,0)",
    "address2": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N3,0,ST,1)",
    "city": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,ST,0)",
    "state": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,ST,1)",
    "zip": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,ST,2)",
    "country": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,ST,3)",
    "phone": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,ST,3)",
    "email": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,ST,5)"
  },
  "billing": {
    "id": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,BT,3)",
    "locationCode": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,BT,3)",
    "name": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,BT,1)",
    "address1": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N3,0,BT,0)",
    "address2": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N3,0,BT,1)",
    "city": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BT,0)",
    "state": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BT,1)",
    "zip": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BT,2)",
    "country": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BT,3)",
    "phone": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,BT,3)",
    "email": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,BT,5)"
  },
  "buyer": {
    "id": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,BY,3)",
    "locationCode": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,BY,3)",
    "name": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,BY,1)",
    "address1": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N3,0,BY,0)",
    "address2": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N3,0,BY,1)",
    "city": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BY,0)",
    "state": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BY,1)",
    "zip": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BY,2)",
    "country": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BY,3)",
    "phone": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,BY,3)",
    "email": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,BY,5)"
  },
  "items": {
    "#loop($.Content[?(@.Name==''L_PO1'')])": {
      "lineNo": "#tostring(#add(#currentindex(), 1))",
      "ediLineId": "#currentvalueatpath($.Content[?(@.Name==''PO1'')].Content[0].E)",
      "upc": "#concat(#ifcondition(#currentvalueatpath($.Content[?(@.Name==''PO1'')].Content[7].E),UP,#currentvalueatpath($.Content[?(@.Name==''PO1'')].Content[8].E),),#ifcondition(#currentvalueatpath($.Content[?(@.Name==''PO1'')].Content[5].E),UP,#currentvalueatpath($.Content[?(@.Name==''PO1'')].Content[6].E),))",
      "partNo": "#concat(#ifcondition(#currentvalueatpath($.Content[?(@.Name==''PO1'')].Content[7].E),UP,#currentvalueatpath($.Content[?(@.Name==''PO1'')].Content[6].E),),#ifcondition(#currentvalueatpath($.Content[?(@.Name==''PO1'')].Content[5].E),UP,#currentvalueatpath($.Content[?(@.Name==''PO1'')].Content[8].E),))",
      "vendorStyle": "",
      "description": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_PID'')]),PID,PID,0,F,4)",
      "quantity": "#tointeger(#currentvalueatpath($.Content[?(@.Name==''PO1'')].Content[1].E))",
      "price": "#todecimal(#currentvalueatpath($.Content[?(@.Name==''PO1'')].Content[3].E))",
      "uom": "#currentvalueatpath($.Content[?(@.Name==''PO1'')].Content[2].E)",
      "discount": 0,
      "taxes": [],
      "department": "#currentvalueatpath($.Content[?(@.Name==''REF'')].Content[0].E)",
      "plannedDeliveryDate": "",
      "salesReference": "#currentvalueatpath($.Content[?(@.Name==''REF'')].Content[1].E)",
      "salesEventEndDate": "#customfunction(EDI.Processor,EDI.Maps.Transformations.formatDate,#currentvalueatpath($.Content[?(@.Name==''REF'')].Content[2].E),MM/dd/yyyy)",
      "acceptedQuantity": "",
      "allowances": "#currentvalueatpath($.Content[?(@.Name==''SAC'')].Content[0].E)",
      "rebate": "#currentvalueatpath($.Content[?(@.Name==''SAC'')].Content[1].E)",
      "locations": "#customfunction(EDI.Processor,EDI.Maps.Transformations.createSDQArray,#currentvalueatpath($.Content[?(@.Name==''SDQ'')]))"
    }
  },  
}', 1)
GO

INSERT INTO Maps (id, Name, TypeId, Map, CreatedBy)
VALUES(2, '850 DB Fields', 2, '{
  "OrderDate": "#customfunction(EDI.Processor,EDI.Maps.Transformations.formatDate,#valueof($.Content[?(@.Name==''BEG'')].Content[4].E),MM/dd/yyyy)",
  "OrderNumber": "#valueof($.Content[?(@.Name==''BEG'')].Content[2].E)",
  "VendorNumber": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''REF'')]),0,IA,1)",
  "OrderType": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findMapTagValue, #valueof($.Content[?(@.Name==''BEG'')].Content[1].E),orderTypeMap)",
  "ReferenceNo": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''REF'')]),0,CO,1)",
  "CustomerOrderNo": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''REF'')]),0,CO,1)",
  "ExternalId": "",
  "ShippingMethod": "#concat(#concat(#customfunction(EDI.Processor,EDI.Maps.Transformations.findFirstLoopTagValue,#valueof($.Content[?(@.Name==''L_PO1'')]),TD5,1,ZZ,2), '' ''), #customfunction(EDI.Processor,EDI.Maps.Transformations.findFirstLoopTagValue,#valueof($.Content[?(@.Name==''L_PO1'')]),TD5,1,ZZ,11))",
  "ShipToId": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,ST,3)",
  "ShipToName": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,ST,1)",
  "ShipToAddress1": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N3,0,ST,0)",
  "ShipToAddress2": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N3,0,ST,1)",
  "ShipToCity": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,ST,0)",
  "ShipToState": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,ST,1)",
  "ShipToZip": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,ST,2)",
  "ShipToCountry": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,ST,3)",
  "ShipToPhone": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,ST,3)",
  "ShipToEmail": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,ST,5)",
  "BillToId": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,BT,3)",
  "BillToName": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,BT,1)",
  "BillToAddress1": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N3,0,BT,0)",
  "BillToAddress2": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N3,0,BT,1)",
  "BillToCity": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BT,0)",
  "BillToState": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BT,1)",
  "BillToZip": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BT,2)",
  "BillToCountry": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BT,3)",
  "BillToPhone": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,BT,3)",
  "BillToEmail": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,BT,5)",
  "BuyerId": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,BY,3)",
  "BuyerName": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,BY,1)",
  "BuyerAddress1": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N3,0,BY,0)",
  "BuyerAddress2": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N3,0,BY,1)",
  "BuyerCity": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BY,0)",
  "BuyerState": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BY,1)",
  "BuyerZip": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BY,2)",
  "BuyerCountry": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BY,3)",
  "BuyerPhone": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,BY,3)",
  "BuyerEmail": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,BY,5)",
  "IsStoreOrder": "#ifcondition(#length(#valueof($.Content[?(@.Name==''L_PO1'')].Content[?(@.Name==''SDQ'')])),0,false,true)"
}', 1)
GO

INSERT INTO Maps (id, Name, TypeId, Map, CreatedBy)
VALUES (3, 'SAMSCLUB 856', 3, '{
	"StartNodes": [
		{ "Name": "BSN", "Data": [ "00", "#valueof($.fulfillments[0].docNo)", "#customfunction(EDI.Processor,EDI.Maps.Transformations.formatDate,#valueof($.fulfillments[0].date),yyyyMMdd)", "#customfunction(EDI.Processor,EDI.Maps.Transformations.getCurrentTime,)", "0001" ] },
		{ "Name": "HL", "Data": [ "1", "", "S" ] },
		{ "Name": "TD5", "Data": [ "B", "2", "#valueof($.fulfillments[0].shipment.carrier)", "M", "", "", "", "", "", "", "", "#valueof($.fulfillments[0].shipment.service)" ] },
		{ "Name": "REF", "Data": [ "BM", "#valueof($.fulfillments[0].packages[0].trackingNo)" ] },
		{ "Name": "DTM", "Data": [ "011", "#customfunction(EDI.Processor,EDI.Maps.Transformations.formatDate,#valueof($.plannedDeliveryDate),yyyyMMdd)" ] },
		{ "Name": "N1", "Data": [ "BY", "SAMS CLUB 4727", "UL" , "0605388007940" ] },
		{ "Name": "N1", "Data": [ "SF", "#valueof($.fulfillments[0].shipFrom.name)" ] },
		{ "Name": "HL", "Data": [ "2", "1", "O" ] },
		{ "Name": "PRF", "Data": [ "#valueof($.customerPO)", "", "", "#customfunction(EDI.Processor,EDI.Maps.Transformations.formatDate,#valueof($.orderDate),yyyyMMdd)" ] },
		{ "Name": "REF", "Data": [ "IA", "587462631" ] },
		{ "Name": "REF", "Data": [ "CO", "#valueof($.customerOrderNo)" ] },
		{ "Name": "REF", "Data": [ "AN", "#valueof($.orderRefNo)" ] },
	],
	"Packs": {
		"#loop($.fulfillments[0].packages)": {
			"Data": [
				{ "Name": "HL", "Data": [ "@HL_IDX@", "2", "P" ] },
				{ "Name": "MAN", "Data": [ "CP", "#currentvalueatpath($.trackingNo)" ] },
			],
			"Items": {
				"#loop($.items)": {
					"Data": [
						{ "Name": "HL", "Data": [ "@HL_IDX@", "@HL_P_IDX@", "I" ] },
						{ "Name": "LIN", "Data": [ "", "IN", "#currentvalueatpath($.vendorNo)", "UP", "#currentvalueatpath($.upc)" ] },
						{ "Name": "SN1", "Data": [ "", "#currentvalueatpath($.quantity)", "EA" ] },
					]
				} 
			} 
		}
	},
	"EndNodes": [
		{ "Name": "CTT", "Data": [ "@HLCOUNT@" ] },
	]
}', 1)
GO

INSERT INTO Maps (id, Name, TypeId, Map, CreatedBy)
VALUES (4, '810', 4, '{
	"orderNumber": "#valueof($.customerPO)",
	"shipDate": "#valueof($.fulfillments[0].date)",
	"products": {
		"#loop($.fulfillments[0].items)": {
			"partNumber": "#currentvalueatpath($.itemUpc)",
			"quantityOrdered": "#currentvalueatpath($.quantity)",
			"quantityShipped": "#currentvalueatpath($.quantity)",
		}
	}
}', 1)
GO

INSERT INTO Maps (id, Name, TypeId, Map, CreatedBy)
VALUES (5, 'SAMSCLUB 810', 5, '{
	"StartNodes": [
		{ "Name": "BIG", "Data": [ "#customfunction(EDI.Processor,EDI.Maps.Transformations.formatDate,#valueof($.fulfillments[0].date),yyyyMMdd)", "#valueof($.fulfillments[0].docNo)", "#customfunction(EDI.Processor,EDI.Maps.Transformations.formatDate,#valueof($.fulfillments[0].date),yyyyMMdd)", "#valueof($.customerPO)" ] },
		{ "Name": "REF", "Data": [ "DP", "00063" ] },
		{ "Name": "REF", "Data": [ "IA", "587462631" ] },
		{ "Name": "REF", "Data": [ "MR", "0020" ] },
		{ "Name": "N1", "Data": [ "SU", "LALTITUDE LLC" ] },
		{ "Name": "N1", "Data": [ "ST", "SAMS CLUB 4727", "UL", "0605388007940" ] },
		{ "Name": "N3", "Data": [ "2101 SE SIMPLE SAVINGS DR" ] },
		{ "Name": "N4", "Data": [ "BENTONVILLE", "AR", "72712", "US" ] },
		{ "Name": "ITD", "Data": [ "05", "15", "", "", "", "", "15" ] },
		{ "Name": "DTM", "Data": [ "011", "#customfunction(EDI.Processor,EDI.Maps.Transformations.formatDate,#valueof($.fulfillments[0].date),yyyyMMdd)" ] },
		{ "Name": "FOB", "Data": [ "CC" ] },
	],
	"Items": {
		"#loop($.fulfillments[0].items)": {
			"Data": [
				{ "Name": "IT1", "Data": [ "#currentvalueatpath($.ediLineId)", "#currentvalueatpath($.quantity)", "EA", "#currentvalueatpath($.amount)", "", "IN", "#currentvalueatpath($.itemSku)", "UP", "#currentvalueatpath($.itemUpc)" ] },
			],
		}
	},
	"EndNodes": [
		{ "Name": "TDS", "Data": [ "#customfunction(EDI.Processor,EDI.Maps.Transformations.formatTotalAmount,#sumatpath(#valueof($.fulfillments[0].items),$.amount))" ] },
		{ "Name": "ISS", "Data": [ "#sumatpath(#valueof($.fulfillments[0].items),$.quantity)", "EA" ] },
		{ "Name": "CTT", "Data": [ "#length(#valueof($.fulfillments[0].items))" ] },
	]
}', 1)
GO

INSERT INTO Maps (id, Name, TypeId, Map, CreatedBy)
VALUES (6, 'SAMSCLUB 855', 6, '{
	"StartNodes": [
		{ "Name": "BAK", "Data": [ "00", "AD", "#valueof($.customerPO)", "#customfunction(EDI.Processor,EDI.Maps.Transformations.formatDate,#valueof($.orderDate),yyyyMMdd)", "", "", "", "", "#customfunction(EDI.Processor,EDI.Maps.Transformations.formatDate,#valueof($.orderDate),yyyyMMdd)" ] },
		{ "Name": "REF", "Data": [ "DP", "#valueof($.deptNo)" ] },
		{ "Name": "REF", "Data": [ "MR", "0020" ] },
		{ "Name": "REF", "Data": [ "IA", "587462631" ] },
		{ "Name": "REF", "Data": [ "CO", "#valueof($.customerOrderNo)" ] },
		{ "Name": "REF", "Data": [ "AN", "#valueof($.orderRefNo)" ] },
		{ "Name": "N9", "Data": [ "ZZ", "US" ] },
		{ "Name": "N1", "Data": [ "BY", "SAMS CLUB 4727", "UL", "0605388007940" ] },
		{ "Name": "N1", "Data": [ "SU", "LALTITUDE LLC" ] },
	],
	"Items": {
		"#loop($.items)": {
			"Data": [
				{ "Name": "PO1", "Data": [ "#currentvalueatpath($.ediLineId)", "#currentvalueatpath($.quantity)", "EA", "", "", "IN", "#currentvalueatpath($.partNo)", "UP", "#currentvalueatpath($.upc)" ] },
				{ "Name": "ACK", "Data": [ "#ifcondition(#currentvalueatpath($.quantity),#currentvalueatpath($.quantityCommitted),IA,IB)", "#currentvalueatpath($.quantity)", "EA" ] },
			],
		}
	},
	"EndNodes": [
		{ "Name": "CTT", "Data": [ "#length(#valueof($.items))" ] },
	]
}', 1)
GO

INSERT INTO Maps (id, Name, TypeId, Map, CreatedBy)
VALUES(7, '850 NordStorm Store', 1, '{
  "orderDate": "#customfunction(EDI.Processor,EDI.Maps.Transformations.formatDate,#valueof($.Content[?(@.Name==''BEG'')].Content[4].E),MM/dd/yyyy)",
  "shipDate": "#customfunction(EDI.Processor,EDI.Maps.Transformations.formatDate,#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''DTM'')]),0,037,1),MM/dd/yyyy)",
  "orderNumber": "#valueof($.Content[?(@.Name==''BEG'')].Content[2].E)",
  "customerName": "@@CUSTOMER@@",
  "vendorNumber": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''REF'')]),0,IA,1)",
  "label": "#valueof($.Content[?(@.Name==''BEG'')].Content[2].E)",
  "status": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findMapTagValue, #valueof($.Content[?(@.Name==''BEG'')].Content[0].E),purposeMap)",
  "marketplace": "@@MARKETPLACE@@",
  "poType": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findMapTagValue, #valueof($.Content[?(@.Name==''BEG'')].Content[1].E),orderTypeMap)",
  "plannedDeliveryDate": "#customfunction(EDI.Processor,EDI.Maps.Transformations.formatDate,#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''DTM'')]),0,038,1),MM/dd/yyyy)",
  "brand": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''REF'')]),0,2P,1)",
  "terms": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''TD5'')]),3,ZZ,4)",
  "deptNo": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''REF'')]),0,DP,1)",
  "refNo": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''REF'')]),0,AN,1)",
  "customerOrderNo": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''REF'')]),0,CO,1)",
  "externalid": "",
  "shippingPrice": 0,
  "shippingTax": 0,
  "shippingTaxes": [],
  "shippingMethod": "#concat(#customfunction(EDI.Processor,EDI.Maps.Transformations.findFirstLoopTagValue,#valueof($.Content[?(@.Name==''L_PO1'')]),TD5,1,ZZ,2), #customfunction(EDI.Processor,EDI.Maps.Transformations.findFirstLoopTagValue,#valueof($.Content[?(@.Name==''L_PO1'')]),TD5,1,ZZ,11))",
  "carrier": {
    "name": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findFirstLoopTagValue,#valueof($.Content[?(@.Name==''L_PO1'')]),TD5,1,ZZ,2)",
    "service": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findFirstLoopTagValue,#valueof($.Content[?(@.Name==''L_PO1'')]),TD5,1,ZZ,11)",
  },
  "paymentType": "",
  "discount": 0,
  "department": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''REF'')]),0,DP,1)",
  "division": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''REF'')]),0,19,1)",
  "merchandiseType": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''REF'')]),0,MR,1)",
  "deliveryReference": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''REF'')]),0,KK,1)",
  "placementMethod": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''REF'')]),0,EVI,1)",
  "shipping": {
    "value": 0,
    "tax": 0,
    "taxes": []
  },
  "shipNotBefore": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''DTM'')]),0,037,1)",
  "cancelDate": "#customfunction(EDI.Processor,EDI.Maps.Transformations.formatDate,#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''DTM'')]),0,001,1),MM/dd/yyyy)",
  "shipmentDate": "#customfunction(EDI.Processor,EDI.Maps.Transformations.formatDate,#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''DTM'')]),0,010,1),MM/dd/yyyy)",
  "shipAccountNumber": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N9'')]),N9,N9,0,TH,1)",
  "packingSlipUrl": "#valueof($.Content[?(@.Name==''MSG'')].Content[0].E)",
  "priceIdentifier": {
    "code": "#valueof($.Content[?(@.Name==''CTP'')].Content[1].E)",
    "name": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findMapTagValue, #valueof($.Content[?(@.Name==''CTP'')].Content[1].E),priceIdentifierMap)"
  },
  "paymentMode": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findMapTagValue, #valueof($.Content[?(@.Name==''FOB'')].Content[0].E),paymentMethodMap)",
  "allowanceOrCharge": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findMapTagValue, #valueof($.Content[?(@.Name==''SAC'')].Content[0].E),allowanceChargeMap)",
  "grossAmount": "#valueof($.Content[?(@.Name==''AMT'')].Content[1].E)",
  "supplier": {
    "id": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,SU,3)",
    "name": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,SU,1)",
    "code": '''',
    "warehouseZip": "",
    "phone": '''',
    "email": '''',
    "fax": ''''
  },
  "shipToCustomer": {
    "id": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,ST,3)",
    "locationCode": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,ST,3)",
    "name": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,ST,1)",
    "address1": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N3,0,ST,0)",
    "address2": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N3,0,ST,1)",
    "city": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,ST,0)",
    "state": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,ST,1)",
    "zip": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,ST,2)",
    "country": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,ST,3)",
    "phone": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,ST,3)",
    "email": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,ST,5)"
  },
  "billing": {
    "id": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,BT,3)",
    "locationCode": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,BT,3)",
    "name": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,BT,1)",
    "address1": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N3,0,BT,0)",
    "address2": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N3,0,BT,1)",
    "city": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BT,0)",
    "state": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BT,1)",
    "zip": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BT,2)",
    "country": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BT,3)",
    "phone": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,BT,3)",
    "email": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,BT,5)"
  },
  "buyer": {
    "id": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,BY,3)",
    "locationCode": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,BY,3)",
    "name": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,BY,1)",
    "address1": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N3,0,BY,0)",
    "address2": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N3,0,BY,1)",
    "city": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BY,0)",
    "state": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BY,1)",
    "zip": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BY,2)",
    "country": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BY,3)",
    "phone": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,BY,3)",
    "email": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,BY,5)"
  },
  "items": {
    "#loop($.Content[?(@.Name==''L_PO1'')])": {
      "lineNo": "#tostring(#add(#currentindex(), 1))",
      "ediLineId": "#currentvalueatpath($.Content[?(@.Name==''PO1'')].Content[0].E)",
      "upc": "#concat(#ifcondition(#currentvalueatpath($.Content[?(@.Name==''PO1'')].Content[7].E),UP,#currentvalueatpath($.Content[?(@.Name==''PO1'')].Content[8].E),),#ifcondition(#currentvalueatpath($.Content[?(@.Name==''PO1'')].Content[5].E),UP,#currentvalueatpath($.Content[?(@.Name==''PO1'')].Content[6].E),))",
      "partNo": "#concat(#ifcondition(#currentvalueatpath($.Content[?(@.Name==''PO1'')].Content[7].E),UP,#currentvalueatpath($.Content[?(@.Name==''PO1'')].Content[6].E),),#ifcondition(#currentvalueatpath($.Content[?(@.Name==''PO1'')].Content[5].E),UP,#currentvalueatpath($.Content[?(@.Name==''PO1'')].Content[8].E),))",
      "vendorStyle": "",
      "description": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_PID'')]),PID,PID,0,F,4)",
      "quantity": "#tointeger(#currentvalueatpath($.Content[?(@.Name==''PO1'')].Content[1].E))",
      "price": "#todecimal(#currentvalueatpath($.Content[?(@.Name==''PO1'')].Content[3].E))",
      "uom": "#currentvalueatpath($.Content[?(@.Name==''PO1'')].Content[2].E)",
      "discount": 0,
      "taxes": [],
      "department": "#currentvalueatpath($.Content[?(@.Name==''REF'')].Content[0].E)",
      "plannedDeliveryDate": "",
      "salesReference": "#currentvalueatpath($.Content[?(@.Name==''REF'')].Content[1].E)",
      "salesEventEndDate": "#customfunction(EDI.Processor,EDI.Maps.Transformations.formatDate,#currentvalueatpath($.Content[?(@.Name==''REF'')].Content[2].E),MM/dd/yyyy)",
      "acceptedQuantity": "",
      "allowances": "#currentvalueatpath($.Content[?(@.Name==''SAC'')].Content[0].E)",
      "rebate": "#currentvalueatpath($.Content[?(@.Name==''SAC'')].Content[1].E)",
      "locations": "#customfunction(EDI.Processor,EDI.Maps.Transformations.createSDQArray,#currentvalueatpath($.Content[?(@.Name==''SDQ'')]))"
    }
  },  
}', 1)
GO

INSERT INTO Maps (id, Name, TypeId, Map, CreatedBy)
VALUES(8, '850 DB Fields NordStorm Store', 2, '{
  "OrderDate": "#customfunction(EDI.Processor,EDI.Maps.Transformations.formatDate,#valueof($.Content[?(@.Name==''BEG'')].Content[4].E),MM/dd/yyyy)",
  "OrderNumber": "#valueof($.Content[?(@.Name==''BEG'')].Content[2].E)",
  "VendorNumber": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''REF'')]),0,IA,1)",
  "OrderType": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findMapTagValue, #valueof($.Content[?(@.Name==''BEG'')].Content[1].E),orderTypeMap)",
  "ReferenceNo": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''REF'')]),0,CO,1)",
  "CustomerOrderNo": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findTagValue,#valueof($.Content[?(@.Name==''REF'')]),0,CO,1)",
  "ExternalId": "",
  "ShippingMethod": "#concat(#concat(#customfunction(EDI.Processor,EDI.Maps.Transformations.findFirstLoopTagValue,#valueof($.Content[?(@.Name==''L_PO1'')]),TD5,1,ZZ,2), '' ''), #customfunction(EDI.Processor,EDI.Maps.Transformations.findFirstLoopTagValue,#valueof($.Content[?(@.Name==''L_PO1'')]),TD5,1,ZZ,11))",
  "ShipToId": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,ST,3)",
  "ShipToName": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,ST,1)",
  "ShipToAddress1": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N3,0,ST,0)",
  "ShipToAddress2": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N3,0,ST,1)",
  "ShipToCity": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,ST,0)",
  "ShipToState": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,ST,1)",
  "ShipToZip": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,ST,2)",
  "ShipToCountry": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,ST,3)",
  "ShipToPhone": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,ST,3)",
  "ShipToEmail": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,ST,5)",
  "BillToId": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,BT,3)",
  "BillToName": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,BT,1)",
  "BillToAddress1": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N3,0,BT,0)",
  "BillToAddress2": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N3,0,BT,1)",
  "BillToCity": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BT,0)",
  "BillToState": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BT,1)",
  "BillToZip": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BT,2)",
  "BillToCountry": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BT,3)",
  "BillToPhone": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,BT,3)",
  "BillToEmail": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,BT,5)",
  "BuyerId": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,BY,3)",
  "BuyerName": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N1,0,BY,1)",
  "BuyerAddress1": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N3,0,BY,0)",
  "BuyerAddress2": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N3,0,BY,1)",
  "BuyerCity": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BY,0)",
  "BuyerState": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BY,1)",
  "BuyerZip": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BY,2)",
  "BuyerCountry": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,N4,0,BY,3)",
  "BuyerPhone": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,BY,3)",
  "BuyerEmail": "#customfunction(EDI.Processor,EDI.Maps.Transformations.findLoopTagValue,#valueof($.Content[?(@.Name==''L_N1'')]),N1,PER,0,BY,5)",
  "IsStoreOrder": "#ifcondition(#length(#valueof($.Content[?(@.Name==''L_PO1'')].Content[?(@.Name==''SDQ'')])),0,false,true)"
}', 1)
GO

INSERT INTO Maps (id, Name, TypeId, Map, CreatedBy)
VALUES (9, 'NORDSTROM 856', 3, '{
	"StartNodes": [
		{ "Name": "BSN", "Data": [ "00", "#valueof($.SONo)", "#customfunction(EDI.Processor,EDI.Maps.Transformations.formatDate,#valueof($.ShipDate),yyyyMMdd)", "#customfunction(EDI.Processor,EDI.Maps.Transformations.getCurrentTime,)", "0001" ] },
		{ "Name": "HL", "Data": [ "1", "", "S" ] },
		{ "Name": "TD1", "Data": [ "CTN", "#valueof($.TotalCarton)", "", "", "", "G", "#valueof($.TotalCartonWeight)", "LB" ] },
		{ "Name": "TD5", "Data": [ "B", "2", "#valueof($.Carrier)" ] },
		{ "Name": "REF", "Data": [ "IA", "0005166334" ] },
		{ "Name": "REF", "Data": [ "BM", "#valueof($.BOL)" ] },
		{ "Name": "REF", "Data": [ "MB", "#valueof($.BOL)" ] },
		{ "Name": "REF", "Data": [ "CN", "#valueof($.BOL)" ] },
		{ "Name": "DTM", "Data": [ "011", "#customfunction(EDI.Processor,EDI.Maps.Transformations.formatDate,#valueof($.ShipDate),yyyyMMdd)" ] },
		{ "Name": "N1", "Data": [ "ST", "", "92" , "#valueof($.DC)" ] },
	],
	"Orders": {
		"#loop($.Stores)": {
			"Data": [
		        { "Name": "HL", "Data": [ "@HL_IDX@", "1", "O" ] },
		        { "Name": "PRF", "Data": [ "#valueof($.CustomerPO)", "", "", "#customfunction(EDI.Processor,EDI.Maps.Transformations.formatDate,#valueof($.OrderDate),yyyyMMdd)" ] },
        		{ "Name": "TD1", "Data": [ "CTN", "#currentvalueatpath($.TotalCarton)", "", "", "", "G", "#currentvalueatpath($.TotalCartonWeight)", "LB" ] },
		        { "Name": "REF", "Data": [ "DP", "0893" ] },
		        { "Name": "N1", "Data": [ "BY", "", "92" , "#currentvalueatpath($.StoreNo)" ] },
			],
	        "Packs": {
		        "#loop($.Cartons)": {
			        "Data": [
				        { "Name": "HL", "Data": [ "@HL_IDX@", "@HL_P_IDX@", "P" ] },
				        { "Name": "MAN", "Data": [ "GM", "#currentvalueatpath($.TrackingNo)" ] },
			        ],
			        "Items": {
				        "#loop($.Items)": {
					        "Data": [
						        { "Name": "HL", "Data": [ "@HL_IDX@", "@HL_P_IDX@", "I" ] },
						        { "Name": "LIN", "Data": [ "", "UP", "#currentvalueatpath($.UPC)" ] },
						        { "Name": "SN1", "Data": [ "", "#currentvalueatpath($.Qty)", "EA" ] },
					        ]
				        } 
			        } 
		        }
	        },
		}
	},
	"EndNodes": [
		{ "Name": "CTT", "Data": [ "@HLCOUNT@" ] },
	]
}', 1)
GO