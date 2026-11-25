/*==============================================================================
  PS 03 - Shopify (Task 00006), customer WKE1000: verify the Sync Products setup and its runs

  READ-ONLY: only SELECTs (grid 3 runs the read-only branch of Sp_SCS_GetProductCatalog).
  Run it after PS_02 (grids 1 - 4), and again after the Test Run of "Shopify - Sync Products" (grids 5 - 8).

  A good Test Run:
    grid 5  a line "Shopify items for [WKE1000]: N in P page(s). Missing in CustomerProductCatalogPrices: M ..."
            and a line "Inserted M item(s) into CustomerProductCatalogPrices", no ERROR / EXCEPTION line
    grid 6  ShopifyIds = M
    grid 7  the items of the feed that still have no id (not in the store, or another SKU there)
    grid 8  WESAS21704EO1 -> gid://shopify/InventoryItem/43748597956674
==============================================================================*/

SET NOCOUNT ON;

DECLARE @ErpCustomerID VARCHAR(50) = 'WKE1000';
DECLARE @Since         DATETIME    = DATEADD(HOUR, -24, GETDATE());

/*--- 1. Route type --------------------------------------------------------------------*/
SELECT '1. Route type' AS Grid, T.Id, T.Name, T.Description
  FROM dbo.RouteTypes T
 WHERE T.Id = 81;

/*--- 2. Branch in the procedure, and the backup of PS_02 -------------------------------*/
SELECT '2. SP branch' AS Grid,
       CASE WHEN CHARINDEX('''ShopifyProductSync''', OBJECT_DEFINITION(OBJECT_ID('dbo.Sp_SCS_GetProductCatalog'))) > 0 THEN 'present' ELSE 'MISSING' END AS ProductSyncBranch,
       CASE WHEN CHARINDEX('''ShopifyInventoryUpload''', OBJECT_DEFINITION(OBJECT_ID('dbo.Sp_SCS_GetProductCatalog'))) > 0 THEN 'present' ELSE 'MISSING' END AS UploadBranch,
       CASE WHEN OBJECT_ID('dbo.ZDB_PS02_SpBackup', 'U') IS NOT NULL THEN 'backup table present' ELSE 'no backup table' END AS [Backup];

/*--- 3. What the branch returns: two rows per item (SKU = ItemId, SKU = CustomerItemCode) */
DECLARE @Pairs TABLE (ItemId VARCHAR(250), SKU VARCHAR(500));

BEGIN TRY
    INSERT INTO @Pairs
    EXEC dbo.Sp_SCS_GetProductCatalog @p_CustomerID = @ErpCustomerID, @p_RouteTypeID = 'ShopifyProductSync', @p_UserNo = 1;
END TRY
BEGIN CATCH
    PRINT 'Grid 3: the branch could not be read: ' + ERROR_MESSAGE();
END CATCH

SELECT '3. Branch output' AS Grid, COUNT(*) AS [Rows], COUNT(DISTINCT ItemId) AS Items,
       (SELECT COUNT(*) FROM (SELECT SKU FROM @Pairs GROUP BY SKU HAVING COUNT(DISTINCT ItemId) > 1) d) AS SkusOnTwoItems_Skipped
  FROM @Pairs;

SELECT '3. Branch sample' AS Grid, ItemId, SKU FROM @Pairs WHERE ItemId = 'WESAS21704EO1';

/*--- 4. The route ----------------------------------------------------------------------*/
SELECT '4. Route' AS Grid, R.Id, R.Name, R.TypeId, R.Status, R.CustomerName, R.SourcePartyId, R.DestinationPartyId, R.PartyGroupId,
       sc.Name AS SourceConnector, dc.Name AS DestinationConnector, R.FrequencyType, R.RepeatCount, R.JobID,
       CASE WHEN EXISTS (SELECT 1 FROM dbo.VW_Routes v WHERE v.Id = R.Id) THEN 'yes' ELSE 'NO' END AS ShownOnRoutesScreen
  FROM dbo.Routes R
  LEFT JOIN dbo.Connectors sc ON sc.Id = R.SourceConnectorId
  LEFT JOIN dbo.Connectors dc ON dc.Id = R.DestinationConnectorId
 WHERE R.TypeId = 81;

/*--- 5. Route log of the sync route ----------------------------------------------------
      No line = the route did not run.                                                           */
SELECT '5. Route log' AS Grid, L.Id, L.RouteId,
       CASE L.[Type] WHEN 1 THEN 'Info' WHEN 2 THEN 'Warning' WHEN 3 THEN 'EXCEPTION' WHEN 4 THEN 'Debug' WHEN 5 THEN 'ERROR' WHEN 6 THEN 'RouteInfo' END AS LogType,
       L.CreatedDate, L.[Message], LEFT(L.Details, 3000) AS Details
  FROM dbo.RouteLog L
  JOIN dbo.Routes R ON R.Id = L.RouteId
 WHERE R.CustomerName = @ErpCustomerID AND R.TypeId = 81 AND L.CreatedDate >= @Since
 ORDER BY L.Id DESC;

/*--- 6. Ids stored for the customer ----------------------------------------------------*/
SELECT '6. Catalog ids' AS Grid, COUNT(*) AS [Rows],
       SUM(CASE WHEN PR.ProductId LIKE 'gid://shopify/InventoryItem/%' THEN 1 ELSE 0 END) AS ShopifyIds,
       (SELECT COUNT(*) FROM dbo.SCSInventoryFeed F WHERE F.CustomerID = @ErpCustomerID) AS FeedItems
  FROM dbo.CustomerProductCatalogPrices PR
 WHERE PR.CustomerID = @ErpCustomerID;

/*--- 7. Feed items that still have no Shopify id (the upload cannot send these) --------*/
SELECT '7. Feed items without id' AS Grid, F.ItemId, F.CustomerItemCode, F.Total_ATS, F.[Status]
  FROM dbo.SCSInventoryFeed F
 WHERE F.CustomerID = @ErpCustomerID
   AND NOT EXISTS (SELECT 1 FROM dbo.CustomerProductCatalogPrices PR
                    WHERE PR.CustomerID = F.CustomerID AND PR.ItemId = F.ItemId AND PR.ProductId LIKE 'gid://shopify/InventoryItem/%')
 ORDER BY F.Total_ATS DESC, F.ItemId;

/*--- 8. The test item -------------------------------------------------------------------*/
SELECT '8. Test item' AS Grid, F.ItemId, F.CustomerItemCode, PR.ProductId, F.Total_ATS, F.ATS_L55, F.ATS_L56, F.[Status],
       CASE WHEN PR.ProductId = 'gid://shopify/InventoryItem/43748597956674' THEN 'OK - the id seen in Shopify admin'
            WHEN PR.ProductId IS NULL THEN 'no id yet - run the sync route'
            ELSE 'CHECK - another id than seen in Shopify admin on 2026-11-24' END AS [Check]
  FROM dbo.SCSInventoryFeed F
  LEFT JOIN dbo.CustomerProductCatalogPrices PR ON PR.CustomerID = F.CustomerID AND PR.ItemId = F.ItemId
 WHERE F.CustomerID = @ErpCustomerID AND F.ItemId = 'WESAS21704EO1';
GO
