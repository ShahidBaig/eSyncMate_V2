/*==============================================================================
  18 - Shopify (Task 00006): Send Cancellations to ERP (eSyncMate -> ERP)
       Route 2 of the customer-side cancellation: the existing RouteType 79 (WalmartSendCancellationsToERPRoute,
       Task 00005), which only filters by customer - no Shopify code

    <P> - Send Cancellations to ERP   RouteType 79, eSyncMate -> SPARS, Minutely @RepeatMinutes
         source  <P> - eSyncMate Orders To Cancel In ERP   copy of "<P> - WHSW Inventory Get Data" (script 03),
                                                           EXEC Sp_GetWalmartOrdersToCancelInERP '@CUSTOMERID@'
         dest    <P> - SPARS Cancel Order                  copy of "<P> - SPARS Full Inventory Feed" (script 04),
                                                           POST Cancel_Order
         -> PortalCancel orders (script 17) -> ERP Cancel_Order (OrderNo = ExternalId): CANCELLED, or CancelError
            (not retried by itself; Re-Process on the Orders screen sends it again)

  Needs Task 00005 scripts 01 + 02 and scripts 02, 03, 04 here.
  No secret here. Run it as often as needed: missing -> inserted, existing -> updated (an encrypted connector is
  listed and left alone; an existing route keeps its Status, schedule and JobID).
==============================================================================*/

SET NOCOUNT ON;

/*==============================================================================
  CONFIG
==============================================================================*/
DECLARE @ErpCustomerID NVARCHAR(50)  = 'WKE1000';   -- same as scripts 02 / 03 / 04 / 17
DECLARE @Prefix        NVARCHAR(100) = 'Shopify';
DECLARE @RepeatMinutes INT           = 30;
/*==============================================================================*/

BEGIN TRY
    IF @ErpCustomerID LIKE '%[^A-Za-z0-9_]%'
        THROW 51800, '@ErpCustomerID may only contain letters, digits and underscore.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.RouteTypes WHERE Id = 79) OR OBJECT_ID('dbo.Sp_GetWalmartOrdersToCancelInERP', 'P') IS NULL
       OR OBJECT_ID('dbo.Sp_SetWalmartERPCancelResult', 'P') IS NULL
        THROW 51801, 'RouteType 79 or the cancellation SPs are missing. Run 2026-10-22_Task00005_WalmartCancellations\01 and 02 first.', 1;

    DECLARE @SparsId   INT = (SELECT Id FROM dbo.Customers WHERE Name = 'SPARS');
    DECLARE @EsyncId   INT = (SELECT Id FROM dbo.Customers WHERE Name = 'eSyncMate');
    DECLARE @PartnerId INT = (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = @ErpCustomerID);
    DECLARE @PgSpars   INT = (SELECT Id FROM dbo.PartnerGroups WHERE SourcePartyId = @PartnerId AND DestinationPartyId = @SparsId);
    DECLARE @SqlFrom   NVARCHAR(MAX) = (SELECT Data FROM dbo.Connectors WHERE Name = @Prefix + ' - WHSW Inventory Get Data');
    DECLARE @SparsFrom NVARCHAR(MAX) = (SELECT Data FROM dbo.Connectors WHERE Name = @Prefix + ' - SPARS Full Inventory Feed');
    DECLARE @SrcName   NVARCHAR(200) = @Prefix + ' - eSyncMate Orders To Cancel In ERP';
    DECLARE @DstName   NVARCHAR(200) = @Prefix + ' - SPARS Cancel Order';
    DECLARE @RouteName VARCHAR(100)  = @Prefix + ' - Send Cancellations to ERP';

    IF @PartnerId IS NULL OR @PgSpars IS NULL THROW 51802, 'Customer / partner groups missing. Run script 02 first.', 1;
    IF @SqlFrom IS NULL THROW 51803, 'Connector "<P> - WHSW Inventory Get Data" missing. Run script 03 first.', 1;
    IF @SparsFrom IS NULL THROW 51804, 'Connector "<P> - SPARS Full Inventory Feed" missing. Run script 04 first.', 1;
    IF ISJSON(@SqlFrom) = 0 OR ISJSON(@SparsFrom) = 0
        THROW 51805, 'A connector this script copies from is encrypted (saved from Setup > Connectors). Create the 2 connectors by hand.', 1;

    DECLARE @c TABLE (Seq INT, Nm NVARCHAR(200), TypeId INT, Payload NVARCHAR(MAX));

    -- WalmartSendCancellationsToERPRoute replaces @CUSTOMERID@ in the Command with the source connector's CustomerID
    INSERT INTO @c VALUES
      (1, @SrcName, 8, JSON_MODIFY(JSON_MODIFY(@SqlFrom, '$.Command', 'EXEC Sp_GetWalmartOrdersToCancelInERP ''@CUSTOMERID@'''), '$.CustomerID', @ErpCustomerID)),
      (2, @DstName, 7, JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(@SparsFrom, '$.Url', 'Cancel_Order'), '$.Method', 'POST'), '$.CustomerID', @ErpCustomerID));

    IF EXISTS (SELECT 1 FROM @c WHERE ISJSON(Payload) = 0) THROW 51806, 'A connector payload is not valid JSON.', 1;

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

    -- Re-Process of a CancelError order finds this route by RouteType 79 + CustomerName.
    -- An existing route gets these parties / connectors / group; its Status, schedule and JobID are left alone.
    IF EXISTS (SELECT 1 FROM dbo.Routes WHERE Name = @RouteName)
    BEGIN
        UPDATE dbo.Routes
           SET TypeId = 79, SourcePartyId = @EsyncId, DestinationPartyId = @SparsId,
               SourceConnectorId = (SELECT Id FROM dbo.Connectors WHERE Name = @SrcName),
               DestinationConnectorId = (SELECT Id FROM dbo.Connectors WHERE Name = @DstName),
               MapId = 0, PartyGroupId = @PgSpars, RouteGroup = 'Orders', CustomerName = @ErpCustomerID,
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
        SELECT ISNULL(MAX(Id), 0) + 1, 79, 'In-Active', @EsyncId, @SparsId,
               (SELECT Id FROM dbo.Connectors WHERE Name = @SrcName),
               (SELECT Id FROM dbo.Connectors WHERE Name = @DstName),
               0, @PgSpars, GETDATE(), 1, 'Minutely', CAST(GETDATE() AS DATE), NULL, @RepeatMinutes, '', '', '', NULL,
               @RouteName, 'Orders', @ErpCustomerID
          FROM dbo.Routes WITH (UPDLOCK, HOLDLOCK);

        PRINT 'Created route ' + @RouteName;
    END

    COMMIT TRAN;

    /*--- Verification (no secrets shown) ----------------------------------*/
    SELECT c.Id, c.TypeId, c.Name,
           CASE WHEN ISJSON(c.Data) = 1 THEN 'Plain JSON' ELSE 'Encrypted' END AS Storage,
           CASE WHEN ISJSON(c.Data) = 1 THEN ISNULL(JSON_VALUE(c.Data, '$.Command'), JSON_VALUE(c.Data, '$.Url')) END AS CommandOrUrl,
           CASE WHEN ISJSON(c.Data) = 1 THEN JSON_VALUE(c.Data, '$.Method') END AS Method,
           CASE WHEN ISJSON(c.Data) = 1 THEN JSON_VALUE(c.Data, '$.CustomerID') END AS CustomerID
      FROM dbo.Connectors c JOIN @c x ON x.Nm = c.Name ORDER BY c.Id;

    -- VW_Routes uses INNER JOINs: the route must show up here too
    SELECT Id, Name, TypeId, Status, FrequencyType, RepeatCount, CustomerName FROM dbo.VW_Routes WHERE Name = @RouteName;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO

/*==============================================================================
  POST-STEPS
  1. Test Run "<P> - Send Cancellations to ERP" after script 17's route marked an order PortalCancel. Route log line:
       "SPARS Cancel_Order for [<customer>]: N order(s), N CANCELLED, N CancelError."
  2. A CancelError order: Orders screen > Re-Process sends Cancel_Order again (not for a partial cancel).
  3. Activate from the UI (Setup > Routes or a Flow), never with an UPDATE - that leaves JobID NULL.
==============================================================================*/
