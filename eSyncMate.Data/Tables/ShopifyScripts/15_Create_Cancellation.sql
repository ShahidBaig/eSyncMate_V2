/*==============================================================================
  15 - Shopify (Task 00006): Create Cancellation (eSyncMate -> Shopify)

    eSyncMate -> Shopify  "<P> - Send Cancellation to Portal"    RouteType 84 (ShopifyCancellationRoute)
         source  <P> - eSyncMate Cancel Lines   copy of "<P> - WHSW Inventory Get Data" (script 03),
                                                EXEC SP_SyncCancelQty (the existing SP the Target / Knot / Michaels
                                                "Cancel Lines" connectors use: one row per line with CANRVD units)
         dest    <P> - Cancel Order             copy of "<P> - WHSW Inventory Upload" (same store / app / token)
                                                + Parameters notifyCustomer / restock / refund / reason
         -> line-wise: each line's Shopify quantity set to LineQty - CancelQty (order edit);
            nothing left on the Shopify order: orderCancel instead
            units CANSNT (sent) or CANERR (Shopify said no); OrderData ERPCANLN-JSON / ERPCANLN-ERR

    1. RouteType 84 ShopifyCancellationLines
    2. Connectors (no secret here; they must still be plain JSON - not saved from the UI yet)
    3. Route (In-Active)

  Needs scripts 02, 03 (and 14 for the ERP side). Run it as often as needed: missing -> inserted, existing ->
  updated (RouteType description, both connectors incl. the Parameters below, the route's parties / connectors /
  group). An encrypted connector is listed and left alone; an existing route keeps its Status, schedule and JobID.
==============================================================================*/

SET NOCOUNT ON;

/*==============================================================================
  CONFIG
==============================================================================*/
DECLARE @ErpCustomerID  NVARCHAR(50)  = 'WKE1000';   -- same as scripts 02 / 03
DECLARE @Prefix         NVARCHAR(100) = 'Shopify';
DECLARE @RepeatMinutes  INT           = 15;

--- Shopify settings for the cancellation (destination connector Parameters)
DECLARE @NotifyCustomer VARCHAR(5)  = 'false';   -- true: Shopify e-mails the customer the cancellation / changed order
DECLARE @Restock        VARCHAR(5)  = 'true';    -- true: the cancelled units go back into Shopify stock
DECLARE @Refund         VARCHAR(5)  = 'false';   -- whole-order cancel only: true = refund to the original payment method
DECLARE @Reason         VARCHAR(20) = 'OTHER';   -- CUSTOMER / DECLINED / FRAUD / INVENTORY / OTHER / STAFF
/*==============================================================================*/

BEGIN TRY
    IF EXISTS (SELECT 1 FROM dbo.RouteTypes WHERE Id = 84 AND Name <> 'ShopifyCancellationLines')
        THROW 51550, 'RouteType 84 already exists with another name - check before continuing.', 1;

    DECLARE @EsyncId   INT = (SELECT Id FROM dbo.Customers WHERE Name = 'eSyncMate');
    DECLARE @PartnerId INT = (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = @ErpCustomerID);
    DECLARE @PgEsync   INT = (SELECT Id FROM dbo.PartnerGroups WHERE SourcePartyId = @PartnerId AND DestinationPartyId = @EsyncId);
    DECLARE @SqlFrom   NVARCHAR(MAX) = (SELECT Data FROM dbo.Connectors WHERE Name = @Prefix + ' - WHSW Inventory Get Data');
    DECLARE @ShopFrom  NVARCHAR(MAX) = (SELECT Data FROM dbo.Connectors WHERE Name = @Prefix + ' - WHSW Inventory Upload');
    DECLARE @ShopType  INT           = (SELECT TypeId FROM dbo.Connectors WHERE Name = @Prefix + ' - WHSW Inventory Upload');

    IF @PartnerId IS NULL OR @PgEsync IS NULL THROW 51551, 'Customer / partner groups missing. Run script 02 first.', 1;
    IF @SqlFrom IS NULL OR @ShopFrom IS NULL THROW 51552, 'Connectors "<P> - WHSW Inventory Get Data" / "<P> - WHSW Inventory Upload" missing. Run script 03 first.', 1;
    IF ISJSON(@SqlFrom) = 0 OR ISJSON(@ShopFrom) = 0
        THROW 51553, 'A connector this script copies from is encrypted (saved from Setup > Connectors). Create the 2 connectors by hand (see MANUAL FALLBACK).', 1;

    DECLARE @CancelParams NVARCHAR(MAX) = '[{"Name":"notifyCustomer","Value":""},{"Name":"restock","Value":""},{"Name":"refund","Value":""},{"Name":"reason","Value":""}]';

    SET @CancelParams = JSON_MODIFY(@CancelParams, '$[0].Value', CASE WHEN LOWER(@NotifyCustomer) = 'true' THEN 'true' ELSE 'false' END);
    SET @CancelParams = JSON_MODIFY(@CancelParams, '$[1].Value', CASE WHEN LOWER(@Restock) = 'false' THEN 'false' ELSE 'true' END);
    SET @CancelParams = JSON_MODIFY(@CancelParams, '$[2].Value', CASE WHEN LOWER(@Refund) = 'true' THEN 'true' ELSE 'false' END);
    SET @CancelParams = JSON_MODIFY(@CancelParams, '$[3].Value', UPPER(@Reason));

    DECLARE @c TABLE (Seq INT, Nm NVARCHAR(200), TypeId INT, Payload NVARCHAR(MAX));

    -- ShopifyCancellationRoute replaces @CUSTOMERID@ in the Command with the connector's CustomerID
    INSERT INTO @c VALUES
      (1, @Prefix + ' - eSyncMate Cancel Lines', 8,         JSON_MODIFY(@SqlFrom, '$.Command', 'EXEC SP_SyncCancelQty @p_CustomerID = ''@CUSTOMERID@''')),
      (2, @Prefix + ' - Cancel Order',           @ShopType, JSON_MODIFY(@ShopFrom, '$.Parmeters', JSON_QUERY(@CancelParams)));

    IF EXISTS (SELECT 1 FROM @c WHERE ISJSON(Payload) = 0) THROW 51554, 'A connector payload is not valid JSON.', 1;

    BEGIN TRAN;

    DECLARE @RtDescription NVARCHAR(500) = 'Shopify: ERP cancellations -> Shopify orderCancel (whole order) or order edit (lines)';

    IF NOT EXISTS (SELECT 1 FROM dbo.RouteTypes WHERE Id = 84)
        INSERT INTO dbo.RouteTypes (Id, Name, Description, CreatedDate, CreatedBy)
        VALUES (84, 'ShopifyCancellationLines', @RtDescription, GETDATE(), 1);
    ELSE
        UPDATE dbo.RouteTypes SET Description = @RtDescription, ModifiedDate = GETDATE(), ModifiedBy = 1 WHERE Id = 84;

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

    DECLARE @RouteName VARCHAR(100) = @Prefix + ' - Send Cancellation to Portal';

    -- an existing route gets these parties / connectors / group; its Status, schedule and JobID are left alone
    IF EXISTS (SELECT 1 FROM dbo.Routes WHERE Name = @RouteName)
    BEGIN
        UPDATE dbo.Routes
           SET TypeId = 84, SourcePartyId = @EsyncId, DestinationPartyId = @PartnerId,
               SourceConnectorId = (SELECT Id FROM dbo.Connectors WHERE Name = @Prefix + ' - eSyncMate Cancel Lines'),
               DestinationConnectorId = (SELECT Id FROM dbo.Connectors WHERE Name = @Prefix + ' - Cancel Order'),
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
        SELECT ISNULL(MAX(Id), 0) + 1, 84, 'In-Active', @EsyncId, @PartnerId,
               (SELECT Id FROM dbo.Connectors WHERE Name = @Prefix + ' - eSyncMate Cancel Lines'),
               (SELECT Id FROM dbo.Connectors WHERE Name = @Prefix + ' - Cancel Order'),
               0, @PgEsync, GETDATE(), 1, 'Minutely', CAST(GETDATE() AS DATE), NULL, @RepeatMinutes, '', '', '', NULL,
               @RouteName, 'Orders', @ErpCustomerID
          FROM dbo.Routes WITH (UPDLOCK, HOLDLOCK);

        PRINT 'Created route ' + @RouteName;
    END

    COMMIT TRAN;

    /*--- Verification (no secrets shown) ----------------------------------*/
    SELECT Id, Name FROM dbo.RouteTypes WHERE Id = 84;
    SELECT c.Id, c.TypeId, c.Name,
           CASE WHEN ISJSON(c.Data) = 1 THEN 'Plain JSON' ELSE 'Encrypted' END AS Storage,
           CASE WHEN ISJSON(c.Data) = 1 THEN ISNULL(JSON_VALUE(c.Data, '$.Command'), JSON_VALUE(c.Data, '$.Url')) END AS CommandOrUrl,
           CASE WHEN ISJSON(c.Data) = 1 THEN JSON_QUERY(c.Data, '$.Parmeters') END AS Parameters
      FROM dbo.Connectors c JOIN @c x ON x.Nm = c.Name
     ORDER BY c.Id;
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
  1. Deploy Processor AND RouteWorker (RouteType 84).
  2. Test Run "Send Cancellation to Portal". Route log line:
       "Shopify cancellations for [<customer>]: N order(s), N sent, N rejected by Shopify (CANERR), N call failure(s) ..."
  3. Activate from the UI (Setup > Routes or a Flow), never with an UPDATE - that leaves JobID NULL.
  To send a CANERR line again: UPDATE OrderDetail SET Status = 'CANRVD' WHERE OrderId = <id> AND Status = 'CANERR'.

  MANUAL FALLBACK (connectors already encrypted): in Setup > Connectors copy
    "<P> - WHSW Inventory Get Data" -> Command EXEC SP_SyncCancelQty @p_CustomerID = '@CUSTOMERID@'
    "<P> - WHSW Inventory Upload"   -> Parameters notifyCustomer / restock / refund / reason
  under the names above, then run this script again for the route.
==============================================================================*/
