/*==============================================================================
  03 - Shopify inventory (Task 00006): connectors

  Fill in the CONFIG block, run, then clear the secret again.
  Do not commit or e-mail a filled-in copy. The secret can also be left blank
  here and typed in Setup > Connectors.

    <P> - WHSW Inventory Get Data   eSyncMate SQL  SP    -> route source
                                    EXEC Sp_SCS_GetProductCatalog, branch ShopifyInventoryUpload (script 06)
    <P> - WHSW Inventory Upload     Shopify        REST  -> route destination
                                    BaseUrl https://<shop>.myshopify.com/admin/api/<version>/
                                    Url graphql.json, POST, AuthType SHOPIFYGetToken
                                    ConsumerKey = app Client ID, ConsumerSecret = app Client secret
                                    (Dev Dashboard > eSyncMate Connector > Settings > Credentials)

  Written as plain JSON; encrypted the first time each connector is saved from
  Setup > Connectors. Connectors.Id is NOT identity -> MAX(Id)+1.
  Run it as often as needed: a missing connector is inserted, an existing one is updated with these values.
  A secret / connection string left blank in CONFIG keeps the value already stored. An encrypted connector
  (saved from Setup > Connectors) cannot be read by SQL: it is left as it is and listed at the end.
  Later scripts COPY these two connectors: after changing a value here, run the later scripts again too.
==============================================================================*/

SET NOCOUNT ON;

/*==============================================================================
  CONFIG
==============================================================================*/
DECLARE @ErpCustomerID       VARCHAR(50)    = 'WKE1000';      -- same as script 02
DECLARE @Prefix              NVARCHAR(100)  = 'Shopify';         -- connector / route names start with this

--- eSyncMate DB - same string the other eSyncMate SQL connectors use
---   format: Server=<server>,<port>;Database=ESYNCMATE;UID=<user>;PWD=<password>;
DECLARE @DbConnectionString  NVARCHAR(500)  = '';

--- Shopify store + app
--- REQUIRED, left empty on purpose so a re-run can never point this customer at another store / app
---   dev was: esyncmate-dev-store.myshopify.com / 2878ddf0df5aeb8e4839b6392e701d02
--- DEMO VALUES (2026-11-11): the eSoftage demo store and its app "eSyncMate Connector".
--- TEMPORARY - replace @ShopDomain / @ClientId with the client's own store and app, then run 03, 10, 13, 15 and 17 again
--- (they copy this connector). Until then nobody saves a Shopify connector in Setup > Connectors (saving encrypts it).
DECLARE @ShopDomain          NVARCHAR(200)  = 'esyncmate-dev-store.myshopify.com';  -- client: e.g. walker-edison.myshopify.com
DECLARE @ApiVersion          NVARCHAR(20)   = '2026-10';
DECLARE @ClientId            NVARCHAR(200)  = '2878ddf0df5aeb8e4839b6392e701d02';   -- the app's Client ID (Dev Dashboard > app > Settings)
DECLARE @ClientSecret        NVARCHAR(500)  = '';                                   -- fill here or in the UI (blank keeps the stored one)
/*==============================================================================*/

BEGIN TRY
    IF LTRIM(RTRIM(@ShopDomain)) = '' OR LTRIM(RTRIM(@ClientId)) = ''
        THROW 51373, 'Fill @ShopDomain and @ClientId in the CONFIG block.', 1;

    IF @ShopDomain NOT LIKE '%.myshopify.com'
        THROW 51370, '@ShopDomain must be the <store>.myshopify.com domain, not a custom domain.', 1;

    DECLARE @ShopifyTypeId INT = (SELECT Id FROM dbo.ConnectorTypes WHERE Name = 'Shopify');
    IF @ShopifyTypeId IS NULL THROW 51371, 'ConnectorType Shopify missing. Run script 01 first.', 1;

    DECLARE @c TABLE (Seq INT IDENTITY(1,1), TypeId INT, Nm NVARCHAR(200), Payload NVARCHAR(MAX));

    DECLARE @Shopify NVARCHAR(MAX) =
        '{"ConnectivityType":"Rest","BaseUrl":"","AuthType":"SHOPIFYGetToken","Host":"",'
      + '"Url":"graphql.json","Method":"POST","Headers":[],"Parmeters":[],"BodyFormat":"JSON","ConsumerKey":"","ConsumerSecret":"",'
      + '"Token":"","TokenSecret":"","Realm":"","CustomerID":""}';

    DECLARE @Sql NVARCHAR(MAX) =
        '{"ConnectivityType":"SqlServer","CommandType":"SP",'
      + '"Command":"EXEC Sp_SCS_GetProductCatalog @p_CustomerID = ''@CUSTOMERID@'', @p_RouteTypeID = ''@ROUTETYPEID@'', @p_UserNo = @USERNO@",'
      + '"KeyFieldName":"","DataFieldName":"","CustomerID":"","JsonDataCollectionName":"","ConnectionString":""}';

    SET @Shopify = JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(@Shopify,
                       '$.BaseUrl', 'https://' + @ShopDomain + '/admin/api/' + @ApiVersion + '/'),
                       '$.ConsumerKey', @ClientId), '$.ConsumerSecret', @ClientSecret), '$.CustomerID', @ErpCustomerID);

    SET @Sql = JSON_MODIFY(JSON_MODIFY(@Sql, '$.ConnectionString', @DbConnectionString), '$.CustomerID', @ErpCustomerID);

    INSERT INTO @c (TypeId, Nm, Payload) VALUES
      (8,              @Prefix + ' - WHSW Inventory Get Data', @Sql),
      (@ShopifyTypeId, @Prefix + ' - WHSW Inventory Upload',   @Shopify);

    IF EXISTS (SELECT 1 FROM @c WHERE ISJSON(Payload) = 0)
        THROW 51372, 'A connector payload is not valid JSON.', 1;

    BEGIN TRAN;

    /*--- Upsert (same block in every Task 00005 / 00006 script) ----------------
      A new name is inserted. An existing plain-JSON connector gets this payload, but a secret / connection
      string left blank here keeps its stored value. An encrypted connector (saved from Setup > Connectors)
      cannot be read by SQL: it is left as it is and listed below - change it in the UI.                      */
    DECLARE @u_Nm NVARCHAR(200), @u_TypeId INT, @u_New NVARCHAR(MAX), @u_Old NVARCHAR(MAX), @u_Ins INT = 0, @u_Upd INT = 0;
    DECLARE @u_Encrypted TABLE (Name NVARCHAR(200));
    DECLARE u_cc CURSOR LOCAL FAST_FORWARD FOR SELECT Nm, TypeId, Payload FROM @c ORDER BY Seq;

    OPEN u_cc;
    FETCH NEXT FROM u_cc INTO @u_Nm, @u_TypeId, @u_New;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.Connectors WHERE Name = @u_Nm)
        BEGIN
            INSERT INTO dbo.Connectors (Id, TypeId, Name, Data, CreatedDate, CreatedBy)
            SELECT ISNULL(MAX(Id), 0) + 1, @u_TypeId, @u_Nm, @u_New, GETDATE(), 1 FROM dbo.Connectors WITH (UPDLOCK, HOLDLOCK);
            SET @u_Ins += 1;
        END
        ELSE
        BEGIN
            SET @u_Old = ISNULL((SELECT TOP 1 Data FROM dbo.Connectors WHERE Name = @u_Nm), '{}');

            IF ISJSON(@u_Old) = 0
                INSERT INTO @u_Encrypted VALUES (@u_Nm);
            ELSE
            BEGIN
                IF ISNULL(JSON_VALUE(@u_New, '$.ConnectionString'), '') = '' AND ISNULL(JSON_VALUE(@u_Old, '$.ConnectionString'), '') <> ''
                    SET @u_New = JSON_MODIFY(@u_New, '$.ConnectionString', JSON_VALUE(@u_Old, '$.ConnectionString'));
                IF ISNULL(JSON_VALUE(@u_New, '$.ConsumerKey'), '') = '' AND ISNULL(JSON_VALUE(@u_Old, '$.ConsumerKey'), '') <> ''
                    SET @u_New = JSON_MODIFY(@u_New, '$.ConsumerKey', JSON_VALUE(@u_Old, '$.ConsumerKey'));
                IF ISNULL(JSON_VALUE(@u_New, '$.ConsumerSecret'), '') = '' AND ISNULL(JSON_VALUE(@u_Old, '$.ConsumerSecret'), '') <> ''
                    SET @u_New = JSON_MODIFY(@u_New, '$.ConsumerSecret', JSON_VALUE(@u_Old, '$.ConsumerSecret'));
                IF ISNULL(JSON_VALUE(@u_New, '$.Token'), '') = '' AND ISNULL(JSON_VALUE(@u_Old, '$.Token'), '') <> ''
                    SET @u_New = JSON_MODIFY(@u_New, '$.Token', JSON_VALUE(@u_Old, '$.Token'));
                IF ISNULL(JSON_VALUE(@u_New, '$.TokenSecret'), '') = '' AND ISNULL(JSON_VALUE(@u_Old, '$.TokenSecret'), '') <> ''
                    SET @u_New = JSON_MODIFY(@u_New, '$.TokenSecret', JSON_VALUE(@u_Old, '$.TokenSecret'));
                IF ISNULL(JSON_VALUE(@u_New, '$.Password'), '') = '' AND ISNULL(JSON_VALUE(@u_Old, '$.Password'), '') <> ''
                    SET @u_New = JSON_MODIFY(@u_New, '$.Password', JSON_VALUE(@u_Old, '$.Password'));
                IF ISNULL(JSON_VALUE(@u_New, '$.BaseUrl'), '') = '' AND ISNULL(JSON_VALUE(@u_Old, '$.BaseUrl'), '') <> ''
                    SET @u_New = JSON_MODIFY(@u_New, '$.BaseUrl', JSON_VALUE(@u_Old, '$.BaseUrl'));

                UPDATE dbo.Connectors SET TypeId = @u_TypeId, Data = @u_New, ModifiedDate = GETDATE(), ModifiedBy = 1 WHERE Name = @u_Nm;
                SET @u_Upd += 1;
            END
        END

        FETCH NEXT FROM u_cc INTO @u_Nm, @u_TypeId, @u_New;
    END

    CLOSE u_cc;
    DEALLOCATE u_cc;

    DECLARE @u_Enc INT = (SELECT COUNT(*) FROM @u_Encrypted);   -- PRINT takes no subquery

    PRINT CONCAT('Connectors inserted: ', @u_Ins, ', updated: ', @u_Upd, ', encrypted and NOT changed: ', @u_Enc);

    SELECT Name AS EncryptedConnector_NotChanged_EditInUI FROM @u_Encrypted;

    COMMIT TRAN;

    /*--- Verification (no secrets shown) ------------------------------------*/
    SELECT c.Id, c.TypeId, c.Name,
           CASE WHEN c.Data LIKE 'ENC:%' THEN 'Encrypted' ELSE 'Plain JSON' END AS Storage,
           CASE WHEN ISJSON(c.Data) = 1 THEN JSON_VALUE(c.Data, '$.ConnectivityType') END AS ConnectivityType,
           CASE WHEN ISJSON(c.Data) = 1 THEN JSON_VALUE(c.Data, '$.BaseUrl')          END AS BaseUrl,
           CASE WHEN ISJSON(c.Data) = 1 THEN JSON_VALUE(c.Data, '$.AuthType')         END AS AuthType,
           CASE WHEN ISJSON(c.Data) = 1 THEN JSON_VALUE(c.Data, '$.CustomerID')       END AS CustomerID,
           CASE WHEN ISJSON(c.Data) = 1 THEN JSON_VALUE(c.Data, '$.Command')          END AS Command,
           CASE WHEN ISJSON(c.Data) = 0 THEN NULL
                WHEN JSON_VALUE(c.Data, '$.ConnectivityType') = 'Rest'
                     THEN CASE WHEN ISNULL(JSON_VALUE(c.Data, '$.ConsumerSecret'), '') = '' THEN 'Client secret' END
                ELSE CASE WHEN ISNULL(JSON_VALUE(c.Data, '$.ConnectionString'), '') = '' THEN 'ConnectionString' END
           END AS StillToFillInFromUI
      FROM dbo.Connectors c JOIN @c x ON x.Nm = c.Name
     ORDER BY c.Id;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
