/*==============================================================================
  INV 99 - Shopify (Task 00006), customer WKE1000: revert of INV_03_Fix_If_Needed.sql

  Puts back what INV_03 changed, from its backup, and drops the backup:
    ZDB_INV03_Temp     -> the removed rows go back into Temp_SCSInventoryFeed
    ZDB_INV03_Batches  -> the batches get their old Status / FinishDate back

  @RestoreTempRows = 0 (default): the temp rows are NOT put back, only the backup is dropped.
      Putting them back is rarely wanted: they are the half-read pages of a run that was cut off, and the next
      run would start in the middle again. Set it to 1 only when the rows are really needed.

  @Apply = 0 (default) DRY RUN. Stops when a feed of the customer started in the last 30 minutes.
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ErpCustomerID   VARCHAR(50) = 'WKE1000';
DECLARE @Apply           BIT = 0;   -- 0 = dry run, 1 = change
DECLARE @RestoreTempRows BIT = 0;   -- 1 = put the removed temp rows back

BEGIN TRY
    IF OBJECT_ID('dbo.ZDB_INV03_Temp', 'U') IS NULL AND OBJECT_ID('dbo.ZDB_INV03_Batches', 'U') IS NULL
    BEGIN
        PRINT 'No backup of INV_03 found: nothing to revert.';
        RETURN;
    END

    IF OBJECT_ID('dbo.ZDB_INV03_Temp', 'U') IS NOT NULL
        SELECT 'Backup: temp rows' AS Grid, COUNT(*) AS [Rows], MIN(CurrentPage) AS FromPage, MAX(CurrentPage) AS ToPage FROM dbo.ZDB_INV03_Temp;

    IF OBJECT_ID('dbo.ZDB_INV03_Batches', 'U') IS NOT NULL
        SELECT 'Backup: batches' AS Grid, Z.BatchID, Z.[Status] AS OldStatus, B.[Status] AS StatusNow, Z.BackupDate
          FROM dbo.ZDB_INV03_Batches Z
          LEFT JOIN dbo.InventoryBatchWise B ON B.BatchID = Z.BatchID;

    IF @Apply = 0
    BEGIN
        PRINT 'DRY RUN: nothing changed. Set @Apply = 1 to revert.';
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.InventoryBatchWise
                WHERE CustomerID = @ErpCustomerID AND [Status] = 'Processing'
                  AND RouteType IN ('SCSFullInventoryFeed', 'SCSDifferentialInventoryFeed')
                  AND StartDate >= DATEADD(MINUTE, -30, GETDATE()))
        THROW 51911, 'A feed of this customer is running (or started a few minutes ago). Wait until it has finished, then run again.', 1;

    BEGIN TRAN;

    DECLARE @InsTemp INT = 0, @UpdBatch INT = 0;

    IF OBJECT_ID('dbo.ZDB_INV03_Temp', 'U') IS NOT NULL
    BEGIN
        IF @RestoreTempRows = 1
        BEGIN
            INSERT INTO dbo.Temp_SCSInventoryFeed SELECT * FROM dbo.ZDB_INV03_Temp;
            SET @InsTemp = @@ROWCOUNT;
        END

        DROP TABLE dbo.ZDB_INV03_Temp;
    END

    IF OBJECT_ID('dbo.ZDB_INV03_Batches', 'U') IS NOT NULL
    BEGIN
        -- EXEC: the table may be missing when this batch is compiled
        EXEC ('UPDATE B SET B.[Status] = Z.[Status], B.FinishDate = Z.FinishDate
                 FROM dbo.InventoryBatchWise B
                 JOIN dbo.ZDB_INV03_Batches Z ON Z.BatchID = B.BatchID;');
        SET @UpdBatch = @@ROWCOUNT;

        DROP TABLE dbo.ZDB_INV03_Batches;
    END

    COMMIT TRAN;

    PRINT CONCAT('Temp rows put back: ', @InsTemp, '. Batches restored: ', @UpdBatch, '. Backup tables dropped.');
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
