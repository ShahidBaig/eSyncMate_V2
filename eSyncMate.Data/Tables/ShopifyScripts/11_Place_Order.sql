/*==============================================================================
  11 - Shopify (Task 00006): Place Order (eSyncMate -> ERP), like every other customer

    1. Map "Shopify Order Transformation" (TypeId 1): the API-JSON that "Shopify - Get Orders"
       saves (Shopify order + lineNo / itemId per line) -> SPARS Place_Order body
    2. Connectors (copies - no secret / connection string typed here):
         <P> - eSyncMate Get Orders    copy of "<P> - WHSW Inventory Get Data" (script 03), EXEC SP_OrdersData
                                       (also the source of "Send ASN Shipment Notification", script 13)
         <P> - SPARS Create Orders     copy of "<P> - SPARS Full Inventory Feed" (script 04), TypeId 1;
                                       Url set at run time (Place_Order / Get_OrderInfo)
    3. Route <P> - Place Orders in ERP   RouteType 9, eSyncMate -> SPARS, Minutely @RepeatMinutes, MapId = the map

  Needs scripts 02, 03, 04, 10. Run it as often as needed: missing -> inserted, existing -> updated
  (the map text, both connectors, the route's parties / connectors / map / group). An encrypted connector is
  listed and left alone; an existing route keeps its Status, schedule and JobID.
==============================================================================*/

SET NOCOUNT ON;

/*==============================================================================
  CONFIG
==============================================================================*/
DECLARE @ErpCustomerID NVARCHAR(50)  = 'WKE1000';   -- same as scripts 02 / 03 / 04
DECLARE @Prefix        NVARCHAR(100) = 'Shopify';
DECLARE @ShipViaCode   NVARCHAR(20)  = 'FEDX';         -- SPARS ship-via code; Amazon uses FEDX, Walmart FEDH (client to confirm)
DECLARE @RepeatMinutes INT           = 5;
/*==============================================================================*/

DECLARE @MapName NVARCHAR(200) = 'Shopify Order Transformation';

/* JUST map. Paths are the fields ShopifyGetOrdersRoute saves in OrderData API-JSON:
     legacyResourceId (Shopify order id = eSyncMate OrderNumber, sent as CustomerPO and ExternalID like the
     Walmart / Amazon maps send their order id), createdAt (2026-10-23T10:31:00Z),
     shippingAddress.*, lineItems.nodes[] with itemId (ERP ItemId), currentQuantity, lineNo,
     discountedUnitPriceSet.shopMoney.amount (price per unit after line discounts).
   Shopify has no ship-by / cancel-by date, so both take the order date, as the Amazon map does with LatestShipDate. */
DECLARE @Map NVARCHAR(MAX) = N'{
  "order": {
    "header": {
      "CustomerID": "@CUSTOMERID@",
      "CustomerPO": "#valueof($.legacyResourceId)",
      "AddressName": "#valueof($.shippingAddress.name)",
      "CompanyName": "#valueof($.shippingAddress.name)",
      "ShipToCode": "",
      "Address1": "#valueof($.shippingAddress.address1)",
      "Address2": "#valueof($.shippingAddress.address2)",
      "City": "#valueof($.shippingAddress.city)",
      "State": "#valueof($.shippingAddress.provinceCode)",
      "Zip": "#valueof($.shippingAddress.zip)",
      "Country": "#valueof($.shippingAddress.countryCodeV2)",
      "Phone": "#valueof($.shippingAddress.phone)",
      "OrderTakenBy": "API",
      "Instructions": "",
      "ShipViaCode": "@SHIPVIACODE@",
      "OrderDate": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.formatDate,#valueof($.createdAt),MM/dd/yyyy)",
      "ShipDate": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.formatDate,#valueof($.createdAt),MM/dd/yyyy)",
      "CancelDate": "#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.formatDate,#valueof($.createdAt),MM/dd/yyyy)",
      "ExternalID": "#valueof($.legacyResourceId)"
    },
    "detail": {
      "#loop($.lineItems.nodes)": {
        "ItemID": "#currentvalueatpath($.itemId)",
        "OrderQty": "#currentvalueatpath($.currentQuantity)",
        "UnitPrice": "#currentvalueatpath($.discountedUnitPriceSet.shopMoney.amount)",
        "Discount": "",
        "WHSID": "",
        "Remarks": "",
        "ETA_Date": "",
        "TaxAmount": "",
        "APIOrderLineNo": "#currentvalueatpath($.lineNo)"
      }
    }
  }
}';

BEGIN TRY
    SET @Map = REPLACE(@Map, '@SHIPVIACODE@', REPLACE(@ShipViaCode, '"', ''));

    IF ISJSON(@Map) = 0 THROW 51520, 'The map is not valid JSON.', 1;

    DECLARE @SparsId   INT = (SELECT Id FROM dbo.Customers WHERE Name = 'SPARS');
    DECLARE @EsyncId   INT = (SELECT Id FROM dbo.Customers WHERE Name = 'eSyncMate');
    DECLARE @PartnerId INT = (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = @ErpCustomerID);
    DECLARE @PgSpars   INT = (SELECT Id FROM dbo.PartnerGroups WHERE SourcePartyId = @PartnerId AND DestinationPartyId = @SparsId);
    DECLARE @SqlFrom   NVARCHAR(MAX) = (SELECT Data FROM dbo.Connectors WHERE Name = @Prefix + ' - WHSW Inventory Get Data');
    DECLARE @SparsFrom NVARCHAR(MAX) = (SELECT Data FROM dbo.Connectors WHERE Name = @Prefix + ' - SPARS Full Inventory Feed');

    IF @PartnerId IS NULL OR @PgSpars IS NULL THROW 51521, 'Customer / partner groups missing. Run script 02 first.', 1;
    IF @SqlFrom IS NULL THROW 51522, 'Connector "<P> - WHSW Inventory Get Data" missing. Run script 03 first.', 1;
    IF @SparsFrom IS NULL THROW 51523, 'Connector "<P> - SPARS Full Inventory Feed" missing. Run script 04 first.', 1;
    IF ISJSON(@SqlFrom) = 0 OR ISJSON(@SparsFrom) = 0
        THROW 51524, 'A connector this script copies from is encrypted (saved from Setup > Connectors). Create the 2 connectors by hand.', 1;

    DECLARE @c TABLE (Seq INT, Nm NVARCHAR(200), TypeId INT, Payload NVARCHAR(MAX));

    INSERT INTO @c VALUES
      -- the customer id is a literal (the route only replaces @DATATYPE@ / @ORDERSTATUS@ / @ORDERDATASTATUS@)
      (1, @Prefix + ' - eSyncMate Get Orders', 8,
          JSON_MODIFY(@SqlFrom, '$.Command', 'EXEC SP_OrdersData @p_CustomerID = ''' + @ErpCustomerID + ''', @p_DataType = ''@DATATYPE@'', @p_OrderStatus = ''@ORDERSTATUS@'', @p_OrderDataStatus = ''@ORDERDATASTATUS@''')),
      -- Url is overwritten at run time with Place_Order / Get_OrderInfo; POST (the feed connector it is copied from is GET)
      (2, @Prefix + ' - SPARS Create Orders',  1, JSON_MODIFY(JSON_MODIFY(@SparsFrom, '$.Url', 'Place_Order'), '$.Method', 'POST'));

    IF EXISTS (SELECT 1 FROM @c WHERE ISJSON(Payload) = 0) THROW 51525, 'A connector payload is not valid JSON.', 1;

    BEGIN TRAN;

    /*--- 1. Map (Maps.Id is NOT identity) -----------------------------------*/
    IF NOT EXISTS (SELECT 1 FROM dbo.Maps WHERE Name = @MapName)
    BEGIN
        INSERT INTO dbo.Maps (Id, Name, TypeId, Map, CreatedDate, CreatedBy)
        SELECT ISNULL(MAX(Id), 0) + 1, @MapName, 1, @Map, GETDATE(), 1
          FROM dbo.Maps WITH (UPDLOCK, HOLDLOCK);

        PRINT 'Created map ' + @MapName;
    END
    ELSE
    BEGIN
        UPDATE dbo.Maps SET TypeId = 1, Map = @Map, ModifiedDate = GETDATE(), ModifiedBy = 1 WHERE Name = @MapName;
        PRINT 'Updated map ' + @MapName;
    END

    DECLARE @MapId INT = (SELECT Id FROM dbo.Maps WHERE Name = @MapName);

    /*--- 2. Connectors: upsert (same block in every Task 00005 / 00006 script) --
      A new name is inserted. An existing plain-JSON connector gets this payload, but a secret / connection
      string left blank here keeps its stored value. An encrypted connector (saved from Setup > Connectors)
      cannot be read by SQL: it is left as it is and listed below - change it in the UI.                      */
    DECLARE @u_Nm NVARCHAR(200), @u_TypeId INT, @u_New NVARCHAR(MAX), @u_Old NVARCHAR(MAX), @u_Ins INT = 0, @u_Upd INT = 0;
    DECLARE @u_Encrypted TABLE (Name NVARCHAR(200));
    DECLARE u_cc CURSOR LOCAL FAST_FORWARD FOR SELECT Nm, TypeId, Payload FROM @c ORDER BY Seq;

    OPEN u_cc;
    FETCH NEXT FROM u_cc INTO @u_Nm, @u_TypeId, @u_New;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.Connectors WHERE Name = @u_Nm)
        BEGIN
            INSERT INTO dbo.Connectors (Id, TypeId, Name, Data, CreatedDate, CreatedBy)
            SELECT ISNULL(MAX(Id), 0) + 1, @u_TypeId, @u_Nm, @u_New, GETDATE(), 1 FROM dbo.Connectors WITH (UPDLOCK, HOLDLOCK);
            SET @u_Ins += 1;
        END
        ELSE
        BEGIN
            SET @u_Old = ISNULL((SELECT TOP 1 Data FROM dbo.Connectors WHERE Name = @u_Nm), '{}');

            IF ISJSON(@u_Old) = 0
                INSERT INTO @u_Encrypted VALUES (@u_Nm);
            ELSE
            BEGIN
                IF ISNULL(JSON_VALUE(@u_New, '$.ConnectionString'), '') = '' AND ISNULL(JSON_VALUE(@u_Old, '$.ConnectionString'), '') <> ''
                    SET @u_New = JSON_MODIFY(@u_New, '$.ConnectionString', JSON_VALUE(@u_Old, '$.ConnectionString'));
                IF ISNULL(JSON_VALUE(@u_New, '$.ConsumerKey'), '') = '' AND ISNULL(JSON_VALUE(@u_Old, '$.ConsumerKey'), '') <> ''
                    SET @u_New = JSON_MODIFY(@u_New, '$.ConsumerKey', JSON_VALUE(@u_Old, '$.ConsumerKey'));
                IF ISNULL(JSON_VALUE(@u_New, '$.ConsumerSecret'), '') = '' AND ISNULL(JSON_VALUE(@u_Old, '$.ConsumerSecret'), '') <> ''
                    SET @u_New = JSON_MODIFY(@u_New, '$.ConsumerSecret', JSON_VALUE(@u_Old, '$.ConsumerSecret'));
                IF ISNULL(JSON_VALUE(@u_New, '$.Token'), '') = '' AND ISNULL(JSON_VALUE(@u_Old, '$.Token'), '') <> ''
                    SET @u_New = JSON_MODIFY(@u_New, '$.Token', JSON_VALUE(@u_Old, '$.Token'));
                IF ISNULL(JSON_VALUE(@u_New, '$.TokenSecret'), '') = '' AND ISNULL(JSON_VALUE(@u_Old, '$.TokenSecret'), '') <> ''
                    SET @u_New = JSON_MODIFY(@u_New, '$.TokenSecret', JSON_VALUE(@u_Old, '$.TokenSecret'));
                IF ISNULL(JSON_VALUE(@u_New, '$.Password'), '') = '' AND ISNULL(JSON_VALUE(@u_Old, '$.Password'), '') <> ''
                    SET @u_New = JSON_MODIFY(@u_New, '$.Password', JSON_VALUE(@u_Old, '$.Password'));
                IF ISNULL(JSON_VALUE(@u_New, '$.BaseUrl'), '') = '' AND ISNULL(JSON_VALUE(@u_Old, '$.BaseUrl'), '') <> ''
                    SET @u_New = JSON_MODIFY(@u_New, '$.BaseUrl', JSON_VALUE(@u_Old, '$.BaseUrl'));

                UPDATE dbo.Connectors SET TypeId = @u_TypeId, Data = @u_New, ModifiedDate = GETDATE(), ModifiedBy = 1 WHERE Name = @u_Nm;
                SET @u_Upd += 1;
            END
        END

        FETCH NEXT FROM u_cc INTO @u_Nm, @u_TypeId, @u_New;
    END

    CLOSE u_cc;
    DEALLOCATE u_cc;

    DECLARE @u_Enc INT = (SELECT COUNT(*) FROM @u_Encrypted);   -- PRINT takes no subquery

    PRINT CONCAT('Connectors inserted: ', @u_Ins, ', updated: ', @u_Upd, ', encrypted and NOT changed: ', @u_Enc);

    SELECT Name AS EncryptedConnector_NotChanged_EditInUI FROM @u_Encrypted;

    /*--- 3. Route (Routes.Id is NOT identity) ------------------------------*/
    DECLARE @RouteName VARCHAR(100) = @Prefix + ' - Place Orders in ERP';

    -- an existing route gets these parties / connectors / map / group; its Status, schedule and JobID are left alone
    IF EXISTS (SELECT 1 FROM dbo.Routes WHERE Name = @RouteName)
    BEGIN
        UPDATE dbo.Routes
           SET TypeId = 9, SourcePartyId = @EsyncId, DestinationPartyId = @SparsId,
               SourceConnectorId = (SELECT Id FROM dbo.Connectors WHERE Name = @Prefix + ' - eSyncMate Get Orders'),
               DestinationConnectorId = (SELECT Id FROM dbo.Connectors WHERE Name = @Prefix + ' - SPARS Create Orders'),
               MapId = @MapId, PartyGroupId = @PgSpars, RouteGroup = 'Orders', CustomerName = @ErpCustomerID,
               ModifiedDate = GETDATE(), ModifiedBy = 1
         WHERE Name = @RouteName;

        PRINT 'Updated route ' + @RouteName + ' (Status / schedule / JobID left as they are)';
    END
    ELSE
    BEGIN
        INSERT INTO dbo.Routes
            (Id, TypeId, Status, SourcePartyId, DestinationPartyId, SourceConnectorId, DestinationConnectorId, MapId, PartyGroupId,
             CreatedDate, CreatedBy, FrequencyType, StartDate, EndDate, RepeatCount, WeekDays, OnDay, ExecutionTime, JobID,
             Name, RouteGroup, CustomerName)
        SELECT ISNULL(MAX(Id), 0) + 1, 9, 'In-Active', @EsyncId, @SparsId,
               (SELECT Id FROM dbo.Connectors WHERE Name = @Prefix + ' - eSyncMate Get Orders'),
               (SELECT Id FROM dbo.Connectors WHERE Name = @Prefix + ' - SPARS Create Orders'),
               @MapId, @PgSpars, GETDATE(), 1, 'Minutely', CAST(GETDATE() AS DATE), NULL, @RepeatMinutes, '', '', '', NULL,
               @RouteName, 'Orders', @ErpCustomerID
          FROM dbo.Routes WITH (UPDLOCK, HOLDLOCK);

        PRINT 'Created route ' + @RouteName;
    END

    COMMIT TRAN;

    /*--- Verification (no secrets shown) ----------------------------------*/
    SELECT Id, Name, TypeId, ISJSON(Map) AS MapIsJson FROM dbo.Maps WHERE Name = @MapName;
    SELECT c.Id, c.TypeId, c.Name, CASE WHEN ISJSON(c.Data) = 1 THEN ISNULL(JSON_VALUE(c.Data, '$.Command'), JSON_VALUE(c.Data, '$.Url')) END AS CommandOrUrl
      FROM dbo.Connectors c JOIN @c x ON x.Nm = c.Name ORDER BY c.Id;
    -- VW_Routes uses INNER JOINs: the route must show up here too
    SELECT Id, Name, TypeId, Status, MapId, FrequencyType, RepeatCount FROM dbo.VW_Routes WHERE Name = @RouteName;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO

/*==============================================================================
  POST-STEPS
  Get Orders -> Place Orders in ERP. Order status: New -> InProgress -> SYNCED (or ERROR + ERP-ERROR row).
  The body sent to SPARS is in OrderData Type ERP-JSON.
  Activate from the UI (Setup > Routes or a Flow), never with an UPDATE - that leaves JobID NULL.
==============================================================================*/
