/*==============================================================================
  05 - Shopify (Task 00006): Differential Inventory Feed (ERP -> eSyncMate), like every other customer

    <P> - Fetch Differential Inventory from ERP   RouteType 8 (existing code), SPARS -> eSyncMate, Minutely @RepeatMinutes
         source  <P> - SPARS Differential Inventory Feed   copy of "<P> - SPARS Full Inventory Feed" (script 04), Url @SparsDiffInvUrl
         dest    <P> - eSyncMate Inventory feed            from script 04

  No secret here. Needs script 04. Run it as often as needed: missing -> inserted, existing -> updated
  (an encrypted connector is listed and left alone; an existing route keeps its Status, schedule and JobID).
==============================================================================*/

SET NOCOUNT ON;

/*==============================================================================
  CONFIG
==============================================================================*/
DECLARE @ErpCustomerID   NVARCHAR(50)  = 'WKE1000';   -- same as scripts 02 / 03 / 04
DECLARE @Prefix          NVARCHAR(100) = 'Shopify';
DECLARE @SparsDiffInvUrl NVARCHAR(200) = 'differential-inventory-feed';
DECLARE @RepeatMinutes   INT           = 15;
/*==============================================================================*/

BEGIN TRY
    DECLARE @SparsId   INT = (SELECT Id FROM dbo.Customers WHERE Name = 'SPARS');
    DECLARE @EsyncId   INT = (SELECT Id FROM dbo.Customers WHERE Name = 'eSyncMate');
    DECLARE @PartnerId INT = (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = @ErpCustomerID);
    DECLARE @PgSpars   INT = (SELECT Id FROM dbo.PartnerGroups WHERE SourcePartyId = @PartnerId AND DestinationPartyId = @SparsId);
    DECLARE @SparsFrom NVARCHAR(MAX) = (SELECT Data FROM dbo.Connectors WHERE Name = @Prefix + ' - SPARS Full Inventory Feed');

    IF @PartnerId IS NULL OR @PgSpars IS NULL THROW 51510, 'Customer / partner groups missing. Run script 02 first.', 1;
    IF @SparsFrom IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.Connectors WHERE Name = @Prefix + ' - eSyncMate Inventory feed')
        THROW 51511, 'Connectors "<P> - SPARS Full Inventory Feed" / "<P> - eSyncMate Inventory feed" missing. Run script 04 first.', 1;
    IF ISJSON(@SparsFrom) = 0 THROW 51512, '"<P> - SPARS Full Inventory Feed" is encrypted (saved from Setup > Connectors). Create the Differential connector by hand.', 1;

    DECLARE @ConnName  NVARCHAR(200) = @Prefix + ' - SPARS Differential Inventory Feed';
    DECLARE @RouteName VARCHAR(100)  = @Prefix + ' - Fetch Differential Inventory from ERP';

    DECLARE @c TABLE (Seq INT, Nm NVARCHAR(200), TypeId INT, Payload NVARCHAR(MAX));

    -- Method GET like the Full feed: SPARS answers a POST to the inventory feeds with HTTP 405
    INSERT INTO @c VALUES (1, @ConnName, 7, JSON_MODIFY(JSON_MODIFY(@SparsFrom, '$.Url', @SparsDiffInvUrl), '$.Method', 'GET'));

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

    -- an existing route gets these parties / connectors / group; its Status, schedule and JobID are left alone
    IF EXISTS (SELECT 1 FROM dbo.Routes WHERE Name = @RouteName)
    BEGIN
        UPDATE dbo.Routes
           SET TypeId = 8, SourcePartyId = @SparsId, DestinationPartyId = @EsyncId,
               SourceConnectorId = (SELECT Id FROM dbo.Connectors WHERE Name = @ConnName),
               DestinationConnectorId = (SELECT Id FROM dbo.Connectors WHERE Name = @Prefix + ' - eSyncMate Inventory feed'),
               MapId = 0, PartyGroupId = @PgSpars, RouteGroup = 'Inventory', CustomerName = @ErpCustomerID,
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
        SELECT ISNULL(MAX(Id), 0) + 1, 8, 'In-Active', @SparsId, @EsyncId,
               (SELECT Id FROM dbo.Connectors WHERE Name = @ConnName),
               (SELECT Id FROM dbo.Connectors WHERE Name = @Prefix + ' - eSyncMate Inventory feed'),
               0, @PgSpars, GETDATE(), 1, 'Minutely', CAST(GETDATE() AS DATE), NULL, @RepeatMinutes, '', '', '', NULL,
               @RouteName, 'Inventory', @ErpCustomerID
          FROM dbo.Routes WITH (UPDLOCK, HOLDLOCK);

        PRINT 'Created route ' + @RouteName;
    END

    COMMIT TRAN;

    SELECT Id, TypeId, Name, CASE WHEN ISJSON(Data) = 1 THEN JSON_VALUE(Data, '$.Url') END AS Url FROM dbo.Connectors WHERE Name = @ConnName;
    -- VW_Routes uses INNER JOINs: the route must show up here too
    SELECT Id, Name, TypeId, Status, FrequencyType, RepeatCount FROM dbo.VW_Routes WHERE Name = @RouteName;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO

/*==============================================================================
  POST-STEPS
  Activate from the UI (Setup > Routes or a Flow), never with an UPDATE - that leaves JobID NULL.
==============================================================================*/
