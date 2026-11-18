/*==============================================================================
  CT 01 APPLY - Shopify (Task 00006): inventory upload test AT THE CLIENT against OUR demo store

  WHERE: the CLIENT database (customer WKE1000). TEMPORARY: undo it with CT_99_Revert.sql before the client store
         (walker-edison) goes live.

  WHY: the token of our own store esyncmate-dev-store is in IntegrationTokens and selected on WKE1000, so the
       route "Shopify - Upload Warehouse-Wise Inventory" can be proven end to end on the client installation
       without touching the client's Shopify store.

  WHAT IT CHANGES (one transaction, customer WKE1000 only):
    0. BACKUP first, into two new tables (CT_99 restores from them and drops them):
         ZDB_CT01_ShipNodes   = the customer's TargetPlusShipNodes rows as they are now
         ZDB_CT01_Connectors  = the ONE connector changed in step 2, as it is now
       If these tables already exist the script STOPS (the test is already applied: run CT_99 first).
    1. TargetPlusShipNodes -> exactly  L55 -> Shop location (88131272890),  L56 -> My Custom Location (88131305658)
       (locations of esyncmate-dev-store; any other mapping row of the customer is removed and comes back with CT_99).
    2. ONLY the connector "Shopify - WHSW Inventory Upload": the store in BaseUrl -> esyncmate-dev-store.
       The other Shopify connectors (orders, ASN, cancellations) are NOT changed on purpose: they keep pointing at
       walker-edison, so with the demo token selected they refuse to run. No demo order can reach the ERP.
    3. SCSInventoryFeed: two test items, Status NEW
         ESM-SB-ICE   inventory item 52944920346810   L55 = 41, L56 = 17   (store now: 50 / 50)
         ESM-SB-DAWN  inventory item 52944920379578   L55 =  9, L56 =  4   (store now: 12 / 3)
  NOT CHANGED: routes, Flows, schedules, Status, the token selection, Marketplace, any other customer.

  IT STOPS, changing nothing, when:
    - the token selected on WKE1000 is not the one of esyncmate-dev-store
    - WKE1000 already has items waiting to be sent (Status NEW / UPDATED): they would go to the demo store too
    - the upload connector is encrypted (saved from Setup > Connectors): SQL cannot change it, see the message

  AFTER IT: run CT_01_Verify.sql, then Test Run / activate ONLY the route "Shopify - Upload Warehouse-Wise Inventory".
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

/*==============================================================================
  CONFIG
==============================================================================*/
DECLARE @ErpCustomerID  VARCHAR(50)   = 'WKE1000';
DECLARE @DemoStore      NVARCHAR(200) = 'esyncmate-dev-store.myshopify.com';
DECLARE @UploadConnName NVARCHAR(200) = 'Shopify - WHSW Inventory Upload';

--- ERP warehouse -> location of esyncmate-dev-store (read from the store 2026-11-18)
DECLARE @Locations TABLE (WHSID VARCHAR(10), LocationGid NVARCHAR(500));
INSERT INTO @Locations VALUES
    ('L55', 'gid://shopify/Location/88131272890'),   -- Shop location
    ('L56', 'gid://shopify/Location/88131305658');   -- My Custom Location

--- ItemId = SKU in the store, InventoryItemId = Shopify inventory item number (read from the store 2026-11-18)
DECLARE @Items TABLE (ItemId VARCHAR(50), InventoryItemId VARCHAR(50), ATS_L55 INT, ATS_L56 INT);
INSERT INTO @Items VALUES
    ('ESM-SB-ICE',  '52944920346810', 41, 17),
    ('ESM-SB-DAWN', '52944920379578',  9,  4);
/*==============================================================================*/

BEGIN TRY
    /*--- Checks (nothing is changed before all of them pass) --------------------*/
    DECLARE @CustomerId INT = (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = @ErpCustomerID);
    IF @CustomerId IS NULL
        THROW 51600, 'Customer WKE1000 not found.', 1;

    IF OBJECT_ID('dbo.ZDB_CT01_ShipNodes', 'U') IS NOT NULL OR OBJECT_ID('dbo.ZDB_CT01_Connectors', 'U') IS NOT NULL
        THROW 51601, 'The test is already applied (backup tables ZDB_CT01_* exist). Run CT_99_Revert.sql first.', 1;

    IF OBJECT_ID('dbo.IntegrationTokens', 'U') IS NULL OR COL_LENGTH('dbo.Customers', 'IntegrationTokenId') IS NULL
        THROW 51602, 'Run v2\V2_02_Integration_Tokens.sql first.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Customers C JOIN dbo.IntegrationTokens T ON T.Id = C.IntegrationTokenId
                    WHERE C.Id = @CustomerId AND T.Platform = 'Shopify' AND T.AccountKey = @DemoStore)
        THROW 51603, 'The token selected on WKE1000 is not the token of esyncmate-dev-store. Select it in Customers > Edit > Tokens.', 1;

    DECLARE @Waiting INT = (SELECT COUNT(*) FROM dbo.SCSInventoryFeed
                             WHERE CustomerID = @ErpCustomerID AND [Status] IN ('NEW', 'UPDATED')
                               AND ItemId NOT IN (SELECT ItemId FROM @Items));
    IF @Waiting > 0
    BEGIN
        SELECT 'STOPPED - items of WKE1000 already waiting to be sent' AS Grid, ItemId, CustomerItemCode, [Status], ModifiedDate
          FROM dbo.SCSInventoryFeed
         WHERE CustomerID = @ErpCustomerID AND [Status] IN ('NEW', 'UPDATED') AND ItemId NOT IN (SELECT ItemId FROM @Items);
        THROW 51604, 'WKE1000 has items waiting to be sent (Status NEW / UPDATED). They would be sent to the demo store. Nothing changed - send me the grid.', 1;
    END

    DECLARE @ConnId INT, @ConnCount INT;
    SELECT @ConnCount = COUNT(*), @ConnId = MAX(Id) FROM dbo.Connectors WHERE Name = @UploadConnName;
    IF @ConnCount <> 1
        THROW 51605, 'Connector "Shopify - WHSW Inventory Upload" not found exactly once.', 1;

    DECLARE @Data NVARCHAR(MAX) = (SELECT Data FROM dbo.Connectors WHERE Id = @ConnId);
    IF ISJSON(@Data) <> 1
        THROW 51606, 'The upload connector is encrypted (saved from Setup > Connectors), SQL cannot change it. Nothing changed - tell me and I give the steps for the UI.', 1;

    DECLARE @OldBaseUrl NVARCHAR(500) = JSON_VALUE(@Data, '$.BaseUrl');
    IF @OldBaseUrl NOT LIKE 'https://%.myshopify.com/admin/api/%'
        THROW 51607, 'The upload connector BaseUrl does not look like https://<store>.myshopify.com/admin/api/<version>/. Nothing changed.', 1;

    -- keep the API version, change only the store
    DECLARE @NewBaseUrl NVARCHAR(500) = 'https://' + @DemoStore + SUBSTRING(@OldBaseUrl, CHARINDEX('/admin/api/', @OldBaseUrl), 500);

    BEGIN TRAN;

    /*--- 0. Backup ---------------------------------------------------------------*/
    SELECT S.WHSID, S.ShipNode, S.CustomerID, GETDATE() AS BackupDate
      INTO dbo.ZDB_CT01_ShipNodes
      FROM dbo.TargetPlusShipNodes S
     WHERE S.CustomerID = @ErpCustomerID;

    SELECT C.Id, C.Name, C.Data, C.ModifiedDate, C.ModifiedBy, GETDATE() AS BackupDate
      INTO dbo.ZDB_CT01_Connectors
      FROM dbo.Connectors C
     WHERE C.Id = @ConnId;

    /*--- 1. Warehouse -> demo store location: exactly @Locations ------------------*/
    DELETE S
      FROM dbo.TargetPlusShipNodes S
     WHERE S.CustomerID = @ErpCustomerID
       AND NOT EXISTS (SELECT 1 FROM @Locations L WHERE L.WHSID = S.WHSID);

    UPDATE S SET S.ShipNode = L.LocationGid
      FROM dbo.TargetPlusShipNodes S
      JOIN @Locations L ON L.WHSID = S.WHSID
     WHERE S.CustomerID = @ErpCustomerID;

    INSERT INTO dbo.TargetPlusShipNodes (WHSID, ShipNode, CustomerID)
    SELECT L.WHSID, L.LocationGid, @ErpCustomerID
      FROM @Locations L
     WHERE NOT EXISTS (SELECT 1 FROM dbo.TargetPlusShipNodes S WHERE S.CustomerID = @ErpCustomerID AND S.WHSID = L.WHSID);

    /*--- 2. Upload connector -> demo store ----------------------------------------*/
    UPDATE dbo.Connectors
       SET Data = JSON_MODIFY(Data, '$.BaseUrl', @NewBaseUrl), ModifiedDate = GETDATE(), ModifiedBy = 1
     WHERE Id = @ConnId;

    /*--- 3. Two test items ---------------------------------------------------------*/
    DELETE F FROM dbo.SCSInventoryFeed F
     WHERE F.CustomerID = @ErpCustomerID AND F.ItemId IN (SELECT ItemId FROM @Items);

    INSERT INTO dbo.SCSInventoryFeed
           (CustomerID, ItemId, CustomerItemCode, ETA_Date, ETA_Qty, Total_ATS,
            ATS_L10, ATS_L21, ATS_L28, ATS_L29, ATS_L30, ATS_L34, ATS_L35, ATS_L36, ATS_L37, ATS_L40, ATS_L41,
            ATS_L55, ATS_L56, ATS_L57, ATS_L60, ATS_L65, ATS_L70, ATS_L91,
            [Status], CreatedDate, CreatedBy, ModifiedDate, ModifiedBy)
    SELECT @ErpCustomerID, I.ItemId, I.InventoryItemId, NULL, 0, I.ATS_L55 + I.ATS_L56,
           0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
           I.ATS_L55, I.ATS_L56, 0, 0, 0, 0, 0,
           'NEW', GETDATE(), 1, GETDATE(), 1
      FROM @Items I;

    COMMIT TRAN;

    PRINT 'CT 01 applied. Backup in ZDB_CT01_ShipNodes / ZDB_CT01_Connectors. Undo with CT_99_Revert.sql.';

    /*--- Result -------------------------------------------------------------------*/
    SELECT '1. Mapping now' AS Grid, S.WHSID, S.ShipNode FROM dbo.TargetPlusShipNodes S WHERE S.CustomerID = @ErpCustomerID ORDER BY S.WHSID;

    SELECT '2. Mapping before (backup)' AS Grid, WHSID, ShipNode FROM dbo.ZDB_CT01_ShipNodes ORDER BY WHSID;

    SELECT '3. Upload connector' AS Grid, @ConnId AS Id, @UploadConnName AS Name, @OldBaseUrl AS BaseUrlBefore,
           JSON_VALUE(Data, '$.BaseUrl') AS BaseUrlNow
      FROM dbo.Connectors WHERE Id = @ConnId;

    SELECT '4. Test items' AS Grid, F.ItemId, F.CustomerItemCode AS InventoryItemId, F.Total_ATS, F.ATS_L55, F.ATS_L56, F.[Status]
      FROM dbo.SCSInventoryFeed F
     WHERE F.CustomerID = @ErpCustomerID AND F.ItemId IN (SELECT ItemId FROM @Items)
     ORDER BY F.ItemId;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR (nothing changed): ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
