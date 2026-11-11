/*==============================================================================
  DEV ONLY - NEVER RUN AT THE CLIENT.  Not part of the deployment (see README).

  Renames the Shopify test customer of the DEV database from @From to @To, so the dev setup uses the same
  customer id as the client. The client gets the customer from scripts 01 - 18; this script only moves the
  rows a dev test run already made. It stops when the database is not @DevDatabase.

  What is renamed (live data only):
    Customers (ERPCustomerID, Name when it was the id), PartnerGroups.Description, Connectors.Data (plain JSON:
    CustomerID and the SP_OrdersData literal), Routes.CustomerName, TargetPlusShipNodes, SCSInventoryFeed,
    Temp_SCSInventoryFeed, InventoryBatchWise, InventoryBatchWiseFeedDetail, RouteExecutionLock,
    CustomerProductCatalogPrices, Users.CustomerName (the user's customer list),
    tables SCSInventoryFeedData_<id> / SCSInventoryFeed_<id>_Log (+ their IX_ indexes and CustomerId column),
    VW_AllSCSInventoryFeedData, the customer's ASN branch of SP_OrdersData.

  What is NOT changed:
    - history: ConnectorsLogs, CustomersLogs, PartnerGroupsLogs, RoutesLogs, RouteLog, RouteData, OrderData
    - Customers.Marketplace, Status / schedule / JobID of the routes, the Shopify store and its locations
    - ENCRYPTED connectors (saved from Setup > Connectors): SQL cannot read them. They are listed at the end -
      open each one in Setup > Connectors and set the Customer ID to @To.
    - the ERP: orders already placed in the ERP under @From stay under @From there.

  @Commit = 0 (default): everything is done and ROLLED BACK, so the output shows what a real run would do.
  @Commit = 1: the changes are kept. To go back, run it again with @From and @To swapped.
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

/*==============================================================================
  CONFIG
==============================================================================*/
DECLARE @From        VARCHAR(50) = 'SHOPIFY01';
DECLARE @To          VARCHAR(50) = 'WKE1000';
DECLARE @DevDatabase SYSNAME     = 'eSyncmate_Dev';
DECLARE @Commit      BIT         = 0;
/*==============================================================================*/

DECLARE @CustomerId INT, @n INT, @sql NVARCHAR(MAX), @Def NVARCHAR(MAX), @Msg NVARCHAR(600);
DECLARE @OldData SYSNAME = N'SCSInventoryFeedData_' + @From, @NewData SYSNAME = N'SCSInventoryFeedData_' + @To;
DECLARE @OldLog  SYSNAME = N'SCSInventoryFeed_' + @From + N'_Log', @NewLog SYSNAME = N'SCSInventoryFeed_' + @To + N'_Log';
DECLARE @Create INT, @Alter INT, @Ix SYSNAME, @NewIx SYSNAME, @Qualified NVARCHAR(600);

BEGIN TRY
    /*--- Checks -----------------------------------------------------------------*/
    IF DB_NAME() <> @DevDatabase
    BEGIN
        SET @Msg = 'This is database [' + DB_NAME() + '], not [' + @DevDatabase + ']. DEV ONLY script - nothing changed.';
        THROW 51900, @Msg, 1;
    END

    IF @From LIKE '%[^A-Za-z0-9_]%' OR @To LIKE '%[^A-Za-z0-9_]%' OR @From = @To OR @From = '' OR @To = ''
        THROW 51901, '@From / @To may only hold letters, digits and _ and must differ.', 1;

    SET @CustomerId = (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = @From);

    IF @CustomerId IS NULL
        THROW 51902, 'Customer @From not found - nothing to rename.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Customers WHERE ERPCustomerID = @To)
       OR EXISTS (SELECT 1 FROM dbo.SCSInventoryFeed WHERE CustomerID = @To)
       OR EXISTS (SELECT 1 FROM dbo.TargetPlusShipNodes WHERE CustomerID = @To)
       OR EXISTS (SELECT 1 FROM dbo.Routes WHERE CustomerName = @To)
       OR OBJECT_ID(N'dbo.' + @NewData, 'U') IS NOT NULL OR OBJECT_ID(N'dbo.' + @NewLog, 'U') IS NOT NULL
        THROW 51903, 'Customer @To already has rows / tables in this database - nothing changed.', 1;

    -- an Active route has a Hangfire job running with the old id: de-activate it from the UI first
    IF EXISTS (SELECT 1 FROM dbo.Routes WHERE CustomerName = @From AND Status = 'Active')
        THROW 51904, 'A route of @From is Active. De-activate it from the UI (Setup > Routes / the Flow) first.', 1;

    SET @Def = OBJECT_DEFINITION(OBJECT_ID('dbo.SP_OrdersData'));

    IF @Def IS NOT NULL AND CHARINDEX('''' + @To + '''', @Def) > 0
        THROW 51905, 'SP_OrdersData already names @To - nothing changed.', 1;

    BEGIN TRAN;

    /*--- 1. Setup rows ------------------------------------------------------------*/
    UPDATE dbo.Customers
       SET ERPCustomerID = @To, Name = CASE WHEN Name = @From THEN @To ELSE Name END, ModifiedDate = GETDATE(), ModifiedBy = 1
     WHERE Id = @CustomerId;
    PRINT CONCAT('Customers: ', @@ROWCOUNT);

    UPDATE dbo.PartnerGroups SET Description = REPLACE(Description, @From, @To)
     WHERE (SourcePartyId = @CustomerId OR DestinationPartyId = @CustomerId) AND Description LIKE '%' + @From + '%';
    PRINT CONCAT('PartnerGroups: ', @@ROWCOUNT);

    -- plain JSON connectors of this customer: CustomerID and the literal in the SP_OrdersData command
    UPDATE dbo.Connectors
       SET Data = REPLACE(CAST(Data AS NVARCHAR(MAX)), @From, @To), ModifiedDate = GETDATE(), ModifiedBy = 1
     WHERE ISJSON(CAST(Data AS NVARCHAR(MAX))) = 1 AND JSON_VALUE(CAST(Data AS NVARCHAR(MAX)), '$.CustomerID') = @From;
    PRINT CONCAT('Connectors (plain JSON): ', @@ROWCOUNT);

    UPDATE dbo.Routes SET CustomerName = @To, ModifiedDate = GETDATE(), ModifiedBy = 1 WHERE CustomerName = @From;
    PRINT CONCAT('Routes: ', @@ROWCOUNT);

    UPDATE dbo.TargetPlusShipNodes SET CustomerID = @To WHERE CustomerID = @From;
    PRINT CONCAT('TargetPlusShipNodes: ', @@ROWCOUNT);

    UPDATE dbo.Users SET CustomerName = REPLACE(CustomerName, @From, @To) WHERE CustomerName LIKE '%' + @From + '%';
    PRINT CONCAT('Users (customer list): ', @@ROWCOUNT);

    /*--- 2. Data rows -------------------------------------------------------------*/
    UPDATE dbo.SCSInventoryFeed SET CustomerID = @To WHERE CustomerID = @From;
    PRINT CONCAT('SCSInventoryFeed: ', @@ROWCOUNT);

    UPDATE dbo.Temp_SCSInventoryFeed SET CustomerID = @To WHERE CustomerID = @From;
    PRINT CONCAT('Temp_SCSInventoryFeed: ', @@ROWCOUNT);

    UPDATE dbo.InventoryBatchWise SET CustomerID = @To WHERE CustomerID = @From;
    PRINT CONCAT('InventoryBatchWise: ', @@ROWCOUNT);

    UPDATE dbo.InventoryBatchWiseFeedDetail SET CustomerID = @To WHERE CustomerID = @From;
    PRINT CONCAT('InventoryBatchWiseFeedDetail: ', @@ROWCOUNT);

    UPDATE dbo.RouteExecutionLock SET CustomerID = @To WHERE CustomerID = @From;
    PRINT CONCAT('RouteExecutionLock: ', @@ROWCOUNT);

    UPDATE dbo.CustomerProductCatalogPrices SET CustomerID = @To WHERE CustomerID = @From;
    PRINT CONCAT('CustomerProductCatalogPrices: ', @@ROWCOUNT);

    /*--- 3. The customer's own inventory tables -----------------------------------*/
    IF OBJECT_ID(N'dbo.' + @OldData, 'U') IS NOT NULL
    BEGIN
        SET @Qualified = N'dbo.' + @OldData;
        EXEC sp_rename @Qualified, @NewData;

        SET @sql = N'UPDATE dbo.' + QUOTENAME(@NewData) + N' SET CustomerId = @To WHERE CustomerId = @From;';
        EXEC sp_executesql @sql, N'@From VARCHAR(50), @To VARCHAR(50)', @From = @From, @To = @To;
        PRINT CONCAT(@OldData, ' -> ', @NewData, ', rows: ', @@ROWCOUNT);
    END

    IF OBJECT_ID(N'dbo.' + @OldLog, 'U') IS NOT NULL
    BEGIN
        SET @Qualified = N'dbo.' + @OldLog;
        EXEC sp_rename @Qualified, @NewLog;

        SET @sql = N'UPDATE dbo.' + QUOTENAME(@NewLog) + N' SET CustomerID = @To WHERE CustomerID = @From;';
        EXEC sp_executesql @sql, N'@From VARCHAR(50), @To VARCHAR(50)', @From = @From, @To = @To;
        PRINT CONCAT(@OldLog, ' -> ', @NewLog, ', rows: ', @@ROWCOUNT);
    END

    -- the IX_<id>_... indexes script 02 made
    DECLARE ix_cc CURSOR LOCAL FAST_FORWARD FOR
        SELECT OBJECT_NAME(i.object_id), i.name
          FROM sys.indexes i
         WHERE i.object_id IN (OBJECT_ID(N'dbo.' + @NewData), OBJECT_ID(N'dbo.' + @NewLog)) AND i.name LIKE 'IX[_]' + @From + '[_]%';

    DECLARE @IxTable SYSNAME;

    OPEN ix_cc;
    FETCH NEXT FROM ix_cc INTO @IxTable, @Ix;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @NewIx = REPLACE(@Ix, 'IX_' + @From + '_', 'IX_' + @To + '_');
        SET @Qualified = N'dbo.' + @IxTable + N'.' + @Ix;
        EXEC sp_rename @Qualified, @NewIx, N'INDEX';

        FETCH NEXT FROM ix_cc INTO @IxTable, @Ix;
    END

    CLOSE ix_cc;
    DEALLOCATE ix_cc;

    /*--- 4. VW_AllSCSInventoryFeedData: the renamed data table ----------------------*/
    SET @Def = OBJECT_DEFINITION(OBJECT_ID('dbo.VW_AllSCSInventoryFeedData'));

    IF @Def IS NOT NULL AND CHARINDEX(@OldData, @Def) > 0
    BEGIN
        SET @Def = REPLACE(@Def, @OldData, @NewData);
        SET @Def = STUFF(@Def, CHARINDEX('CREATE', @Def), LEN('CREATE'), 'ALTER');
        EXEC sp_executesql @Def;
        PRINT 'VW_AllSCSInventoryFeedData: ' + @OldData + ' -> ' + @NewData;
    END
    ELSE
        PRINT 'VW_AllSCSInventoryFeedData does not name ' + @OldData + ' - left as it is.';

    /*--- 5. SP_OrdersData: the customer's ASN branch ---------------------------------*/
    SET @Def = OBJECT_DEFINITION(OBJECT_ID('dbo.SP_OrdersData'));
    SET @n   = CASE WHEN @Def IS NULL THEN 0 ELSE (LEN(@Def) - LEN(REPLACE(@Def, '''' + @From + '''', ''))) / LEN('''' + @From + '''') END;

    IF @n = 1
    BEGIN
        SET @Def = REPLACE(@Def, '''' + @From + '''', '''' + @To + '''');

        -- CREATE or ALTER header -> ALTER (only the first keyword), as scripts 06 / 13 do
        SET @Create = PATINDEX('%CREATE%PROC%', @Def);
        SET @Alter  = PATINDEX('%ALTER%PROC%', @Def);

        IF @Create > 0 AND (@Alter = 0 OR @Create < @Alter) AND SUBSTRING(@Def, @Create, 15) <> 'CREATE OR ALTER'
            SET @Def = STUFF(@Def, @Create, LEN('CREATE'), 'ALTER');

        EXEC sp_executesql @Def;
        PRINT 'SP_OrdersData: branch ''' + @From + ''' -> ''' + @To + '''';
    END
    ELSE IF @n = 0
        PRINT 'SP_OrdersData has no ''' + @From + ''' branch - left as it is (script 13 adds the branch).';
    ELSE
        THROW 51906, 'SP_OrdersData names @From more than once - change it by hand. Nothing changed.', 1;

    /*--- Verification (inside the transaction, so a dry run shows the result too) ----*/
    SELECT Id, Name, ERPCustomerID, Marketplace FROM dbo.Customers WHERE Id = @CustomerId;

    SELECT r.Id, r.Name, r.TypeId, r.Status, r.CustomerName FROM dbo.Routes r WHERE r.CustomerName = @To ORDER BY r.Id;

    SELECT c.Id, c.Name,
           CASE WHEN ISJSON(CAST(c.Data AS NVARCHAR(MAX))) = 1 THEN JSON_VALUE(CAST(c.Data AS NVARCHAR(MAX)), '$.CustomerID')
                ELSE 'ENCRYPTED - set the Customer ID to ' + @To + ' in Setup > Connectors' END AS CustomerID,
           CASE WHEN ISJSON(CAST(c.Data AS NVARCHAR(MAX))) = 1 THEN JSON_VALUE(CAST(c.Data AS NVARCHAR(MAX)), '$.Command') END AS Command
      FROM dbo.Connectors c
     WHERE c.Id IN (SELECT SourceConnectorId FROM dbo.Routes WHERE CustomerName = @To
                    UNION SELECT DestinationConnectorId FROM dbo.Routes WHERE CustomerName = @To)
     ORDER BY c.Id;

    SELECT 'still ' + @From AS Leftover, x.Place, x.Cnt
      FROM (SELECT 'SCSInventoryFeed' AS Place, COUNT(*) AS Cnt FROM dbo.SCSInventoryFeed WHERE CustomerID = @From
            UNION ALL SELECT 'TargetPlusShipNodes', COUNT(*) FROM dbo.TargetPlusShipNodes WHERE CustomerID = @From
            UNION ALL SELECT 'Routes', COUNT(*) FROM dbo.Routes WHERE CustomerName = @From
            UNION ALL SELECT 'Connectors (plain JSON)', COUNT(*) FROM dbo.Connectors WHERE ISJSON(CAST(Data AS NVARCHAR(MAX))) = 1 AND CAST(Data AS NVARCHAR(MAX)) LIKE '%' + @From + '%'
            UNION ALL SELECT 'SP / view text', COUNT(*) FROM sys.sql_modules WHERE definition LIKE '%' + @From + '%') x;

    SELECT name AS InventoryTable FROM sys.tables WHERE name IN (@NewData, @NewLog, @OldData, @OldLog);

    IF @Commit = 1
    BEGIN
        COMMIT TRAN;
        PRINT 'DONE: ' + @From + ' -> ' + @To + ' committed.';
    END
    ELSE
    BEGIN
        ROLLBACK TRAN;
        PRINT 'DRY RUN: everything above was rolled back. Set @Commit = 1 to keep it.';
    END
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
