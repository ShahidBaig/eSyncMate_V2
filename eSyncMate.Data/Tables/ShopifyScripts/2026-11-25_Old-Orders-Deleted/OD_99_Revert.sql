/*==============================================================================
  OD 99 - Shopify (Task 00006), customer WKE1000: give the 74 orders their old status back

  Restores Orders.Status from dbo.ZDB_OD01_Orders (written by OD_01) and drops the backup table.
  Only orders that are still DELETED are restored. @Apply = 0 (default) DRY RUN.

  Stops (nothing changed):
    52040 no backup table (OD_01 was not applied)     52041 Place Orders (161) is Active
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Apply BIT = 0;   -- 0 = dry run, 1 = change

BEGIN TRY
    IF OBJECT_ID('dbo.ZDB_OD01_Orders', 'U') IS NULL
        THROW 52040, 'No backup table dbo.ZDB_OD01_Orders: OD_01 was not applied. Nothing changed.', 1;

    EXEC sp_executesql N'
        SELECT ''Will be restored'' AS Grid, O.Id, O.OrderNumber AS [Shopify PO], O.Status AS [Status now], B.Status AS [Status before]
          FROM dbo.Orders O
          JOIN dbo.ZDB_OD01_Orders B ON B.Id = O.Id
         WHERE O.Status = ''DELETED''
         ORDER BY O.OrderNumber;';

    IF @Apply = 0
    BEGIN
        PRINT 'DRY RUN: nothing changed. Set @Apply = 1 to restore the old status.';
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.Routes R WHERE R.CustomerName = 'WKE1000' AND R.TypeId = 9 AND (R.Status = 'Active' OR R.JobID IS NOT NULL))
        THROW 52041, 'Place Orders in ERP (route 161) is Active. Set it In-Active on the Flows screen first. Nothing changed.', 1;

    BEGIN TRAN;

    EXEC sp_executesql N'
        UPDATE O SET O.Status = B.Status
          FROM dbo.Orders O
          JOIN dbo.ZDB_OD01_Orders B ON B.Id = O.Id
         WHERE O.Status = ''DELETED'';
        PRINT ''Orders restored: '' + CAST(@@ROWCOUNT AS VARCHAR(10));';

    DROP TABLE dbo.ZDB_OD01_Orders;

    COMMIT TRAN;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
