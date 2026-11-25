/*==============================================================================
  L65 99 - Shopify (Task 00006), customer WKE1000: remove the L65 mapping again

  Removes the row L65_01 added and drops the backup table dbo.ZDB_L65_ShipNodes.
  It does NOT change anything in Shopify: quantities already uploaded to the L65 location stay as they are.

  @Apply = 0 (default) DRY RUN.

  Stops (nothing changed):
    52020 no backup table (L65_01 was not applied)      52022 the upload route is Active
    52021 L65 was already mapped before L65_01
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ErpCustomerID VARCHAR(50)   = 'WKE1000';
DECLARE @Apply         BIT           = 0;   -- 0 = dry run, 1 = change
DECLARE @WHSID         VARCHAR(10)   = 'L65';
DECLARE @LocationGid   NVARCHAR(500) = 'gid://shopify/Location/82231951426';

BEGIN TRY
    SELECT 'Mapping now' AS Grid, S.ID, S.WHSID, S.ShipNode,
           CASE WHEN S.WHSID = @WHSID AND S.ShipNode = @LocationGid THEN 'will be removed' ELSE 'stays' END AS [Action]
      FROM dbo.TargetPlusShipNodes S
     WHERE S.CustomerID = @ErpCustomerID
     ORDER BY S.WHSID;

    IF OBJECT_ID('dbo.ZDB_L65_ShipNodes', 'U') IS NULL
        THROW 52020, 'No backup table dbo.ZDB_L65_ShipNodes: L65_01 was not applied. Nothing changed.', 1;

    DECLARE @WasThere INT;

    EXEC sp_executesql N'SELECT @n = COUNT(*) FROM dbo.ZDB_L65_ShipNodes WHERE WHSID = @w',
                       N'@n INT OUTPUT, @w VARCHAR(10)', @n = @WasThere OUTPUT, @w = @WHSID;

    IF @WasThere > 0
        THROW 52021, 'L65 was already mapped before L65_01 ran (it is in the backup). Nothing changed - send me the grid.', 1;

    IF @Apply = 0
    BEGIN
        PRINT 'DRY RUN: nothing changed. Set @Apply = 1 to remove the L65 mapping.';
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.Routes R WHERE R.CustomerName = @ErpCustomerID AND R.TypeId = 80 AND (R.Status = 'Active' OR R.JobID IS NOT NULL))
        THROW 52022, 'The upload route is Active. Set it In-Active on the Flows screen first. Nothing changed.', 1;

    BEGIN TRAN;

    DELETE FROM dbo.TargetPlusShipNodes
     WHERE CustomerID = @ErpCustomerID AND WHSID = @WHSID AND ShipNode = @LocationGid;

    PRINT 'L65 mapping rows removed: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

    DROP TABLE dbo.ZDB_L65_ShipNodes;

    COMMIT TRAN;

    SELECT 'Mapping after' AS Grid, S.ID, S.WHSID, S.ShipNode
      FROM dbo.TargetPlusShipNodes S
     WHERE S.CustomerID = @ErpCustomerID
     ORDER BY S.WHSID;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
