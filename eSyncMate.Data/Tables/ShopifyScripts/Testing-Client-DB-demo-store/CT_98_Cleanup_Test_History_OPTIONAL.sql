/*==============================================================================
  CT 98 CLEANUP (OPTIONAL) - Shopify (Task 00006): history rows the demo-store tests left at the client

  WHERE: the CLIENT database. Run it AFTER CT_02_Revert.sql and CT_99_Revert.sql.

  The two revert scripts already remove the test DATA:
     CT_99_Revert  -> the two demo products ESM-SB-ICE / ESM-SB-DAWN in SCSInventoryFeed
     CT_02_Revert  -> the demo orders (Orders + OrderDetail + OrderData)
  What is left is only HISTORY of the test runs. This script removes it:
     1. SCSInventoryFeedData_WKE1000  - what was sent / received per item (rows of the two demo products)
     2. InventoryBatchWise            - the upload batches of WKE1000 (RouteType ShopifyInventoryUpload) since @TestStart
     3. RouteData and RouteLog of the routes "Shopify - Upload Warehouse-Wise Inventory" and "Shopify - Get Orders"
        since @TestStart - ONLY when @AlsoRouteLogs = 1 (default 0: the logs are the record of the test)

  @Delete = 0 (default): DRY RUN - only shows how many rows WOULD be deleted. Set @Delete = 1 to delete.
  Nothing else is touched: no other customer, no other item, nothing before @TestStart.
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

/*==============================================================================
  CONFIG
==============================================================================*/
DECLARE @Delete        BIT          = 0;                       -- 0 = dry run (counts only), 1 = delete
DECLARE @AlsoRouteLogs BIT          = 0;                       -- 1 = also RouteLog / RouteData of the two test routes
DECLARE @ErpCustomerID VARCHAR(50)  = 'WKE1000';
DECLARE @TestStart     DATETIME     = '2026-11-18T00:00:00';   -- day of the test (server time)
/*==============================================================================*/

BEGIN TRY
    DECLARE @FeedTable SYSNAME = 'SCSInventoryFeedData_' + @ErpCustomerID;
    DECLARE @HasFeedTable BIT = CASE WHEN OBJECT_ID('dbo.' + @FeedTable, 'U') IS NOT NULL THEN 1 ELSE 0 END;
    DECLARE @Sql NVARCHAR(MAX), @FeedRows INT = 0, @BatchRows INT = 0, @LogRows INT = 0, @DataRows INT = 0;

    DECLARE @Routes TABLE (Id INT PRIMARY KEY);
    INSERT INTO @Routes (Id)
    SELECT Id FROM dbo.Routes WHERE Name IN ('Shopify - Upload Warehouse-Wise Inventory', 'Shopify - Get Orders');

    /*--- Counts ------------------------------------------------------------------*/
    IF @HasFeedTable = 1
    BEGIN
        SET @Sql = N'SELECT @n = COUNT(*) FROM dbo.' + QUOTENAME(@FeedTable) + N' WHERE ItemId IN (''ESM-SB-ICE'', ''ESM-SB-DAWN'')';
        EXEC sp_executesql @Sql, N'@n INT OUTPUT', @n = @FeedRows OUTPUT;
    END

    SELECT @BatchRows = COUNT(*) FROM dbo.InventoryBatchWise
     WHERE CustomerID = @ErpCustomerID AND RouteType = 'ShopifyInventoryUpload' AND StartDate >= @TestStart;

    SELECT @LogRows  = COUNT(*) FROM dbo.RouteLog  WHERE RouteId IN (SELECT Id FROM @Routes) AND CreatedDate >= @TestStart;
    SELECT @DataRows = COUNT(*) FROM dbo.RouteData WHERE RouteId IN (SELECT Id FROM @Routes) AND CreatedDate >= @TestStart;

    SELECT CASE WHEN @Delete = 1 THEN 'DELETING' ELSE 'DRY RUN - nothing deleted' END AS Mode,
           @FeedRows  AS FeedDataRows_TwoDemoProducts,
           @BatchRows AS UploadBatches,
           @LogRows   AS RouteLogRows,
           @DataRows  AS RouteDataRows,
           CASE WHEN @AlsoRouteLogs = 1 THEN 'route logs included' ELSE 'route logs kept' END AS RouteLogs;

    IF @Delete = 0 RETURN;

    BEGIN TRAN;

    IF @HasFeedTable = 1
    BEGIN
        SET @Sql = N'DELETE FROM dbo.' + QUOTENAME(@FeedTable) + N' WHERE ItemId IN (''ESM-SB-ICE'', ''ESM-SB-DAWN'')';
        EXEC sp_executesql @Sql;
    END

    DELETE FROM dbo.InventoryBatchWise
     WHERE CustomerID = @ErpCustomerID AND RouteType = 'ShopifyInventoryUpload' AND StartDate >= @TestStart;

    IF @AlsoRouteLogs = 1
    BEGIN
        DELETE FROM dbo.RouteData WHERE RouteId IN (SELECT Id FROM @Routes) AND CreatedDate >= @TestStart;
        DELETE FROM dbo.RouteLog  WHERE RouteId IN (SELECT Id FROM @Routes) AND CreatedDate >= @TestStart;
    END

    COMMIT TRAN;

    PRINT 'CT 98 done: test history removed.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR (nothing changed): ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
