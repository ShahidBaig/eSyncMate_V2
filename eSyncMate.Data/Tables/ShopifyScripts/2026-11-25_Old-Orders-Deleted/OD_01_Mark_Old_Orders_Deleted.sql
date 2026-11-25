/*==============================================================================
  OD 01 - Shopify (Task 00006), customer WKE1000: mark the 74 older orders DELETED

  Why: on 2026-11-25 (12:45 server time) Get Orders ran once BEFORE the go-live cut-off was in the connector and
       saved 74 orders of the last 10 days; Place Orders sent them to SPARS. The user decided (2026-11-25) to mark
       them DELETED in eSyncMate. The two orders that came AFTER the cut-off (175273, 175274) stay as they are.

  What it changes: Orders.Status -> 'DELETED' for the orders of WKE1000 (CustomerId 15) saved between
  2026-11-25 12:40 and 12:50 (the one Get Orders run at 12:45), except the PO numbers in @Keep.
  Not changed: OrderDetail, OrderData, the orders' other columns, anything in SPARS or Shopify.

  Effect:
    - the orders disappear from the Orders screen and the dashboard (they filter Status <> 'DELETED');
    - Place Orders, the ASN routes and Fetch Cancellations from ERP no longer pick them;
    - Get Orders still counts them as "already in eSyncMate" (its check does not look at the status), so they are
      not fetched again.
  NOT done by this script: the sales orders eSyncMate created in SPARS stay in SPARS - the client cancels or keeps
  them there. Shopify fulfillment of these orders will not come from eSyncMate.

  @Apply = 0 (default) DRY RUN. With @Apply = 1 the rows as they are now are saved in dbo.ZDB_OD01_Orders first
  (OD_99 restores the old status from it).

  Stops (nothing changed):
    52030 the number of orders is not @ExpectedCount      52032 the backup exists already (run OD_99 first)
    52031 an order of @Keep would be changed               52033 Place Orders (161) is Active
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @CustomerId    INT          = 15;                       -- WKE1000
DECLARE @Apply         BIT          = 0;                        -- 0 = dry run, 1 = change
DECLARE @From          DATETIME     = '2026-11-25 12:40';       -- the Get Orders run of 12:45 (server time)
DECLARE @To            DATETIME     = '2026-11-25 12:50';
DECLARE @ExpectedCount INT          = 74;

DECLARE @Keep TABLE (OrderNumber VARCHAR(50));
INSERT INTO @Keep VALUES ('175273'), ('175274');                -- came after the cut-off: NOT changed

BEGIN TRY
    DECLARE @Target TABLE (Id INT PRIMARY KEY);

    INSERT INTO @Target (Id)
    SELECT O.Id
      FROM dbo.Orders O
     WHERE O.CustomerId = @CustomerId
       AND O.CreatedDate >= @From AND O.CreatedDate < @To
       AND O.OrderNumber NOT IN (SELECT OrderNumber FROM @Keep)
       AND ISNULL(O.Status, '') <> 'DELETED';

    DECLARE @n INT = (SELECT COUNT(*) FROM @Target);

    SELECT 'Will be DELETED' AS Grid, ROW_NUMBER() OVER (ORDER BY O.OrderNumber) AS [No],
           O.Id, O.OrderNumber AS [Shopify PO], O.ExternalId AS [SPARS Sales Order], O.Status AS [Status now], O.CreatedDate
      FROM dbo.Orders O
      JOIN @Target T ON T.Id = O.Id
     ORDER BY O.OrderNumber;

    SELECT 'Totals' AS Grid, @n AS OrdersToMark, @ExpectedCount AS Expected,
           (SELECT COUNT(*) FROM dbo.Orders O JOIN @Target T ON T.Id = O.Id WHERE O.Status = 'SYNCED') AS NowSynced;

    SELECT 'Kept (not changed)' AS Grid, O.Id, O.OrderNumber AS [Shopify PO], O.Status, O.CreatedDate
      FROM dbo.Orders O
     WHERE O.CustomerId = @CustomerId AND O.OrderNumber IN (SELECT OrderNumber FROM @Keep);

    IF @n <> @ExpectedCount
        THROW 52030, 'The number of orders to mark is not 74 (see grid "Totals"). Nothing changed - send me the grids.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Orders O JOIN @Target T ON T.Id = O.Id WHERE O.OrderNumber IN (SELECT OrderNumber FROM @Keep))
        THROW 52031, 'An order of the keep list would be changed. Nothing changed.', 1;

    IF @Apply = 0
    BEGIN
        PRINT 'DRY RUN: nothing changed. Set @Apply = 1 to mark these orders DELETED.';
        RETURN;
    END

    IF OBJECT_ID('dbo.ZDB_OD01_Orders', 'U') IS NOT NULL
        THROW 52032, 'The backup dbo.ZDB_OD01_Orders exists already. Run OD_99 first. Nothing changed.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Routes R WHERE R.CustomerName = 'WKE1000' AND R.TypeId = 9 AND (R.Status = 'Active' OR R.JobID IS NOT NULL))
        THROW 52033, 'Place Orders in ERP (route 161) is Active. Set it In-Active on the Flows screen first. Nothing changed.', 1;

    BEGIN TRAN;

    SELECT O.Id, O.OrderNumber, O.CustomerOrderNo, O.ExternalId, O.Status, GETDATE() AS BackupDate
      INTO dbo.ZDB_OD01_Orders
      FROM dbo.Orders O
      JOIN @Target T ON T.Id = O.Id;

    UPDATE O SET O.Status = 'DELETED'
      FROM dbo.Orders O
      JOIN @Target T ON T.Id = O.Id;

    IF @@ROWCOUNT <> @ExpectedCount
    BEGIN
        ROLLBACK TRAN;
        THROW 52034, 'The update did not change exactly 74 orders. Rolled back, nothing changed.', 1;
    END

    COMMIT TRAN;

    PRINT 'Done: 74 orders marked DELETED. Backup of the old status in dbo.ZDB_OD01_Orders.';

    SELECT 'After' AS Grid, O.Status, COUNT(*) AS Orders
      FROM dbo.Orders O
     WHERE O.CustomerId = @CustomerId AND O.CreatedDate >= @From
     GROUP BY O.Status;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
