/*==============================================================================
  INV 03 - Shopify (Task 00006), customer WKE1000: fixes for the ERP inventory feed - ONLY IF INV_01 / INV_02 ask for it

  Fix A  @ClearTempRows = 1      rows left in Temp_SCSInventoryFeed by a run that was cut off.
                                 Why: the next run starts at page [highest saved page + 1] instead of page 1,
                                 and SP_InsertSCSInventoryFeed would save the old rows together with the new ones.
  Fix B  @CloseStuckBatches = 1  batches of the customer that say "Processing" for more than @StuckMinutes
                                 (the run was cut off). Set to "Error" so the Inventory screen does not show a run for ever.

  Nothing else is changed: no inventory row, no route, no connector.

  @Apply = 0 (default)  DRY RUN: shows what would be changed, changes nothing.
  @Apply = 1            makes a backup (ZDB_INV03_Temp / ZDB_INV03_Batches), then changes.
  Revert: INV_99_Revert_Fix.sql.

  Stops when: a feed of the customer is running or started in the last @RunningMinutes minutes (wait for it),
              or a backup of an earlier apply still exists (run INV_99 or drop it first).
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

/*==============================================================================
  CONFIG
==============================================================================*/
DECLARE @ErpCustomerID     VARCHAR(50) = 'WKE1000';
DECLARE @Apply             BIT = 0;     -- 0 = dry run, 1 = change
DECLARE @ClearTempRows     BIT = 1;     -- fix A
DECLARE @CloseStuckBatches BIT = 1;     -- fix B
DECLARE @StuckMinutes      INT = 60;    -- fix B: "Processing" for longer than this
DECLARE @RunningMinutes    INT = 30;    -- a batch started within this time counts as still running
/*==============================================================================*/

BEGIN TRY
    DECLARE @TempRows INT = (SELECT COUNT(*) FROM dbo.Temp_SCSInventoryFeed WHERE CustomerID = @ErpCustomerID);
    DECLARE @Stuck    INT = (SELECT COUNT(*) FROM dbo.InventoryBatchWise
                              WHERE CustomerID = @ErpCustomerID AND [Status] = 'Processing'
                                AND RouteType IN ('SCSFullInventoryFeed', 'SCSDifferentialInventoryFeed')
                                AND StartDate < DATEADD(MINUTE, -@StuckMinutes, GETDATE()));
    DECLARE @Running  INT = (SELECT COUNT(*) FROM dbo.InventoryBatchWise
                              WHERE CustomerID = @ErpCustomerID AND [Status] = 'Processing'
                                AND RouteType IN ('SCSFullInventoryFeed', 'SCSDifferentialInventoryFeed')
                                AND StartDate >= DATEADD(MINUTE, -@RunningMinutes, GETDATE()));
    DECLARE @Locks    INT = 0;

    IF OBJECT_ID('dbo.RouteExecutionLock', 'U') IS NOT NULL
        EXEC sp_executesql N'SELECT @n = COUNT(*) FROM dbo.RouteExecutionLock
                              WHERE CustomerID = @c AND IsActive = 1 AND RouteTypeId IN (7, 8)
                                AND AcquiredAt >= DATEADD(MINUTE, -ISNULL(TimeoutMinutes, 60), GETDATE());',
                           N'@c VARCHAR(50), @n INT OUTPUT', @c = @ErpCustomerID, @n = @Locks OUTPUT;

    /*--- what the script sees ---------------------------------------------------------*/
    SELECT 'State' AS Grid, @TempRows AS TempRowsLeft, @Stuck AS StuckBatches, @Running AS BatchesStartedRecently, @Locks AS FeedLocksHeld,
           CASE WHEN @Apply = 1 THEN 'APPLY' ELSE 'DRY RUN - nothing is changed' END AS Mode;

    SELECT 'A. Temp rows that would be removed' AS Grid, COUNT(*) AS [Rows], MIN(CurrentPage) AS FromPage, MAX(CurrentPage) AS ToPage,
           MAX(TotalPages) AS TotalPages
      FROM dbo.Temp_SCSInventoryFeed
     WHERE CustomerID = @ErpCustomerID AND @ClearTempRows = 1;

    SELECT 'B. Batches that would be set to Error' AS Grid, BatchID, RouteType, [Status], StartDate, PageCount
      FROM dbo.InventoryBatchWise
     WHERE CustomerID = @ErpCustomerID AND [Status] = 'Processing' AND @CloseStuckBatches = 1
       AND RouteType IN ('SCSFullInventoryFeed', 'SCSDifferentialInventoryFeed')
       AND StartDate < DATEADD(MINUTE, -@StuckMinutes, GETDATE());

    IF @Apply = 0
    BEGIN
        PRINT 'DRY RUN: nothing changed. Set @Apply = 1 to change.';
        RETURN;
    END

    /*--- stop conditions --------------------------------------------------------------*/
    IF @Running > 0 OR @Locks > 0
        THROW 51901, 'A feed of this customer is running (or started a few minutes ago). Wait until it has finished, then run again.', 1;

    IF OBJECT_ID('dbo.ZDB_INV03_Temp', 'U') IS NOT NULL OR OBJECT_ID('dbo.ZDB_INV03_Batches', 'U') IS NOT NULL
        THROW 51902, 'A backup of an earlier apply exists (ZDB_INV03_Temp / ZDB_INV03_Batches). Run INV_99_Revert_Fix.sql, or drop the backup when it is no longer needed.', 1;

    IF (@ClearTempRows = 0 OR @TempRows = 0) AND (@CloseStuckBatches = 0 OR @Stuck = 0)
    BEGIN
        PRINT 'Nothing to fix: no leftover temp rows and no stuck batch (or both fixes are switched off).';
        RETURN;
    END

    BEGIN TRAN;

    DECLARE @DelTemp INT = 0, @UpdBatch INT = 0;

    /*--- backup -----------------------------------------------------------------------*/
    SELECT * INTO dbo.ZDB_INV03_Temp
      FROM dbo.Temp_SCSInventoryFeed
     WHERE CustomerID = @ErpCustomerID AND @ClearTempRows = 1;

    SELECT BatchID, [Status], FinishDate, GETDATE() AS BackupDate INTO dbo.ZDB_INV03_Batches
      FROM dbo.InventoryBatchWise
     WHERE CustomerID = @ErpCustomerID AND [Status] = 'Processing' AND @CloseStuckBatches = 1
       AND RouteType IN ('SCSFullInventoryFeed', 'SCSDifferentialInventoryFeed')
       AND StartDate < DATEADD(MINUTE, -@StuckMinutes, GETDATE());

    /*--- fix A ------------------------------------------------------------------------*/
    IF @ClearTempRows = 1
    BEGIN
        DELETE FROM dbo.Temp_SCSInventoryFeed WHERE CustomerID = @ErpCustomerID;
        SET @DelTemp = @@ROWCOUNT;
    END

    /*--- fix B ------------------------------------------------------------------------*/
    IF @CloseStuckBatches = 1
    BEGIN
        UPDATE B SET B.[Status] = 'Error', B.FinishDate = GETDATE()
          FROM dbo.InventoryBatchWise B
          JOIN dbo.ZDB_INV03_Batches Z ON Z.BatchID = B.BatchID;
        SET @UpdBatch = @@ROWCOUNT;
    END

    COMMIT TRAN;

    PRINT CONCAT('Temp rows removed: ', @DelTemp, '. Batches set to Error: ', @UpdBatch, '. Backup: ZDB_INV03_Temp / ZDB_INV03_Batches.');

    SELECT 'After' AS Grid,
           (SELECT COUNT(*) FROM dbo.Temp_SCSInventoryFeed WHERE CustomerID = @ErpCustomerID) AS TempRowsLeft,
           (SELECT COUNT(*) FROM dbo.InventoryBatchWise WHERE CustomerID = @ErpCustomerID AND [Status] = 'Processing'
               AND RouteType IN ('SCSFullInventoryFeed', 'SCSDifferentialInventoryFeed')) AS BatchesStillProcessing,
           (SELECT COUNT(*) FROM dbo.ZDB_INV03_Temp) AS TempRowsInBackup,
           (SELECT COUNT(*) FROM dbo.ZDB_INV03_Batches) AS BatchesInBackup;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO

/*==============================================================================
  AFTER THE FIX
  Test Run "Shopify - Fetch Full Inventory from ERP" again (it starts at page 1), then INV_02_Verify_After_Run.sql.
==============================================================================*/
