/*==============================================================================
  PO 01 - Shopify (Task 00006), customer WKE1000: SPARS gets the Shopify order number as Customer PO

  Why: the client knows an order by its Shopify number (e.g. 175205). The map sent the long internal Shopify id
  (7490956558402) as CustomerPO, so the order could not be found in SPARS under the number the client uses.

  What it changes: ONE value in the map "Shopify Order Transformation":
      order.header.CustomerPO   from   the Shopify id ($.legacyResourceId)
                                to     the order name without its leading "#"  ($.name: "#175205" -> "175205")
  Not changed: order.header.ExternalID (stays the Shopify id), every other field of the map, eSyncMate's own
  order number (Orders.OrderNumber stays the Shopify id - the Shopify routes need it), routes, connectors.

  The new expression was tested with the map engine of the repo (JUST.net) on 2026-11-25:
      "#1001" -> "1001",  "#175205" -> "175205",  "#175205-A" -> "175205-A".
  It always drops the first character, so it relies on the store's order names starting with "#".

  @Apply = 0 (default) DRY RUN. With @Apply = 1 the map as it is now is saved in dbo.ZDB_PO01_Map first
  (PO_99 restores from it).

  Stops (nothing changed):
    51980 map not found once / not valid JSON           51982 the backup exists already (run PO_99 first)
    51981 CustomerPO in the map is not what is expected  51983 the map is used by a route of another customer
    51984 the Place Orders route is Active
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ErpCustomerID VARCHAR(50)   = 'WKE1000';
DECLARE @Apply         BIT           = 0;   -- 0 = dry run, 1 = change
DECLARE @MapName       NVARCHAR(200) = 'Shopify Order Transformation';

DECLARE @OldExpr NVARCHAR(400) = N'#valueof($.legacyResourceId)';
DECLARE @NewExpr NVARCHAR(400) = N'#substring(#valueof($.name),1,#subtract(#length(#valueof($.name)),1))';

BEGIN TRY
    DECLARE @MapId INT, @Count INT, @Map NVARCHAR(MAX);

    SELECT @Count = COUNT(*), @MapId = MAX(Id) FROM dbo.Maps WHERE Name = @MapName;

    IF @Count <> 1
        THROW 51980, 'Map "Shopify Order Transformation" not found exactly once. Nothing changed.', 1;

    SELECT @Map = Map FROM dbo.Maps WHERE Id = @MapId;

    IF ISJSON(@Map) <> 1
        THROW 51980, 'The map is not valid JSON. Nothing changed.', 1;

    DECLARE @Current NVARCHAR(400) = JSON_VALUE(@Map, '$.order.header.CustomerPO');

    SELECT 'Map now' AS Grid, @MapId AS MapId, @MapName AS Name,
           @Current AS CustomerPO_Now, JSON_VALUE(@Map, '$.order.header.ExternalID') AS ExternalID_Now,
           CASE WHEN @Current = @NewExpr THEN 'already changed - nothing to do'
                WHEN @Current = @OldExpr THEN 'will be changed to the order name without #'
                ELSE 'UNEXPECTED value' END AS [Action];

    SELECT 'Routes using the map' AS Grid, R.Id, R.Name, R.CustomerName, R.Status, R.JobID
      FROM dbo.Routes R
     WHERE R.MapId = @MapId;

    -- what the orders waiting for the ERP would get as CustomerPO
    SELECT 'Orders waiting' AS Grid, O.Id, O.OrderNumber AS ShopifyId_SentUntilNow, O.CustomerOrderNo,
           STUFF(JSON_VALUE(D.Data, '$.name'), 1, 1, '') AS CustomerPO_AfterTheChange, O.Status
      FROM dbo.Orders O
      JOIN dbo.Customers C ON C.Id = O.CustomerId
     CROSS APPLY (SELECT TOP 1 X.Data FROM dbo.OrderData X WHERE X.OrderId = O.Id AND X.Type = 'API-JSON' ORDER BY X.Id DESC) D
     WHERE C.ERPCustomerID = @ErpCustomerID AND O.Status IN ('New', 'InProgress');

    IF @Current = @NewExpr
    BEGIN
        PRINT 'The map already sends the order name. Nothing to do.';
        RETURN;
    END

    IF @Apply = 0
    BEGIN
        PRINT 'DRY RUN: nothing changed. Set @Apply = 1 to change the map.';
        RETURN;
    END

    IF @Current IS NULL OR @Current <> @OldExpr
        THROW 51981, 'CustomerPO in the map is not "#valueof($.legacyResourceId)" (see grid "Map now"). Nothing changed - send me the grid.', 1;

    IF OBJECT_ID('dbo.ZDB_PO01_Map', 'U') IS NOT NULL
        THROW 51982, 'The backup dbo.ZDB_PO01_Map exists already. Run PO_99_Revert_Map.sql first. Nothing changed.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Routes R WHERE R.MapId = @MapId AND ISNULL(R.CustomerName, '') <> @ErpCustomerID)
        THROW 51983, 'The map is used by a route of another customer (see grid). Nothing changed.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Routes R WHERE R.MapId = @MapId AND (R.Status = 'Active' OR R.JobID IS NOT NULL))
        THROW 51984, 'A route that uses the map is Active. Set it In-Active on the Flows screen first. Nothing changed.', 1;

    BEGIN TRAN;

    SELECT M.Id, M.Name, M.Map, M.ModifiedDate, M.ModifiedBy, GETDATE() AS BackupDate
      INTO dbo.ZDB_PO01_Map
      FROM dbo.Maps M
     WHERE M.Id = @MapId;

    UPDATE dbo.Maps
       SET Map = JSON_MODIFY(Map, '$.order.header.CustomerPO', @NewExpr), ModifiedDate = GETDATE(), ModifiedBy = 1
     WHERE Id = @MapId;

    -- only that one value may differ from the backup
    IF (SELECT JSON_VALUE(Map, '$.order.header.CustomerPO') FROM dbo.Maps WHERE Id = @MapId) <> @NewExpr
       OR (SELECT ISJSON(Map) FROM dbo.Maps WHERE Id = @MapId) <> 1
       OR (SELECT JSON_MODIFY(Map, '$.order.header.CustomerPO', @OldExpr) FROM dbo.Maps WHERE Id = @MapId)
          <> (SELECT JSON_MODIFY(Map, '$.order.header.CustomerPO', @OldExpr) FROM dbo.ZDB_PO01_Map)
    BEGIN
        ROLLBACK TRAN;
        THROW 51985, 'The changed map did not pass the check (more than CustomerPO would differ). Rolled back, nothing changed.', 1;
    END

    COMMIT TRAN;

    PRINT 'Map changed: CustomerPO = the Shopify order name without "#". Backup in dbo.ZDB_PO01_Map.';

    SELECT 'Map after' AS Grid, M.Id, JSON_VALUE(M.Map, '$.order.header.CustomerPO') AS CustomerPO,
           JSON_VALUE(M.Map, '$.order.header.ExternalID') AS ExternalID, M.ModifiedDate
      FROM dbo.Maps M
     WHERE M.Id = @MapId;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
