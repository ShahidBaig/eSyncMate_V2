/*==============================================================================
  CT 01 VERIFY - Shopify (Task 00006): state of the inventory test at the client against our demo store

  READ-ONLY: only SELECTs (grid 6 calls the route's source procedure, which only reads). No token or secret shown.
  Run it BEFORE the route (everything must be OK / NOTE) and AFTER it (grid 5: Status must be SYNCED).
==============================================================================*/

SET NOCOUNT ON;

DECLARE @ErpCustomerID VARCHAR(50)   = 'WKE1000';
DECLARE @DemoStore     NVARCHAR(200) = 'esyncmate-dev-store.myshopify.com';

/*--- 1. Token selected on the customer --------------------------------------------*/
SELECT '1. Token' AS Grid, C.ERPCustomerID, C.IntegrationTokenId, T.Name AS SelectedToken,
       CASE WHEN T.AccountKey = @DemoStore THEN 'OK - demo store token' ELSE 'CHECK - not the demo store token' END AS [Check]
  FROM dbo.Customers C
  LEFT JOIN dbo.IntegrationTokens T ON T.Id = C.IntegrationTokenId
 WHERE C.ERPCustomerID = @ErpCustomerID;

/*--- 2. Warehouse mapping -----------------------------------------------------------*/
SELECT '2. Mapping' AS Grid, S.WHSID, S.ShipNode,
       CASE WHEN S.WHSID = 'L55' AND S.ShipNode = 'gid://shopify/Location/88131272890' THEN 'OK - Shop location'
            WHEN S.WHSID = 'L56' AND S.ShipNode = 'gid://shopify/Location/88131305658' THEN 'OK - My Custom Location'
            ELSE 'CHECK - not a demo store location' END AS [Check]
  FROM dbo.TargetPlusShipNodes S
 WHERE S.CustomerID = @ErpCustomerID
 ORDER BY S.WHSID;

/*--- 3. Shopify connectors: which store ---------------------------------------------
      Only "WHSW Inventory Upload" must be on the demo store. The others stay on walker-edison on purpose. */
SELECT '3. Connectors' AS Grid, x.Id, x.Name, x.Store,
       CASE WHEN x.Fmt = 'Encrypted' THEN 'NOTE - encrypted, check in Setup > Connectors'
            WHEN x.Name LIKE '%WHSW Inventory Upload' AND x.Store = @DemoStore THEN 'OK - demo store'
            WHEN x.Name LIKE '%WHSW Inventory Upload' THEN 'CHECK - must be the demo store for this test'
            WHEN x.Store = @DemoStore THEN 'CHECK - should NOT be on the demo store'
            ELSE 'OK - client store, will refuse to run while the demo token is selected' END AS [Check]
  FROM (SELECT c.Id, c.Name, CASE WHEN ISJSON(c.Data) = 1 THEN 'Plain' ELSE 'Encrypted' END AS Fmt,
               CASE WHEN ISJSON(c.Data) = 1 AND JSON_VALUE(c.Data, '$.BaseUrl') LIKE 'https://%/%'
                    THEN SUBSTRING(JSON_VALUE(c.Data, '$.BaseUrl'), 9, CHARINDEX('/', JSON_VALUE(c.Data, '$.BaseUrl'), 9) - 9) END AS Store
          FROM dbo.Connectors c
          JOIN dbo.ConnectorTypes t ON t.Id = c.TypeId
         WHERE t.Name = 'Shopify' AND c.Name LIKE 'Shopify - %') x
 ORDER BY x.Id;

/*--- 4. Routes of the customer: only the inventory upload should be run --------------*/
SELECT '4. Routes' AS Grid, r.Id, r.Name, r.Status,
       CASE WHEN r.Name LIKE '%Upload Warehouse-Wise Inventory' THEN 'the route to Test Run'
            WHEN r.Status = 'Active' THEN 'CHECK - Active: keep every other Shopify route In-Active during this test'
            ELSE 'OK - not active' END AS [Check]
  FROM dbo.Routes r
 WHERE r.CustomerName = @ErpCustomerID OR r.Name LIKE 'Shopify - %'
 ORDER BY r.Id;

/*--- 5. Inventory feed rows of the customer ------------------------------------------
      Before the route: the two test items NEW.  After the route: SYNCED.              */
SELECT '5. Feed rows' AS Grid, F.ItemId, F.CustomerItemCode AS InventoryItemId, F.Total_ATS, F.ATS_L55, F.ATS_L56,
       F.[Status], F.ModifiedDate,
       CASE WHEN F.ItemId IN ('ESM-SB-ICE', 'ESM-SB-DAWN') THEN 'test item' ELSE 'CHECK - not a test item' END AS [Check]
  FROM dbo.SCSInventoryFeed F
 WHERE F.CustomerID = @ErpCustomerID
 ORDER BY F.ItemId;

/*--- 6. Exactly what the upload route will read ---------------------------------------*/
EXEC dbo.Sp_SCS_GetProductCatalog @p_CustomerID = @ErpCustomerID, @p_RouteTypeID = 'ShopifyInventoryUpload', @p_UserNo = 1;

/*--- 7. Backup tables of the test (present = the test is applied) ---------------------*/
SELECT '7. Backup' AS Grid,
       CASE WHEN OBJECT_ID('dbo.ZDB_CT01_ShipNodes', 'U')  IS NOT NULL THEN 'present' ELSE 'none' END AS ShipNodesBackup,
       CASE WHEN OBJECT_ID('dbo.ZDB_CT01_Connectors', 'U') IS NOT NULL THEN 'present' ELSE 'none' END AS ConnectorBackup;
GO
