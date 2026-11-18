/*==============================================================================
  CT 99 REVERT - Shopify (Task 00006): undo CT_01 at the client (back to the client store setup)

  WHERE: the CLIENT database. Run it when the test against our demo store is finished, and in any case BEFORE the
         client store (walker-edison) goes live.

  WHAT IT DOES (one transaction, customer WKE1000 only), from the backup tables CT_01 made:
    1. TargetPlusShipNodes        -> exactly the rows of ZDB_CT01_ShipNodes (the client store locations)
    2. The upload connector       -> its Data as saved in ZDB_CT01_Connectors (BaseUrl back to walker-edison)
    3. SCSInventoryFeed           -> the two test items ESM-SB-ICE / ESM-SB-DAWN are deleted
    4. The backup tables are dropped (only after 1 - 3 succeeded)
  OPTIONAL (CONFIG): @ClearTokenSelection = 1 also removes the demo store token from the customer
       (Customers.IntegrationTokenId -> NULL). Default 0: you set it in the UI, so you decide. The token row itself
       stays in IntegrationTokens either way.
  NOT CHANGED: routes, Flows, Status (set the upload route / Flow In-Active yourself if you activated it),
       route logs and feed history of the test, and the stock in our demo store (it keeps the test quantities;
       before the test: ICE 50 / 50, DAWN 12 / 3).

  It STOPS, changing nothing, when the backup tables are missing (CT_01 was not applied or is already reverted).
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

/*==============================================================================
  CONFIG
==============================================================================*/
DECLARE @ErpCustomerID       VARCHAR(50) = 'WKE1000';
DECLARE @ClearTokenSelection BIT         = 0;     -- 1 = also set Customers.IntegrationTokenId to NULL
/*==============================================================================*/

BEGIN TRY
    IF OBJECT_ID('dbo.ZDB_CT01_ShipNodes', 'U') IS NULL OR OBJECT_ID('dbo.ZDB_CT01_Connectors', 'U') IS NULL
        THROW 51650, 'Backup tables ZDB_CT01_ShipNodes / ZDB_CT01_Connectors not found: CT_01 is not applied or already reverted. Nothing changed.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.ZDB_CT01_Connectors)
        THROW 51651, 'ZDB_CT01_Connectors is empty. Nothing changed.', 1;

    BEGIN TRAN;

    /*--- 1. Warehouse mapping back ---------------------------------------------*/
    DELETE FROM dbo.TargetPlusShipNodes WHERE CustomerID = @ErpCustomerID;

    INSERT INTO dbo.TargetPlusShipNodes (WHSID, ShipNode, CustomerID)
    SELECT B.WHSID, B.ShipNode, B.CustomerID FROM dbo.ZDB_CT01_ShipNodes B;

    DECLARE @n INT = @@ROWCOUNT;
    PRINT CONCAT('Warehouse mappings restored: ', @n);

    /*--- 2. Upload connector back ----------------------------------------------*/
    UPDATE C
       SET C.Data = B.Data, C.ModifiedDate = GETDATE(), C.ModifiedBy = 1
      FROM dbo.Connectors C
      JOIN dbo.ZDB_CT01_Connectors B ON B.Id = C.Id;

    SET @n = @@ROWCOUNT;
    PRINT CONCAT('Connectors restored: ', @n);

    /*--- 3. Test items out -----------------------------------------------------*/
    DELETE FROM dbo.SCSInventoryFeed
     WHERE CustomerID = @ErpCustomerID AND ItemId IN ('ESM-SB-ICE', 'ESM-SB-DAWN');

    SET @n = @@ROWCOUNT;
    PRINT CONCAT('Test items deleted: ', @n);

    /*--- Optional: token selection ---------------------------------------------*/
    IF @ClearTokenSelection = 1
    BEGIN
        UPDATE dbo.Customers SET IntegrationTokenId = NULL, ModifiedDate = GETDATE(), ModifiedBy = 1
         WHERE ERPCustomerID = @ErpCustomerID;
        PRINT 'Token selection removed from the customer.';
    END

    /*--- 4. Backup tables ------------------------------------------------------*/
    DROP TABLE dbo.ZDB_CT01_ShipNodes;
    DROP TABLE dbo.ZDB_CT01_Connectors;

    COMMIT TRAN;

    PRINT 'CT 99 done: back to the client store setup.';

    /*--- Result ----------------------------------------------------------------*/
    SELECT '1. Mapping now' AS Grid, S.WHSID, S.ShipNode FROM dbo.TargetPlusShipNodes S WHERE S.CustomerID = @ErpCustomerID ORDER BY S.WHSID;

    SELECT '2. Shopify connectors' AS Grid, c.Id, c.Name,
           CASE WHEN ISJSON(c.Data) = 1 THEN JSON_VALUE(c.Data, '$.BaseUrl') ELSE 'encrypted' END AS BaseUrl
      FROM dbo.Connectors c
      JOIN dbo.ConnectorTypes t ON t.Id = c.TypeId
     WHERE t.Name = 'Shopify' AND c.Name LIKE 'Shopify - %'
     ORDER BY c.Id;

    SELECT '3. Feed rows left for the customer' AS Grid, COUNT(*) AS Rows_ FROM dbo.SCSInventoryFeed WHERE CustomerID = @ErpCustomerID;

    SELECT '4. Token on the customer' AS Grid, C.ERPCustomerID, C.IntegrationTokenId, T.Name AS SelectedToken,
           CASE WHEN T.AccountKey LIKE 'esyncmate-dev-store%' THEN 'NOTE - still the DEMO store token: select the client store token before go-live' ELSE 'OK' END AS [Check]
      FROM dbo.Customers C
      LEFT JOIN dbo.IntegrationTokens T ON T.Id = C.IntegrationTokenId
     WHERE C.ERPCustomerID = @ErpCustomerID;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR (nothing changed): ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
