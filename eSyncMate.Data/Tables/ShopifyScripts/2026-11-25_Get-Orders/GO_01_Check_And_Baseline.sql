/*==============================================================================
  GO 01 - Shopify (Task 00006), customer WKE1000: check before the first Get Orders run on the LIVE store,
          and record a baseline so the test orders can be taken out again

  The route "Shopify - Get Orders" only READS from Shopify. It writes the orders into eSyncMate
  (Orders, OrderData, OrderDetail) with Status New. Nothing goes to SPARS unless
  "Shopify - Place Orders in ERP" runs - that route must stay In-Active.

  @Apply = 0 (default): READ-ONLY, only the check grids.
  @Apply = 1: also creates dbo.ZDB_GO_Baseline (customer + highest Orders.Id now). GO_99 deletes exactly the
              orders of the customer above it. Nothing else is changed.

  Stops with @Apply = 1 (nothing changed):
    51960 a baseline exists already (run GO_99 first)
    51961 "Shopify - Place Orders in ERP" is Active, or the Get Orders route is Active
    51962 the token selected on the customer is not the walker-edison one
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ErpCustomerID VARCHAR(50)   = 'WKE1000';
DECLARE @Apply         BIT           = 0;   -- 0 = check only, 1 = also record the baseline
DECLARE @Store         NVARCHAR(200) = 'walker-edison.myshopify.com';

BEGIN TRY
    DECLARE @CustomerId INT = (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = @ErpCustomerID);

    /*--- 1. Token of the customer -----------------------------------------------------------*/
    SELECT '1. Token' AS Grid, C.Id AS CustomerId, C.ERPCustomerID, T.Id AS TokenId, T.Name AS SelectedToken,
           CASE WHEN T.AccountKey = @Store THEN 'OK - live store token' ELSE 'CHECK - not the walker-edison token' END AS [Check]
      FROM dbo.Customers C
      LEFT JOIN dbo.IntegrationTokens T ON T.Id = C.IntegrationTokenId
     WHERE C.Id = @CustomerId;

    /*--- 2. The two order routes --------------------------------------------------------------*/
    SELECT '2. Routes' AS Grid, R.Id, R.Name, R.TypeId, R.Status, R.JobID,
           CASE WHEN R.TypeId = 82 AND R.Status = 'Active' THEN 'CHECK - Active: it would read every open order on its schedule'
                WHEN R.TypeId = 82 THEN 'OK - In-Active, use Run Now'
                WHEN R.Status = 'Active' OR R.JobID IS NOT NULL THEN 'STOP - Active: saved orders would be placed in the LIVE ERP'
                ELSE 'OK - In-Active, keep it so' END AS [Check]
      FROM dbo.Routes R
     WHERE R.CustomerName = @ErpCustomerID AND R.TypeId IN (82, 9)
     ORDER BY R.Id;

    /*--- 3. Connectors of the Get Orders route ------------------------------------------------
          An encrypted connector cannot be read by SQL: check Base URL and Parameters in Setup > Connectors.  */
    SELECT '3. Get Orders connectors' AS Grid,
           CASE WHEN c.Id = R.SourceConnectorId THEN 'Source (Shopify)' ELSE 'Destination (eSyncMate DB)' END AS UsedAs,
           c.Id, c.Name, CASE WHEN ISJSON(c.Data) = 1 THEN 'Plain' ELSE 'Encrypted - check in Setup > Connectors' END AS Storage,
           CASE WHEN ISJSON(c.Data) = 1 THEN JSON_VALUE(c.Data, '$.BaseUrl') END AS BaseUrl,
           CASE WHEN ISJSON(c.Data) = 1 THEN JSON_VALUE(c.Data, '$.Url') END AS Url,
           CASE WHEN ISJSON(c.Data) = 1 THEN JSON_VALUE(c.Data, '$.CustomerID') END AS CustomerID,
           CASE WHEN ISJSON(c.Data) = 1 THEN JSON_QUERY(c.Data, '$.Parmeters') END AS Parameters_query_days
      FROM dbo.Routes R
      JOIN dbo.Connectors c ON c.Id IN (R.SourceConnectorId, R.DestinationConnectorId)
     WHERE R.CustomerName = @ErpCustomerID AND R.TypeId = 82;

    /*--- 4. Orders of the customer now ----------------------------------------------------------*/
    SELECT '4. Orders now' AS Grid, ISNULL(O.Status, '(all)') AS [Status], COUNT(*) AS Orders_, MAX(O.CreatedDate) AS LastCreated
      FROM dbo.Orders O
     WHERE O.CustomerId = @CustomerId
     GROUP BY ROLLUP (O.Status);

    /*--- 5. Baseline ----------------------------------------------------------------------------*/
    SELECT '5. Baseline' AS Grid,
           CASE WHEN OBJECT_ID('dbo.ZDB_GO_Baseline', 'U') IS NOT NULL THEN 'exists - a test is open (GO_99 closes it)' ELSE 'none yet' END AS Baseline,
           (SELECT ISNULL(MAX(Id), 0) FROM dbo.Orders) AS HighestOrderIdNow;

    IF @Apply = 0
    BEGIN
        PRINT 'CHECK ONLY: nothing changed. Set @Apply = 1 to record the baseline.';
        RETURN;
    END

    IF OBJECT_ID('dbo.ZDB_GO_Baseline', 'U') IS NOT NULL
        THROW 51960, 'A baseline exists already (dbo.ZDB_GO_Baseline). Run GO_99_Revert_Test_Orders.sql first. Nothing changed.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Routes R WHERE R.CustomerName = @ErpCustomerID AND R.TypeId IN (82, 9) AND (R.Status = 'Active' OR R.JobID IS NOT NULL))
        THROW 51961, 'An order route of the customer is Active or has a job (grid 2). Set it In-Active on the Flows screen first. Nothing changed.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Customers C JOIN dbo.IntegrationTokens T ON T.Id = C.IntegrationTokenId
                    WHERE C.Id = @CustomerId AND T.AccountKey = @Store)
        THROW 51962, 'The token selected on the customer is not the walker-edison token (grid 1). Nothing changed.', 1;

    SELECT @CustomerId AS CustomerId, ISNULL((SELECT MAX(Id) FROM dbo.Orders), 0) AS MaxOrderId, GETDATE() AS AppliedDate
      INTO dbo.ZDB_GO_Baseline;

    PRINT 'Baseline recorded in dbo.ZDB_GO_Baseline. Orders of the customer above it are test orders.';

    EXEC ('SELECT ''Baseline recorded'' AS Grid, CustomerId, MaxOrderId, AppliedDate FROM dbo.ZDB_GO_Baseline');
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
