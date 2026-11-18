/*==============================================================================
  TEST 01 VERIFY - Shopify (Task 00006): check the DEV database after TEST 01

  READ-ONLY: only SELECTs, changes nothing. No secret or password is shown.
  Every grid has a [Check] column: OK = as expected for devshop-we, CHECK = look at it.
  Run it after TEST_01_Switch_Dev_DB_To_devshop-we.sql and paste the grids.
==============================================================================*/

SET NOCOUNT ON;

DECLARE @ErpCustomerID VARCHAR(50)   = 'WKE1000';
DECLARE @Prefix        NVARCHAR(100) = 'Shopify';
DECLARE @ShopDomain    NVARCHAR(200) = 'devshop-we.myshopify.com';
DECLARE @ApiVersion    NVARCHAR(20)  = '2026-10';
DECLARE @ClientId      NVARCHAR(200) = '2878ddf0df5aeb8e4839b6392e701d02';
DECLARE @BaseUrl       NVARCHAR(500) = 'https://' + @ShopDomain + '/admin/api/' + @ApiVersion + '/';

DECLARE @Expected TABLE (WHSID VARCHAR(10), LocationGid NVARCHAR(500));
INSERT INTO @Expected VALUES
    ('L55', 'gid://shopify/Location/92849602710'),
    ('L56', 'gid://shopify/Location/92849635478');

/*--- 1. Where am I ------------------------------------------------------------*/
SELECT '1. Database' AS Grid, @@SERVERNAME AS ServerName, DB_NAME() AS DatabaseName, GETDATE() AS ServerTime,
       CASE WHEN DB_NAME() = 'eSyncmate_Dev' THEN 'OK' ELSE 'CHECK - not the dev database' END AS [Check];

/*--- 2. Customer --------------------------------------------------------------*/
SELECT '2. Customer' AS Grid, c.Id, c.Name, c.ERPCustomerID, c.Marketplace,
       CASE WHEN c.Marketplace = 'SHOPIFY DEVSHOP-WE' THEN 'OK'
            WHEN UPPER(c.Marketplace) LIKE '%WALMART%' THEN 'CHECK - must not contain WALMART'
            ELSE 'CHECK - expected SHOPIFY DEVSHOP-WE' END AS [Check]
  FROM dbo.Customers c
 WHERE c.ERPCustomerID = @ErpCustomerID;

/*--- 3. Warehouse -> Shopify location -----------------------------------------*/
SELECT '3. Warehouse mapping' AS Grid,
       ISNULL(s.WHSID, e.WHSID) AS WHSID, s.ShipNode, e.LocationGid AS Expected,
       CASE WHEN s.WHSID IS NULL THEN 'CHECK - mapping missing'
            WHEN e.WHSID IS NULL THEN 'CHECK - extra mapping (not a devshop-we location)'
            WHEN s.ShipNode = e.LocationGid THEN 'OK'
            ELSE 'CHECK - other location id' END AS [Check]
  FROM (SELECT WHSID, ShipNode FROM dbo.TargetPlusShipNodes WHERE CustomerID = @ErpCustomerID) s
  FULL OUTER JOIN @Expected e ON e.WHSID = s.WHSID
 ORDER BY 2;

/*--- 4. Connectors of the customer --------------------------------------------*/
SELECT '4. Connectors' AS Grid, x.Id, x.Name, x.Fmt, x.ConnectivityType, x.Target, x.AuthType,
       CASE WHEN x.Fmt = 'Encrypted' THEN 'CHECK - encrypted, open it in Setup > Connectors and compare by hand'
            WHEN x.BaseUrl LIKE '%.myshopify.com%' AND x.BaseUrl <> @BaseUrl THEN 'CHECK - another Shopify store'
            WHEN x.BaseUrl = @BaseUrl AND ISNULL(x.ClientId, '') <> @ClientId THEN 'CHECK - other Client ID'
            WHEN x.BaseUrl = @BaseUrl AND x.SecretLen = 0 THEN 'CHECK - Client secret is blank'
            WHEN x.BaseUrl = @BaseUrl AND ISNULL(x.AuthType, '') <> 'SHOPIFYGetToken' THEN 'CHECK - AuthType'
            WHEN x.ConnectivityType = 'SqlServer' AND x.Target NOT LIKE '%eSyncmate_Dev%' THEN 'CHECK - SQL connector is not on the dev database'
            WHEN x.BaseUrl LIKE '%api.safavieh.com%' THEN 'NOTE - LIVE SPARS API (real ERP)'
            ELSE 'OK' END AS [Check]
  FROM (SELECT c.Id, c.Name,
               CASE WHEN ISJSON(c.Data) = 1 THEN 'Plain JSON' ELSE 'Encrypted' END AS Fmt,
               CASE WHEN ISJSON(c.Data) = 1 THEN JSON_VALUE(c.Data, '$.ConnectivityType') END AS ConnectivityType,
               CASE WHEN ISJSON(c.Data) = 1 THEN JSON_VALUE(c.Data, '$.BaseUrl') END AS BaseUrl,
               CASE WHEN ISJSON(c.Data) = 1 THEN JSON_VALUE(c.Data, '$.AuthType') END AS AuthType,
               CASE WHEN ISJSON(c.Data) = 1 THEN JSON_VALUE(c.Data, '$.ConsumerKey') END AS ClientId,
               CASE WHEN ISJSON(c.Data) = 1 THEN LEN(ISNULL(JSON_VALUE(c.Data, '$.ConsumerSecret'), '')) ELSE 0 END AS SecretLen,
               -- BaseUrl for REST; for SQL only "Server=...;Database=..." (never the user or password)
               CASE WHEN ISJSON(c.Data) = 1 THEN
                    COALESCE(NULLIF(JSON_VALUE(c.Data, '$.BaseUrl'), ''),
                             LEFT(JSON_VALUE(c.Data, '$.ConnectionString'),
                                  CHARINDEX(';', JSON_VALUE(c.Data, '$.ConnectionString') + ';',
                                            CHARINDEX('Database=', JSON_VALUE(c.Data, '$.ConnectionString') + 'Database=')) - 1))
               END AS Target
          FROM dbo.Connectors c
         WHERE c.Name LIKE @Prefix + ' - %') x
 ORDER BY x.Id;

/*--- 5. Routes: all should still be In-Active until we test them one by one ----*/
SELECT '5. Routes' AS Grid, r.Id, r.Name, r.TypeId, r.Status, r.JobID,
       sc.Name AS SourceConnector, dc.Name AS DestinationConnector,
       CASE WHEN sc.Id IS NULL OR dc.Id IS NULL THEN 'CHECK - connector missing'
            WHEN r.Status = 'In-Active' AND r.JobID IS NULL THEN 'OK'
            WHEN r.Status = 'Active' THEN 'NOTE - Active'
            ELSE 'CHECK' END AS [Check]
  FROM dbo.Routes r
  LEFT JOIN dbo.Connectors sc ON sc.Id = r.SourceConnectorId
  LEFT JOIN dbo.Connectors dc ON dc.Id = r.DestinationConnectorId
 WHERE r.Name LIKE @Prefix + ' - %'
 ORDER BY r.Id;

/*--- 6. Flows -----------------------------------------------------------------*/
SELECT '6. Flows' AS Grid, f.Id, f.Title, f.Status,
       (SELECT COUNT(*) FROM dbo.FlowDetails d WHERE d.FlowId = f.Id AND d.Status <> 'DELETED') AS Routes
  FROM dbo.Flows f
 WHERE f.Title LIKE @Prefix + ' - %' AND f.Status <> 'DELETED'
 ORDER BY f.Id;

/*--- 7. Test data left from the demo store (not touched by TEST 01) -----------*/
SELECT '7. Old test data' AS Grid,
       (SELECT COUNT(*) FROM dbo.SCSInventoryFeed WHERE CustomerID = @ErpCustomerID) AS InventoryFeedRows,
       (SELECT COUNT(*) FROM dbo.Orders o JOIN dbo.Customers c ON c.Id = o.CustomerId WHERE c.ERPCustomerID = @ErpCustomerID) AS Orders,
       'NOTE - from esyncmate-dev-store; these SKUs / orders do not exist in devshop-we' AS [Check];

/*--- 8. Backup of the old mapping ---------------------------------------------*/
IF OBJECT_ID('dbo.ZDB_TargetPlusShipNodes_Shopify') IS NOT NULL
    SELECT '8. Backup (demo store mapping)' AS Grid, WHSID, ShipNode, CustomerID FROM dbo.ZDB_TargetPlusShipNodes_Shopify;
ELSE
    SELECT '8. Backup (demo store mapping)' AS Grid, 'CHECK - table ZDB_TargetPlusShipNodes_Shopify not found' AS [Check];
GO
