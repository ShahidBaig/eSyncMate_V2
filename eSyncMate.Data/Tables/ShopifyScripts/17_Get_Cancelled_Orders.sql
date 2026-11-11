/*==============================================================================
  17 - Shopify (Task 00006): Get Cancelled Orders (Shopify -> eSyncMate)
       Route 1 of the customer-side cancellation, the Shopify twin of Walmart RouteType 78 (Task 00005)

    <P> - Get Cancelled Orders   RouteType 85 (ShopifyGetCancelledOrdersRoute), customer -> eSyncMate, Minutely @RepeatMinutes
         source  <P> - Get Cancelled Orders           copy of "<P> - WHSW Inventory Upload" (script 03, same store / app / token)
                                                      + Parameter days = @LookbackDays
         dest    <P> - eSyncMate Data Connection      from script 10
         -> orders the customer cancelled in Shopify (whole order, or fewer units on a line the ERP has not shipped)
            are handed to Sp_SetWalmartPortalCancel (Task 00005):
              not in ERP yet -> CANCELLED;  in ERP -> PortalCancel (script 18 cancels it in ERP);
              shipped / partial -> CancelError for the operator (Re-Process on the Orders screen)
            OrderData PortalCancel-JSON + WMCANCEL-MSG (hover the status badge)

    1. RouteType 85 ShopifyGetCancelledOrders
    2. Connector, 3. Route (In-Active)

  Needs Task 00005 scripts 01 + 02 (RouteTypes 78/79 + the cancellation SPs) and scripts 02, 03, 10 here.
  No secret here. Run it as often as needed: missing -> inserted, existing -> updated (an encrypted connector is
  listed and left alone; an existing route keeps its Status, schedule and JobID).
==============================================================================*/

SET NOCOUNT ON;

/*==============================================================================
  CONFIG
==============================================================================*/
DECLARE @ErpCustomerID NVARCHAR(50)  = 'WKE1000';   -- same as scripts 02 / 03 / 10
DECLARE @Prefix        NVARCHAR(100) = 'Shopify';
DECLARE @LookbackDays  INT           = 3;              -- orders Shopify updated in the last N days are checked
DECLARE @RepeatMinutes INT           = 30;
/*==============================================================================*/

BEGIN TRY
    IF @ErpCustomerID LIKE '%[^A-Za-z0-9_]%'
        THROW 51700, '@ErpCustomerID may only contain letters, digits and underscore.', 1;

    IF OBJECT_ID('dbo.Sp_SetWalmartPortalCancel', 'P') IS NULL OR OBJECT_ID('dbo.Sp_SaveWalmartCancelMessage', 'P') IS NULL
        THROW 51701, 'The cancellation SPs are missing. Run 2026-10-22_Task00005_WalmartCancellations\01 and 02 first.', 1;

    IF EXISTS (SELECT 1 FROM dbo.RouteTypes WHERE Id = 85 AND Name <> 'ShopifyGetCancelledOrders')
        THROW 51702, 'RouteType 85 already exists with another name - check before continuing.', 1;

    DECLARE @EsyncId   INT = (SELECT Id FROM dbo.Customers WHERE Name = 'eSyncMate');
    DECLARE @PartnerId INT = (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = @ErpCustomerID);
    DECLARE @PgEsync   INT = (SELECT Id FROM dbo.PartnerGroups WHERE SourcePartyId = @PartnerId AND DestinationPartyId = @EsyncId);
    DECLARE @UploadId  INT = (SELECT Id FROM dbo.Connectors WHERE Name = @Prefix + ' - WHSW Inventory Upload');
    DECLARE @DstId     INT = (SELECT Id FROM dbo.Connectors WHERE Name = @Prefix + ' - eSyncMate Data Connection');
    DECLARE @SrcName   NVARCHAR(200) = @Prefix + ' - Get Cancelled Orders';
    DECLARE @RouteName VARCHAR(100)  = @Prefix + ' - Get Cancelled Orders';

    IF @PartnerId IS NULL OR @PgEsync IS NULL THROW 51703, 'Customer / partner group missing. Run script 02 first.', 1;
    IF @UploadId IS NULL THROW 51704, 'Connector "<P> - WHSW Inventory Upload" missing. Run script 03 first.', 1;
    IF @DstId IS NULL THROW 51705, 'Connector "<P> - eSyncMate Data Connection" missing. Run script 10 first.', 1;

    BEGIN TRAN;

    DECLARE @RtDescription NVARCHAR(500) = 'Shopify: orders the customer cancelled in Shopify -> PortalCancel / CANCELLED / CancelError (RouteType 79 cancels them in ERP)';

    IF NOT EXISTS (SELECT 1 FROM dbo.RouteTypes WHERE Id = 85)
        INSERT INTO dbo.RouteTypes (Id, Name, Description, CreatedDate, CreatedBy)
        VALUES (85, 'ShopifyGetCancelledOrders', @RtDescription, GETDATE(), 1);
    ELSE
        UPDATE dbo.RouteTypes SET Description = @RtDescription, ModifiedDate = GETDATE(), ModifiedBy = 1 WHERE Id = 85;

    /*--- 2. Connector (Connectors.Id is NOT identity) --------------------------*/
    DECLARE @SrcData VARCHAR(MAX) = (SELECT Data FROM dbo.Connectors WHERE Id = @UploadId);

    -- the values go in through JSON_MODIFY, which escapes them; an encrypted source is copied as it is (code default 3 days)
    IF ISJSON(@SrcData) = 1
    BEGIN
        SET @SrcData = JSON_MODIFY(@SrcData, '$.Parmeters', JSON_QUERY('[{"Name":"days","Value":""}]'));
        SET @SrcData = JSON_MODIFY(@SrcData, '$.Parmeters[0].Value', CAST(@LookbackDays AS VARCHAR(10)));
        SET @SrcData = JSON_MODIFY(@SrcData, '$.CustomerID', @ErpCustomerID);
    END

    DECLARE @c TABLE (Seq INT, Nm NVARCHAR(200), TypeId INT, Payload NVARCHAR(MAX));

    DECLARE @SrcTypeId INT = (SELECT TypeId FROM dbo.Connectors WHERE Id = @UploadId);

    INSERT INTO @c VALUES (1, @SrcName, @SrcTypeId, @SrcData);

    /*--- Upsert (same block in every Task 00005 / 00006 script) ----------------
      A new name is inserted. An existing plain-JSON connector gets this payload, but a secret / connection
      string left blank here keeps its stored value. An encrypted connector (saved from Setup > Connectors),
      or an encrypted payload copied from one, cannot be read by SQL: left as it is and listed below.          */
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

            IF ISJSON(@u_Old) = 0 OR ISJSON(@u_New) = 0
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

    /*--- 3. Route --------------------------------------------------------------*/
    -- an existing route gets these parties / connectors / group; its Status, schedule and JobID are left alone
    IF EXISTS (SELECT 1 FROM dbo.Routes WHERE Name = @RouteName)
    BEGIN
        UPDATE dbo.Routes
           SET TypeId = 85, SourcePartyId = @PartnerId, DestinationPartyId = @EsyncId,
               SourceConnectorId = (SELECT Id FROM dbo.Connectors WHERE Name = @SrcName), DestinationConnectorId = @DstId,
               MapId = 0, PartyGroupId = @PgEsync, RouteGroup = 'Orders', CustomerName = @ErpCustomerID,
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
        SELECT ISNULL(MAX(Id), 0) + 1, 85, 'In-Active', @PartnerId, @EsyncId,
               (SELECT Id FROM dbo.Connectors WHERE Name = @SrcName), @DstId,
               0, @PgEsync, GETDATE(), 1, 'Minutely', CAST(GETDATE() AS DATE), NULL, @RepeatMinutes, '', '', '', NULL,
               @RouteName, 'Orders', @ErpCustomerID
          FROM dbo.Routes WITH (UPDLOCK, HOLDLOCK);

        PRINT 'Created route ' + @RouteName;
    END

    COMMIT TRAN;

    /*--- Verification (no secrets shown) ----------------------------------*/
    SELECT Id, Name FROM dbo.RouteTypes WHERE Id IN (79, 85) ORDER BY Id;

    SELECT c.Id, c.TypeId, c.Name,
           CASE WHEN ISJSON(c.Data) = 1 THEN 'Plain JSON' ELSE 'Encrypted' END AS Storage,
           CASE WHEN ISJSON(c.Data) = 1 THEN JSON_VALUE(c.Data, '$.CustomerID') END AS CustomerID,
           CASE WHEN ISJSON(c.Data) = 1 THEN JSON_QUERY(c.Data, '$.Parmeters') END AS Parameters
      FROM dbo.Connectors c WHERE c.Name = @SrcName;

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
  1. Deploy Processor AND RouteWorker (RouteType 85).
  2. Test Run "<P> - Get Cancelled Orders". Route log line:
       "Shopify cancelled orders for [<customer>] since ...: N order(s) updated in Shopify, N of them open in eSyncMate;
        N fully cancelled, N partially cancelled. N CANCELLED, N PortalCancel, N CancelError, N skipped, N not found."
  3. Then script 18 ("<P> - Send Cancellations to ERP") sends the PortalCancel orders to the ERP.
  4. Activate from the UI (Setup > Routes or a Flow), never with an UPDATE - that leaves JobID NULL.
     Schedule: every 30 minutes; "Send Cancellations to ERP" about 15 minutes later.
==============================================================================*/
