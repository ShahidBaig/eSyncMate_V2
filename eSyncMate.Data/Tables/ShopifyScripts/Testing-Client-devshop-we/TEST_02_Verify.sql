/*==============================================================================
  TEST 02 VERIFY - Shopify (Task 00006): check the inventory test data in the DEV database

  READ-ONLY: only SELECTs (and the read-only source procedure of the upload route). Changes nothing.
  Every grid has a [Check] column: OK, NOTE or CHECK.
  Run it before / after TEST_02_Inventory_Test_Data.sql and after the upload route, and paste the grids.
==============================================================================*/

SET NOCOUNT ON;

DECLARE @ErpCustomerID VARCHAR(50) = 'WKE1000';

DECLARE @Expected TABLE (ItemId VARCHAR(50), ATS_L55 INT, ATS_L56 INT);
INSERT INTO @Expected VALUES
    ('ESM-TEST-001', 10, 5),
    ('ESM-TEST-002',  7, 0);

/*--- 1. Where am I ------------------------------------------------------------*/
SELECT '1. Database' AS Grid, @@SERVERNAME AS ServerName, DB_NAME() AS DatabaseName, GETDATE() AS ServerTime,
       CASE WHEN DB_NAME() = 'eSyncmate_Dev' THEN 'OK' ELSE 'CHECK - not the dev database' END AS [Check];

/*--- 2. The two test products in the feed table --------------------------------*/
SELECT '2. Test rows' AS Grid, E.ItemId,
       F.CustomerItemCode AS InventoryItemId, F.Total_ATS, F.ATS_L55, F.ATS_L56, F.[Status], F.ModifiedDate,
       CASE WHEN F.ItemId IS NULL THEN 'CHECK - row missing: TEST 02 has not written it yet'
            WHEN ISNULL(F.CustomerItemCode, '') = '' OR F.CustomerItemCode LIKE '%[^0-9]%'
                 THEN 'CHECK - CustomerItemCode is not a Shopify inventory item number'
            WHEN ISNULL(F.ATS_L55, -1) <> E.ATS_L55 OR ISNULL(F.ATS_L56, -1) <> E.ATS_L56
                 THEN 'NOTE - quantities differ from the script defaults (fine if you changed them)'
            WHEN F.[Status] IN ('NEW', 'UPDATED') THEN 'OK - waiting for the upload route'
            WHEN F.[Status] = 'SYNCED' THEN 'OK - sent to Shopify'
            WHEN F.[Status] = 'ERROR'  THEN 'CHECK - upload failed, see grid 6'
            ELSE 'CHECK - status' END AS [Check]
  FROM @Expected E
  LEFT JOIN dbo.SCSInventoryFeed F ON F.CustomerID = @ErpCustomerID AND F.ItemId = E.ItemId
 ORDER BY E.ItemId;

/*--- 3. Warehouse -> location the quantities go to -----------------------------*/
SELECT '3. Locations used' AS Grid, S.WHSID, S.ShipNode,
       CASE WHEN S.WHSID IN ('L55', 'L56') THEN 'OK' ELSE 'CHECK - not a devshop-we location' END AS [Check]
  FROM dbo.TargetPlusShipNodes S
 WHERE S.CustomerID = @ErpCustomerID
 ORDER BY S.WHSID;

/*--- 4. Every inventory row of the customer ------------------------------------*/
SELECT '4. All rows of the customer' AS Grid, F.ItemId, F.CustomerItemCode, F.Total_ATS, F.ATS_L55, F.ATS_L56, F.[Status], F.ModifiedDate,
       CASE WHEN F.ItemId IN (SELECT ItemId FROM @Expected) THEN 'test product (devshop-we)'
            WHEN F.[Status] IN ('NEW', 'UPDATED') THEN 'CHECK - old demo store row that WOULD be sent to devshop-we'
            ELSE 'old demo store row - not sent' END AS [Check]
  FROM dbo.SCSInventoryFeed F
 WHERE F.CustomerID = @ErpCustomerID
 ORDER BY CASE WHEN F.ItemId IN (SELECT ItemId FROM @Expected) THEN 0 ELSE 1 END, F.ItemId;

/*--- 5. Catalog ids: a ProductID here wins over CustomerItemCode in the route ---*/
SELECT '5. Catalog (CustomerProductCatalogPrices)' AS Grid, E.ItemId, PR.id, PR.ProductId,
       CASE WHEN PR.ItemId IS NULL THEN 'OK - no catalog row, the route uses CustomerItemCode'
            ELSE 'NOTE - catalog row exists, the route uses ITS ProductId' END AS [Check]
  FROM @Expected E
  LEFT JOIN dbo.CustomerProductCatalogPrices PR ON PR.CustomerID = @ErpCustomerID AND PR.ItemId = E.ItemId
 ORDER BY E.ItemId;

/*--- 6. Last sent / received data of the test products (after the upload) ------*/
IF OBJECT_ID('dbo.SCSInventoryFeedData_' + @ErpCustomerID) IS NOT NULL
BEGIN
    DECLARE @Sql NVARCHAR(MAX) = N'
        SELECT TOP 12 ''6. Sent / received'' AS Grid, D.Id, D.ItemId, D.[Type], D.CreatedDate, LEFT(D.[Data], 400) AS DataStart
          FROM dbo.' + QUOTENAME('SCSInventoryFeedData_' + @ErpCustomerID) + N' D WITH (NOLOCK)
         WHERE D.ItemId LIKE ''ESM-TEST-%''
         ORDER BY D.Id DESC;';
    EXEC sp_executesql @Sql;
END
ELSE
    SELECT '6. Sent / received' AS Grid, 'NOTE - table SCSInventoryFeedData_' + @ErpCustomerID + ' not found' AS [Check];

/*--- 7. Exactly what the upload route reads now (rows with Status NEW / UPDATED) -
      No rows = nothing is waiting to be sent.                                    */
EXEC dbo.Sp_SCS_GetProductCatalog @p_CustomerID = @ErpCustomerID, @p_RouteTypeID = 'ShopifyInventoryUpload', @p_UserNo = 1;
GO
