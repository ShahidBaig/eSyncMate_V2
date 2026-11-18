/*==============================================================================
  CT 02 APPLY - Shopify (Task 00006): Get Orders test AT THE CLIENT against OUR demo store

  WHERE: the CLIENT database (customer WKE1000). TEMPORARY: undo it with CT_02_Revert.sql.
  Independent of CT_01 (inventory test): either can be applied / reverted without the other.

  WHAT IT CHANGES (one transaction):
    0. BACKUP first, into two new tables (CT_02_Revert uses them and drops them):
         ZDB_CT02_Connectors = the connector "Shopify - Get Orders" as it is now
         ZDB_CT02_Baseline   = the highest Orders.Id at this moment, so the revert can delete exactly the
                               orders the test brings in, and nothing older
       If these tables already exist the script STOPS (already applied: run CT_02_Revert.sql first).
    1. ONLY the connector "Shopify - Get Orders": the store in BaseUrl -> esyncmate-dev-store.
  NOT CHANGED: routes, Flows, Status, the other connectors, the warehouse mapping, the token selection.

  IT STOPS, changing nothing, when:
    - the token selected on WKE1000 is not the one of esyncmate-dev-store
    - a route that sends orders on is Active ("Shopify - Place Orders in ERP"): a demo order must never reach the ERP
    - the Get Orders connector is encrypted (saved from Setup > Connectors): SQL cannot change it

  AFTER IT: run CT_02_Verify.sql, then Test Run ONLY the route "Shopify - Get Orders".
  KEEP "Shopify - Place Orders in ERP" In-Active until CT_02_Revert.sql has been run.
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

/*==============================================================================
  CONFIG
==============================================================================*/
DECLARE @ErpCustomerID VARCHAR(50)   = 'WKE1000';
DECLARE @DemoStore     NVARCHAR(200) = 'esyncmate-dev-store.myshopify.com';
DECLARE @ConnName      NVARCHAR(200) = 'Shopify - Get Orders';
DECLARE @PlaceOrders   NVARCHAR(200) = 'Shopify - Place Orders in ERP';

--- ONLY when the connector is ENCRYPTED (saved from Setup > Connectors; SQL can neither read nor change it):
--- open Setup > Connectors > "Shopify - Get Orders" and look at Base URL.
---   It must still show  https://walker-edison.myshopify.com/admin/api/2026-10/  (do NOT change it yet).
---   Then set this to 1 and run the script: it makes the backup + baseline, and you change the Base URL in the UI after.
DECLARE @EncryptedAndStillOnClientStore BIT = 0;
/*==============================================================================*/

BEGIN TRY
    /*--- Checks (nothing is changed before all of them pass) --------------------*/
    DECLARE @CustomerId INT = (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = @ErpCustomerID);
    IF @CustomerId IS NULL
        THROW 51700, 'Customer WKE1000 not found.', 1;

    IF OBJECT_ID('dbo.ZDB_CT02_Connectors', 'U') IS NOT NULL OR OBJECT_ID('dbo.ZDB_CT02_Baseline', 'U') IS NOT NULL
        THROW 51701, 'The test is already applied (backup tables ZDB_CT02_* exist). Run CT_02_Revert.sql first.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Customers C JOIN dbo.IntegrationTokens T ON T.Id = C.IntegrationTokenId
                    WHERE C.Id = @CustomerId AND T.Platform = 'Shopify' AND T.AccountKey = @DemoStore)
        THROW 51702, 'The token selected on WKE1000 is not the token of esyncmate-dev-store. Select it in Customers > Edit > Tokens.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Routes WHERE Name = @PlaceOrders AND Status = 'Active')
        THROW 51703, 'Route "Shopify - Place Orders in ERP" is Active. Set it In-Active first: a demo order must never reach the ERP. Nothing changed.', 1;

    DECLARE @ConnId INT, @ConnCount INT;
    SELECT @ConnCount = COUNT(*), @ConnId = MAX(Id) FROM dbo.Connectors WHERE Name = @ConnName;
    IF @ConnCount <> 1
        THROW 51704, 'Connector "Shopify - Get Orders" not found exactly once.', 1;

    DECLARE @Data NVARCHAR(MAX) = (SELECT Data FROM dbo.Connectors WHERE Id = @ConnId);
    DECLARE @IsPlain BIT = CASE WHEN ISJSON(@Data) = 1 THEN 1 ELSE 0 END;

    IF @IsPlain = 0 AND @EncryptedAndStillOnClientStore = 0
        THROW 51705, 'The Get Orders connector is encrypted. Check in Setup > Connectors that its Base URL still shows walker-edison, set @EncryptedAndStillOnClientStore = 1 in CONFIG and run again. Nothing changed.', 1;

    DECLARE @OldBaseUrl NVARCHAR(500) = CASE WHEN @IsPlain = 1 THEN JSON_VALUE(@Data, '$.BaseUrl') END;
    IF @IsPlain = 1 AND @OldBaseUrl NOT LIKE 'https://%.myshopify.com/admin/api/%'
        THROW 51706, 'The Get Orders connector BaseUrl does not look like https://<store>.myshopify.com/admin/api/<version>/. Nothing changed.', 1;

    -- keep the API version, change only the store
    DECLARE @NewBaseUrl NVARCHAR(500) = CASE WHEN @IsPlain = 1
                                             THEN 'https://' + @DemoStore + SUBSTRING(@OldBaseUrl, CHARINDEX('/admin/api/', @OldBaseUrl), 500) END;

    BEGIN TRAN;

    /*--- 0. Backup ---------------------------------------------------------------*/
    SELECT C.Id, C.Name, C.Data, C.ModifiedDate, C.ModifiedBy, GETDATE() AS BackupDate
      INTO dbo.ZDB_CT02_Connectors
      FROM dbo.Connectors C
     WHERE C.Id = @ConnId;

    SELECT @CustomerId AS CustomerId, ISNULL((SELECT MAX(Id) FROM dbo.Orders), 0) AS MaxOrderId, GETDATE() AS AppliedDate
      INTO dbo.ZDB_CT02_Baseline;

    /*--- 1. Get Orders connector -> demo store ------------------------------------*/
    -- an encrypted connector is only backed up here (the encrypted value is restored as it is by the revert);
    -- its Base URL is changed in Setup > Connectors
    IF @IsPlain = 1
        UPDATE dbo.Connectors
           SET Data = JSON_MODIFY(Data, '$.BaseUrl', @NewBaseUrl), ModifiedDate = GETDATE(), ModifiedBy = 1
         WHERE Id = @ConnId;

    COMMIT TRAN;

    PRINT 'CT 02 applied. Backup in ZDB_CT02_Connectors / ZDB_CT02_Baseline. Undo with CT_02_Revert.sql.';

    IF @IsPlain = 0
        PRINT 'NOW in Setup > Connectors > "Shopify - Get Orders": change Base URL to https://' + @DemoStore + '/admin/api/2026-10/ and save.';

    /*--- Result -------------------------------------------------------------------*/
    SELECT '1. Get Orders connector' AS Grid, @ConnId AS Id, @ConnName AS Name,
           CASE WHEN @IsPlain = 1 THEN @OldBaseUrl ELSE 'encrypted - backed up as it is' END AS BaseUrlBefore,
           CASE WHEN @IsPlain = 1 THEN JSON_VALUE(Data, '$.BaseUrl')
                ELSE 'NOT changed by SQL - change Base URL in Setup > Connectors to the demo store' END AS BaseUrlNow
      FROM dbo.Connectors WHERE Id = @ConnId;

    SELECT '2. Baseline' AS Grid, CustomerId, MaxOrderId, AppliedDate FROM dbo.ZDB_CT02_Baseline;

    SELECT '3. Orders of WKE1000 before the test' AS Grid, COUNT(*) AS Orders_ FROM dbo.Orders WHERE CustomerId = @CustomerId;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR (nothing changed): ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
