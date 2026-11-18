/*==============================================================================
  TEST 03 VERIFY - Shopify (Task 00006): customer -> selected token -> store of the connectors

  READ-ONLY: only SELECTs. No token or secret is shown. Works on any database (dev or client).
  Every grid has a [Check] column: OK, NOTE or CHECK.

  What must be true for the customer's Shopify routes to get a token:
    1. the store authorized the app                 -> a row in IntegrationTokens          (grid 2)
    2. that token is selected on the customer       -> Customers.IntegrationTokenId         (grid 1)
       (Customers > Edit > Tokens)
    3. the customer's Shopify connectors point at   -> connector BaseUrl = token AccountKey (grid 3)
       the SAME store as the selected token
  A customer with NO token selected uses the client credentials grant, which only works for a store in the
  app's own Shopify organisation (esyncmate-dev-store).
==============================================================================*/

SET NOCOUNT ON;

DECLARE @ErpCustomerID VARCHAR(50)   = 'WKE1000';
DECLARE @Prefix        NVARCHAR(100) = 'Shopify';

IF OBJECT_ID('dbo.IntegrationTokens', 'U') IS NULL OR COL_LENGTH('dbo.Customers', 'IntegrationTokenId') IS NULL
BEGIN
    SELECT 'CHECK - run v2\V2_02_Integration_Tokens.sql first (table IntegrationTokens / column Customers.IntegrationTokenId missing)' AS [Check];
    RETURN;
END

DECLARE @TokenId INT, @TokenStore NVARCHAR(250), @TokenName NVARCHAR(250);

SELECT @TokenId = T.Id, @TokenStore = T.AccountKey, @TokenName = T.Name
  FROM dbo.Customers C
  JOIN dbo.IntegrationTokens T ON T.Id = C.IntegrationTokenId
 WHERE C.ERPCustomerID = @ErpCustomerID;

/*--- 1. The customer and its selected token -------------------------------------*/
SELECT '1. Customer' AS Grid, C.Id, C.ERPCustomerID, C.Marketplace, C.IntegrationTokenId, T.Name AS SelectedToken, T.AccountKey AS TokenStore,
       CASE WHEN C.IntegrationTokenId IS NULL THEN 'NOTE - no token selected: client credentials grant (own organisation stores only)'
            WHEN T.Id IS NULL THEN 'CHECK - the selected token no longer exists'
            WHEN T.Platform <> 'Shopify' THEN 'CHECK - the selected token is not a Shopify token'
            ELSE 'OK' END AS [Check]
  FROM dbo.Customers C
  LEFT JOIN dbo.IntegrationTokens T ON T.Id = C.IntegrationTokenId
 WHERE C.ERPCustomerID = @ErpCustomerID;

/*--- 2. Every token in the list ---------------------------------------------------*/
SELECT '2. Tokens' AS Grid, T.Id, T.Platform, T.Name, T.AccountKey AS Store, T.Scope,
       CASE WHEN T.AccessToken LIKE 'ENC:%' THEN 'encrypted' ELSE 'CHECK - not encrypted' END AS AccessToken,
       T.AccessTokenExpiryUtc, T.ModifiedDate AS SavedOn,
       ISNULL(STUFF((SELECT ', ' + C.ERPCustomerID FROM dbo.Customers C WHERE C.IntegrationTokenId = T.Id ORDER BY C.ERPCustomerID
                        FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, ''), '') AS SelectedOnCustomers,
       CASE WHEN T.AccessTokenExpiryUtc IS NOT NULL AND T.AccessTokenExpiryUtc < GETUTCDATE() AND T.RefreshToken IS NULL
                 THEN 'CHECK - expired, the store must authorize again'
            WHEN T.Id = @TokenId THEN 'OK - selected on ' + @ErpCustomerID
            ELSE 'OK - in the list, not selected on ' + @ErpCustomerID END AS [Check]
  FROM dbo.IntegrationTokens T
 ORDER BY T.Platform, T.Name;

/*--- 3. The customer's Shopify connectors against the selected token --------------
      An encrypted connector (saved from Setup > Connectors) cannot be read by SQL.   */
SELECT '3. Connectors' AS Grid, x.Id, x.Name, x.Store AS ConnectorStore, @TokenStore AS TokenStore,
       CASE WHEN x.Fmt = 'Encrypted' THEN 'NOTE - encrypted: check the BaseUrl in Setup > Connectors (or enc_connector.py)'
            WHEN @TokenId IS NULL AND x.Store = 'esyncmate-dev-store.myshopify.com' THEN 'OK - no token selected, own organisation store'
            WHEN @TokenId IS NULL THEN 'CHECK - no token selected and the store is not in our organisation: the routes get shop_not_permitted'
            WHEN x.Store = @TokenStore THEN 'OK'
            ELSE 'CHECK - the connector store is not the store of the selected token: the routes stop with a mismatch message' END AS [Check]
  FROM (SELECT c.Id, c.Name,
               CASE WHEN ISJSON(c.Data) = 1 THEN 'Plain JSON' ELSE 'Encrypted' END AS Fmt,
               -- https://<store>/admin/api/<version>/  ->  <store>
               CASE WHEN ISJSON(c.Data) = 1 AND JSON_VALUE(c.Data, '$.BaseUrl') LIKE 'https://%/%'
                    THEN SUBSTRING(JSON_VALUE(c.Data, '$.BaseUrl'), 9, CHARINDEX('/', JSON_VALUE(c.Data, '$.BaseUrl'), 9) - 9) END AS Store
          FROM dbo.Connectors c
          JOIN dbo.ConnectorTypes t ON t.Id = c.TypeId
         WHERE t.Name = 'Shopify' AND c.Name LIKE @Prefix + ' - %') x
 ORDER BY x.Id;

/*--- 4. Warehouse -> location of the customer (must be locations of the same store) */
SELECT '4. Warehouse mapping' AS Grid, S.WHSID, S.ShipNode,
       'NOTE - must be locations of ' + ISNULL(@TokenStore, 'the connector store') AS [Check]
  FROM dbo.TargetPlusShipNodes S
 WHERE S.CustomerID = @ErpCustomerID
 ORDER BY S.WHSID;
GO
