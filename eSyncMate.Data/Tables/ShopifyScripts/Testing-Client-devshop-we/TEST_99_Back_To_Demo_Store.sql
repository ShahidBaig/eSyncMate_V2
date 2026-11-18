/*==============================================================================
  TEST 99 - Shopify (Task 00006): point the DEV database BACK at the eSoftage demo store

  DEV ONLY. Stops on any database other than eSyncmate_Dev. Never run it at the client.
  The client DB is switched with ..2\V2_01_Switch_To_Client_Store.sql (store walker-edison).

  WHAT IT CHANGES (one transaction, only for @ErpCustomerID):
    1. Customers.Marketplace              -> @Marketplace
    2. TargetPlusShipNodes                -> exactly the rows in @Locations (a mapping not listed is DELETED)
    3. Every Shopify REST connector of the customer -> BaseUrl, ConsumerKey (Client ID), ConsumerSecret
  NOT CHANGED: routes, Flows, schedules, Status, SQL / SPARS connectors, test data.

  To the client's dev store again: TEST_01_Switch_Dev_DB_To_devshop-we.sql
  (backup of the old mapping, made by the user 2026-11-17: ZDB_TargetPlusShipNodes_Shopify).

  An encrypted connector (saved from Setup > Connectors) cannot be changed by SQL: it is listed, change it in the UI.
==============================================================================*/

SET NOCOUNT ON;

IF DB_NAME() <> 'eSyncmate_Dev'
BEGIN
    DECLARE @Db SYSNAME = DB_NAME(); RAISERROR('TEST 99 is for the DEV database eSyncmate_Dev only. Current database: %s', 16, 1, @Db);
    SET NOEXEC ON;
END
GO

/*==============================================================================
  CONFIG
==============================================================================*/
DECLARE @ErpCustomerID  VARCHAR(50)    = 'WKE1000';
DECLARE @Prefix         NVARCHAR(100)  = 'Shopify';
DECLARE @Marketplace    NVARCHAR(250)  = 'SHOPIFY ESYNCMATE DEV STORE';              -- must NOT contain WALMART

--- eSoftage demo store (own organisation): the token request works here.
DECLARE @ShopDomain     NVARCHAR(200)  = 'esyncmate-dev-store.myshopify.com';
DECLARE @ApiVersion     NVARCHAR(20)   = '2026-10';
DECLARE @ClientId       NVARCHAR(200)  = '2878ddf0df5aeb8e4839b6392e701d02'; -- eSyncMate Connector (eSoftage app)
DECLARE @ClientSecret   NVARCHAR(500)  = '';                                 -- Blank = keep the secret already stored (same Client ID)

--- ERP warehouse -> demo store location (as in ZDB_TargetPlusShipNodes_Shopify, backup of 2026-11-17)
DECLARE @Locations TABLE (WHSID VARCHAR(10), LocationGid NVARCHAR(500));
INSERT INTO @Locations VALUES
    ('L10', 'gid://shopify/Location/88131272890'),
    ('L21', 'gid://shopify/Location/88131305658');
/*==============================================================================*/

BEGIN TRY
    /*--- Checks ---------------------------------------------------------------*/
    IF LTRIM(RTRIM(@ShopDomain)) = '' OR LTRIM(RTRIM(@ClientId)) = ''
        THROW 51400, 'Fill @ShopDomain and @ClientId in the CONFIG block.', 1;

    IF @ShopDomain NOT LIKE '%.myshopify.com'
        THROW 51401, '@ShopDomain must be the <store>.myshopify.com domain, not a custom domain.', 1;

    IF LTRIM(RTRIM(@Marketplace)) = '' OR UPPER(@Marketplace) LIKE '%WALMART%'
        THROW 51403, 'Fill @Marketplace; it must not contain WALMART (Warehouse Mapping would use WalmartShipNodes).', 1;

    IF NOT EXISTS (SELECT 1 FROM @Locations)
        THROW 51404, 'Fill @Locations in the CONFIG block (ERP warehouse -> the client store''s Shopify location).', 1;

    IF EXISTS (SELECT 1 FROM @Locations WHERE LocationGid NOT LIKE 'gid://shopify/Location/[0-9]%' OR LocationGid LIKE '%<%')
        THROW 51405, 'Every LocationGid must look like gid://shopify/Location/<number>.', 1;

    IF EXISTS (SELECT WHSID FROM @Locations GROUP BY WHSID HAVING COUNT(*) > 1)
        THROW 51407, 'A WHSID is listed twice in @Locations.', 1;

    DECLARE @CustomerId INT = (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = @ErpCustomerID);
    IF @CustomerId IS NULL
        THROW 51408, 'Customer not found. Run scripts 01 - 19 first.', 1;

    DECLARE @ShopifyTypeId INT = (SELECT Id FROM dbo.ConnectorTypes WHERE Name = 'Shopify');
    IF @ShopifyTypeId IS NULL
        THROW 51409, 'ConnectorType Shopify missing. Run scripts 01 - 19 first.', 1;

    DECLARE @BaseUrl NVARCHAR(500) = 'https://' + @ShopDomain + '/admin/api/' + @ApiVersion + '/';

    /*--- The customer's Shopify connectors -------------------------------------*/
    DECLARE @Conn TABLE (Id INT, Name NVARCHAR(200), IsPlain BIT, OldBaseUrl NVARCHAR(500));

    INSERT INTO @Conn (Id, Name, IsPlain, OldBaseUrl)
    SELECT c.Id, c.Name, ISJSON(c.Data),
           CASE WHEN ISJSON(c.Data) = 1 THEN JSON_VALUE(c.Data, '$.BaseUrl') END
      FROM dbo.Connectors c
     WHERE c.TypeId = @ShopifyTypeId
       AND c.Name LIKE @Prefix + ' - %'
       -- CASE: JSON_VALUE must never see an encrypted (non-JSON) value
       AND CASE WHEN ISJSON(c.Data) = 1 THEN ISNULL(NULLIF(JSON_VALUE(c.Data, '$.CustomerID'), ''), @ErpCustomerID)
                ELSE @ErpCustomerID END = @ErpCustomerID;

    IF NOT EXISTS (SELECT 1 FROM @Conn)
        THROW 51410, 'No Shopify connector found for this prefix / customer. Run scripts 01 - 19 first.', 1;

    BEGIN TRAN;

    /*--- 1. Customer -----------------------------------------------------------*/
    UPDATE dbo.Customers SET Marketplace = @Marketplace, ModifiedDate = GETDATE(), ModifiedBy = 1 WHERE Id = @CustomerId;

    /*--- 2. Warehouse -> location: exactly @Locations --------------------------*/
    DECLARE @Removed TABLE (WHSID VARCHAR(10), ShipNode NVARCHAR(500));

    DELETE S
    OUTPUT deleted.WHSID, deleted.ShipNode INTO @Removed
      FROM dbo.TargetPlusShipNodes S
     WHERE S.CustomerID = @ErpCustomerID
       AND NOT EXISTS (SELECT 1 FROM @Locations L WHERE L.WHSID = S.WHSID);

    DECLARE @n INT = @@ROWCOUNT;
    PRINT 'Warehouse mappings removed (not in @Locations): ' + CAST(@n AS VARCHAR(10));

    UPDATE S SET S.ShipNode = L.LocationGid
      FROM dbo.TargetPlusShipNodes S
      JOIN @Locations L ON L.WHSID = S.WHSID
     WHERE S.CustomerID = @ErpCustomerID AND ISNULL(S.ShipNode, '') <> L.LocationGid;

    SET @n = @@ROWCOUNT;
    PRINT 'Warehouse mappings updated: ' + CAST(@n AS VARCHAR(10));

    INSERT INTO dbo.TargetPlusShipNodes (WHSID, ShipNode, CustomerID)
    SELECT L.WHSID, L.LocationGid, @ErpCustomerID
      FROM @Locations L
     WHERE NOT EXISTS (SELECT 1 FROM dbo.TargetPlusShipNodes S WHERE S.CustomerID = @ErpCustomerID AND S.WHSID = L.WHSID);

    SET @n = @@ROWCOUNT;
    PRINT 'Warehouse mappings inserted: ' + CAST(@n AS VARCHAR(10));

    /*--- 3. Shopify connectors: store + app ------------------------------------
      A blank @ClientSecret keeps the stored secret, but only while the Client ID stays the same:
      the secret of another app is never kept.                                              */
    UPDATE c
       SET c.Data = JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(c.Data,
                        '$.BaseUrl', @BaseUrl), '$.ConsumerKey', @ClientId), '$.ConsumerSecret',
                        CASE WHEN @ClientSecret <> '' THEN @ClientSecret
                             WHEN ISNULL(JSON_VALUE(c.Data, '$.ConsumerKey'), '') = @ClientId THEN ISNULL(JSON_VALUE(c.Data, '$.ConsumerSecret'), '')
                             ELSE '' END),
           c.ModifiedDate = GETDATE(), c.ModifiedBy = 1
      FROM dbo.Connectors c
      JOIN @Conn x ON x.Id = c.Id
     WHERE x.IsPlain = 1;

    SET @n = @@ROWCOUNT;
    DECLARE @Enc INT = (SELECT COUNT(*) FROM @Conn WHERE IsPlain = 0);
    PRINT CONCAT('Shopify connectors updated: ', @n, ', encrypted and NOT changed: ', @Enc);

    COMMIT TRAN;

    /*--- Verification (no secrets shown) ----------------------------------------*/
    SELECT Id, Name, ERPCustomerID, Marketplace FROM dbo.Customers WHERE Id = @CustomerId;

    SELECT WHSID, ShipNode FROM dbo.TargetPlusShipNodes WHERE CustomerID = @ErpCustomerID ORDER BY WHSID;

    SELECT WHSID AS RemovedWHSID, ShipNode AS RemovedShipNode FROM @Removed ORDER BY WHSID;

    SELECT c.Id, c.Name,
           CASE WHEN x.IsPlain = 1 THEN 'Plain JSON - updated' ELSE 'Encrypted - NOT changed, edit in Setup > Connectors' END AS Result,
           x.OldBaseUrl,
           CASE WHEN x.IsPlain = 1 THEN JSON_VALUE(c.Data, '$.BaseUrl') END AS NewBaseUrl,
           CASE WHEN x.IsPlain = 1 THEN CASE WHEN JSON_VALUE(c.Data, '$.ConsumerKey') = @ClientId THEN 'as in CONFIG' ELSE 'CHECK' END END AS ClientId,
           CASE WHEN x.IsPlain = 1 THEN CASE WHEN ISNULL(JSON_VALUE(c.Data, '$.ConsumerSecret'), '') = '' THEN 'BLANK - type it in Setup > Connectors' ELSE 'filled' END END AS ClientSecret
      FROM dbo.Connectors c
      JOIN @Conn x ON x.Id = c.Id
     ORDER BY c.Id;

    -- Anything of this customer still pointing at another store (expected: no rows)
    SELECT y.Id, y.Name, y.BaseUrl AS BaseUrl_StillAnotherStore
      FROM (SELECT c.Id, c.Name, CASE WHEN ISJSON(c.Data) = 1 THEN JSON_VALUE(c.Data, '$.BaseUrl') END AS BaseUrl
              FROM dbo.Connectors c
             WHERE c.Name LIKE @Prefix + ' - %') y
     WHERE y.BaseUrl LIKE '%.myshopify.com%' AND y.BaseUrl <> @BaseUrl;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO

SET NOEXEC OFF;
GO
