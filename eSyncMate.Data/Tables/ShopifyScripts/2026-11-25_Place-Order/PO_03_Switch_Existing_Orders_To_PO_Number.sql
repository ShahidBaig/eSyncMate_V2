/*==============================================================================
  PO 03 - Shopify (Task 00006), customer WKE1000: switch the orders saved by Release6 to the PO number

  From Release7 on "Shopify - Get Orders" saves an order like this:
      Orders.OrderNumber      = the PO number      (175205)          <- Customer PO on the Orders screen, CustomerPO in SPARS
      Orders.CustomerOrderNo  = the Shopify id     (7490956558402)   <- used for the calls to Shopify
  Release6 saved them the other way round (OrderNumber = Shopify id, CustomerOrderNo = "#175205").
  This script brings the orders that Release6 saved (order 786371) to the new form, so the Orders screen shows
  175205 and the check "does this order exist in SPARS already" asks SPARS with 175205.

  RUN IT ONLY AFTER RELEASE7 IS DEPLOYED (API + RouteWorker). With Release6 code the switched order would be
  fetched a second time by Get Orders and its shipment could not be sent to Shopify.
  Set @Release7Deployed = 1 to confirm.

  @Apply = 0 (default) DRY RUN. With @Apply = 1 the rows as they are now are saved in dbo.ZDB_PO03_Orders
  (PO_97 restores from it).

  Changes per order: Orders.OrderNumber, Orders.CustomerOrderNo, OrderData.OrderNumber. Nothing else.
  Only orders that are still New and have no ERP number are switched; others are listed and left alone.

  Stops (nothing changed): 51995 @Release7Deployed is 0     51996 the backup exists already (run PO_97 first)
                           51997 an order route of the customer is Active
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ErpCustomerID    VARCHAR(50) = 'WKE1000';
DECLARE @Apply            BIT = 0;   -- 0 = dry run, 1 = change
DECLARE @Release7Deployed BIT = 0;   -- set to 1 only when Release7 runs at the client

BEGIN TRY
    DECLARE @CustomerId INT = (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = @ErpCustomerID);

    -- Release6 form: OrderNumber = the Shopify id of the saved order, CustomerOrderNo = "#<number>"
    DECLARE @T TABLE (Id INT PRIMARY KEY, OldOrderNumber NVARCHAR(100), OldCustomerOrderNo NVARCHAR(100), NewOrderNumber NVARCHAR(100),
                      Status NVARCHAR(50), ExternalId NVARCHAR(100), CanSwitch BIT);

    INSERT INTO @T
    SELECT O.Id, O.OrderNumber, O.CustomerOrderNo, STUFF(O.CustomerOrderNo, 1, 1, ''), O.Status, O.ExternalId,
           CASE WHEN O.Status = 'New' AND ISNULL(O.ExternalId, '') = ''
                 AND NOT EXISTS (SELECT 1 FROM dbo.Orders X WHERE X.CustomerId = O.CustomerId AND X.Id <> O.Id
                                    AND X.OrderNumber = STUFF(O.CustomerOrderNo, 1, 1, ''))
                THEN 1 ELSE 0 END
      FROM dbo.Orders O
     CROSS APPLY (SELECT TOP 1 D.Data FROM dbo.OrderData D WHERE D.OrderId = O.Id AND D.Type = 'API-JSON' ORDER BY D.Id DESC) J
     WHERE O.CustomerId = @CustomerId
       AND O.CustomerOrderNo LIKE '#_%'
       AND ISJSON(J.Data) = 1
       AND JSON_VALUE(J.Data, '$.legacyResourceId') = O.OrderNumber;

    SELECT 'Orders in the Release6 form' AS Grid, T.Id, T.Status, T.ExternalId,
           T.OldOrderNumber AS OrderNumber_Now, T.OldCustomerOrderNo AS CustomerOrderNo_Now,
           T.NewOrderNumber AS OrderNumber_After, T.OldOrderNumber AS CustomerOrderNo_After,
           CASE WHEN T.CanSwitch = 1 THEN 'will be switched' ELSE 'LEFT ALONE - not New, has an ERP number, or the PO number is taken' END AS [Action]
      FROM @T T
     ORDER BY T.Id;

    IF NOT EXISTS (SELECT 1 FROM @T WHERE CanSwitch = 1)
    BEGIN
        PRINT 'No order to switch.';
        RETURN;
    END

    IF @Apply = 0
    BEGIN
        PRINT 'DRY RUN: nothing changed. Set @Apply = 1 (and @Release7Deployed = 1) to switch the orders.';
        RETURN;
    END

    IF @Release7Deployed = 0
        THROW 51995, 'Release7 must be deployed first (set @Release7Deployed = 1 to confirm). Nothing changed.', 1;

    IF OBJECT_ID('dbo.ZDB_PO03_Orders', 'U') IS NOT NULL
        THROW 51996, 'The backup dbo.ZDB_PO03_Orders exists already. Run PO_97_Revert_Switch.sql first. Nothing changed.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Routes R WHERE R.CustomerName = @ErpCustomerID AND R.TypeId IN (82, 9) AND (R.Status = 'Active' OR R.JobID IS NOT NULL))
        THROW 51997, 'An order route of the customer is Active. Set it In-Active on the Flows screen first. Nothing changed.', 1;

    BEGIN TRAN;

    SELECT T.Id, T.OldOrderNumber, T.OldCustomerOrderNo, T.NewOrderNumber, GETDATE() AS BackupDate
      INTO dbo.ZDB_PO03_Orders
      FROM @T T
     WHERE T.CanSwitch = 1;

    UPDATE O
       SET O.OrderNumber = T.NewOrderNumber, O.CustomerOrderNo = T.OldOrderNumber
      FROM dbo.Orders O
      JOIN @T T ON T.Id = O.Id AND T.CanSwitch = 1;

    DECLARE @n INT = @@ROWCOUNT;

    UPDATE D
       SET D.OrderNumber = T.NewOrderNumber
      FROM dbo.OrderData D
      JOIN @T T ON T.Id = D.OrderId AND T.CanSwitch = 1;

    COMMIT TRAN;

    PRINT CONCAT('Orders switched to the PO number: ', @n, '. Backup in dbo.ZDB_PO03_Orders.');

    SELECT 'After' AS Grid, O.Id, O.OrderNumber AS CustomerPO_OnTheScreen, O.CustomerOrderNo AS ShopifyId, O.Status, O.ExternalId
      FROM dbo.Orders O
      JOIN @T T ON T.Id = O.Id AND T.CanSwitch = 1;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
