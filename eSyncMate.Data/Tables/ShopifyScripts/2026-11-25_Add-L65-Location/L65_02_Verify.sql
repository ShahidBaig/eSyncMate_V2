/*==============================================================================
  L65 02 - Shopify (Task 00006), customer WKE1000: verify the L65 mapping and the test item (READ ONLY)

  Run it after L65_01 (@Apply = 1), and again after an upload run to see the status of the test item.
  It changes nothing.
==============================================================================*/

SET NOCOUNT ON;

DECLARE @ErpCustomerID VARCHAR(50)   = 'WKE1000';
DECLARE @LocationGid   NVARCHAR(500) = 'gid://shopify/Location/82231951426';   -- L65 - Yaraghi Patterson
DECLARE @TestItem      VARCHAR(100)  = 'ALSD1EIV';

SELECT 'Mapping' AS Grid, S.ID, S.WHSID, S.ShipNode,
       CASE WHEN S.WHSID = 'L65' AND S.ShipNode = @LocationGid THEN 'OK - the new row'
            WHEN S.WHSID = 'L65' THEN 'WRONG location'
            ELSE 'unchanged row' END AS [Check]
  FROM dbo.TargetPlusShipNodes S
 WHERE S.CustomerID = @ErpCustomerID
 ORDER BY S.WHSID;

SELECT 'Backup' AS Grid,
       CASE WHEN OBJECT_ID('dbo.ZDB_L65_ShipNodes', 'U') IS NOT NULL THEN 'dbo.ZDB_L65_ShipNodes exists (L65_99 can revert)'
            ELSE 'no backup table' END AS [State];

SELECT 'Test item in the feed' AS Grid, INV.ItemId, INV.CustomerItemCode, INV.[Status],
       INV.Total_ATS, INV.ATS_L55, INV.ATS_L56, INV.ATS_L65, PR.ProductId AS ShopifyInventoryItemId,
       CASE WHEN PR.ProductId LIKE 'gid://shopify/InventoryItem/%' THEN 'has its Shopify id'
            ELSE 'NO Shopify id - run Shopify - Sync Products first' END AS [Check]
  FROM dbo.SCSInventoryFeed INV WITH (NOLOCK)
  LEFT JOIN dbo.CustomerProductCatalogPrices PR WITH (NOLOCK) ON PR.CustomerID = INV.CustomerID AND PR.ItemId = INV.ItemId
 WHERE INV.CustomerID = @ErpCustomerID AND (INV.ItemId = @TestItem OR INV.CustomerItemCode = @TestItem);

SELECT 'Feed L65 totals' AS Grid, COUNT(*) AS Items,
       SUM(CASE WHEN ISNULL(INV.ATS_L65, 0) > 0 THEN 1 ELSE 0 END) AS ItemsWithStockAtL65,
       SUM(ISNULL(INV.ATS_L65, 0)) AS UnitsAtL65
  FROM dbo.SCSInventoryFeed INV WITH (NOLOCK)
 WHERE INV.CustomerID = @ErpCustomerID;

SELECT 'Feed status' AS Grid, INV.[Status], COUNT(*) AS Items
  FROM dbo.SCSInventoryFeed INV WITH (NOLOCK)
 WHERE INV.CustomerID = @ErpCustomerID
 GROUP BY INV.[Status]
 ORDER BY INV.[Status];
GO
