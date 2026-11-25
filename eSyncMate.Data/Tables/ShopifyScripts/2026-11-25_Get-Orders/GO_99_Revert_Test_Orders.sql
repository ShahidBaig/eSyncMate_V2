/*==============================================================================
  GO 99 - Shopify (Task 00006), customer WKE1000: take the test orders of the Get Orders run out again

  Deletes the orders of the customer with an Id above the baseline of GO_01 (Orders + their OrderDetail +
  OrderData) and drops the baseline table. Orders from before the baseline and orders of other customers are
  not touched. Nothing in Shopify is changed. Route log lines stay as history.

  Note: a deleted order is read again by the next Get Orders run when it still matches the filter.

  @Apply = 0 (default) DRY RUN: lists what would be deleted.

  Stops (nothing changed):
    51970 no baseline (GO_01 with @Apply = 1 was not run, or already reverted)
    51971 a test order was processed further (Status other than New / ERROR, or an ERP id on it): it may exist
          in the ERP. The orders are listed - send me the grid. (@Force = 1 deletes them anyway.)
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Apply BIT = 0;   -- 0 = dry run, 1 = delete
DECLARE @Force BIT = 0;   -- 1 = delete even when an order was processed further (after checking the ERP)

BEGIN TRY
    IF OBJECT_ID('dbo.ZDB_GO_Baseline', 'U') IS NULL
        THROW 51970, 'No baseline (dbo.ZDB_GO_Baseline): GO_01 with @Apply = 1 was not run, or it is already reverted. Nothing changed.', 1;

    DECLARE @CustomerId INT, @MaxOrderId INT;

    -- EXEC: the table may be missing when this batch is compiled
    EXEC sp_executesql N'SELECT @c = CustomerId, @m = MaxOrderId FROM dbo.ZDB_GO_Baseline',
                       N'@c INT OUTPUT, @m INT OUTPUT', @c = @CustomerId OUTPUT, @m = @MaxOrderId OUTPUT;

    IF @CustomerId IS NULL OR @MaxOrderId IS NULL
        THROW 51970, 'dbo.ZDB_GO_Baseline is empty. Nothing changed.', 1;

    DECLARE @Test TABLE (Id INT PRIMARY KEY);

    INSERT INTO @Test (Id)
    SELECT O.Id FROM dbo.Orders O WHERE O.CustomerId = @CustomerId AND O.Id > @MaxOrderId;

    SELECT 'Test orders' AS Grid, O.Id, O.OrderNumber, O.CustomerOrderNo, O.Status, O.ExternalId, O.CreatedDate,
           CASE WHEN O.Status NOT IN ('New', 'ERROR') OR ISNULL(O.ExternalId, '') <> '' THEN 'PROCESSED FURTHER - may exist in the ERP' ELSE 'will be deleted' END AS [Action]
      FROM dbo.Orders O
      JOIN @Test T ON T.Id = O.Id
     ORDER BY O.Id;

    IF @Apply = 0
    BEGIN
        PRINT 'DRY RUN: nothing changed. Set @Apply = 1 to delete the test orders and drop the baseline.';
        RETURN;
    END

    IF @Force = 0 AND EXISTS (SELECT 1 FROM dbo.Orders O JOIN @Test T ON T.Id = O.Id
                               WHERE O.Status NOT IN ('New', 'ERROR') OR ISNULL(O.ExternalId, '') <> '')
        THROW 51971, 'A test order was processed further (see the grid). Nothing changed - send me the grid.', 1;

    BEGIN TRAN;

    DECLARE @Lines INT, @Data INT, @Orders INT;

    DELETE D FROM dbo.OrderDetail D JOIN @Test T ON T.Id = D.OrderId;
    SET @Lines = @@ROWCOUNT;

    DELETE X FROM dbo.OrderData X JOIN @Test T ON T.Id = X.OrderId;
    SET @Data = @@ROWCOUNT;

    DELETE O FROM dbo.Orders O JOIN @Test T ON T.Id = O.Id;
    SET @Orders = @@ROWCOUNT;

    DROP TABLE dbo.ZDB_GO_Baseline;

    COMMIT TRAN;

    PRINT CONCAT('Reverted. Orders deleted: ', @Orders, ', order lines: ', @Lines, ', order data rows: ', @Data, '. Baseline dropped.');

    SELECT 'Orders left for the customer' AS Grid, COUNT(*) AS Orders_ FROM dbo.Orders WHERE CustomerId = @CustomerId;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR (nothing changed): ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
