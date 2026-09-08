-- ============================================================================
-- Task 00003 - EDI & API Integration with BizMate EU - W7-11
-- Run in: ESYNCMATE_EU  (also safe in ESYNCMATE_TEST)
-- Run AFTER: 01 (nothing here depends on it, but the route this seeds writes
--            to the ledger, so deploy the ledger first)
--
-- Seeds what ESYNCMATE_EU is missing before an X12 850 can be processed at all
-- (finding F-16): the database was cloned from the marketplace product and has
-- only the two marketplace map types, no X12 maps, and no partner carrying an
-- ISA identity, so RepaintGetOrderRoute-style intake fails with "Required maps
-- ... are missing" before it reads a segment.
--
-- What it adds, all by natural key so it is safe to re-run:
--
--   MapTypes        the six X12 types from InitialSetup/MapTypesData.sql, by
--                   NAME - the ids there (1, 2) are taken in this database by
--                   the marketplace types, and the code looks maps up by
--                   MapTypeName, never by id.
--   Maps            the production 850 map and the 850 DB Fields map, verbatim
--                   from InitialSetup/MapsData.sql. Note the 850 map emits BOTH
--                   lineNo (ordinal) and ediLineId (PO1-01) - see EQ-13 before
--                   changing it.
--   Customers       'BizMate' as the ERP-side party (the role SPARS plays in
--                   the US instance), and 'BELL-D12', the BizMate non-production
--                   test partner. Its ISACustomerID is BELL-D12 because the M1
--                   contract defines partnerId as "the ISA/UNB sender id or the
--                   API party code" - the identity on the wire and the identity
--                   on the wrapper are the same string.
--   CustomerMaps    BELL-D12 -> both maps.
--   PartnerGroups   BELL-D12 - BizMate.
--   ConnectorTypes  'BizMate' and 'EDI Partner'.
--   Connectors      the partner's inbound FOLDER and the BizMate bridge.
--                   BizLink hands eSyncMate its EDI as files in an ordinary
--                   Windows folder, not over SFTP, so the source connector is
--                   ConnectivityType/AuthType 'File' (ConnectorTypesEnum.File,
--                   served by FileConnector). BaseUrl is the folder BizLink
--                   writes into; Url is the folder eSyncMate writes
--                   acknowledgements and outbound documents into.
--
--                   Stored as PLAIN JSON: EncryptionHelper.Decrypt passes a
--                   value through when it does not start with ENC:, and a
--                   folder carries no credential to protect. Note the Connectors
--                   SCREEN only offers SqlServer and Rest, so File, SFTP and FTP
--                   connectors are seeded here and edited with an UPDATE:
--
--                     DECLARE @BS NVARCHAR(4) = REPLICATE(CHAR(92), 2);
--                     UPDATE dbo.Connectors
--                        SET Data = REPLACE(REPLACE(Data,
--                              '<<SET_INBOUND_FOLDER>>',
--                              'D:' + @BS + 'eSyncMate' + @BS + 'EDI' + @BS + 'BELL-D12' + @BS + 'in'),
--                              '<<SET_OUTBOUND_FOLDER>>',
--                              'D:' + @BS + 'eSyncMate' + @BS + 'EDI' + @BS + 'BELL-D12' + @BS + 'out')
--                      WHERE Name = 'BELL-D12 - Inbound Folder';
--
--                   Backslashes must be DOUBLED in the stored value: the Data
--                   column is JSON, and "C:\eSyncMate" is an invalid escape
--                   sequence that the deserialiser refuses. The UI never has
--                   this problem because JSON.stringify escapes for you - it is
--                   only hand-written SQL that has to remember. Forward slashes
--                   work just as well on Windows and avoid the question.
--
--                   The route refuses to run while the placeholders are in
--                   place, so it cannot read from the wrong folder by accident.
--   RouteTypes      600 'BizMate - Receive EDI' (dispatched by RouteEngine).
--   Routes          'BELL-D12 - Receive 850s for BizMate', Active, Minutely /
--                   5, with no Hangfire job: add it to a Flow to schedule it,
--                   or fire it once with POST api/flows/testRun/{routeId}.
--
-- Idempotent - every row guarded by its natural key. Safe to re-run.
-- ============================================================================
SET NOCOUNT ON;

IF DB_NAME() NOT LIKE 'ESYNCMATE%'
BEGIN
    RAISERROR('Wrong database context (%s). Run this script in ESYNCMATE_EU.', 16, 1, @@SERVERNAME);
    RETURN;
END
GO

-- ---------------------------------------------------------------------------
-- MapTypes - by name
-- ---------------------------------------------------------------------------
DECLARE @MapTypeNames TABLE (Name NVARCHAR(500));
INSERT INTO @MapTypeNames VALUES
    ('850 Transformation'), ('850 DB Fields'), ('856 Transformation'),
    ('INV Transformation'), ('810 Transformation'), ('855 Transformation');

DECLARE @Name NVARCHAR(500);
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT Name FROM @MapTypeNames;
OPEN c; FETCH NEXT FROM c INTO @Name;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.MapTypes WHERE Name = @Name)
    BEGIN
        INSERT INTO dbo.MapTypes (Id, Name, CreatedDate, CreatedBy)
        VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.MapTypes), @Name, GETDATE(), 1);
        PRINT 'MapTypes: added ' + @Name;
    END
    FETCH NEXT FROM c INTO @Name;
END
CLOSE c; DEALLOCATE c;
GO

-- ---------------------------------------------------------------------------
-- Maps - the production 850 map and the 850 DB Fields map, verbatim
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Maps WHERE Name = '850')
BEGIN
    INSERT INTO dbo.Maps (Id, Name, TypeId, Map, CreatedDate, CreatedBy)
    VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.Maps), '850',
            (SELECT Id FROM dbo.MapTypes WHERE Name = '850 Transformation'),
            N'{
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
}', GETDATE(), 1);
    PRINT 'Maps: added 850 (850 Transformation)';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Maps WHERE Name = '850 DB Fields')
BEGIN
    INSERT INTO dbo.Maps (Id, Name, TypeId, Map, CreatedDate, CreatedBy)
    VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.Maps), '850 DB Fields',
            (SELECT Id FROM dbo.MapTypes WHERE Name = '850 DB Fields'),
            N'{
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
}', GETDATE(), 1);
    PRINT 'Maps: added 850 DB Fields';
END
GO

-- ---------------------------------------------------------------------------
-- Customers - the BizMate party and the test partner
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE ERPCustomerID = 'BIZMATE')
BEGIN
    INSERT INTO dbo.Customers (Id, Name, ERPCustomerID, ISACustomerID, ISA810ReceiverId, ISA856ReceiverId, Marketplace, CreatedDate, CreatedBy, UseNewAuthentication)
    VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.Customers),
            'BizMate', 'BIZMATE', NULL, NULL, NULL, 'BizMate ERP (EU)', GETDATE(), 1, 0);
    PRINT 'Customers: added BizMate (ERP-side party)';
END

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE ERPCustomerID = 'BELL-D12')
BEGIN
    INSERT INTO dbo.Customers (Id, Name, ERPCustomerID, ISACustomerID, ISA810ReceiverId, ISA856ReceiverId, Marketplace, CreatedDate, CreatedBy, UseNewAuthentication)
    VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.Customers),
            'BELL-D12', 'BELL-D12', 'BELL-D12', 'BELL-D12', 'BELL-D12', 'BizMate test partner (EU non-production)', GETDATE(), 1, 0);
    PRINT 'Customers: added BELL-D12 (ISACustomerID = BELL-D12)';
END
GO

-- ---------------------------------------------------------------------------
-- CustomerMaps - BELL-D12 -> both 850 maps
-- ---------------------------------------------------------------------------
DECLARE @Bell INT = (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = 'BELL-D12');
DECLARE @Map850 INT = (SELECT Id FROM dbo.Maps WHERE Name = '850');
DECLARE @Map850F INT = (SELECT Id FROM dbo.Maps WHERE Name = '850 DB Fields');

IF @Bell IS NOT NULL AND @Map850 IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.CustomerMaps WHERE CustomerId = @Bell AND MapId = @Map850)
BEGIN
    INSERT INTO dbo.CustomerMaps (Id, CustomerId, MapId, CreatedDate, CreatedBy)
    VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.CustomerMaps), @Bell, @Map850, GETDATE(), 1);
    PRINT 'CustomerMaps: BELL-D12 -> 850';
END

IF @Bell IS NOT NULL AND @Map850F IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.CustomerMaps WHERE CustomerId = @Bell AND MapId = @Map850F)
BEGIN
    INSERT INTO dbo.CustomerMaps (Id, CustomerId, MapId, CreatedDate, CreatedBy)
    VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.CustomerMaps), @Bell, @Map850F, GETDATE(), 1);
    PRINT 'CustomerMaps: BELL-D12 -> 850 DB Fields';
END
GO

-- ---------------------------------------------------------------------------
-- PartnerGroups - BELL-D12 - BizMate
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.PartnerGroups WHERE Description = 'BELL-D12 - BizMate')
BEGIN
    INSERT INTO dbo.PartnerGroups (Id, SourcePartyId, DestinationPartyId, Description, CreatedDate, CreatedBy)
    VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.PartnerGroups),
            (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = 'BELL-D12'),
            (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = 'BIZMATE'),
            'BELL-D12 - BizMate', GETDATE(), 1);
    PRINT 'PartnerGroups: added BELL-D12 - BizMate';
END
GO

-- ---------------------------------------------------------------------------
-- ConnectorTypes and Connectors
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.ConnectorTypes WHERE Name = 'BizMate')
BEGIN
    INSERT INTO dbo.ConnectorTypes (Id, Name, Party, CreatedDate, CreatedBy)
    VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.ConnectorTypes), 'BizMate', 'BizMate', GETDATE(), 1);
    PRINT 'ConnectorTypes: added BizMate';
END

IF NOT EXISTS (SELECT 1 FROM dbo.ConnectorTypes WHERE Name = 'EDI Partner')
BEGIN
    INSERT INTO dbo.ConnectorTypes (Id, Name, Party, CreatedDate, CreatedBy)
    VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.ConnectorTypes), 'EDI Partner', 'Partner', GETDATE(), 1);
    PRINT 'ConnectorTypes: added EDI Partner';
END
GO

-- The partner transfer. BaseUrl is the folder BizLink writes 850s into; Url is
-- where eSyncMate writes the 997 back; Method is the file pattern; Realm is the
-- ISA sender id the route insists on, which is how it finds the Customers row
-- (ISACustomerID) and which the M1 contract also uses as partnerId.
--
-- Convergence: the first revision of this script seeded an SFTP connector,
-- before it was settled that BizLink delivers to a Windows folder. Rename and
-- rewrite it in place rather than adding a second row, so route 141 keeps
-- pointing at the same connector id.
IF EXISTS (SELECT 1 FROM dbo.Connectors WHERE Name = 'BELL-D12 - Inbound SFTP')
BEGIN
    UPDATE dbo.Connectors
       SET Name = 'BELL-D12 - Inbound Folder',
           Data = '{"ConnectivityType":"File","AuthType":"File","BaseUrl":"<<SET_INBOUND_FOLDER>>","Url":"<<SET_OUTBOUND_FOLDER>>","Method":"*","Realm":"BELL-D12","CustomerID":"BELL-D12"}',
           ModifiedDate = SYSUTCDATETIME(),
           ModifiedBy = 1
     WHERE Name = 'BELL-D12 - Inbound SFTP';

    PRINT 'Connectors: converged BELL-D12 - Inbound SFTP -> BELL-D12 - Inbound Folder (File transfer)';
END

IF NOT EXISTS (SELECT 1 FROM dbo.Connectors WHERE Name = 'BELL-D12 - Inbound Folder')
BEGIN
    INSERT INTO dbo.Connectors (Id, TypeId, Name, Data, CreatedDate, CreatedBy)
    VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.Connectors),
            (SELECT Id FROM dbo.ConnectorTypes WHERE Name = 'EDI Partner'),
            'BELL-D12 - Inbound Folder',
            '{"ConnectivityType":"File","AuthType":"File","BaseUrl":"<<SET_INBOUND_FOLDER>>","Url":"<<SET_OUTBOUND_FOLDER>>","Method":"*","Realm":"BELL-D12","CustomerID":"BELL-D12"}',
            GETDATE(), 1);
    PRINT 'Connectors: added BELL-D12 - Inbound Folder (placeholders - set the two folders with the UPDATE in this header)';
END

-- The BizMate bridge. Credentials come from ApplicationSettings (BizMate_*),
-- never from here; this row exists so the route has a destination party and
-- the Flow diagram has something to draw.
IF NOT EXISTS (SELECT 1 FROM dbo.Connectors WHERE Name = 'BizMate - EDI Bridge')
BEGIN
    INSERT INTO dbo.Connectors (Id, TypeId, Name, Data, CreatedDate, CreatedBy)
    VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.Connectors),
            (SELECT Id FROM dbo.ConnectorTypes WHERE Name = 'BizMate'),
            'BizMate - EDI Bridge',
            '{"ConnectivityType":"Rest","AuthType":"BizMate","BaseUrl":"http://localhost:8000","Url":"/v1/integration","CustomerID":"BELL-D12"}',
            GETDATE(), 1);
    PRINT 'Connectors: added BizMate - EDI Bridge';
END
GO

-- ---------------------------------------------------------------------------
-- RouteTypes and the route
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.RouteTypes WHERE Id = 600)
BEGIN
    INSERT INTO dbo.RouteTypes (Id, Name, Description, CreatedDate, CreatedBy)
    VALUES (600, 'BizMate - Receive EDI',
            'Reads partner X12 from the source SFTP, records each interchange in InboundEDI and the ledger, registers it with BizMate, creates the eSyncMate order, posts the canonical document to BizMate and returns a 997',
            GETDATE(), 1);
    PRINT 'RouteTypes: added 600 BizMate - Receive EDI';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Routes WHERE Name = 'BELL-D12 - Receive 850s for BizMate')
BEGIN
    INSERT INTO dbo.Routes (Id, TypeId, Status, SourcePartyId, DestinationPartyId, SourceConnectorId, DestinationConnectorId,
                            MapId, PartyGroupId, CreatedDate, CreatedBy, FrequencyType, StartDate, EndDate, RepeatCount,
                            WeekDays, OnDay, ExecutionTime, JobID, Name, RouteGroup, CustomerName)
    VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.Routes),
            600, 'Active',
            (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = 'BELL-D12'),
            (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = 'BIZMATE'),
            (SELECT Id FROM dbo.Connectors WHERE Name = 'BELL-D12 - Inbound Folder'),
            (SELECT Id FROM dbo.Connectors WHERE Name = 'BizMate - EDI Bridge'),
            (SELECT Id FROM dbo.Maps WHERE Name = '850'),
            (SELECT Id FROM dbo.PartnerGroups WHERE Description = 'BELL-D12 - BizMate'),
            GETDATE(), 1, 'Minutely', GETDATE(), DATEADD(year, 3, GETDATE()), 5,
            '', '', '', NULL, 'BELL-D12 - Receive 850s for BizMate', 'BizMate', 'BELL-D12');
    PRINT 'Routes: added BELL-D12 - Receive 850s for BizMate (Active, no Hangfire job yet - schedule it through a Flow or test-run it)';
END
GO

-- ---------------------------------------------------------------------------
-- Verify
-- ---------------------------------------------------------------------------
SELECT 'MapTypes' AS Item, Name AS Value FROM dbo.MapTypes WHERE Name IN ('850 Transformation','850 DB Fields','856 Transformation','INV Transformation','810 Transformation','855 Transformation')
UNION ALL SELECT 'Maps', Name + ' (' + CAST(LEN(Map) AS varchar(10)) + ' chars)' FROM dbo.Maps WHERE Name IN ('850','850 DB Fields')
UNION ALL SELECT 'Customers', Name + ' / ISA ' + ISNULL(ISACustomerID, '-') FROM dbo.Customers WHERE ERPCustomerID IN ('BIZMATE','BELL-D12')
UNION ALL SELECT 'CustomerMaps', c.ERPCustomerID + ' -> ' + m.Name FROM dbo.CustomerMaps cm JOIN dbo.Customers c ON c.Id = cm.CustomerId JOIN dbo.Maps m ON m.Id = cm.MapId WHERE c.ERPCustomerID = 'BELL-D12'
UNION ALL SELECT 'PartnerGroups', Description FROM dbo.PartnerGroups WHERE Description = 'BELL-D12 - BizMate'
UNION ALL SELECT 'Connectors', Name + CASE WHEN Data LIKE '%<<SET_%' THEN '  <-- folders still placeholders; the route will refuse until set' ELSE '  (configured)' END FROM dbo.Connectors WHERE Name IN ('BELL-D12 - Inbound Folder','BizMate - EDI Bridge')
UNION ALL SELECT 'RouteTypes', CAST(Id AS varchar(10)) + ' ' + Name FROM dbo.RouteTypes WHERE Id = 600
UNION ALL SELECT 'Routes', CAST(Id AS varchar(10)) + ' ' + Name + ' [' + Status + ']' FROM dbo.Routes WHERE Name = 'BELL-D12 - Receive 850s for BizMate'
UNION ALL SELECT 'VW_Routes', CASE WHEN EXISTS (SELECT 1 FROM dbo.VW_Routes WHERE Name = 'BELL-D12 - Receive 850s for BizMate') THEN 'route visible (all INNER JOINs satisfied)' ELSE 'ROUTE NOT VISIBLE - a foreign key is missing' END;
GO
