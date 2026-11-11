/*==============================================================================
  04 - Shopify (Task 00006): Full Inventory Feed (ERP -> eSyncMate), like every other customer

    <P> - Fetch Full Inventory from ERP     RouteType 7 (SCSFullInventoryFeedRoute, existing code)
                                            SPARS -> eSyncMate, Daily @FullFeedTime
         source  <P> - SPARS Full Inventory Feed   SPARS REST (TypeId 7), Url @SparsFullInvUrl
         dest    <P> - eSyncMate Inventory feed    eSyncMate SQL (TypeId 8), EXEC SP_InsertSCSInventoryFeed
                                                   (copy of "<P> - WHSW Inventory Get Data" from script 03: same DB)

  The SPARS key / secret are typed ONLY here: scripts 05, 11, 12 and 14 copy this SPARS connector.
  Fill the CONFIG block, run, then clear the secret again. Do not commit or e-mail a filled-in copy.
  Needs scripts 02 and 03. Run it as often as needed: missing -> inserted, existing -> updated (a SPARS value
  left blank keeps the stored one; an encrypted connector is listed and left alone; an existing route keeps its
  Status, schedule and JobID). After changing the SPARS values, run 05, 11, 12, 14 and 18 again (they copy it).
==============================================================================*/

SET NOCOUNT ON;

/*==============================================================================
  CONFIG
==============================================================================*/
DECLARE @ErpCustomerID       NVARCHAR(50)  = 'WKE1000';   -- same as scripts 02 / 03
DECLARE @Prefix              NVARCHAR(100) = 'Shopify';

--- SPARS (ERP) REST endpoint - copy from any existing SPARS connector
DECLARE @SparsBaseUrl        NVARCHAR(500) = '';            -- e.g. https://<host>/SPARS.API/api/service/
DECLARE @SparsConsumerKey    NVARCHAR(500) = '';
DECLARE @SparsConsumerSecret NVARCHAR(500) = '';
DECLARE @SparsFullInvUrl     NVARCHAR(200) = 'full-inventory-feed';

DECLARE @FullFeedTime        VARCHAR(10)   = '06:45';        -- Daily, server time
/*==============================================================================*/

BEGIN TRY
    DECLARE @SparsId   INT = (SELECT Id FROM dbo.Customers WHERE Name = 'SPARS');
    DECLARE @EsyncId   INT = (SELECT Id FROM dbo.Customers WHERE Name = 'eSyncMate');
    DECLARE @PartnerId INT = (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = @ErpCustomerID);
    DECLARE @PgEsync   INT = (SELECT Id FROM dbo.PartnerGroups WHERE SourcePartyId = @PartnerId AND DestinationPartyId = @EsyncId);
    DECLARE @SqlFrom   NVARCHAR(MAX) = (SELECT Data FROM dbo.Connectors WHERE Name = @Prefix + ' - WHSW Inventory Get Data');

    IF @SparsId IS NULL OR @EsyncId IS NULL THROW 51500, 'Customers [SPARS] / [eSyncMate] not found.', 1;
    IF @PartnerId IS NULL OR @PgEsync IS NULL THROW 51501, 'Customer / partner groups missing. Run script 02 first.', 1;
    IF @SqlFrom IS NULL THROW 51502, 'Connector "<P> - WHSW Inventory Get Data" missing. Run script 03 first.', 1;
    IF ISJSON(@SqlFrom) = 0 THROW 51503, '"<P> - WHSW Inventory Get Data" is encrypted (saved from Setup > Connectors). Create "<P> - eSyncMate Inventory feed" by hand.', 1;

    -- values go in through JSON_MODIFY, which escapes them
    -- Method GET: SPARS full-inventory-feed / differential-inventory-feed answer a POST with HTTP 405
    -- ("The requested resource does not support http method 'POST'"); every other customer's feed connector is GET.
    -- Scripts 11, 12, 14 and 18 copy this connector for POST endpoints and set Method POST themselves.
    DECLARE @Spars NVARCHAR(MAX) = '{"ConnectivityType":"Rest","AuthType":"SPARSGetToken","BaseUrl":"","ConsumerKey":"","ConsumerSecret":"","Url":"","Method":"GET","BodyFormat":"Json","CustomerID":"","KeyFieldName":"","DataFieldName":"","JsonDataCollectionName":""}';

    SET @Spars = JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(@Spars, '$.BaseUrl', @SparsBaseUrl), '$.ConsumerKey', @SparsConsumerKey),
                                         '$.ConsumerSecret', @SparsConsumerSecret), '$.CustomerID', @ErpCustomerID), '$.Url', @SparsFullInvUrl);

    DECLARE @c TABLE (Seq INT, Nm NVARCHAR(200), TypeId INT, Payload NVARCHAR(MAX));

    INSERT INTO @c VALUES
      (1, @Prefix + ' - SPARS Full Inventory Feed', 7, @Spars),
      -- destination of both feed routes; @CUSTOMERID@ / @BATCHID@ are replaced by SCSFullInventoryFeedRoute
      (2, @Prefix + ' - eSyncMate Inventory feed',  8, JSON_MODIFY(@SqlFrom, '$.Command', 'EXEC SP_InsertSCSInventoryFeed @p_CustomerID = ''@CUSTOMERID@'', @p_BatchID = @BATCHID@'));

    IF EXISTS (SELECT 1 FROM @c WHERE ISJSON(Payload) = 0) THROW 51504, 'A connector payload is not valid JSON.', 1;

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

    DECLARE @RouteName VARCHAR(100) = @Prefix + ' - Fetch Full Inventory from ERP';

    -- an existing route gets these parties / connectors / group; its Status, schedule and JobID are left alone
    IF EXISTS (SELECT 1 FROM dbo.Routes WHERE Name = @RouteName)
    BEGIN
        UPDATE dbo.Routes
           SET TypeId = 7, SourcePartyId = @SparsId, DestinationPartyId = @EsyncId,
               SourceConnectorId = (SELECT Id FROM dbo.Connectors WHERE Name = @Prefix + ' - SPARS Full Inventory Feed'),
               DestinationConnectorId = (SELECT Id FROM dbo.Connectors WHERE Name = @Prefix + ' - eSyncMate Inventory feed'),
               MapId = 0, PartyGroupId = @PgEsync, RouteGroup = 'Inventory', CustomerName = @ErpCustomerID,
               ModifiedDate = GETDATE(), ModifiedBy = 1
         WHERE Name = @RouteName;

        PRINT 'Updated route ' + @RouteName + ' (Status / schedule / JobID left as they are)';
    END
    ELSE
    BEGIN
        INSERT INTO dbo.Routes
            (Id, TypeId, Status, SourcePartyId, DestinationPartyId, SourceConnectorId, DestinationConnectorId, MapId, PartyGroupId,
             CreatedDate, CreatedBy, FrequencyType, StartDate, EndDate, RepeatCount, WeekDays, OnDay, ExecutionTime, JobID,
             Name, RouteGroup, CustomerName)
        SELECT ISNULL(MAX(Id), 0) + 1, 7, 'In-Active', @SparsId, @EsyncId,
               (SELECT Id FROM dbo.Connectors WHERE Name = @Prefix + ' - SPARS Full Inventory Feed'),
               (SELECT Id FROM dbo.Connectors WHERE Name = @Prefix + ' - eSyncMate Inventory feed'),
               0, @PgEsync, GETDATE(), 1, 'Daily', CAST(GETDATE() AS DATE), NULL, 0, '', '', @FullFeedTime, NULL,
               @RouteName, 'Inventory', @ErpCustomerID
          FROM dbo.Routes WITH (UPDLOCK, HOLDLOCK);

        PRINT 'Created route ' + @RouteName;
    END

    COMMIT TRAN;

    /*--- Verification (no secrets shown) ----------------------------------*/
    SELECT c.Id, c.TypeId, c.Name,
           CASE WHEN ISJSON(c.Data) = 1 THEN 'Plain JSON' ELSE 'Encrypted' END AS Storage,
           CASE WHEN ISJSON(c.Data) = 1 THEN ISNULL(JSON_VALUE(c.Data, '$.Command'), JSON_VALUE(c.Data, '$.Url')) END AS CommandOrUrl,
           CASE WHEN ISJSON(c.Data) = 0 THEN NULL
                WHEN JSON_VALUE(c.Data, '$.ConnectivityType') = 'Rest'
                     THEN CASE WHEN ISNULL(JSON_VALUE(c.Data, '$.BaseUrl'), '') = '' OR ISNULL(JSON_VALUE(c.Data, '$.ConsumerSecret'), '') = '' THEN 'SPARS BaseUrl / secret BLANK' ELSE 'filled' END
                ELSE CASE WHEN ISNULL(JSON_VALUE(c.Data, '$.ConnectionString'), '') = '' THEN 'ConnectionString BLANK' ELSE 'filled' END
           END AS Values_
      FROM dbo.Connectors c JOIN @c x ON x.Nm = c.Name
     ORDER BY c.Id;

    -- VW_Routes uses INNER JOINs: the route must show up here too
    SELECT Id, Name, TypeId, Status, FrequencyType, ExecutionTime FROM dbo.VW_Routes WHERE Name = @RouteName;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO

/*==============================================================================
  POST-STEPS
  Test Run "<P> - Fetch Full Inventory from ERP" -> SCSInventoryFeed rows for the customer (also fills
  CustomerItemCode -> ItemId, which Get Orders uses for the line items).
  Activate from the UI (Setup > Routes or a Flow), never with an UPDATE - that leaves JobID NULL.
==============================================================================*/
