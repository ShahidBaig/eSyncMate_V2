/*==============================================================================
  TEST 02 - Shopify (Task 00006): two test products in the inventory feed table (DEV DB)

  DEV ONLY. Stops on any database other than eSyncmate_Dev.

  WHY: on the dev DB there is no ERP feed for devshop-we (the SPARS routes stay In-Active), so the rows the
       "Fetch Full Inventory from ERP" route would write to SCSInventoryFeed are written here by hand.
       Route "Shopify - Upload Warehouse-Wise Inventory" then sends them to the store.

  BEFORE RUNNING: the same two products must exist in devshop-we
       - SKU = the ItemId below, "Track quantity" ON,
       - stocked at BOTH locations "L55 - YARAGHI INDIANA" and "L56 - YARAGHI INDIANA 2".
       Put each product's Shopify INVENTORY ITEM id into @Items (CustomerItemCode): the upload route sends
       stock by inventory item id, not by SKU.
       Where to find it: Shopify admin > Products > the product > the variant > "Adjustment history" - the page
       address ends with /inventory_items/<number> ... or ask me, I read it from the store.

  WHAT IT DOES (one transaction, customer WKE1000 only):
       - inserts / replaces ONLY the rows of the ItemIds in @Items, Status = NEW (so the upload route picks them up)
       - every other SCSInventoryFeed row is left as it is (the 8 rows of the old demo store test are SYNCED / ERROR
         and are not sent again)
  Run it again to send the same items again (change the quantities to see the store change).
==============================================================================*/

SET NOCOUNT ON;

IF DB_NAME() <> 'eSyncmate_Dev'
BEGIN
    DECLARE @Db SYSNAME = DB_NAME(); RAISERROR('TEST 02 is for the DEV database eSyncmate_Dev only. Current database: %s', 16, 1, @Db);
    SET NOEXEC ON;
END
GO

/*==============================================================================
  CONFIG
==============================================================================*/
DECLARE @ErpCustomerID VARCHAR(50) = 'WKE1000';

--- ItemId = SKU in the store. InventoryItemId = the Shopify inventory item NUMBER of that product in devshop-we.
--- Quantities per ERP warehouse; L55 / L56 are the two mapped locations (TEST 01).
DECLARE @Items TABLE (ItemId VARCHAR(50), InventoryItemId VARCHAR(50), ATS_L55 INT, ATS_L56 INT);
INSERT INTO @Items VALUES
    ('ESM-TEST-001', '<inventory item id of ESM-TEST-001>', 10, 5),
    ('ESM-TEST-002', '<inventory item id of ESM-TEST-002>',  7, 0);
/*==============================================================================*/

BEGIN TRY
    IF EXISTS (SELECT 1 FROM @Items WHERE InventoryItemId LIKE '%[^0-9]%' OR LTRIM(RTRIM(InventoryItemId)) = '')
        THROW 51501, 'Fill InventoryItemId in @Items with the Shopify inventory item NUMBER of each product in devshop-we (digits only).', 1;

    IF EXISTS (SELECT ItemId FROM @Items GROUP BY ItemId HAVING COUNT(*) > 1)
       OR EXISTS (SELECT InventoryItemId FROM @Items GROUP BY InventoryItemId HAVING COUNT(*) > 1)
        THROW 51502, 'An ItemId or InventoryItemId is listed twice in @Items.', 1;

    IF EXISTS (SELECT 1 FROM @Items WHERE ATS_L55 < 0 OR ATS_L56 < 0)
        THROW 51503, 'Quantities must be 0 or more.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE ERPCustomerID = @ErpCustomerID)
        THROW 51504, 'Customer not found.', 1;

    IF (SELECT COUNT(*) FROM dbo.TargetPlusShipNodes WHERE CustomerID = @ErpCustomerID AND WHSID IN ('L55', 'L56')) <> 2
        THROW 51505, 'Warehouse mapping L55 / L56 missing. Run TEST_01_Switch_Dev_DB_To_devshop-we.sql first.', 1;

    BEGIN TRAN;

    DELETE F
      FROM dbo.SCSInventoryFeed F
     WHERE F.CustomerID = @ErpCustomerID
       AND F.ItemId IN (SELECT ItemId FROM @Items);

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

    PRINT CONCAT('Test inventory rows written: ', @@ROWCOUNT);

    COMMIT TRAN;

    /*--- Verification -----------------------------------------------------------*/
    SELECT '1. Test rows' AS Grid, F.CustomerID, F.ItemId, F.CustomerItemCode AS InventoryItemId,
           F.Total_ATS, F.ATS_L55, F.ATS_L56, F.[Status], F.ModifiedDate
      FROM dbo.SCSInventoryFeed F
     WHERE F.CustomerID = @ErpCustomerID AND F.ItemId IN (SELECT ItemId FROM @Items)
     ORDER BY F.ItemId;

    SELECT '2. Waiting to be sent' AS Grid, F.[Status], COUNT(*) AS Items
      FROM dbo.SCSInventoryFeed F
     WHERE F.CustomerID = @ErpCustomerID
     GROUP BY F.[Status];

    -- 3. Exactly what the upload route will read (read-only call of its source procedure)
    EXEC dbo.Sp_SCS_GetProductCatalog @p_CustomerID = @ErpCustomerID, @p_RouteTypeID = 'ShopifyInventoryUpload', @p_UserNo = 1;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO

SET NOEXEC OFF;
GO
