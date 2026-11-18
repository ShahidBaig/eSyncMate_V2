/*==============================================================================
  CT 02 VERIFY - Shopify (Task 00006): state of the Get Orders test at the client against our demo store

  READ-ONLY: only SELECTs. No token or secret shown.
  Run it BEFORE the route (grids 1 - 3 must be OK) and AFTER it (grids 4 - 6 show the orders that came in).
==============================================================================*/

SET NOCOUNT ON;

DECLARE @ErpCustomerID VARCHAR(50)   = 'WKE1000';
DECLARE @DemoStore     NVARCHAR(200) = 'esyncmate-dev-store.myshopify.com';

DECLARE @CustomerId INT = (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = @ErpCustomerID);
DECLARE @MaxOrderId INT = NULL;

IF OBJECT_ID('dbo.ZDB_CT02_Baseline', 'U') IS NOT NULL
    EXEC sp_executesql N'SELECT @m = MaxOrderId FROM dbo.ZDB_CT02_Baseline', N'@m INT OUTPUT', @m = @MaxOrderId OUTPUT;

/*--- 1. Token + test applied? ------------------------------------------------------*/
SELECT '1. Token' AS Grid, C.ERPCustomerID, T.Name AS SelectedToken,
       CASE WHEN T.AccountKey = @DemoStore THEN 'OK - demo store token' ELSE 'CHECK - not the demo store token' END AS Token,
       CASE WHEN @MaxOrderId IS NULL THEN 'CHECK - CT_02 is not applied (no baseline)' ELSE CONCAT('OK - applied, orders above Id ', @MaxOrderId, ' are test orders') END AS Test
  FROM dbo.Customers C
  LEFT JOIN dbo.IntegrationTokens T ON T.Id = C.IntegrationTokenId
 WHERE C.Id = @CustomerId;

/*--- 2. Shopify connectors: which store --------------------------------------------*/
SELECT '2. Connectors' AS Grid, x.Id, x.Name, x.Store,
       CASE WHEN x.Fmt = 'Encrypted' THEN 'NOTE - encrypted, check in Setup > Connectors'
            WHEN x.Name LIKE '%- Get Orders' AND x.Store = @DemoStore THEN 'OK - demo store'
            WHEN x.Name LIKE '%- Get Orders' THEN 'CHECK - must be the demo store for this test'
            WHEN x.Store = @DemoStore THEN 'NOTE - on the demo store (OK for WHSW Inventory Upload while CT_01 is applied)'
            ELSE 'OK - client store, refuses to run while the demo token is selected' END AS [Check]
  FROM (SELECT c.Id, c.Name, CASE WHEN ISJSON(c.Data) = 1 THEN 'Plain' ELSE 'Encrypted' END AS Fmt,
               CASE WHEN ISJSON(c.Data) = 1 AND JSON_VALUE(c.Data, '$.BaseUrl') LIKE 'https://%/%'
                    THEN SUBSTRING(JSON_VALUE(c.Data, '$.BaseUrl'), 9, CHARINDEX('/', JSON_VALUE(c.Data, '$.BaseUrl'), 9) - 9) END AS Store
          FROM dbo.Connectors c
          JOIN dbo.ConnectorTypes t ON t.Id = c.TypeId
         WHERE t.Name = 'Shopify' AND c.Name LIKE 'Shopify - %') x
 ORDER BY x.Id;

/*--- 3. Routes: nothing that sends an order on may be Active -------------------------*/
SELECT '3. Routes' AS Grid, r.Id, r.Name, r.Status,
       CASE WHEN r.Name LIKE '%- Get Orders' THEN 'the route to Test Run'
            WHEN r.Name LIKE '%Place Orders in ERP' AND r.Status = 'Active' THEN 'STOP - Active: a demo order would go to the ERP. Set it In-Active now'
            WHEN r.Status = 'Active' THEN 'CHECK - Active: keep it In-Active during this test'
            ELSE 'OK - not active' END AS [Check]
  FROM dbo.Routes r
 WHERE r.Name LIKE 'Shopify - %'
 ORDER BY r.Id;

/*--- 4. Orders the test brought in ---------------------------------------------------*/
SELECT '4. Test orders' AS Grid, O.Id, O.OrderNumber, O.CustomerOrderNo, O.Status, O.OrderDate, O.ShipToName, O.ShipToCity,
       O.ShipToCountry, O.ShippingMethod, O.CreatedDate,
       (SELECT COUNT(*) FROM dbo.OrderDetail D WHERE D.OrderId = O.Id) AS Lines,
       (SELECT COUNT(*) FROM dbo.OrderData  X WHERE X.OrderId = O.Id) AS DataRows
  FROM dbo.Orders O
 WHERE O.CustomerId = @CustomerId AND O.Id > ISNULL(@MaxOrderId, 2147483647)
 ORDER BY O.Id;

/*--- 5. Their lines ---------------------------------------------------------------------*/
SELECT '5. Test order lines' AS Grid, D.OrderId, D.[LineNo], D.ItemID, D.LineQty, D.UnitPrice, D.Status, D.order_line_id
  FROM dbo.OrderDetail D
  JOIN dbo.Orders O ON O.Id = D.OrderId
 WHERE O.CustomerId = @CustomerId AND O.Id > ISNULL(@MaxOrderId, 2147483647)
 ORDER BY D.OrderId, D.[LineNo];

/*--- 6. Orders of the customer from BEFORE the test (left alone by the revert) ---------*/
SELECT '6. Older orders of WKE1000' AS Grid, COUNT(*) AS Orders_
  FROM dbo.Orders O
 WHERE O.CustomerId = @CustomerId AND O.Id <= ISNULL(@MaxOrderId, 2147483647);
GO
