/*==============================================================================
  GO 02 - Shopify (Task 00006), customer WKE1000: what did the Get Orders run bring in?

  READ-ONLY: only SELECTs. Run it after Run Now of "Shopify - Get Orders".

  A good run:
    grid 1  a line "Shopify orders for [WKE1000] (...): N read in P page(s), M new saved (0 with Status ERROR) ..."
            and no ERROR / EXCEPTION line
    grid 2  the M orders, Status New, with ship-to name and address filled
    grid 3  their lines; ItemInFeed = yes means SPARS knows the item for this customer
    grid 4  the order routes are still In-Active
==============================================================================*/

SET NOCOUNT ON;

DECLARE @ErpCustomerID VARCHAR(50) = 'WKE1000';
DECLARE @Since         DATETIME    = DATEADD(HOUR, -24, GETDATE());

DECLARE @CustomerId INT = (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = @ErpCustomerID);
DECLARE @MaxOrderId INT = NULL;

IF OBJECT_ID('dbo.ZDB_GO_Baseline', 'U') IS NOT NULL
    EXEC sp_executesql N'SELECT @m = MaxOrderId FROM dbo.ZDB_GO_Baseline', N'@m INT OUTPUT', @m = @MaxOrderId OUTPUT;

IF @MaxOrderId IS NULL
    PRINT 'No baseline (GO_01 with @Apply = 1 was not run): grids 2 and 3 show the orders created in the last 24 hours instead.';

/*--- 1. Route log of the Get Orders route ------------------------------------------------*/
SELECT '1. Route log' AS Grid, L.Id, L.RouteId,
       CASE L.[Type] WHEN 1 THEN 'Info' WHEN 2 THEN 'Warning' WHEN 3 THEN 'EXCEPTION' WHEN 4 THEN 'Debug' WHEN 5 THEN 'ERROR' WHEN 6 THEN 'RouteInfo' END AS LogType,
       L.CreatedDate, L.[Message], LEFT(L.Details, 2000) AS Details
  FROM dbo.RouteLog L
  JOIN dbo.Routes R ON R.Id = L.RouteId
 WHERE R.CustomerName = @ErpCustomerID AND R.TypeId = 82 AND L.CreatedDate >= @Since
 ORDER BY L.Id DESC;

/*--- 2. The orders the run saved -----------------------------------------------------------*/
SELECT '2. Orders' AS Grid, O.Id, O.OrderNumber, O.CustomerOrderNo, O.Status, O.OrderDate, O.ShippingMethod,
       O.ShipToName, O.ShipToAddress1, O.ShipToCity, O.ShipToState, O.ShipToZip, O.ShipToCountry,
       CASE WHEN ISNULL(O.ShipToPhone, '') = '' THEN 'blank' ELSE 'filled' END AS Phone,
       CASE WHEN ISNULL(O.ShipToEmail, '') = '' THEN 'blank' ELSE 'filled' END AS Email,
       O.ExternalId, O.CreatedDate,
       (SELECT COUNT(*) FROM dbo.OrderDetail D WHERE D.OrderId = O.Id) AS Units,
       (SELECT COUNT(*) FROM dbo.OrderData  X WHERE X.OrderId = O.Id) AS DataRows
  FROM dbo.Orders O
 WHERE O.CustomerId = @CustomerId
   AND ((@MaxOrderId IS NOT NULL AND O.Id > @MaxOrderId) OR (@MaxOrderId IS NULL AND O.CreatedDate >= @Since))
 ORDER BY O.Id;

/*--- 3. Their lines (one row per item of an order; Units = OrderDetail rows) ----------------*/
SELECT '3. Lines' AS Grid, O.CustomerOrderNo, D.[LineNo], D.ItemID, COUNT(*) AS Units, MAX(D.UnitPrice) AS UnitPrice, MAX(D.Status) AS LineStatus,
       CASE WHEN EXISTS (SELECT 1 FROM dbo.SCSInventoryFeed F WHERE F.CustomerID = @ErpCustomerID AND F.ItemId = D.ItemID)
            THEN 'yes' ELSE 'NO - SPARS has not sent this item for the customer' END AS ItemInFeed
  FROM dbo.OrderDetail D
  JOIN dbo.Orders O ON O.Id = D.OrderId
 WHERE O.CustomerId = @CustomerId
   AND ((@MaxOrderId IS NOT NULL AND O.Id > @MaxOrderId) OR (@MaxOrderId IS NULL AND O.CreatedDate >= @Since))
 GROUP BY O.Id, O.CustomerOrderNo, D.[LineNo], D.ItemID
 ORDER BY O.Id, D.[LineNo];

/*--- 4. The order routes must still be In-Active ---------------------------------------------*/
SELECT '4. Routes' AS Grid, R.Id, R.Name, R.Status, R.JobID,
       CASE WHEN R.TypeId = 9 AND (R.Status = 'Active' OR R.JobID IS NOT NULL) THEN 'STOP - Active: the saved orders go to the LIVE ERP. Set it In-Active now'
            WHEN R.Status = 'Active' OR R.JobID IS NOT NULL THEN 'CHECK - Active'
            ELSE 'OK - In-Active' END AS [Check]
  FROM dbo.Routes R
 WHERE R.CustomerName = @ErpCustomerID AND R.TypeId IN (82, 9)
 ORDER BY R.Id;
GO
