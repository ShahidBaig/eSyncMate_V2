/*==============================================================================
  PS 99 - Shopify (Task 00006), customer WKE1000: revert of PS_02_Setup_Sync_Products.sql

  Puts back what PS_02 changed:
    C. deletes the route "Shopify - Sync Products" and its Flow row of PS_04 (its RouteLog lines stay as history)
    B. Sp_SCS_GetProductCatalog goes back to the text saved in dbo.ZDB_PS02_SpBackup; the backup table is dropped
    A. deletes RouteType 81 (only when no route uses it)

  @RemoveCatalogIds = 0 (default): the ids the sync route stored in CustomerProductCatalogPrices STAY.
      They are correct Shopify ids and the upload needs them. Set it to 1 only to go back to the state before
      the first sync (the customer had 0 rows then): it deletes the customer's gid://shopify/InventoryItem/ rows.

  @Apply = 0 (default) DRY RUN.

  Stops (nothing changed):
    51930 the route is Active or has a job - set it In-Active from the screen first
    51931 the route is Active in a Flow - set it In-Active on the Flows screen first
    51932 the procedure was changed by someone else after PS_02 (the ShopifyProductSync branch is gone)
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ErpCustomerID    VARCHAR(50)  = 'WKE1000';
DECLARE @Apply            BIT          = 0;   -- 0 = dry run, 1 = change
DECLARE @RemoveCatalogIds BIT          = 0;   -- 1 = also delete the stored Shopify ids of the customer
DECLARE @RouteName        VARCHAR(100) = 'Shopify - Sync Products';

BEGIN TRY
    DECLARE @RouteId INT = (SELECT Id FROM dbo.Routes WHERE Name = @RouteName AND TypeId = 81);
    DECLARE @HasBackup BIT = CASE WHEN OBJECT_ID('dbo.ZDB_PS02_SpBackup', 'U') IS NOT NULL THEN 1 ELSE 0 END;

    SELECT 'State now' AS Grid,
           @RouteId AS RouteId,
           (SELECT Status FROM dbo.Routes WHERE Id = @RouteId) AS RouteStatus,
           (SELECT JobID FROM dbo.Routes WHERE Id = @RouteId) AS RouteJobID,
           CASE WHEN @HasBackup = 1 THEN 'present' ELSE 'none - the procedure is left as it is' END AS SpBackup,
           (SELECT COUNT(*) FROM dbo.Routes WHERE TypeId = 81) AS RoutesOfType81,
           (SELECT COUNT(*) FROM dbo.CustomerProductCatalogPrices
             WHERE CustomerID = @ErpCustomerID AND ProductId LIKE 'gid://shopify/InventoryItem/%') AS StoredShopifyIds;

    IF @RouteId IS NOT NULL AND EXISTS (SELECT 1 FROM dbo.Routes WHERE Id = @RouteId AND (Status = 'Active' OR JobID IS NOT NULL))
        THROW 51930, 'The route is Active or has a job. Set it In-Active from the screen first, then run again.', 1;

    DECLARE @InFlow INT = 0;

    -- EXEC: the table may be missing on this server
    IF @RouteId IS NOT NULL AND OBJECT_ID('dbo.FlowDetails', 'U') IS NOT NULL
        EXEC sp_executesql N'SELECT @n = COUNT(*) FROM dbo.FlowDetails WHERE RouteId = @r AND Status = ''Active''',
                           N'@r INT, @n INT OUTPUT', @r = @RouteId, @n = @InFlow OUTPUT;

    IF @InFlow > 0
        THROW 51931, 'The route is Active in a Flow. Set it In-Active on the Flows screen first, then run again.', 1;

    IF @HasBackup = 1
       AND CHARINDEX('''ShopifyProductSync''', OBJECT_DEFINITION(OBJECT_ID('dbo.Sp_SCS_GetProductCatalog'))) = 0
        THROW 51932, 'Sp_SCS_GetProductCatalog has no ShopifyProductSync branch any more: it was changed after PS_02. Restoring the backup would undo that change - check by hand.', 1;

    IF @Apply = 0
    BEGIN
        PRINT 'DRY RUN: nothing changed. Set @Apply = 1 to revert.';
        RETURN;
    END

    BEGIN TRAN;

    DECLARE @DelIds INT = 0;

    -- C
    IF @RouteId IS NOT NULL
    BEGIN
        -- the Flow row of PS_04 (In-Active; an Active one stopped the script above)
        IF OBJECT_ID('dbo.FlowDetails', 'U') IS NOT NULL
            EXEC sp_executesql N'DELETE FROM dbo.FlowDetails WHERE RouteId = @r', N'@r INT', @r = @RouteId;

        DELETE FROM dbo.Routes WHERE Id = @RouteId;
    END

    -- B
    IF @HasBackup = 1
    BEGIN
        DECLARE @Def NVARCHAR(MAX);

        -- EXEC: the table may be missing when this batch is compiled
        EXEC sp_executesql N'SELECT TOP 1 @d = Definition FROM dbo.ZDB_PS02_SpBackup WHERE ProcName = ''Sp_SCS_GetProductCatalog'' ORDER BY BackupDate',
                           N'@d NVARCHAR(MAX) OUTPUT', @d = @Def OUTPUT;

        IF @Def IS NOT NULL
        BEGIN
            DECLARE @Create INT = PATINDEX('%CREATE%PROC%', @Def);
            DECLARE @Alter  INT = PATINDEX('%ALTER%PROC%', @Def);

            IF @Create > 0 AND (@Alter = 0 OR @Create < @Alter) AND SUBSTRING(@Def, @Create, 15) <> 'CREATE OR ALTER'
                SET @Def = STUFF(@Def, @Create, LEN('CREATE'), 'ALTER');

            EXEC sp_executesql @Def;
        END

        DROP TABLE dbo.ZDB_PS02_SpBackup;
    END

    -- A
    IF NOT EXISTS (SELECT 1 FROM dbo.Routes WHERE TypeId = 81)
        DELETE FROM dbo.RouteTypes WHERE Id = 81 AND Name = 'ShopifyProductSync';

    IF @RemoveCatalogIds = 1
    BEGIN
        DELETE FROM dbo.CustomerProductCatalogPrices
         WHERE CustomerID = @ErpCustomerID AND ProductId LIKE 'gid://shopify/InventoryItem/%';
        SET @DelIds = @@ROWCOUNT;
    END

    COMMIT TRAN;

    PRINT CONCAT('Reverted. Route deleted: ', CASE WHEN @RouteId IS NULL THEN 'none' ELSE CAST(@RouteId AS VARCHAR(10)) END,
                 '. Procedure restored: ', CASE WHEN @HasBackup = 1 THEN 'yes' ELSE 'no backup' END,
                 '. Stored Shopify ids deleted: ', @DelIds, '.');

    SELECT 'After' AS Grid,
           (SELECT COUNT(*) FROM dbo.RouteTypes WHERE Id = 81) AS RouteType81,
           (SELECT COUNT(*) FROM dbo.Routes WHERE TypeId = 81) AS RoutesOfType81,
           CASE WHEN CHARINDEX('''ShopifyProductSync''', OBJECT_DEFINITION(OBJECT_ID('dbo.Sp_SCS_GetProductCatalog'))) > 0 THEN 'present' ELSE 'gone' END AS ProductSyncBranch,
           CASE WHEN CHARINDEX('''ShopifyInventoryUpload''', OBJECT_DEFINITION(OBJECT_ID('dbo.Sp_SCS_GetProductCatalog'))) > 0 THEN 'present' ELSE 'MISSING' END AS UploadBranch;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
