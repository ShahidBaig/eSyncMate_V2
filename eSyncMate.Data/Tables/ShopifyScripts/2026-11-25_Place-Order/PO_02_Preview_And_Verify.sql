/*==============================================================================
  PO 02 - Shopify (Task 00006), customer WKE1000: the order for SPARS - preview before, result after

  READ-ONLY: only SELECTs. Nothing is sent, nothing is changed.
  Run it BEFORE "Shopify - Place Orders in ERP" (grids 1 - 3 = what would be sent)
  and AFTER it (grids 4 - 6 = what was sent and what SPARS answered).

  It mirrors the map "Shopify Order Transformation"; it does not run the map engine. The body really sent is
  saved by the route as OrderData Type ERP-SNT (grid 5).

  A good run (after):
    grid 4  Status Synced (SYNCED) and an ERP SO number in ExternalId
    grid 5  ERP-SNT with "CustomerPO":"175205", and ERP-JSON (the SPARS answer); no ERP-ERROR
    grid 6  no ERROR / EXCEPTION line
==============================================================================*/

SET NOCOUNT ON;

DECLARE @ErpCustomerID VARCHAR(50)   = 'WKE1000';
DECLARE @MapName       NVARCHAR(200) = 'Shopify Order Transformation';
DECLARE @Since         DATETIME      = DATEADD(HOUR, -24, GETDATE());

DECLARE @CustomerId INT = (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = @ErpCustomerID);
DECLARE @Map NVARCHAR(MAX) = (SELECT Map FROM dbo.Maps WHERE Name = @MapName);
DECLARE @PoExpr NVARCHAR(400) = JSON_VALUE(@Map, '$.order.header.CustomerPO');

/*--- 1. The map and its route ---------------------------------------------------------*/
SELECT '1. Map' AS Grid, M.Id AS MapId, JSON_VALUE(M.Map, '$.order.header.CustomerPO') AS CustomerPO_InMap,
       CASE WHEN JSON_VALUE(M.Map, '$.order.header.CustomerPO') LIKE '%$.name%' THEN 'the Shopify order number (175205)'
            WHEN JSON_VALUE(M.Map, '$.order.header.CustomerPO') LIKE '%legacyResourceId%' THEN 'the Shopify id (7490956558402)'
            ELSE 'CHECK - unknown' END AS CustomerPO_Means,
       JSON_VALUE(M.Map, '$.order.header.ShipViaCode') AS ShipViaCode_InMap,
       R.Id AS RouteId, R.Name AS Route, R.Status AS RouteStatus, R.JobID
  FROM dbo.Maps M
  LEFT JOIN dbo.Routes R ON R.MapId = M.Id
 WHERE M.Name = @MapName;

/*--- Orders of the customer with their saved Shopify JSON -----------------------------*/
DECLARE @O TABLE (Id INT PRIMARY KEY, OrderNumber NVARCHAR(100), CustomerOrderNo NVARCHAR(100), Status NVARCHAR(50), ExternalId NVARCHAR(100), J NVARCHAR(MAX));

INSERT INTO @O (Id, OrderNumber, CustomerOrderNo, Status, ExternalId, J)
SELECT O.Id, O.OrderNumber, O.CustomerOrderNo, O.Status, O.ExternalId,
       (SELECT TOP 1 D.Data FROM dbo.OrderData D WHERE D.OrderId = O.Id AND D.Type = 'API-JSON' ORDER BY D.Id DESC)
  FROM dbo.Orders O
 WHERE O.CustomerId = @CustomerId;

/*--- 2. HEADER that goes to SPARS ---------------------------------------------------------*/
SELECT '2. Header' AS Grid, X.Id AS OrderId, X.Status,
       @ErpCustomerID AS CustomerID,
       CASE WHEN @PoExpr LIKE '%$.name%' THEN STUFF(JSON_VALUE(X.J, '$.name'), 1, 1, '')
            ELSE JSON_VALUE(X.J, '$.legacyResourceId') END              AS CustomerPO,
       JSON_VALUE(X.J, '$.shippingAddress.name')                       AS AddressName_CompanyName,
       JSON_VALUE(X.J, '$.shippingAddress.address1')                   AS Address1,
       JSON_VALUE(X.J, '$.shippingAddress.address2')                   AS Address2,
       JSON_VALUE(X.J, '$.shippingAddress.city')                       AS City,
       JSON_VALUE(X.J, '$.shippingAddress.provinceCode')               AS [State],
       JSON_VALUE(X.J, '$.shippingAddress.zip')                        AS Zip,
       JSON_VALUE(X.J, '$.shippingAddress.countryCodeV2')              AS Country,
       CASE WHEN ISNULL(JSON_VALUE(X.J, '$.shippingAddress.phone'), '') = '' THEN 'empty' ELSE 'filled' END AS Phone,
       JSON_VALUE(@Map, '$.order.header.ShipViaCode')                  AS ShipViaCode,
       FORMAT(TRY_CONVERT(DATETIMEOFFSET, JSON_VALUE(X.J, '$.createdAt')), 'MM/dd/yyyy') AS OrderDate_ShipDate_CancelDate,
       JSON_VALUE(X.J, '$.legacyResourceId')                           AS ExternalID,
       CASE WHEN X.J IS NULL OR ISJSON(X.J) = 0 THEN 'CHECK - no valid API-JSON saved'
            WHEN @PoExpr LIKE '%$.name%' AND LEFT(ISNULL(JSON_VALUE(X.J, '$.name'), ''), 1) <> '#' THEN 'CHECK - the order name does not start with #: its first character would be cut off'
            WHEN ISNULL(JSON_VALUE(X.J, '$.shippingAddress.address1'), '') = '' THEN 'CHECK - no shipping address'
            ELSE 'OK' END AS [Check]
  FROM @O X
 ORDER BY X.Id;

/*--- 3. LINES that go to SPARS ------------------------------------------------------------*/
SELECT '3. Lines' AS Grid, X.Id AS OrderId,
       JSON_VALUE(L.value, '$.lineNo')                                  AS APIOrderLineNo,
       JSON_VALUE(L.value, '$.itemId')                                  AS ItemID,
       JSON_VALUE(L.value, '$.currentQuantity')                         AS OrderQty,
       JSON_VALUE(L.value, '$.discountedUnitPriceSet.shopMoney.amount') AS UnitPrice,
       CASE WHEN EXISTS (SELECT 1 FROM dbo.SCSInventoryFeed F WHERE F.CustomerID = @ErpCustomerID AND F.ItemId = JSON_VALUE(L.value, '$.itemId'))
            THEN 'OK - item of the customer''s inventory feed' ELSE 'NOTE - not in the inventory feed' END AS [Check]
  FROM @O X
 CROSS APPLY OPENJSON(X.J, '$.lineItems.nodes') L
 WHERE ISJSON(X.J) = 1
 ORDER BY X.Id, TRY_CONVERT(INT, JSON_VALUE(L.value, '$.lineNo'));

/*--- 4. AFTER: the order in eSyncMate -----------------------------------------------------*/
SELECT '4. Order now' AS Grid, O.Id, O.OrderNumber, O.CustomerOrderNo, O.Status, O.ExternalId AS ERP_SO_No, O.CreatedDate
  FROM dbo.Orders O
 WHERE O.CustomerId = @CustomerId
 ORDER BY O.Id;

/*--- 5. AFTER: what was sent and what SPARS answered ---------------------------------------*/
SELECT '5. Sent / answer' AS Grid, D.OrderId, D.Type, D.CreatedDate, LEFT(D.Data, 3000) AS Data
  FROM dbo.OrderData D
  JOIN @O X ON X.Id = D.OrderId
 WHERE D.Type IN ('ERP-SNT', 'ERP-JSON', 'ERP-ERROR')
 ORDER BY D.Id;

/*--- 6. AFTER: route log of the Place Orders route -------------------------------------------*/
SELECT '6. Route log' AS Grid, L.Id, L.RouteId,
       CASE L.[Type] WHEN 1 THEN 'Info' WHEN 2 THEN 'Warning' WHEN 3 THEN 'EXCEPTION' WHEN 4 THEN 'Debug' WHEN 5 THEN 'ERROR' WHEN 6 THEN 'RouteInfo' END AS LogType,
       L.CreatedDate, L.[Message], LEFT(L.Details, 1500) AS Details
  FROM dbo.RouteLog L
  JOIN dbo.Routes R ON R.Id = L.RouteId
 WHERE R.CustomerName = @ErpCustomerID AND R.TypeId = 9 AND L.CreatedDate >= @Since
 ORDER BY L.Id DESC;
GO
