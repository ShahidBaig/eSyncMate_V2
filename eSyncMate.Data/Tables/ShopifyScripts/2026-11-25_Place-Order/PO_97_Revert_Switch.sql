/*==============================================================================
  PO 97 - Shopify (Task 00006), customer WKE1000: revert of PO_03_Switch_Existing_Orders_To_PO_Number.sql

  Puts Orders.OrderNumber, Orders.CustomerOrderNo and OrderData.OrderNumber of the switched orders back to the
  values saved in dbo.ZDB_PO03_Orders, and drops the backup.

  An order that was sent to SPARS in the meantime (it has an ERP number) is NOT put back: SPARS knows it under
  the PO number. Such orders are listed.

  @Apply = 0 (default) DRY RUN.   Stops (nothing changed): 51998 no backup
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Apply BIT = 0;   -- 0 = dry run, 1 = change

BEGIN TRY
    IF OBJECT_ID('dbo.ZDB_PO03_Orders', 'U') IS NULL
        THROW 51998, 'No backup (dbo.ZDB_PO03_Orders): PO_03 was not applied, or it is already reverted. Nothing changed.', 1;

    -- EXEC: the table may be missing when this batch is compiled
    EXEC ('SELECT ''Backup'' AS Grid, B.Id, B.OldOrderNumber, B.OldCustomerOrderNo, O.OrderNumber AS OrderNumber_Now, O.Status, O.ExternalId,
                  CASE WHEN O.Id IS NULL THEN ''order no longer exists''
                       WHEN ISNULL(O.ExternalId, '''') <> '''' THEN ''LEFT ALONE - already in the ERP''
                       ELSE ''will be put back'' END AS [Action]
             FROM dbo.ZDB_PO03_Orders B
             LEFT JOIN dbo.Orders O ON O.Id = B.Id');

    IF @Apply = 0
    BEGIN
        PRINT 'DRY RUN: nothing changed. Set @Apply = 1 to put the orders back.';
        RETURN;
    END

    BEGIN TRAN;

    EXEC ('UPDATE D SET D.OrderNumber = B.OldOrderNumber
             FROM dbo.OrderData D
             JOIN dbo.ZDB_PO03_Orders B ON B.Id = D.OrderId
             JOIN dbo.Orders O ON O.Id = B.Id
            WHERE ISNULL(O.ExternalId, '''') = '''';

           UPDATE O SET O.OrderNumber = B.OldOrderNumber, O.CustomerOrderNo = B.OldCustomerOrderNo
             FROM dbo.Orders O
             JOIN dbo.ZDB_PO03_Orders B ON B.Id = O.Id
            WHERE ISNULL(O.ExternalId, '''') = '''';');

    DROP TABLE dbo.ZDB_PO03_Orders;

    COMMIT TRAN;

    PRINT 'Orders put back to the Release6 form. Backup dropped.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR (nothing changed): ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
