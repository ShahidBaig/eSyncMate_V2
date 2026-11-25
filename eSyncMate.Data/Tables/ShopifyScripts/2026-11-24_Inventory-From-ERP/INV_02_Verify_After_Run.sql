/*==============================================================================
  INV 02 - Shopify (Task 00006), customer WKE1000: did the ERP inventory arrive in eSyncMate?

  READ-ONLY: only SELECTs. No token, secret or connection string is shown.
  Run it on the client database AFTER a run of "Shopify - Fetch Full Inventory from ERP"
  (and later again after the Differential feed is on).

  What a good run looks like:
    grid 1  newest batch Completed
    grid 2  a line "... N page(s) with M item(s) received from SPARS", no Error / Exception line
    grid 3  M items in SCSInventoryFeed, Status NEW (first run) or UPDATED (later runs)
    grid 4  the same M items in the history tables of that batch
    grid 5  0 leftover temp rows
  Grids 6 - 9 are about the NEXT step (upload to Shopify): they show whether the rows can be sent.
==============================================================================*/

SET NOCOUNT ON;

DECLARE @ErpCustomerID VARCHAR(50) = 'WKE1000';
DECLARE @Since         DATETIME    = DATEADD(HOUR, -24, GETDATE());   -- route log lines from this time on (server time)

DECLARE @LastBatch NVARCHAR(500) =
       (SELECT TOP 1 B.BatchID FROM dbo.InventoryBatchWise B
         WHERE B.CustomerID = @ErpCustomerID AND B.RouteType IN ('SCSFullInventoryFeed', 'SCSDifferentialInventoryFeed')
         ORDER BY B.StartDate DESC);

/*--- 1. The runs (newest first) -------------------------------------------------------*/
SELECT TOP 10 '1. Batches' AS Grid, B.BatchID, B.RouteType, B.[Status], B.StartDate, B.FinishDate,
       DATEDIFF(SECOND, B.StartDate, B.FinishDate) AS Seconds, B.PageCount,
       CASE WHEN B.[Status] = 'Completed' AND ISNULL(B.PageCount, 0) > 0 THEN 'OK - pages received and saved'
            WHEN B.[Status] = 'Completed' THEN 'CHECK - Completed but no page with items: read grid 2'
            WHEN B.[Status] = 'Error' THEN 'CHECK - Error: read grid 2'
            WHEN B.[Status] = 'Processing' AND B.StartDate > DATEADD(MINUTE, -30, GETDATE()) THEN 'NOTE - still running, run this script again'
            ELSE 'CHECK - Processing for a long time: the run was cut off (fix B in INV_03)' END AS [Check]
  FROM dbo.InventoryBatchWise B
 WHERE B.CustomerID = @ErpCustomerID AND B.RouteType IN ('SCSFullInventoryFeed', 'SCSDifferentialInventoryFeed')
 ORDER BY B.StartDate DESC;

/*--- 2. Route log of the two feed routes ----------------------------------------------
      No line at all = the route did not run (Test Run not started / RouteWorker).               */
SELECT '2. Route log' AS Grid, L.Id, L.RouteId, R.Name AS Route,
       CASE L.[Type] WHEN 1 THEN 'Info' WHEN 2 THEN 'Warning' WHEN 3 THEN 'EXCEPTION' WHEN 4 THEN 'Debug' WHEN 5 THEN 'ERROR' WHEN 6 THEN 'RouteInfo' END AS LogType,
       L.CreatedDate, L.[Message], LEFT(L.Details, 1500) AS Details
  FROM dbo.RouteLog L
  JOIN dbo.Routes R ON R.Id = L.RouteId
 WHERE R.CustomerName = @ErpCustomerID AND R.TypeId IN (7, 8) AND L.CreatedDate >= @Since
 ORDER BY L.Id DESC;

/*--- 3. Inventory rows of the customer -------------------------------------------------*/
SELECT '3. Feed rows' AS Grid, ISNULL(F.[Status], '(all)') AS [Status], COUNT(*) AS Items, SUM(ISNULL(F.Total_ATS, 0)) AS Total_ATS,
       MIN(F.CreatedDate) AS FirstCreated, MAX(ISNULL(F.ModifiedDate, F.CreatedDate)) AS LastChanged
  FROM dbo.SCSInventoryFeed F
 WHERE F.CustomerID = @ErpCustomerID
 GROUP BY ROLLUP (F.[Status]);

/*--- 4. The newest batch in the history tables ----------------------------------------
      Both counts = the item count of the log line in grid 2.                                    */
DECLARE @Hist TABLE (Src VARCHAR(60), Rows_ INT, Items INT);

IF @LastBatch IS NOT NULL AND OBJECT_ID('dbo.SCSInventoryFeedData_' + @ErpCustomerID, 'U') IS NOT NULL
    INSERT INTO @Hist
    EXEC ('SELECT ''SCSInventoryFeedData (ERP-RVD)'', COUNT(*), COUNT(DISTINCT ItemId) FROM dbo.[SCSInventoryFeedData_' + @ErpCustomerID + ']
            WHERE [Type] = ''ERP-RVD'' AND BatchID = ''' + @LastBatch + '''');

IF @LastBatch IS NOT NULL AND OBJECT_ID('dbo.SCSInventoryFeed_' + @ErpCustomerID + '_Log', 'U') IS NOT NULL
    INSERT INTO @Hist
    EXEC ('SELECT ''SCSInventoryFeed_Log (DOWNLOAD)'', COUNT(*), COUNT(DISTINCT ItemId) FROM dbo.[SCSInventoryFeed_' + @ErpCustomerID + '_Log]
            WHERE BatchID = ''' + @LastBatch + '''');

SELECT '4. Newest batch' AS Grid, @LastBatch AS BatchID, H.Src, H.Rows_ AS [Rows], H.Items FROM @Hist H;

/*--- 5. Leftover temp rows: must be 0 after a finished run -----------------------------*/
SELECT '5. Temp rows' AS Grid, COUNT(*) AS LeftoverRows, MAX(CurrentPage) AS MaxCurrentPage, MAX(TotalPages) AS TotalPages,
       CASE WHEN COUNT(*) = 0 THEN 'OK - empty'
            ELSE 'CHECK - rows left: the run stopped before SP_InsertSCSInventoryFeed (fix A in INV_03), or it is still running' END AS [Check]
  FROM dbo.Temp_SCSInventoryFeed
 WHERE CustomerID = @ErpCustomerID;

/*--- 6. NEXT STEP: can the rows be sent to Shopify? -----------------------------------
      The upload needs the Shopify inventory item number of each item. It takes it from
      CustomerProductCatalogPrices.ProductId (gid://shopify/InventoryItem/..., filled by the
      "Shopify - Sync Products" route), otherwise from CustomerItemCode when that is the bare number.  */
SELECT '6. Item id for Shopify' AS Grid, q.Kind, COUNT(*) AS Items,
       MIN(q.ItemId + '  /  ' + ISNULL(NULLIF(LTRIM(RTRIM(q.CustomerItemCode)), ''), '(blank)')) AS Example_ItemId_CustomerItemCode
  FROM (SELECT F.ItemId, F.CustomerItemCode,
               CASE WHEN EXISTS (SELECT 1 FROM dbo.CustomerProductCatalogPrices PR
                                  WHERE PR.CustomerID = F.CustomerID AND PR.ItemId = F.ItemId AND PR.ProductId LIKE 'gid://shopify/InventoryItem/%')
                         THEN '1 OK - id in the product catalog'
                    WHEN LTRIM(RTRIM(ISNULL(F.CustomerItemCode, ''))) <> '' AND LTRIM(RTRIM(F.CustomerItemCode)) NOT LIKE '%[^0-9]%'
                         THEN '2 NOTE - CustomerItemCode is a number: sent as the inventory item number (wrong if it is a UPC / SKU)'
                    WHEN LTRIM(RTRIM(ISNULL(F.CustomerItemCode, ''))) = ''
                         THEN '3 CHECK - CustomerItemCode blank and no id in the catalog: cannot be sent yet'
                    ELSE '4 CHECK - CustomerItemCode is not a number and no id in the catalog: cannot be sent yet' END AS Kind
          FROM dbo.SCSInventoryFeed F
         WHERE F.CustomerID = @ErpCustomerID) q
 GROUP BY q.Kind
 ORDER BY q.Kind;

/*--- 7. NEXT STEP: stock per warehouse, and whether the warehouse is mapped -----------
      Only mapped warehouses are sent to Shopify. Stock in a warehouse that is not mapped stays in eSyncMate.  */
SELECT '7. Stock per warehouse' AS Grid, w.WHSID, SUM(w.Qty) AS TotalQty, SUM(CASE WHEN w.Qty > 0 THEN 1 ELSE 0 END) AS ItemsWithStock,
       SUM(CASE WHEN w.Qty < 0 THEN 1 ELSE 0 END) AS ItemsNegative, MAX(S.ShipNode) AS ShopifyLocation,
       CASE WHEN MAX(S.ShipNode) IS NOT NULL THEN 'mapped - will be sent'
            WHEN SUM(w.Qty) <> 0 THEN 'NOTE - has stock but is NOT mapped: not sent to Shopify'
            ELSE 'not mapped, no stock' END AS [Check]
  FROM dbo.SCSInventoryFeed F
 CROSS APPLY (VALUES ('L10', ISNULL(F.ATS_L10, 0)), ('L21', ISNULL(F.ATS_L21, 0)), ('L28', ISNULL(F.ATS_L28, 0)), ('L29', ISNULL(F.ATS_L29, 0)),
                     ('L30', ISNULL(F.ATS_L30, 0)), ('L34', ISNULL(F.ATS_L34, 0)), ('L35', ISNULL(F.ATS_L35, 0)), ('L36', ISNULL(F.ATS_L36, 0)),
                     ('L37', ISNULL(F.ATS_L37, 0)), ('L40', ISNULL(F.ATS_L40, 0)), ('L41', ISNULL(F.ATS_L41, 0)), ('L55', ISNULL(F.ATS_L55, 0)),
                     ('L56', ISNULL(F.ATS_L56, 0)), ('L57', ISNULL(F.ATS_L57, 0)), ('L60', ISNULL(F.ATS_L60, 0)), ('L65', ISNULL(F.ATS_L65, 0)),
                     ('L70', ISNULL(F.ATS_L70, 0)), ('L91', ISNULL(F.ATS_L91, 0))) w (WHSID, Qty)
  LEFT JOIN dbo.TargetPlusShipNodes S ON S.CustomerID = F.CustomerID AND S.WHSID = w.WHSID
 WHERE F.CustomerID = @ErpCustomerID
 GROUP BY w.WHSID
 ORDER BY w.WHSID;

/*--- 8. NEXT STEP: what is waiting for the upload, and is the upload off? --------------*/
SELECT '8. Waiting for upload' AS Grid,
       (SELECT COUNT(*) FROM dbo.SCSInventoryFeed F WHERE F.CustomerID = @ErpCustomerID AND F.[Status] IN ('NEW', 'UPDATED')) AS ItemsWaiting,
       R.Id AS UploadRouteId, R.Name AS UploadRoute, R.Status AS UploadRouteStatus,
       (SELECT T.Name FROM dbo.Customers C JOIN dbo.IntegrationTokens T ON T.Id = C.IntegrationTokenId WHERE C.ERPCustomerID = @ErpCustomerID) AS SelectedToken,
       CASE WHEN R.Status = 'Active' THEN 'CHECK - the upload is Active: these items go to the store on its next run'
            ELSE 'OK - upload In-Active: nothing is sent to Shopify until the next step' END AS [Check]
  FROM dbo.Routes R
 WHERE R.CustomerName = @ErpCustomerID AND R.Name LIKE '%Upload Warehouse-Wise Inventory';

/*--- 9. The rows themselves: 50 items with the most stock ------------------------------*/
SELECT TOP 50 '9. Sample' AS Grid, F.ItemId, F.CustomerItemCode, F.Total_ATS, F.ATS_L55, F.ATS_L56, F.ATS_L10, F.ATS_L21,
       F.ETA_Date, F.ETA_Qty, F.[Status], F.CreatedDate, F.ModifiedDate
  FROM dbo.SCSInventoryFeed F
 WHERE F.CustomerID = @ErpCustomerID
 ORDER BY F.Total_ATS DESC, F.ItemId;
GO
