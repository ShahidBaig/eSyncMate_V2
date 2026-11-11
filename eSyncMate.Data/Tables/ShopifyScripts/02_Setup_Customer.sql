/*==============================================================================
  02 - Shopify inventory (Task 00006): customer, partner groups, inventory tables,
        warehouse -> Shopify location mapping

  Run it as often as needed - inserts what is missing, updates what exists:
    1. Customers row                 ERPCustomerID = @ErpCustomerID (existing: Name + Marketplace updated)
    2. PartnerGroups                 <customer> -> eSyncMate, <customer> -> SPARS
    3. SCSInventoryFeedData_<id>     request / response per item per batch (route writes here)
    4. SCSInventoryFeed_<id>_Log     UPLOAD snapshots per batch (inventory screen)
    5. VW_AllSCSInventoryFeedData    the new data table appended to the LIVE view body (once)
    6. TargetPlusShipNodes           ERP warehouse (WHSID) -> Shopify location gid
                                     (shown on the Warehouse Mapping screen): a WHSID in @Locations is inserted
                                     or gets this location; a WHSID NOT in @Locations is left alone (listed at the end)

  The ERP Full / Differential inventory feed routes for this customer are set up
  separately (existing flow) - they fill SCSInventoryFeed, which the Shopify route reads.

  Customers.Marketplace must NOT contain the word WALMART: the Warehouse Mapping
  screen then uses TargetPlusShipNodes, which is what the route reads.

  Customers.Id / PartnerGroups.Id are NOT identity -> MAX(Id)+1.
==============================================================================*/

SET NOCOUNT ON;

/*==============================================================================
  CONFIG  - use the same @ErpCustomerID in scripts 03 and 07
==============================================================================*/
DECLARE @ErpCustomerID VARCHAR(50)    = 'WKE1000';                 -- the ERP customer code for the Shopify store
DECLARE @CustomerName  NVARCHAR(250)  = 'WKE1000';                          -- REQUIRED, e.g. 'Shopify (WE)'
DECLARE @Marketplace   NVARCHAR(250)  = 'SHOPIFY ESYNCMATE DEV STORE';                          -- REQUIRED, e.g. 'SHOPIFY WALKER EDISON'

-- ERP warehouse -> Shopify location (Shopify admin > Settings > Locations > click the location: the number at the
-- end of the page address; gid = gid://shopify/Location/<number>). REQUIRED: at least one row, only this store's.
-- Left empty on purpose so a re-run can never put another store's locations on this customer.
--   dev store (esyncmate-dev-store.myshopify.com) was: ('L10', 'gid://shopify/Location/88131272890'), ('L21', 'gid://shopify/Location/88131305658')
DECLARE @Locations TABLE (WHSID VARCHAR(10), LocationGid NVARCHAR(500));
-- DEMO VALUES (2026-11-11): the two locations of the eSoftage demo store esyncmate-dev-store.myshopify.com.
-- TEMPORARY - replace with the client's own store locations (and @CustomerName / @Marketplace above), then run 02 again.
INSERT INTO @Locations VALUES
    ('L10', 'gid://shopify/Location/88131272890'),      -- demo store: Shop location
    ('L21', 'gid://shopify/Location/88131305658');      -- demo store: My Custom Location
/*==============================================================================*/

BEGIN TRY
    IF @ErpCustomerID LIKE '%[^A-Za-z0-9_]%'
        THROW 51360, '@ErpCustomerID may only hold letters, digits and _ (it becomes part of table names).', 1;

    IF LTRIM(RTRIM(@CustomerName)) = '' OR LTRIM(RTRIM(@Marketplace)) = ''
        THROW 51365, 'Fill @CustomerName and @Marketplace in the CONFIG block.', 1;

    IF NOT EXISTS (SELECT 1 FROM @Locations)
        THROW 51366, 'Fill @Locations in the CONFIG block (ERP warehouse -> this store''s Shopify location).', 1;

    IF EXISTS (SELECT 1 FROM @Locations WHERE LocationGid NOT LIKE 'gid://shopify/Location/%')
        THROW 51361, 'Every LocationGid must look like gid://shopify/Location/<number>.', 1;

    IF UPPER(@Marketplace) LIKE '%WALMART%'
        THROW 51362, '@Marketplace must not contain WALMART (Warehouse Mapping would use WalmartShipNodes).', 1;

    BEGIN TRAN;

    DECLARE @SparsId     INT = (SELECT Id FROM dbo.Customers WHERE Name = 'SPARS');
    DECLARE @eSyncMateId INT = (SELECT Id FROM dbo.Customers WHERE Name = 'eSyncMate');

    IF @SparsId     IS NULL THROW 51363, 'Customer [SPARS] not found.', 1;
    IF @eSyncMateId IS NULL THROW 51364, 'Customer [eSyncMate] not found.', 1;

    /*--- 1. Customer --------------------------------------------------------*/
    DECLARE @CustomerId INT = (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = @ErpCustomerID);

    IF @CustomerId IS NULL
    BEGIN
        SET @CustomerId = (SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.Customers WITH (UPDLOCK, HOLDLOCK));

        INSERT INTO dbo.Customers (Id, Name, ERPCustomerID, ISACustomerID, ISA810ReceiverId, ISA856ReceiverId,
                                   Marketplace, CreatedDate, CreatedBy, UseNewAuthentication)
        VALUES (@CustomerId, @CustomerName, @ErpCustomerID, NULL, NULL, NULL, @Marketplace, GETDATE(), 1, 0);

        PRINT 'Created Customer [' + @ErpCustomerID + '] Id = ' + CAST(@CustomerId AS VARCHAR(10));
    END
    ELSE
    BEGIN
        UPDATE dbo.Customers SET Name = @CustomerName, Marketplace = @Marketplace, ModifiedDate = GETDATE(), ModifiedBy = 1 WHERE Id = @CustomerId;

        PRINT 'Updated Customer [' + @ErpCustomerID + '] Id = ' + CAST(@CustomerId AS VARCHAR(10));
    END

    /*--- 2. Partner groups --------------------------------------------------*/
    IF NOT EXISTS (SELECT 1 FROM dbo.PartnerGroups WHERE SourcePartyId = @CustomerId AND DestinationPartyId = @eSyncMateId)
        INSERT INTO dbo.PartnerGroups (Id, SourcePartyId, DestinationPartyId, Description, CreatedDate, CreatedBy)
        SELECT ISNULL(MAX(Id), 0) + 1, @CustomerId, @eSyncMateId, @ErpCustomerID + ' - eSyncMate', GETDATE(), 1
          FROM dbo.PartnerGroups WITH (UPDLOCK, HOLDLOCK);

    IF NOT EXISTS (SELECT 1 FROM dbo.PartnerGroups WHERE SourcePartyId = @CustomerId AND DestinationPartyId = @SparsId)
        INSERT INTO dbo.PartnerGroups (Id, SourcePartyId, DestinationPartyId, Description, CreatedDate, CreatedBy)
        SELECT ISNULL(MAX(Id), 0) + 1, @CustomerId, @SparsId, @ErpCustomerID + ' - SPARS', GETDATE(), 1
          FROM dbo.PartnerGroups WITH (UPDLOCK, HOLDLOCK);

    /*--- 6. Warehouse -> Shopify location: existing WHSID gets the new location, a new WHSID is inserted --*/
    UPDATE S SET S.ShipNode = L.LocationGid
      FROM dbo.TargetPlusShipNodes S
      JOIN @Locations L ON L.WHSID = S.WHSID
     WHERE S.CustomerID = @ErpCustomerID AND ISNULL(S.ShipNode, '') <> L.LocationGid;

    PRINT 'Warehouse mappings updated: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

    INSERT INTO dbo.TargetPlusShipNodes (WHSID, ShipNode, CustomerID)
    SELECT L.WHSID, L.LocationGid, @ErpCustomerID
      FROM @Locations L
     WHERE NOT EXISTS (SELECT 1 FROM dbo.TargetPlusShipNodes S WHERE S.CustomerID = @ErpCustomerID AND S.WHSID = L.WHSID);

    PRINT 'Warehouse mappings inserted: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

    COMMIT TRAN;

    /*--- 3 + 4. Per-customer inventory tables (names built from the customer id) --*/
    DECLARE @Data SYSNAME = N'SCSInventoryFeedData_' + @ErpCustomerID;
    DECLARE @Log  SYSNAME = N'SCSInventoryFeed_' + @ErpCustomerID + N'_Log';
    DECLARE @sql  NVARCHAR(MAX);

    IF OBJECT_ID(N'dbo.' + @Data, 'U') IS NULL
    BEGIN
        SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@Data) + N' (
            Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY, CustomerId VARCHAR(50) NULL, ItemId VARCHAR(50) NULL,
            [Type] VARCHAR(25) NULL, [Data] NVARCHAR(MAX) NULL, BatchID NVARCHAR(500) NULL,
            CreatedDate DATETIME NOT NULL DEFAULT GETDATE(), CreatedBy INT NOT NULL DEFAULT 1);
        CREATE NONCLUSTERED INDEX ' + QUOTENAME(N'IX_' + @ErpCustomerID + N'_FeedData_BatchID') + N' ON dbo.' + QUOTENAME(@Data) + N' (BatchID);
        CREATE NONCLUSTERED INDEX ' + QUOTENAME(N'IX_' + @ErpCustomerID + N'_FeedData_ItemId') + N' ON dbo.' + QUOTENAME(@Data) + N' (CustomerId, ItemId);';
        EXEC sp_executesql @sql;
        PRINT 'Created table ' + @Data;
    END

    IF OBJECT_ID(N'dbo.' + @Log, 'U') IS NULL
    BEGIN
        SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@Log) + N' (
            LogID BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY, BatchID NVARCHAR(500) NOT NULL, LogType VARCHAR(20) NOT NULL,
            CustomerID VARCHAR(50) NULL, ItemId VARCHAR(50) NULL, CustomerItemCode VARCHAR(50) NULL,
            ETA_Date NVARCHAR(30) NULL, ETA_Qty INT NULL, Total_ATS INT NULL,
            ATS_L10 INT NULL, ATS_L21 INT NULL, ATS_L28 INT NULL, ATS_L29 INT NULL, ATS_L30 INT NULL, ATS_L34 INT NULL,
            ATS_L35 INT NULL, ATS_L36 INT NULL, ATS_L37 INT NULL, ATS_L40 INT NULL, ATS_L41 INT NULL, ATS_L55 INT NULL,
            ATS_L56 INT NULL, ATS_L57 INT NULL, ATS_L60 INT NULL, ATS_L65 INT NULL, ATS_L70 INT NULL, ATS_L91 INT NULL,
            LogDate DATETIME NOT NULL DEFAULT GETDATE(), [Status] VARCHAR(20) NULL);
        CREATE NONCLUSTERED INDEX ' + QUOTENAME(N'IX_' + @ErpCustomerID + N'_Log_BatchID') + N' ON dbo.' + QUOTENAME(@Log) + N' (BatchID);
        CREATE NONCLUSTERED INDEX ' + QUOTENAME(N'IX_' + @ErpCustomerID + N'_Log_ItemId') + N' ON dbo.' + QUOTENAME(@Log) + N' (ItemId);';
        EXEC sp_executesql @sql;
        PRINT 'Created table ' + @Log;
    END

    /*--- 5. Append the data table to the LIVE view body (nothing else in the view changes) --*/
    DECLARE @View NVARCHAR(MAX) = OBJECT_DEFINITION(OBJECT_ID('dbo.VW_AllSCSInventoryFeedData'));

    IF @View IS NULL
        PRINT 'WARNING: VW_AllSCSInventoryFeedData not found - nothing appended.';
    ELSE IF CHARINDEX(@Data, @View) > 0
        PRINT 'VW_AllSCSInventoryFeedData already includes ' + @Data;
    ELSE
    BEGIN
        SET @View = STUFF(@View, CHARINDEX('CREATE', @View), LEN('CREATE'), 'ALTER');
        SET @View = RTRIM(@View) + CHAR(13) + CHAR(10) + N'UNION ALL' + CHAR(13) + CHAR(10)
                  + N'SELECT Id, CustomerId, ItemId, [Type], [Data], BatchID, CreatedDate, CreatedBy FROM dbo.' + QUOTENAME(@Data) + N' WITH (NOLOCK)';
        EXEC sp_executesql @View;
        PRINT 'Appended ' + @Data + ' to VW_AllSCSInventoryFeedData';
    END

    /*--- Verification ---------------------------------------------------------*/
    SELECT Id, Name, ERPCustomerID, Marketplace FROM dbo.Customers WHERE ERPCustomerID = @ErpCustomerID;
    SELECT pg.Id, pg.Description FROM dbo.PartnerGroups pg WHERE pg.SourcePartyId = @CustomerId;
    -- NotInScript = a mapping this script did not set (e.g. made on the screen): it still receives stock - remove it there if wrong
    SELECT S.WHSID, S.ShipNode, CASE WHEN L.WHSID IS NULL THEN 'NotInScript' ELSE '' END AS Note
      FROM dbo.TargetPlusShipNodes S
      LEFT JOIN @Locations L ON L.WHSID = S.WHSID
     WHERE S.CustomerID = @ErpCustomerID
     ORDER BY S.WHSID;
    SELECT name AS InventoryTable FROM sys.tables WHERE name IN (@Data, @Log);
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
