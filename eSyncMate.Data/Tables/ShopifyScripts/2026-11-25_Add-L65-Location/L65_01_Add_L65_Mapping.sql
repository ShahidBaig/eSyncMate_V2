/*==============================================================================
  L65 01 - Shopify (Task 00006), customer WKE1000: add warehouse L65 to the Warehouse Mapping

  Why: the client asked (2026-11-25) to send the stock of warehouse L65 too, like L55 and L56.
       Test item named by the client: ALSD1EIV (stocked at the L65 location in Shopify).

  What it changes: ONE new row in dbo.TargetPlusShipNodes for WKE1000:
      L65  ->  gid://shopify/Location/82231951426   ("L65 - Yaraghi Patterson", read in Shopify admin 2026-11-24)
  Not changed: the L55 / L56 rows, routes, connectors, the feed, any code. The upload route already reads
  ATS_L65 from the feed; it only needs the mapping row.

  Effect to know BEFORE applying: from the next run of "Shopify - Upload Warehouse-Wise Inventory" every item
  that is sent gets its L65 quantity written to the Shopify L65 location, also when that quantity is 0.
  The grid "Feed L65 totals" shows what SPARS has for L65 right now.

  @Apply = 0 (default) DRY RUN. With @Apply = 1 the customer's mapping rows as they are now are saved in
  dbo.ZDB_L65_ShipNodes first (L65_99 reverts from it).

  Stops (nothing changed):
    52010 customer not found                          52013 the backup exists already (run L65_99 first)
    52011 L65 is mapped to another location           52014 the upload route is Active
    52012 the location is mapped to another warehouse
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ErpCustomerID VARCHAR(50)   = 'WKE1000';
DECLARE @Apply         BIT           = 0;   -- 0 = dry run, 1 = change
DECLARE @WHSID         VARCHAR(10)   = 'L65';
DECLARE @LocationGid   NVARCHAR(500) = 'gid://shopify/Location/82231951426';   -- L65 - Yaraghi Patterson
DECLARE @TestItem      VARCHAR(100)  = 'ALSD1EIV';                             -- item named by the client

BEGIN TRY
    IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE ERPCustomerID = @ErpCustomerID)
        THROW 52010, 'Customer WKE1000 not found. Nothing changed.', 1;

    DECLARE @Existing NVARCHAR(500) =
        (SELECT TOP 1 ShipNode FROM dbo.TargetPlusShipNodes WHERE CustomerID = @ErpCustomerID AND WHSID = @WHSID);

    SELECT 'Mapping now' AS Grid, S.ID, S.WHSID, S.ShipNode, S.CustomerID
      FROM dbo.TargetPlusShipNodes S
     WHERE S.CustomerID = @ErpCustomerID
     ORDER BY S.WHSID;

    SELECT 'Plan' AS Grid, @WHSID AS WHSID, @LocationGid AS ShipNode,
           CASE WHEN @Existing = @LocationGid THEN 'already mapped - nothing to do'
                WHEN @Existing IS NOT NULL    THEN 'STOP: L65 is mapped to another location'
                ELSE 'will be inserted' END AS [Action];

    -- the client's test item: is it in the feed, what does SPARS have for it, does it have its Shopify id
    SELECT 'Test item in the feed' AS Grid, INV.ItemId, INV.CustomerItemCode, INV.[Status],
           INV.Total_ATS, INV.ATS_L55, INV.ATS_L56, INV.ATS_L65, PR.ProductId AS ShopifyInventoryItemId
      FROM dbo.SCSInventoryFeed INV WITH (NOLOCK)
      LEFT JOIN dbo.CustomerProductCatalogPrices PR WITH (NOLOCK) ON PR.CustomerID = INV.CustomerID AND PR.ItemId = INV.ItemId
     WHERE INV.CustomerID = @ErpCustomerID AND (INV.ItemId = @TestItem OR INV.CustomerItemCode = @TestItem);

    IF @@ROWCOUNT = 0
        PRINT 'NOTE: item ' + @TestItem + ' is NOT in the WKE1000 feed. SPARS must send it before it can be uploaded.';

    SELECT 'Feed L65 totals' AS Grid, COUNT(*) AS Items,
           SUM(CASE WHEN ISNULL(INV.ATS_L65, 0) > 0 THEN 1 ELSE 0 END) AS ItemsWithStockAtL65,
           SUM(ISNULL(INV.ATS_L65, 0)) AS UnitsAtL65,
           SUM(CASE WHEN INV.[Status] IN ('UPDATED', 'NEW') THEN 1 ELSE 0 END) AS ItemsWaitingForUpload
      FROM dbo.SCSInventoryFeed INV WITH (NOLOCK)
     WHERE INV.CustomerID = @ErpCustomerID;

    SELECT 'Upload route' AS Grid, R.Id, R.Name, R.Status, R.JobID
      FROM dbo.Routes R
     WHERE R.CustomerName = @ErpCustomerID AND R.TypeId = 80;

    IF @Existing = @LocationGid
    BEGIN
        PRINT 'L65 is already mapped to this location. Nothing to do.';
        RETURN;
    END

    IF @Apply = 0
    BEGIN
        PRINT 'DRY RUN: nothing changed. Set @Apply = 1 to add the L65 mapping.';
        RETURN;
    END

    IF @Existing IS NOT NULL
        THROW 52011, 'L65 is already mapped to another location (see grid "Mapping now"). Nothing changed - send me the grid.', 1;

    IF EXISTS (SELECT 1 FROM dbo.TargetPlusShipNodes WHERE CustomerID = @ErpCustomerID AND ShipNode = @LocationGid)
        THROW 52012, 'This Shopify location is already mapped to another warehouse of WKE1000. Nothing changed.', 1;

    IF OBJECT_ID('dbo.ZDB_L65_ShipNodes', 'U') IS NOT NULL
        THROW 52013, 'The backup dbo.ZDB_L65_ShipNodes exists already. Run L65_99_Revert.sql first. Nothing changed.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Routes R WHERE R.CustomerName = @ErpCustomerID AND R.TypeId = 80 AND (R.Status = 'Active' OR R.JobID IS NOT NULL))
        THROW 52014, 'The upload route is Active. Set it In-Active on the Flows screen first. Nothing changed.', 1;

    BEGIN TRAN;

    SELECT S.ID, S.WHSID, S.ShipNode, S.CustomerID, GETDATE() AS BackupDate
      INTO dbo.ZDB_L65_ShipNodes
      FROM dbo.TargetPlusShipNodes S
     WHERE S.CustomerID = @ErpCustomerID;

    INSERT INTO dbo.TargetPlusShipNodes (WHSID, ShipNode, CustomerID)
    VALUES (@WHSID, @LocationGid, @ErpCustomerID);

    COMMIT TRAN;

    PRINT 'L65 mapping added. Backup of the rows before in dbo.ZDB_L65_ShipNodes.';

    SELECT 'Mapping after' AS Grid, S.ID, S.WHSID, S.ShipNode, S.CustomerID
      FROM dbo.TargetPlusShipNodes S
     WHERE S.CustomerID = @ErpCustomerID
     ORDER BY S.WHSID;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
