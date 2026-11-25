/*==============================================================================
  PS 02 - Shopify (Task 00006), customer WKE1000: set up the route "Shopify - Sync Products" (RouteType 81)

  What the route does: it reads every inventory item of the Shopify store (id + SKU), finds our item with that
  SKU, and stores  ItemId -> gid://shopify/InventoryItem/<number>  in CustomerProductCatalogPrices.
  The upload route then sends the stock with that number.

  Three changes (from the PS_01 output of 2026-11-24):
    A. RouteTypes: row 81 "ShopifyProductSync"                              (missing at the client)
    B. Sp_SCS_GetProductCatalog: branch 'ShopifyProductSync'                (missing at the client)
       The branch offers BOTH values of an item as the SKU to compare: ItemId and CustomerItemCode.
       In the walker-edison store the SKU is the item code (WESAS21704EO1) and the UPC is the barcode,
       so the old rule (CustomerItemCode first) would match nothing.
       Only this branch is added / replaced in the LIVE procedure; every other branch stays as it is.
    C. Routes: "Shopify - Sync Products", In-Active, with the parties, partner group and connectors of the
       upload route (source 247 "Shopify - WHSW Inventory Get Data", destination 248 "Shopify - WHSW Inventory Upload").

  @Apply = 0 (default) DRY RUN: shows what would be done, changes nothing.
  @Apply = 1: makes the changes. The procedure text before the change is saved in dbo.ZDB_PS02_SpBackup
              (only the first time; PS_99 restores from it).
  Safe to run again: what is there already is left / updated, not duplicated.

  Stops (nothing changed):
    51920 RouteType 81 exists with another name      51923 the upload route (type 80) of the customer is missing
    51921 / 51922 / 51924 the procedure does not have the expected shape
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ErpCustomerID VARCHAR(50)  = 'WKE1000';
DECLARE @Apply         BIT          = 0;                          -- 0 = dry run, 1 = change
DECLARE @RouteName     VARCHAR(100) = 'Shopify - Sync Products';
DECLARE @RepeatHours   INT          = 6;                          -- schedule "Hourly, every 6" (1 - 23); the route is created In-Active
                                                                  -- (never "Minutely 60": the cron text */60 is not valid)

DECLARE @TypeDescription NVARCHAR(500) = 'Shopify product sync: inventory items of the store -> inventory item id per item (CustomerProductCatalogPrices)';

DECLARE @Branch NVARCHAR(MAX) = N'ELSE IF  @l_RouteTypeID = ''ShopifyProductSync''
		BEGIN
			-- Task 00006: values the Shopify SKU is compared with (ItemId and CustomerItemCode) -> ItemId
			SELECT INV.ItemId, LTRIM(RTRIM(INV.ItemId)) AS SKU
			FROM SCSInventoryFeed INV WITH (NOLOCK)
			WHERE INV.CustomerID = @l_CustomerID
			UNION
			SELECT INV.ItemId, LTRIM(RTRIM(INV.CustomerItemCode)) AS SKU
			FROM SCSInventoryFeed INV WITH (NOLOCK)
			WHERE INV.CustomerID = @l_CustomerID AND LTRIM(RTRIM(ISNULL(INV.CustomerItemCode, ''''))) <> ''''
		END
		';

BEGIN TRY
    /*--- checks ---------------------------------------------------------------------------*/
    IF EXISTS (SELECT 1 FROM dbo.RouteTypes WHERE Id = 81 AND Name <> 'ShopifyProductSync')
        THROW 51920, 'RouteType 81 already exists with another name - check before continuing.', 1;

    DECLARE @Def NVARCHAR(MAX) = OBJECT_DEFINITION(OBJECT_ID('dbo.Sp_SCS_GetProductCatalog'));

    IF @Def IS NULL
        THROW 51921, 'Sp_SCS_GetProductCatalog not found on this server (or no VIEW DEFINITION permission).', 1;

    IF CHARINDEX('END TRY', @Def) = 0 OR CHARINDEX('@l_RouteTypeID', @Def) = 0 OR CHARINDEX('@l_CustomerID', @Def) = 0
        THROW 51922, 'Sp_SCS_GetProductCatalog does not have the expected END TRY / @l_RouteTypeID / @l_CustomerID shape - change the branch by hand.', 1;

    -- parties, partner group and connectors: the same as the upload route of the customer
    DECLARE @SrcParty INT, @DstParty INT, @PgId INT, @SrcConn INT, @DstConn INT, @UploadRouteId INT;

    SELECT TOP 1 @UploadRouteId = R.Id, @SrcParty = R.SourcePartyId, @DstParty = R.DestinationPartyId, @PgId = R.PartyGroupId,
                 @SrcConn = R.SourceConnectorId, @DstConn = R.DestinationConnectorId
      FROM dbo.Routes R
     WHERE R.CustomerName = @ErpCustomerID AND R.TypeId = 80
     ORDER BY R.Id;

    IF @UploadRouteId IS NULL
        THROW 51923, 'The upload route (type 80) of the customer was not found: its parties and connectors are needed.', 1;

    /*--- B. the new procedure text (built here, run only with @Apply = 1) -------------------*/
    DECLARE @OldDef NVARCHAR(MAX) = @Def;
    DECLARE @SpAction VARCHAR(20);

    -- the last END TRY (DATALENGTH, not LEN: LEN drops trailing spaces, REVERSE does not)
    DECLARE @EndTry INT = DATALENGTH(@Def) / 2 - CHARINDEX(REVERSE('END TRY'), REVERSE(@Def)) - LEN('END TRY') + 2;
    DECLARE @Marker INT = CHARINDEX('''ShopifyProductSync''', @Def);

    IF @Marker = 0
    BEGIN
        SET @Def = STUFF(@Def, @EndTry, 0, @Branch);
        SET @SpAction = 'ADD branch';
    END
    ELSE
    BEGIN
        IF CHARINDEX('''ShopifyProductSync''', @Def, @Marker + 1) > 0
            THROW 51924, 'Sp_SCS_GetProductCatalog names ShopifyProductSync more than once - change the branch by hand.', 1;

        -- from the ELSE IF of the branch up to the next ELSE IF (or the last END TRY)
        DECLARE @Before NVARCHAR(MAX) = LEFT(@Def, @Marker - 1);
        DECLARE @Start  INT = DATALENGTH(@Before) / 2 - CHARINDEX(REVERSE('ELSE IF'), REVERSE(@Before)) - LEN('ELSE IF') + 2;
        DECLARE @Next   INT = CHARINDEX('ELSE IF', @Def, @Marker);
        DECLARE @End    INT = CASE WHEN @Next > 0 AND @Next < @EndTry THEN @Next ELSE @EndTry END;

        IF CHARINDEX(REVERSE('ELSE IF'), REVERSE(@Before)) = 0 OR @Start <= 0 OR @End <= @Marker
            THROW 51924, 'Could not find where the ShopifyProductSync branch starts / ends - change it by hand.', 1;

        SET @Def = STUFF(@Def, @Start, @End - @Start, @Branch);
        SET @SpAction = 'REPLACE branch';
    END

    -- CREATE header -> ALTER (only the first keyword); "CREATE OR ALTER" is fine as it is
    DECLARE @Create INT = PATINDEX('%CREATE%PROC%', @Def);
    DECLARE @Alter  INT = PATINDEX('%ALTER%PROC%', @Def);

    IF @Create > 0 AND (@Alter = 0 OR @Create < @Alter) AND SUBSTRING(@Def, @Create, 15) <> 'CREATE OR ALTER'
        SET @Def = STUFF(@Def, @Create, LEN('CREATE'), 'ALTER');

    /*--- the plan ---------------------------------------------------------------------------*/
    DECLARE @TypeThere BIT = CASE WHEN EXISTS (SELECT 1 FROM dbo.RouteTypes WHERE Id = 81) THEN 1 ELSE 0 END;
    DECLARE @RouteThere BIT = CASE WHEN EXISTS (SELECT 1 FROM dbo.Routes WHERE Name = @RouteName) THEN 1 ELSE 0 END;

    SELECT 'Plan' AS Grid, 'A. RouteType 81' AS Step,
           CAST(CASE WHEN @TypeThere = 1 THEN 'exists - Description updated' ELSE 'INSERT' END AS NVARCHAR(400)) AS [Action]
    UNION ALL
    SELECT 'Plan', 'B. Sp_SCS_GetProductCatalog',
           CONCAT(@SpAction, ' (text ', DATALENGTH(@OldDef) / 2, ' -> ', DATALENGTH(@Def) / 2, ' characters)')
    UNION ALL
    SELECT 'Plan', 'C. Route ' + @RouteName,
           CONCAT(CASE WHEN @RouteThere = 1 THEN 'exists - parties / connectors updated, Status and schedule left' ELSE 'INSERT In-Active' END,
                  ': parties ', @SrcParty, ' -> ', @DstParty, ', partner group ', @PgId, ', connectors ', @SrcConn, ' / ', @DstConn,
                  ' (from upload route ', @UploadRouteId, ')');

    IF @Apply = 0
    BEGIN
        PRINT 'DRY RUN: nothing changed. Set @Apply = 1 to make the changes.';
        RETURN;
    END

    /*--- apply ------------------------------------------------------------------------------*/
    BEGIN TRAN;

    -- backup of the procedure text, only the first time
    IF OBJECT_ID('dbo.ZDB_PS02_SpBackup', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ZDB_PS02_SpBackup (ProcName SYSNAME NOT NULL, Definition NVARCHAR(MAX) NOT NULL, BackupDate DATETIME NOT NULL);

        INSERT INTO dbo.ZDB_PS02_SpBackup (ProcName, Definition, BackupDate)
        VALUES ('Sp_SCS_GetProductCatalog', @OldDef, GETDATE());
    END

    -- A
    IF NOT EXISTS (SELECT 1 FROM dbo.RouteTypes WHERE Id = 81)
        INSERT INTO dbo.RouteTypes (Id, Name, Description, CreatedDate, CreatedBy)
        VALUES (81, 'ShopifyProductSync', @TypeDescription, GETDATE(), 1);
    ELSE
        UPDATE dbo.RouteTypes SET Description = @TypeDescription, ModifiedDate = GETDATE(), ModifiedBy = 1 WHERE Id = 81;

    -- B
    EXEC sp_executesql @Def;

    -- C (Routes.Id is NOT identity)
    IF EXISTS (SELECT 1 FROM dbo.Routes WHERE Name = @RouteName)
        UPDATE dbo.Routes
           SET TypeId = 81, SourcePartyId = @SrcParty, DestinationPartyId = @DstParty,
               SourceConnectorId = @SrcConn, DestinationConnectorId = @DstConn, MapId = 0, PartyGroupId = @PgId,
               RouteGroup = 'Inventory', CustomerName = @ErpCustomerID, ModifiedDate = GETDATE(), ModifiedBy = 1
         WHERE Name = @RouteName;
    ELSE
        INSERT INTO dbo.Routes
            (Id, TypeId, Status, SourcePartyId, DestinationPartyId, SourceConnectorId, DestinationConnectorId, MapId, PartyGroupId,
             CreatedDate, CreatedBy, FrequencyType, StartDate, EndDate, RepeatCount, WeekDays, OnDay, ExecutionTime, JobID,
             Name, RouteGroup, CustomerName)
        SELECT ISNULL(MAX(Id), 0) + 1, 81, 'In-Active', @SrcParty, @DstParty, @SrcConn, @DstConn, 0, @PgId,
               GETDATE(), 1, 'Hourly', CAST(GETDATE() AS DATE), NULL, @RepeatHours, '', '', '', NULL,
               @RouteName, 'Inventory', @ErpCustomerID
          FROM dbo.Routes WITH (UPDLOCK, HOLDLOCK);

    COMMIT TRAN;

    PRINT 'Done. Run PS_03_Verify_Sync_Products.sql.';

    SELECT 'Result' AS Grid, r.Id, r.Name, r.TypeId, r.Status, r.CustomerName, r.SourceConnectorId, r.DestinationConnectorId, r.JobID,
           CASE WHEN EXISTS (SELECT 1 FROM dbo.VW_Routes v WHERE v.Id = r.Id) THEN 'yes' ELSE 'NO - check parties / connectors' END AS ShownOnRoutesScreen
      FROM dbo.Routes r
     WHERE r.Name = @RouteName;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
