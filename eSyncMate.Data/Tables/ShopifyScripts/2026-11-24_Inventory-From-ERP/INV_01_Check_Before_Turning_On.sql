/*==============================================================================
  INV 01 - Shopify (Task 00006), customer WKE1000: check BEFORE the ERP inventory feed is turned on

  READ-ONLY: only SELECTs. No token, secret or connection string is shown.
  Run it on the client database before the first Test Run of "Shopify - Fetch Full Inventory from ERP".
  Every [Check] must say OK or NOTE. A line that says CHECK is explained in the README of this folder.
==============================================================================*/

SET NOCOUNT ON;

DECLARE @ErpCustomerID VARCHAR(50) = 'WKE1000';

/*--- 1. The customer ------------------------------------------------------------------*/
SELECT '1. Customer' AS Grid, C.Id, C.ERPCustomerID, C.Name, C.Marketplace, C.IntegrationTokenId, T.Name AS SelectedToken,
       'NOTE - the token is not needed for the ERP feed, only for the upload to Shopify (next step)' AS [Check]
  FROM dbo.Customers C
  LEFT JOIN dbo.IntegrationTokens T ON T.Id = C.IntegrationTokenId
 WHERE C.ERPCustomerID = @ErpCustomerID;

/*--- 2. Routes of the customer --------------------------------------------------------
      Step A = Test Run the Full feed once. The Differential feed is turned on after that worked.
      The upload to Shopify stays In-Active in this step.                                        */
SELECT '2. Routes' AS Grid, R.Id, R.Name, R.TypeId, R.Status, R.FrequencyType, R.RepeatCount, R.ExecutionTime, R.JobID,
       CASE WHEN R.TypeId = 7 THEN 'STEP A - Test Run this route first'
            WHEN R.TypeId = 8 AND R.Status = 'Active' THEN 'CHECK - Differential is already Active: fine only when the Full feed has run'
            WHEN R.TypeId = 8 THEN 'OK - turn on after the Full feed worked'
            WHEN R.Name LIKE '%Upload Warehouse-Wise Inventory' AND R.Status = 'Active' THEN 'CHECK - upload to Shopify is Active: it will send the fetched stock to the store'
            WHEN R.Name LIKE '%Upload Warehouse-Wise Inventory' THEN 'OK - In-Active, next step'
            WHEN R.Status = 'Active' THEN 'CHECK - Active, not part of this step'
            ELSE 'OK - not part of this step' END AS [Check]
  FROM dbo.Routes R
 WHERE R.CustomerName = @ErpCustomerID OR R.Name LIKE 'Shopify - %'
 ORDER BY R.Id;

/*--- 3. Connectors of the two feed routes ---------------------------------------------
      SPARS connectors: Method GET, CustomerID = the customer, BaseUrl / key / secret filled.
      SQL connector:    Command = EXEC SP_InsertSCSInventoryFeed ..., CustomerID = the customer.  */
SELECT '3. Connectors' AS Grid, x.RouteId, x.UsedAs, x.Id AS ConnectorId, x.Name, x.Storage, x.ConnType, x.UrlOrCommand, x.Method, x.CustomerID,
       CASE WHEN x.Storage = 'Encrypted' THEN 'NOTE - encrypted, SQL cannot read it: look at it in Setup > Connectors'
            WHEN ISNULL(x.CustomerID, '') <> @ErpCustomerID THEN 'CHECK - CustomerID is not ' + @ErpCustomerID
            WHEN x.ConnType = 'Rest' AND UPPER(ISNULL(x.Method, '')) <> 'GET' THEN 'CHECK - Method must be GET (SPARS answers POST with 405)'
            WHEN x.ConnType = 'Rest' AND x.Filled = 0 THEN 'CHECK - SPARS BaseUrl / key / secret is blank'
            WHEN x.ConnType = 'SqlServer' AND x.UrlOrCommand NOT LIKE '%SP_InsertSCSInventoryFeed%' THEN 'CHECK - Command is not SP_InsertSCSInventoryFeed'
            WHEN x.ConnType = 'SqlServer' AND x.Filled = 0 THEN 'CHECK - ConnectionString is blank'
            ELSE 'OK' END AS [Check]
  FROM (SELECT R.Id AS RouteId, CASE WHEN c.Id = R.SourceConnectorId THEN 'Source' ELSE 'Destination' END AS UsedAs,
               c.Id, c.Name, CASE WHEN ISJSON(c.Data) = 1 THEN 'Plain' ELSE 'Encrypted' END AS Storage,
               CASE WHEN ISJSON(c.Data) = 1 THEN JSON_VALUE(c.Data, '$.ConnectivityType') END AS ConnType,
               CASE WHEN ISJSON(c.Data) = 1 THEN ISNULL(JSON_VALUE(c.Data, '$.Command'), JSON_VALUE(c.Data, '$.Url')) END AS UrlOrCommand,
               CASE WHEN ISJSON(c.Data) = 1 THEN JSON_VALUE(c.Data, '$.Method') END AS Method,
               CASE WHEN ISJSON(c.Data) = 1 THEN JSON_VALUE(c.Data, '$.CustomerID') END AS CustomerID,
               CASE WHEN ISJSON(c.Data) = 0 THEN NULL
                    WHEN JSON_VALUE(c.Data, '$.ConnectivityType') = 'Rest'
                         THEN CASE WHEN ISNULL(JSON_VALUE(c.Data, '$.BaseUrl'), '') = '' OR ISNULL(JSON_VALUE(c.Data, '$.ConsumerKey'), '') = ''
                                     OR ISNULL(JSON_VALUE(c.Data, '$.ConsumerSecret'), '') = '' THEN 0 ELSE 1 END
                    ELSE CASE WHEN ISNULL(JSON_VALUE(c.Data, '$.ConnectionString'), '') = '' THEN 0 ELSE 1 END END AS Filled
          FROM dbo.Routes R
          JOIN dbo.Connectors c ON c.Id IN (R.SourceConnectorId, R.DestinationConnectorId)
         WHERE R.CustomerName = @ErpCustomerID AND R.TypeId IN (7, 8)) x
 ORDER BY x.RouteId, x.UsedAs DESC;

/*--- 4. Rows left in the temp table by an earlier run ---------------------------------
      Must be 0. Rows here make the next run start at page [MaxCurrentPage + 1] instead of page 1
      (fix A in INV_03).                                                                         */
SELECT '4. Temp rows' AS Grid, COUNT(*) AS LeftoverRows, MAX(CurrentPage) AS MaxCurrentPage, MAX(TotalPages) AS TotalPages,
       CASE WHEN COUNT(*) = 0 THEN 'OK - empty' ELSE 'CHECK - leftover rows, see fix A in INV_03' END AS [Check]
  FROM dbo.Temp_SCSInventoryFeed
 WHERE CustomerID = @ErpCustomerID;

/*--- 5. Inventory rows the customer has now (before the first run: none) --------------*/
SELECT '5. Feed rows now' AS Grid, ISNULL(F.[Status], '(all)') AS [Status], COUNT(*) AS Items,
       MIN(F.CreatedDate) AS FirstCreated, MAX(ISNULL(F.ModifiedDate, F.CreatedDate)) AS LastChanged
  FROM dbo.SCSInventoryFeed F
 WHERE F.CustomerID = @ErpCustomerID
 GROUP BY ROLLUP (F.[Status]);

/*--- 6. Objects the feed writes to ----------------------------------------------------*/
SELECT '6. Objects' AS Grid, o.Name,
       CASE WHEN OBJECT_ID('dbo.' + o.Name) IS NOT NULL THEN 'OK - present'
            WHEN o.Needed = 1 THEN 'CHECK - MISSING'
            ELSE 'NOTE - missing: the feed still runs, only this history is not written' END AS [Check]
  FROM (VALUES ('SP_InsertSCSInventoryFeed', 1), ('Temp_SCSInventoryFeed', 1), ('SCSInventoryFeed', 1), ('InventoryBatchWise', 1),
               ('SCSInventoryFeedData_' + @ErpCustomerID, 0), ('SCSInventoryFeed_' + @ErpCustomerID + '_Log', 0)) o (Name, Needed);

/*--- 7. A feed of this customer that is running right now (no row = none) -------------*/
IF OBJECT_ID('dbo.RouteExecutionLock', 'U') IS NOT NULL
    EXEC sp_executesql N'
        SELECT ''7. Running now'' AS Grid, L.RouteId, L.RouteTypeId, L.AcquiredAt, L.MachineName, L.TimeoutMinutes,
               ''NOTE - a run holds the lock: wait for it before a Test Run'' AS [Check]
          FROM dbo.RouteExecutionLock L
         WHERE L.CustomerID = @c AND L.IsActive = 1 AND L.RouteTypeId IN (7, 8)
           AND L.AcquiredAt >= DATEADD(MINUTE, -ISNULL(L.TimeoutMinutes, 60), GETDATE());', N'@c VARCHAR(50)', @c = @ErpCustomerID;

/*--- 8. Warehouse -> Shopify location mapping (information for the next step) ---------*/
SELECT '8. Mapping' AS Grid, S.WHSID, S.ShipNode
  FROM dbo.TargetPlusShipNodes S
 WHERE S.CustomerID = @ErpCustomerID
 ORDER BY S.WHSID;

/*--- 9. Earlier runs of the feed for this customer (history, may be empty) ------------*/
SELECT TOP 5 '9. Earlier batches' AS Grid, B.BatchID, B.RouteType, B.[Status], B.StartDate, B.FinishDate, B.PageCount
  FROM dbo.InventoryBatchWise B
 WHERE B.CustomerID = @ErpCustomerID AND B.RouteType IN ('SCSFullInventoryFeed', 'SCSDifferentialInventoryFeed')
 ORDER BY B.StartDate DESC;
GO
