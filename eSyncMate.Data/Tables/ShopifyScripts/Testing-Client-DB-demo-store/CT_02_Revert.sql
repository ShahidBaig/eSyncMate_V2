/*==============================================================================
  CT 02 REVERT - Shopify (Task 00006): undo CT_02 at the client (Get Orders back on the client store)

  WHERE: the CLIENT database. Run it when the Get Orders test against our demo store is finished, and in any case
         BEFORE "Shopify - Place Orders in ERP" is activated.

  WHAT IT DOES (one transaction):
    1. The connector "Shopify - Get Orders" -> its Data as saved in ZDB_CT02_Connectors (BaseUrl back to walker-edison)
    2. The TEST ORDERS are deleted: orders of WKE1000 with an Id above the baseline CT_02 recorded
       (Orders + their OrderDetail + OrderData). Orders that existed before the test are not touched.
    3. The backup tables are dropped (only after 1 - 2 succeeded)
  NOT CHANGED: routes, Flows, Status, route logs, the token selection, anything in our demo store.

  IT STOPS, changing nothing, when:
    - the backup tables are missing (CT_02 not applied or already reverted)
    - a test order was already processed further (Status other than New / ERROR, or an ERP id on it): that order may
      exist in the ERP. The orders are listed - send me the grid. (@Force = 1 deletes them anyway.)
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

/*==============================================================================
  CONFIG
==============================================================================*/
DECLARE @Force BIT = 0;     -- 1 = delete the test orders even when one was processed further (after checking the ERP)
/*==============================================================================*/

BEGIN TRY
    IF OBJECT_ID('dbo.ZDB_CT02_Connectors', 'U') IS NULL OR OBJECT_ID('dbo.ZDB_CT02_Baseline', 'U') IS NULL
        THROW 51750, 'Backup tables ZDB_CT02_Connectors / ZDB_CT02_Baseline not found: CT_02 is not applied or already reverted. Nothing changed.', 1;

    DECLARE @CustomerId INT, @MaxOrderId INT;
    SELECT @CustomerId = CustomerId, @MaxOrderId = MaxOrderId FROM dbo.ZDB_CT02_Baseline;

    IF @CustomerId IS NULL OR @MaxOrderId IS NULL
        THROW 51751, 'ZDB_CT02_Baseline is empty. Nothing changed.', 1;

    DECLARE @Test TABLE (Id INT PRIMARY KEY);
    INSERT INTO @Test (Id)
    SELECT O.Id FROM dbo.Orders O WHERE O.CustomerId = @CustomerId AND O.Id > @MaxOrderId;

    IF @Force = 0 AND EXISTS (SELECT 1 FROM dbo.Orders O JOIN @Test T ON T.Id = O.Id
                               WHERE O.Status NOT IN ('New', 'ERROR') OR ISNULL(O.ExternalId, '') <> '')
    BEGIN
        SELECT 'STOPPED - test orders already processed further' AS Grid, O.Id, O.OrderNumber, O.CustomerOrderNo, O.Status, O.ExternalId
          FROM dbo.Orders O JOIN @Test T ON T.Id = O.Id
         WHERE O.Status NOT IN ('New', 'ERROR') OR ISNULL(O.ExternalId, '') <> '';
        THROW 51752, 'A test order was processed further (see the grid). Nothing changed - send me the grid.', 1;
    END

    BEGIN TRAN;

    /*--- 1. Connector back -----------------------------------------------------*/
    UPDATE C
       SET C.Data = B.Data, C.ModifiedDate = GETDATE(), C.ModifiedBy = 1
      FROM dbo.Connectors C
      JOIN dbo.ZDB_CT02_Connectors B ON B.Id = C.Id;

    DECLARE @n INT = @@ROWCOUNT;
    PRINT CONCAT('Connectors restored: ', @n);

    /*--- 2. Test orders out ----------------------------------------------------*/
    DELETE D FROM dbo.OrderDetail D JOIN @Test T ON T.Id = D.OrderId;
    SET @n = @@ROWCOUNT;
    PRINT CONCAT('Test order lines deleted: ', @n);

    DELETE X FROM dbo.OrderData X JOIN @Test T ON T.Id = X.OrderId;
    SET @n = @@ROWCOUNT;
    PRINT CONCAT('Test order data rows deleted: ', @n);

    DELETE O FROM dbo.Orders O JOIN @Test T ON T.Id = O.Id;
    SET @n = @@ROWCOUNT;
    PRINT CONCAT('Test orders deleted: ', @n);

    /*--- 3. Backup tables ------------------------------------------------------*/
    DROP TABLE dbo.ZDB_CT02_Connectors;
    DROP TABLE dbo.ZDB_CT02_Baseline;

    COMMIT TRAN;

    PRINT 'CT 02 reverted: Get Orders is back on the client store.';

    /*--- Result ----------------------------------------------------------------*/
    SELECT '1. Shopify connectors' AS Grid, c.Id, c.Name,
           CASE WHEN ISJSON(c.Data) = 1 THEN JSON_VALUE(c.Data, '$.BaseUrl') ELSE 'encrypted' END AS BaseUrl
      FROM dbo.Connectors c
      JOIN dbo.ConnectorTypes t ON t.Id = c.TypeId
     WHERE t.Name = 'Shopify' AND c.Name LIKE 'Shopify - %'
     ORDER BY c.Id;

    SELECT '2. Orders left for the customer' AS Grid, COUNT(*) AS Orders_ FROM dbo.Orders WHERE CustomerId = @CustomerId;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR (nothing changed): ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
