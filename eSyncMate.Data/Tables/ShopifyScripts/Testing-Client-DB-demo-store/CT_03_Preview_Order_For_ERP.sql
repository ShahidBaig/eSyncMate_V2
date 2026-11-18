/*==============================================================================
  CT 03 PREVIEW - Shopify (Task 00006): what "Shopify - Place Orders in ERP" WOULD send to SPARS for the
                  Shopify orders of WKE1000 - WITHOUT sending anything

  READ-ONLY: only SELECTs. Nothing is sent to the ERP, nothing is changed. Works on the client or the dev database.

  HOW: the route takes the order as "Shopify - Get Orders" saved it (OrderData, Type API-JSON) and runs it through
       the map "Shopify Order Transformation". This script reads the same saved JSON with the same paths as the map
       and shows the result field by field, with a [Check] column.
       It mirrors the map; it does not run the map engine itself. The real body is stored by the route as
       OrderData Type ERP-JSON at the moment it is sent.

  NEEDS: at least one Shopify order of the customer in eSyncMate (run the Get Orders test first: CT_02).
==============================================================================*/

SET NOCOUNT ON;

DECLARE @ErpCustomerID VARCHAR(50)   = 'WKE1000';
DECLARE @MapName       NVARCHAR(200) = 'Shopify Order Transformation';

DECLARE @CustomerId INT = (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = @ErpCustomerID);
DECLARE @Map NVARCHAR(MAX) = (SELECT Map FROM dbo.Maps WHERE Name = @MapName);

/*--- 1. The map and the route that uses it -------------------------------------------*/
SELECT '1. Map' AS Grid, M.Id AS MapId, M.Name,
       CASE WHEN ISJSON(M.Map) = 1 THEN 'OK - valid JSON' ELSE 'CHECK - not valid JSON' END AS MapJson,
       JSON_VALUE(M.Map, '$.order.header.CustomerID')  AS CustomerID_InMap,
       JSON_VALUE(M.Map, '$.order.header.ShipViaCode') AS ShipViaCode_InMap,
       R.Id AS RouteId, R.Name AS Route, R.Status AS RouteStatus,
       CASE WHEN R.Id IS NULL THEN 'CHECK - no route uses this map'
            WHEN R.Status = 'Active' THEN 'NOTE - the route is ACTIVE: orders with Status New are being sent'
            ELSE 'OK - route In-Active, nothing is sent' END AS [Check]
  FROM dbo.Maps M
  LEFT JOIN dbo.Routes R ON R.MapId = M.Id AND R.Name LIKE 'Shopify - %'
 WHERE M.Name = @MapName;

/*--- Orders of the customer with their saved Shopify JSON -----------------------------*/
DECLARE @O TABLE (Id INT PRIMARY KEY, OrderNumber NVARCHAR(100), CustomerOrderNo NVARCHAR(100), Status NVARCHAR(50), J NVARCHAR(MAX));

INSERT INTO @O (Id, OrderNumber, CustomerOrderNo, Status, J)
SELECT O.Id, O.OrderNumber, O.CustomerOrderNo, O.Status,
       (SELECT TOP 1 D.Data FROM dbo.OrderData D WHERE D.OrderId = O.Id AND D.Type = 'API-JSON' ORDER BY D.Id DESC)
  FROM dbo.Orders O
 WHERE O.CustomerId = @CustomerId;

IF NOT EXISTS (SELECT 1 FROM @O)
    SELECT 'NOTE - no order of ' + @ErpCustomerID + ' in eSyncMate. Bring one in with the Get Orders test (CT_02), then run this again.' AS [Check];

/*--- 2. HEADER: what goes to SPARS (order.header) --------------------------------------*/
SELECT '2. Header' AS Grid, X.Id AS OrderId, X.Status,
       @ErpCustomerID                                          AS CustomerID,
       JSON_VALUE(X.J, '$.legacyResourceId')                   AS CustomerPO,
       JSON_VALUE(X.J, '$.shippingAddress.name')               AS AddressName,
       JSON_VALUE(X.J, '$.shippingAddress.name')               AS CompanyName,
       JSON_VALUE(X.J, '$.shippingAddress.address1')           AS Address1,
       JSON_VALUE(X.J, '$.shippingAddress.address2')           AS Address2,
       JSON_VALUE(X.J, '$.shippingAddress.city')               AS City,
       JSON_VALUE(X.J, '$.shippingAddress.provinceCode')       AS [State],
       JSON_VALUE(X.J, '$.shippingAddress.zip')                AS Zip,
       JSON_VALUE(X.J, '$.shippingAddress.countryCodeV2')      AS Country,
       JSON_VALUE(X.J, '$.shippingAddress.phone')              AS Phone,
       'API'                                                   AS OrderTakenBy,
       JSON_VALUE(@Map, '$.order.header.ShipViaCode')          AS ShipViaCode,
       FORMAT(TRY_CONVERT(DATETIMEOFFSET, JSON_VALUE(X.J, '$.createdAt')), 'MM/dd/yyyy') AS OrderDate_ShipDate_CancelDate,
       JSON_VALUE(X.J, '$.legacyResourceId')                   AS ExternalID,
       CASE WHEN X.J IS NULL OR ISJSON(X.J) = 0 THEN 'CHECK - no valid API-JSON saved for this order'
            WHEN ISNULL(JSON_VALUE(X.J, '$.legacyResourceId'), '') = '' THEN 'CHECK - order id missing'
            WHEN ISNULL(JSON_VALUE(X.J, '$.shippingAddress.address1'), '') = '' THEN 'CHECK - no shipping address'
            WHEN ISNULL(JSON_VALUE(X.J, '$.shippingAddress.city'), '') = ''
              OR ISNULL(JSON_VALUE(X.J, '$.shippingAddress.provinceCode'), '') = ''
              OR ISNULL(JSON_VALUE(X.J, '$.shippingAddress.zip'), '') = '' THEN 'CHECK - city / state / zip missing'
            WHEN TRY_CONVERT(DATETIMEOFFSET, JSON_VALUE(X.J, '$.createdAt')) IS NULL THEN 'CHECK - order date not readable'
            WHEN ISNULL(JSON_VALUE(X.J, '$.shippingAddress.phone'), '') = '' THEN 'NOTE - no phone (sent empty)'
            ELSE 'OK' END AS [Check]
  FROM @O X
 ORDER BY X.Id;

/*--- 3. LINES: what goes to SPARS (order.detail) ---------------------------------------
      ItemID must be an item the ERP knows for this customer. Get Orders fills it from the inventory feed
      (SCSInventoryFeed.CustomerItemCode = Shopify inventory item number or SKU -> ItemId); when no feed row
      matches, the Shopify SKU itself is sent.                                                             */
SELECT '3. Lines' AS Grid, X.Id AS OrderId,
       JSON_VALUE(L.value, '$.lineNo')                                   AS APIOrderLineNo,
       JSON_VALUE(L.value, '$.itemId')                                   AS ItemID,
       JSON_VALUE(L.value, '$.sku')                                      AS ShopifySku,
       JSON_VALUE(L.value, '$.currentQuantity')                          AS OrderQty,
       JSON_VALUE(L.value, '$.discountedUnitPriceSet.shopMoney.amount')  AS UnitPrice,
       CASE WHEN ISNULL(JSON_VALUE(L.value, '$.itemId'), '') = '' THEN 'CHECK - no ItemID'
            WHEN TRY_CONVERT(INT, JSON_VALUE(L.value, '$.currentQuantity')) IS NULL
              OR TRY_CONVERT(INT, JSON_VALUE(L.value, '$.currentQuantity')) <= 0 THEN 'CHECK - quantity missing or 0'
            WHEN TRY_CONVERT(DECIMAL(18, 4), JSON_VALUE(L.value, '$.discountedUnitPriceSet.shopMoney.amount')) IS NULL THEN 'CHECK - unit price missing'
            WHEN EXISTS (SELECT 1 FROM dbo.SCSInventoryFeed F
                          WHERE F.CustomerID = @ErpCustomerID AND F.ItemId = JSON_VALUE(L.value, '$.itemId'))
                 THEN 'OK - ItemID is an item of the customer''s inventory feed'
            ELSE 'NOTE - ItemID is not in the customer''s inventory feed (the Shopify SKU is sent as it is; the ERP must know it)' END AS [Check]
  FROM @O X
 CROSS APPLY OPENJSON(X.J, '$.lineItems.nodes') L
 WHERE ISJSON(X.J) = 1
 ORDER BY X.Id, TRY_CONVERT(INT, JSON_VALUE(L.value, '$.lineNo'));

/*--- 4. Orders already sent: the body the route really posted ---------------------------*/
SELECT '4. Already sent (ERP-JSON)' AS Grid, D.OrderId, D.OrderNumber, D.Type, D.CreatedDate, LEFT(D.Data, 2000) AS Body
  FROM dbo.OrderData D
  JOIN @O X ON X.Id = D.OrderId
 WHERE D.Type IN ('ERP-JSON', 'ERP-ERROR')
 ORDER BY D.Id;
GO
