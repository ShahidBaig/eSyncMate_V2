/*==============================================================================
  V2 02 - Shopify (Task 00006): token list + the token selected on a customer

  WHY: the client credentials grant (AuthType SHOPIFYGetToken) only works for a store in the app's own Shopify
       organisation. A client's store answers "shop_not_permitted". For those stores the app is authorized once
       (authorization code grant: <API>/api/shopify/oauth/start -> <API>/api/shopify/oauth/callback) and the token Shopify
       hands over is kept in IntegrationTokens.

       A token is NOT tied to a customer when it arrives. The user selects it on the customer
       (Customers > Edit > Tokens), which saves its Id in Customers.IntegrationTokenId. All Shopify routes of that
       customer then use that token. So one customer can be pointed at a demo, UAT or production store.
       A customer with no token selected keeps using the client credentials grant (our own organisation's stores).

  WHAT IT CREATES (only what is missing):
    1. Table  IntegrationTokens            - one row per platform + account (Shopify: store) + app.
                                             Platform is general: Shopify today, TikTok etc. later.
    2. Column Customers.IntegrationTokenId - the token selected on the customer (NULL = none).
    3. If the first version of this script was run (table ShopifyStoreTokens): its rows are copied to
       IntegrationTokens and the old table is dropped.

  AccessToken / RefreshToken are written ENCRYPTED by the API (ENC:..., same key as the connectors).
  Never insert a token by hand: use the API (api/shopify/callback or api/shopify/exchangeCode).

  Needs: API / RouteWorker build with IntegrationTokenStore (2026-11-17 or later) and the UI with the Tokens field.
  Run it as often as needed. Independent of 01 - 19 and of V2 01. Changes no existing data.
==============================================================================*/

SET NOCOUNT ON;

/*--- 1. Token list ---------------------------------------------------------------*/
IF OBJECT_ID('dbo.IntegrationTokens', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.IntegrationTokens
    (
        Id                     INT IDENTITY(1,1) NOT NULL,
        Platform               NVARCHAR(50)   NOT NULL,   -- Shopify
        Name                   NVARCHAR(250)  NOT NULL,   -- shown in the Tokens list: "Shopify - <store>.myshopify.com"
        AccountKey             NVARCHAR(250)  NOT NULL,   -- Shopify: <store>.myshopify.com
        ClientId               NVARCHAR(100)  NOT NULL,   -- the app (connector ConsumerKey)
        AccessToken            NVARCHAR(MAX)  NOT NULL,   -- encrypted
        Scope                  NVARCHAR(1000) NULL,
        AccessTokenExpiryUtc   DATETIME       NULL,       -- NULL = the token does not expire
        RefreshToken           NVARCHAR(MAX)  NULL,       -- encrypted; only for an expiring token
        RefreshTokenExpiryUtc  DATETIME       NULL,
        CreatedDate            DATETIME       NOT NULL CONSTRAINT DF_IntegrationTokens_CreatedDate  DEFAULT GETDATE(),
        ModifiedDate           DATETIME       NOT NULL CONSTRAINT DF_IntegrationTokens_ModifiedDate DEFAULT GETDATE(),
        CONSTRAINT PK_IntegrationTokens PRIMARY KEY (Id),
        CONSTRAINT UQ_IntegrationTokens_Account UNIQUE (Platform, AccountKey, ClientId)
    );

    PRINT 'Table IntegrationTokens created.';
END
ELSE
    PRINT 'Table IntegrationTokens already exists - not changed.';
GO

/*--- 2. Token selected on the customer --------------------------------------------*/
IF COL_LENGTH('dbo.Customers', 'IntegrationTokenId') IS NULL
BEGIN
    ALTER TABLE dbo.Customers ADD IntegrationTokenId INT NULL;

    PRINT 'Column Customers.IntegrationTokenId added.';
END
ELSE
    PRINT 'Column Customers.IntegrationTokenId already exists - not changed.';
GO

/*--- 3. Rows of the first version of this script ----------------------------------*/
IF OBJECT_ID('dbo.ShopifyStoreTokens', 'U') IS NOT NULL
BEGIN
    BEGIN TRY
        BEGIN TRAN;

        INSERT INTO dbo.IntegrationTokens
               (Platform, Name, AccountKey, ClientId, AccessToken, Scope, AccessTokenExpiryUtc, RefreshToken, RefreshTokenExpiryUtc, CreatedDate, ModifiedDate)
        SELECT 'Shopify', 'Shopify - ' + S.ShopDomain, S.ShopDomain, S.ClientId, S.AccessToken, S.Scope, S.AccessTokenExpiryUtc,
               S.RefreshToken, S.RefreshTokenExpiryUtc, S.CreatedDate, S.ModifiedDate
          FROM dbo.ShopifyStoreTokens S
         WHERE NOT EXISTS (SELECT 1 FROM dbo.IntegrationTokens T
                            WHERE T.Platform = 'Shopify' AND T.AccountKey = S.ShopDomain AND T.ClientId = S.ClientId);

        PRINT CONCAT('Rows copied from ShopifyStoreTokens: ', @@ROWCOUNT);

        DROP TABLE dbo.ShopifyStoreTokens;

        PRINT 'Table ShopifyStoreTokens dropped (replaced by IntegrationTokens).';

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        PRINT 'ERROR: ' + ERROR_MESSAGE();
        THROW;
    END CATCH
END
GO

/*--- Verification (no token shown) -------------------------------------------------*/
SELECT '1. Tokens' AS Grid, T.Id, T.Platform, T.Name, T.AccountKey, T.Scope,
       CASE WHEN T.AccessToken LIKE 'ENC:%' THEN 'encrypted' ELSE 'CHECK - not encrypted' END AS AccessToken,
       T.AccessTokenExpiryUtc,
       CASE WHEN T.RefreshToken IS NULL THEN 'none' WHEN T.RefreshToken LIKE 'ENC:%' THEN 'encrypted' ELSE 'CHECK - not encrypted' END AS RefreshToken,
       T.CreatedDate, T.ModifiedDate
  FROM dbo.IntegrationTokens T
 ORDER BY T.Platform, T.Name;

SELECT '2. Customers with a token selected' AS Grid, C.Id, C.Name, C.ERPCustomerID, C.Marketplace,
       C.IntegrationTokenId, T.Name AS TokenName,
       CASE WHEN T.Id IS NULL THEN 'CHECK - the selected token no longer exists' ELSE 'OK' END AS [Check]
  FROM dbo.Customers C
  LEFT JOIN dbo.IntegrationTokens T ON T.Id = C.IntegrationTokenId
 WHERE C.IntegrationTokenId IS NOT NULL
 ORDER BY C.ERPCustomerID;
GO
