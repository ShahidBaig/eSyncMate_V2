/*==============================================================================
  V2 01 - Shopify (Task 00006): switch an installed customer to the client's own store

  WHEN: scripts 01 - 19 were already run with the DEMO store values (client DB, 2026-11-12).
        This script is independent: it does NOT need 02 / 03 / 10 / 13 / 15 / 17 to be run again.
        It only changes values; it creates no connector, route, Flow, table or SP.

  WHAT IT CHANGES (one transaction, only for @ErpCustomerID):
    1. Customers.Marketplace              -> @Marketplace
    2. TargetPlusShipNodes                -> exactly the rows in @Locations
                                             (updated / inserted; a mapping NOT in @Locations is DELETED,
                                              because a demo store location id does not exist in the client's store)
    3. Every Shopify REST connector of the customer ("<Prefix> - ..." with ConnectorType Shopify:
       WHSW Inventory Upload, Get Orders, Create Fulfillment, Cancel Order, Get Cancelled Orders)
                                          -> BaseUrl, ConsumerKey (Client ID), ConsumerSecret (Client secret)
       Everything else in the connector (Url, Method, Parameters, ...) stays as it is.

  NOT CHANGED: routes, Flows, schedules, Status (all stay In-Active), SQL / SPARS connectors.

  An encrypted connector (saved from Setup > Connectors) cannot be changed by SQL: it is listed, change it in the UI.
  Run it as often as needed. Fill the CONFIG block, run, then clear @ClientSecret again.
  Do not commit or e-mail a copy with the secret filled in.
==============================================================================*/

SET NOCOUNT ON;

/*==============================================================================
  CONFIG
==============================================================================*/
DECLARE @ErpCustomerID  VARCHAR(50)    = 'WKE1000';
DECLARE @Prefix         NVARCHAR(100)  = 'Shopify';
DECLARE @Marketplace    NVARCHAR(250)  = 'SHOPIFY WALKER EDISON';            -- must NOT contain WALMART

--- Client store + app
--- The app is the eSoftage app "eSyncMate Connector" (eSoftage Dev Dashboard > app > Settings > Credentials).
--- The client installs THAT app on its store with the custom install link (generated 2026-11-12 for
--- walker-edison.myshopify.com, client organisation 1979525), so the Client ID / secret are the same as on the demo
--- store. Only the store address (BaseUrl) and the locations are the client's own.
DECLARE @ShopDomain     NVARCHAR(200)  = 'walker-edison.myshopify.com';      -- client store (given by the client 2026-11-12)
DECLARE @ApiVersion     NVARCHAR(20)   = '2026-10';
DECLARE @ClientId       NVARCHAR(200)  = '2878ddf0df5aeb8e4839b6392e701d02'; -- eSyncMate Connector (eSoftage app)
DECLARE @ClientSecret   NVARCHAR(500)  = '';                                 -- the app's Client secret; blank = type it in Setup > Connectors

--- ERP warehouse -> the CLIENT store's Shopify location
---   Shopify admin > Settings > Locations > click the location: the number at the end of the page address.
--- REQUIRED: at least one row. Remove the two example lines and add the real ones.
DECLARE @Locations TABLE (WHSID VARCHAR(10), LocationGid NVARCHAR(500));
-- INSERT INTO @Locations VALUES
--     ('L10', 'gid://shopify/Location/<number>'),
--     ('L21', 'gid://shopify/Location/<number>');
/*==============================================================================*/

BEGIN TRY
    /*--- Checks ---------------------------------------------------------------*/
    IF LTRIM(RTRIM(@ShopDomain)) = '' OR LTRIM(RTRIM(@ClientId)) = ''
        THROW 51400, 'Fill @ShopDomain and @ClientId in the CONFIG block.', 1;

    IF @ShopDomain NOT LIKE '%.myshopify.com'
        THROW 51401, '@ShopDomain must be the <store>.myshopify.com domain, not a custom domain.', 1;

    IF @ShopDomain = 'esyncmate-dev-store.myshopify.com'
        THROW 51402, 'This is the DEMO store. This script is for the client''s own store.', 1;

    IF LTRIM(RTRIM(@Marketplace)) = '' OR UPPER(@Marketplace) LIKE '%WALMART%'
        THROW 51403, 'Fill @Marketplace; it must not contain WALMART (Warehouse Mapping would use WalmartShipNodes).', 1;

    IF NOT EXISTS (SELECT 1 FROM @Locations)
        THROW 51404, 'Fill @Locations in the CONFIG block (ERP warehouse -> the client store''s Shopify location).', 1;

    IF EXISTS (SELECT 1 FROM @Locations WHERE LocationGid NOT LIKE 'gid://shopify/Location/[0-9]%' OR LocationGid LIKE '%<%')
        THROW 51405, 'Every LocationGid must look like gid://shopify/Location/<number>.', 1;

    IF EXISTS (SELECT 1 FROM @Locations WHERE LocationGid IN ('gid://shopify/Location/88131272890', 'gid://shopify/Location/88131305658'))
        THROW 51406, '@Locations holds a DEMO store location id.', 1;

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
      The secret always follows the Client ID: a secret of another app is never kept. */
    UPDATE c
       SET c.Data = JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(c.Data,
                        '$.BaseUrl', @BaseUrl), '$.ConsumerKey', @ClientId), '$.ConsumerSecret', @ClientSecret),
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
