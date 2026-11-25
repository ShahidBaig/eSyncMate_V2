/*==============================================================================
  PO 99 - Shopify (Task 00006), customer WKE1000: revert of PO_01_Map_CustomerPO_Apply.sql

  Puts the map "Shopify Order Transformation" back exactly as PO_01 saved it in dbo.ZDB_PO01_Map
  (CustomerPO = the Shopify id again) and drops the backup.

  Note: an order that was already created in SPARS keeps the Customer PO it was created with.

  @Apply = 0 (default) DRY RUN.
  Stops (nothing changed): 51990 no backup    51991 a route that uses the map is Active
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Apply BIT = 0;   -- 0 = dry run, 1 = change

BEGIN TRY
    IF OBJECT_ID('dbo.ZDB_PO01_Map', 'U') IS NULL
        THROW 51990, 'No backup (dbo.ZDB_PO01_Map): PO_01 was not applied, or it is already reverted. Nothing changed.', 1;

    -- EXEC: the table may be missing when this batch is compiled
    EXEC ('SELECT ''Backup'' AS Grid, B.Id AS MapId, B.Name, B.BackupDate,
                  JSON_VALUE(B.Map, ''$.order.header.CustomerPO'') AS CustomerPO_InBackup,
                  JSON_VALUE(M.Map, ''$.order.header.CustomerPO'') AS CustomerPO_Now
             FROM dbo.ZDB_PO01_Map B
             JOIN dbo.Maps M ON M.Id = B.Id');

    IF @Apply = 0
    BEGIN
        PRINT 'DRY RUN: nothing changed. Set @Apply = 1 to put the map back.';
        RETURN;
    END

    DECLARE @Active INT = 0;

    EXEC sp_executesql N'SELECT @n = COUNT(*) FROM dbo.Routes R JOIN dbo.ZDB_PO01_Map B ON B.Id = R.MapId
                          WHERE R.Status = ''Active'' OR R.JobID IS NOT NULL', N'@n INT OUTPUT', @n = @Active OUTPUT;

    IF @Active > 0
        THROW 51991, 'A route that uses the map is Active. Set it In-Active on the Flows screen first. Nothing changed.', 1;

    BEGIN TRAN;

    EXEC ('UPDATE M SET M.Map = B.Map, M.ModifiedDate = GETDATE(), M.ModifiedBy = 1
             FROM dbo.Maps M
             JOIN dbo.ZDB_PO01_Map B ON B.Id = M.Id');

    DROP TABLE dbo.ZDB_PO01_Map;

    COMMIT TRAN;

    PRINT 'Map restored from the backup. Backup dropped.';

    SELECT 'Map after' AS Grid, M.Id, M.Name, JSON_VALUE(M.Map, '$.order.header.CustomerPO') AS CustomerPO
      FROM dbo.Maps M
     WHERE M.Name = 'Shopify Order Transformation';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR (nothing changed): ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
