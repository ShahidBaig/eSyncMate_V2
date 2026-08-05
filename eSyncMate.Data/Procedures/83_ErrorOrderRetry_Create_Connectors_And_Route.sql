/*==============================================================================
  83_ErrorOrderRetry_Create_Connectors_And_Route.sql
  ------------------------------------------------------------------------------
  Creates the Source + Destination Connectors and the Route for the
  ErrorOrderRetry sweep (RouteType 74) on a NEW environment (PRODUCTION).

  Background:
    ErrorOrderRetryRoute auto-retries Sales Orders that failed to place in
    SPARS due to Period / Timeout / "No Response from SPARS". It reads the
    SOURCE connector (SqlServer + SP), runs `EXEC SP_GetErrorOrdersForRetry`,
    then per order does RetryCount++ / ERROR -> InProgress and calls
    SCSPlaceOrderRoute.ExecuteSingle (same code path as the manual reprocess).

  PREREQUISITES — run these FIRST, in order:
      70_Create_RouteType_ErrorOrderRetry.sql   (RouteTypes Id 74)
      71_Add_Orders_RetryCount.sql              (Orders.RetryCount + sp_refreshview)
      72_Create_SP_GetErrorOrdersForRetry.sql   (feeder SP)
    This script hard-stops if any of them is missing.

  >>> BEFORE RUNNING: set @ProdConnectionString in the CONFIG block below. <<<

  Encryption note:
    Connectors.Data is normally AES-256 encrypted with an "ENC:" prefix, using
    the environment's EncryptionKey. EncryptionHelper.Decrypt returns the string
    AS-IS when the "ENC:" prefix is absent, so this script writes PLAIN JSON on
    purpose — a blob encrypted with the DEV key would be undecryptable on PROD.
    See the "OPTIONAL — encrypt after insert" note at the bottom of this file.

  Safe to re-run: connectors and the route are created only if missing.
  Wrapped in a transaction.
==============================================================================*/

SET NOCOUNT ON;

/*==============================================================================
  CONFIG — edit these before running
==============================================================================*/
DECLARE @ProdConnectionString VARCHAR(500) =
        '<<SET_IN_PRODUCTION>>';   -- e.g. 'Server=192.168.0.44,7100;Database=ESYNCMATE;UID=sa;PWD=xxxxx;'
                                   -- Use the SAME connection string the Processor
                                   -- uses in appsettings.json -> ConnectionStrings.

DECLARE @SourceConnectorName NVARCHAR(500) = 'Process Error Order - Source';
DECLARE @DestConnectorName   NVARCHAR(500) = 'Process Error Order - Destination';
DECLARE @RouteName           VARCHAR(100)  = 'Process Error Order';
DECLARE @RouteGroup          VARCHAR(250)  = 'Order''s';
DECLARE @FrequencyType       VARCHAR(100)  = 'Minutely';
DECLARE @RepeatCount         INT           = 5;               -- every 5 minutes
DECLARE @EndDate             DATETIME      = '2028-09-30';
DECLARE @RouteStatus         NVARCHAR(100) = 'In-Active';     -- see POST-STEPS at bottom

/*==============================================================================*/

BEGIN TRY
    BEGIN TRAN;

    /*--- 0. Prerequisite checks ------------------------------------------------*/
    IF @ProdConnectionString = '<<SET_IN_PRODUCTION>>'
        THROW 50000, 'Set @ProdConnectionString in the CONFIG block before running this script.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.RouteTypes WHERE Id = 74)
        THROW 50001, 'RouteTypes Id 74 (ErrorOrderRetry) missing. Run 70_Create_RouteType_ErrorOrderRetry.sql first.', 1;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Orders') AND name = 'RetryCount')
        THROW 50002, 'Orders.RetryCount missing. Run 71_Add_Orders_RetryCount.sql first.', 1;

    IF OBJECT_ID('dbo.SP_GetErrorOrdersForRetry', 'P') IS NULL
        THROW 50003, 'SP_GetErrorOrdersForRetry missing. Run 72_Create_SP_GetErrorOrdersForRetry.sql first.', 1;

    /*--- 1. Resolve FK ids by NAME (ids differ per environment) ----------------*/
    DECLARE @eSyncMateId INT = (SELECT Id FROM dbo.Customers WHERE Name = 'eSyncMate');
    DECLARE @SparsId     INT = (SELECT Id FROM dbo.Customers WHERE Name = 'SPARS');

    IF @eSyncMateId IS NULL THROW 50004, 'Customer [eSyncMate] not found.', 1;
    IF @SparsId     IS NULL THROW 50005, 'Customer [SPARS] not found.', 1;

    -- SPARS connector type (the source/destination parties of this route are internal)
    DECLARE @SparsTypeId INT = (SELECT Id FROM dbo.ConnectorTypes WHERE Name = 'SPARS');
    IF @SparsTypeId IS NULL THROW 50006, 'ConnectorTypes row [SPARS] not found.', 1;

    -- The sweep covers ALL customers, so PartyGroupId is only needed to satisfy the
    -- VW_Routes INNER JOIN. Take any group that targets SPARS.
    DECLARE @PartyGroupId INT =
        (SELECT TOP 1 Id FROM dbo.PartnerGroups WHERE DestinationPartyId = @SparsId ORDER BY Id);
    IF @PartyGroupId IS NULL THROW 50007, 'No PartnerGroups row with DestinationPartyId = SPARS.', 1;

    PRINT 'Resolved: eSyncMate=' + CAST(@eSyncMateId AS VARCHAR(10))
        + ', SPARS=' + CAST(@SparsId AS VARCHAR(10))
        + ', ConnectorType(SPARS)=' + CAST(@SparsTypeId AS VARCHAR(10))
        + ', PartyGroupId=' + CAST(@PartyGroupId AS VARCHAR(10));

    /*--- 2. Connector payload (PLAIN JSON — see encryption note in header) -----*/
    -- Shape must match ConnectorDataModel. ErrorOrderRetryRoute reads:
    --   ConnectivityType = 'SqlServer', CommandType = 'SP', Command, ConnectionString.
    DECLARE @ConnectorJson VARCHAR(MAX) =
        '{"ConnectivityType":"SqlServer","CommandType":"SP","Command":"EXEC SP_GetErrorOrdersForRetry",'
      + '"KeyFieldName":"","DataFieldName":"","CustomerID":"","JsonDataCollectionName":"",'
      + '"ConnectionString":"' + REPLACE(@ProdConnectionString, '"', '\"') + '"}';

    -- Connectors.Id is NOT an IDENTITY column -> assign ids manually.
    DECLARE @nextConnId INT = (SELECT ISNULL(MAX(Id), 0) FROM dbo.Connectors);
    DECLARE @SourceConnId INT, @DestConnId INT;

    /*--- 2a. SOURCE connector — this is the one the route actually reads -------*/
    SELECT @SourceConnId = Id FROM dbo.Connectors WHERE Name = @SourceConnectorName;

    IF @SourceConnId IS NULL
    BEGIN
        SET @nextConnId   = @nextConnId + 1;
        SET @SourceConnId = @nextConnId;

        INSERT INTO dbo.Connectors (Id, TypeId, Name, Data, CreatedDate, CreatedBy)
        VALUES (@SourceConnId, @SparsTypeId, @SourceConnectorName, @ConnectorJson, GETDATE(), 1);

        PRINT 'Created SOURCE connector Id = ' + CAST(@SourceConnId AS VARCHAR(10));
    END
    ELSE
        PRINT 'SOURCE connector already exists Id = ' + CAST(@SourceConnId AS VARCHAR(10));

    /*--- 2b. DESTINATION connector --------------------------------------------
      ErrorOrderRetryRoute does NOT read the destination connector (the actual
      posting is done by SCSPlaceOrderRoute.ExecuteSingle using its own route's
      connectors). It exists because Routes.DestinationConnectorId is NOT NULL and
      VW_Routes INNER JOINs Connectors — without it the route is invisible in the UI.
      Same payload is used so the row is self-describing rather than empty.
    ---------------------------------------------------------------------------*/
    SELECT @DestConnId = Id FROM dbo.Connectors WHERE Name = @DestConnectorName;

    IF @DestConnId IS NULL
    BEGIN
        SET @nextConnId = @nextConnId + 1;
        SET @DestConnId = @nextConnId;

        INSERT INTO dbo.Connectors (Id, TypeId, Name, Data, CreatedDate, CreatedBy)
        VALUES (@DestConnId, @SparsTypeId, @DestConnectorName, @ConnectorJson, GETDATE(), 1);

        PRINT 'Created DESTINATION connector Id = ' + CAST(@DestConnId AS VARCHAR(10));
    END
    ELSE
        PRINT 'DESTINATION connector already exists Id = ' + CAST(@DestConnId AS VARCHAR(10));

    /*--- 3. Route --------------------------------------------------------------*/
    -- Routes.Id is NOT an IDENTITY column -> assign manually.
    DECLARE @RouteId INT = (SELECT TOP 1 Id FROM dbo.Routes WHERE TypeId = 74);

    IF @RouteId IS NULL
    BEGIN
        SET @RouteId = (SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.Routes);

        INSERT INTO dbo.Routes
            (Id, TypeId, Status, SourcePartyId, DestinationPartyId,
             SourceConnectorId, DestinationConnectorId, MapId, PartyGroupId,
             CreatedDate, CreatedBy,
             FrequencyType, StartDate, EndDate, RepeatCount,
             WeekDays, OnDay, ExecutionTime, JobID, Name, RouteGroup, CustomerName)
        VALUES
            (@RouteId, 74, @RouteStatus, @eSyncMateId, @SparsId,
             @SourceConnId, @DestConnId, 0, @PartyGroupId,
             GETDATE(), 1,
             @FrequencyType, CAST(GETDATE() AS DATE), @EndDate, @RepeatCount,
             '', '', '', NULL, @RouteName, @RouteGroup, NULL);

        PRINT 'Created ROUTE Id = ' + CAST(@RouteId AS VARCHAR(10));
    END
    ELSE
    BEGIN
        -- Keep an existing route pointed at the right connectors, leave its
        -- Status/JobID alone (activation is owned by the UI / Flow — see POST-STEPS).
        UPDATE dbo.Routes
           SET SourceConnectorId      = @SourceConnId,
               DestinationConnectorId = @DestConnId,
               ModifiedDate           = GETDATE(),
               ModifiedBy             = 1
         WHERE Id = @RouteId;

        PRINT 'ROUTE already exists Id = ' + CAST(@RouteId AS VARCHAR(10)) + ' — connectors repointed.';
    END

    /*--- 4. Verification -------------------------------------------------------*/
    SELECT r.Id            AS RouteId,
           r.Name          AS RouteName,
           r.TypeId,
           rt.Name         AS RouteType,
           r.Status,
           r.FrequencyType,
           r.RepeatCount,
           r.StartDate,
           r.EndDate,
           r.RouteGroup,
           r.JobID,
           sp.Name         AS SourceParty,
           dp.Name         AS DestinationParty,
           r.SourceConnectorId,
           sc.Name         AS SourceConnector,
           r.DestinationConnectorId,
           dc.Name         AS DestinationConnector,
           pg.Description  AS PartnerGroup
      FROM dbo.Routes r
      JOIN dbo.RouteTypes    rt ON rt.Id = r.TypeId
      JOIN dbo.Customers     sp ON sp.Id = r.SourcePartyId
      JOIN dbo.Customers     dp ON dp.Id = r.DestinationPartyId
      JOIN dbo.Connectors    sc ON sc.Id = r.SourceConnectorId
      JOIN dbo.Connectors    dc ON dc.Id = r.DestinationConnectorId
      JOIN dbo.PartnerGroups pg ON pg.Id = r.PartyGroupId
     WHERE r.TypeId = 74;

    -- Must return 1 row, otherwise the route will not show in Setup > Routes.
    SELECT COUNT(*) AS VisibleInVW_Routes FROM dbo.VW_Routes WHERE TypeId = 74;

    COMMIT TRAN;
    PRINT 'Done.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH


/*==============================================================================
  POST-STEPS (after this script)
  ------------------------------------------------------------------------------
  1. Deploy the matching build. The route type must be dispatchable in BOTH
     processes or execution throws "Unknown route type":
       - eSyncMate.Processor/Managers/RouteEngine.cs
       - eSyncMate.RouteWorker/Program.cs

  2. ACTIVATE FROM THE UI, NOT FROM SQL.
     Setting Routes.Status = 'Active' with an UPDATE does NOT create the Hangfire
     job — Routes.JobID stays NULL and the sweep never fires. Activate through
     Setup > Routes (or by adding the route to a Flow and setting it Active) so
     RouteEngine.ScheduleWaitJob / Schedule register the recurring job.

  3. Verify after activation:
       SELECT Id, Name, Status, JobID FROM Routes WHERE TypeId = 74;   -- JobID must be set
       SELECT TOP 50 * FROM RouteLog WHERE RouteId = <RouteId> ORDER BY Id DESC;
     Expect '[ErrorOrderRetry] Started' / '... error order(s) to retry' / '... Completed'.

  OPTIONAL — encrypt the connector Data
  ------------------------------------------------------------------------------
  The rows above are written as plain JSON, which the app reads fine. To store
  them encrypted instead, the blob must be produced with PRODUCTION's
  EncryptionKey (ApplicationSettings, TagName = 'EncryptionKey') —
  AES-256-CBC/PKCS7, key = SHA256(UTF8(EncryptionKey)), IV prepended, base64,
  'ENC:' prefix — and then applied with:
       UPDATE Connectors SET Data = 'ENC:...' WHERE Id IN (<src>, <dest>);
  Do NOT reuse a blob generated with the DEV key, and do NOT re-save these
  connectors from the Connectors screen to "encrypt" them: the GET masks
  ConnectionString / Command, so saving would wipe both fields.
==============================================================================*/
