/*==============================================================================
  Script : 75_Add_Customers_OAuth_Fields.sql
  Date   : 2026-07-23
  Purpose: Add per-customer OAuth 2.0 authentication fields to the Customers
           table for Target Plus new OAuth (Authorization Code flow + rotating
           refresh token).

           - UseNewAuthentication : flag that gates NEW OAuth vs LEGACY header auth
                                    (default 0 = legacy, so production is unaffected).
           - OAuthClientId/Secret : static per-seller credentials.
           - OAuthAuthUrl/TokenUrl: OAuth endpoints (per seller; optional).
           - OAuthRefreshToken     : rotating refresh token (~30 days) - updated on
                                     every refresh (rotating), single source of truth.
           - OAuthAccessToken      : cached access token (~8 hours).

           Token/secret columns hold values ENCRYPTED by the app (ENC: prefix),
           so column sizes account for encryption overhead.

  Idempotent: safe to run multiple times.
==============================================================================*/

SET NOCOUNT ON;
GO

/*---- 1. Flag: new OAuth on/off per customer (default OFF = legacy headers) ----*/
IF COL_LENGTH('dbo.Customers', 'UseNewAuthentication') IS NULL
    ALTER TABLE dbo.Customers
        ADD UseNewAuthentication BIT NOT NULL
            CONSTRAINT DF_Customers_UseNewAuthentication DEFAULT (0);
GO

/*---- 2. OAuth static credentials (per seller) ----*/
IF COL_LENGTH('dbo.Customers', 'OAuthClientId') IS NULL
    ALTER TABLE dbo.Customers ADD OAuthClientId NVARCHAR(500) NULL;
GO
IF COL_LENGTH('dbo.Customers', 'OAuthClientSecret') IS NULL
    ALTER TABLE dbo.Customers ADD OAuthClientSecret NVARCHAR(1000) NULL;   -- encrypted
GO

/*---- 3. OAuth endpoints (per seller; may also be app constants) ----*/
IF COL_LENGTH('dbo.Customers', 'OAuthAuthUrl') IS NULL
    ALTER TABLE dbo.Customers ADD OAuthAuthUrl NVARCHAR(500) NULL;
GO
IF COL_LENGTH('dbo.Customers', 'OAuthTokenUrl') IS NULL
    ALTER TABLE dbo.Customers ADD OAuthTokenUrl NVARCHAR(500) NULL;
GO

/*---- 4. Rotating refresh token (~30 days) - single source of truth ----*/
IF COL_LENGTH('dbo.Customers', 'OAuthRefreshToken') IS NULL
    ALTER TABLE dbo.Customers ADD OAuthRefreshToken NVARCHAR(2000) NULL;   -- encrypted, rotates each refresh
GO
IF COL_LENGTH('dbo.Customers', 'OAuthRefreshTokenExpiry') IS NULL
    ALTER TABLE dbo.Customers ADD OAuthRefreshTokenExpiry DATETIME NULL;   -- store UTC
GO

/*---- 5. Cached access token (~8 hours) ----*/
IF COL_LENGTH('dbo.Customers', 'OAuthAccessToken') IS NULL
    ALTER TABLE dbo.Customers ADD OAuthAccessToken NVARCHAR(MAX) NULL;     -- encrypted; JWT can be long
GO
IF COL_LENGTH('dbo.Customers', 'OAuthAccessTokenExpiry') IS NULL
    ALTER TABLE dbo.Customers ADD OAuthAccessTokenExpiry DATETIME NULL;    -- store UTC
GO

/*---- 6. Audit: when tokens were last refreshed ----*/
IF COL_LENGTH('dbo.Customers', 'OAuthTokenUpdatedDate') IS NULL
    ALTER TABLE dbo.Customers ADD OAuthTokenUpdatedDate DATETIME NULL;
GO

/*---- 7. VW_Customers: expose flag + derived Authorized status (NO tokens) ----*/
ALTER VIEW [dbo].[VW_Customers]
    AS
    SELECT
        [Id], [Name], [ERPCustomerID], [ISACustomerID], [ISA810ReceiverId],
        [ISA856ReceiverId], [Marketplace], [CreatedDate], [CreatedBy],
        [ModifiedDate], [ModifiedBy],
        [UseNewAuthentication],
        CAST(CASE
                 WHEN [OAuthRefreshToken] IS NOT NULL
                  AND ([OAuthRefreshTokenExpiry] IS NULL OR [OAuthRefreshTokenExpiry] > GETUTCDATE())
                 THEN 1 ELSE 0
             END AS BIT) AS OAuthAuthorized
    FROM Customers;
GO
