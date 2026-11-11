/*==============================================================================
  07 - Shopify inventory (Task 00006): the upload route

    <P> - Upload Warehouse-Wise Inventory     TypeId 80, created In-Active
        source       <P> - WHSW Inventory Get Data   (script 03)
        destination  <P> - WHSW Inventory Upload     (script 03)
        parties      eSyncMate -> <customer>, partner group "<customer> - eSyncMate" (script 02)
        schedule     Minutely every 20, like the other warehouse-wise inventory routes

  Run 01, 02, 03, 06 first. Routes.Id is NOT identity -> MAX(Id)+1.
  Run it as often as needed: missing -> inserted In-Active; existing (same name) -> parties, connectors,
  partner group, group and customer updated; its Status, schedule and JobID are left alone.
==============================================================================*/

SET NOCOUNT ON;

/*==============================================================================
  CONFIG
==============================================================================*/
DECLARE @ErpCustomerID VARCHAR(50)   = 'WKE1000';   -- same as scripts 02 / 03
DECLARE @Prefix        NVARCHAR(100) = 'Shopify';      -- same as script 03
DECLARE @RepeatMinutes INT           = 20;
/*==============================================================================*/

BEGIN TRY
    IF NOT EXISTS (SELECT 1 FROM dbo.RouteTypes WHERE Id = 80)
        THROW 51380, 'RouteType 80 missing. Run script 01 first.', 1;

    DECLARE @CustomerId  INT = (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = @ErpCustomerID);
    DECLARE @eSyncMateId INT = (SELECT Id FROM dbo.Customers WHERE Name = 'eSyncMate');
    DECLARE @PgId        INT = (SELECT Id FROM dbo.PartnerGroups WHERE SourcePartyId = @CustomerId AND DestinationPartyId = @eSyncMateId);
    DECLARE @SrcId       INT = (SELECT Id FROM dbo.Connectors WHERE Name = @Prefix + ' - WHSW Inventory Get Data');
    DECLARE @DstId       INT = (SELECT Id FROM dbo.Connectors WHERE Name = @Prefix + ' - WHSW Inventory Upload');
    DECLARE @Name        VARCHAR(100) = @Prefix + ' - Upload Warehouse-Wise Inventory';

    IF @CustomerId IS NULL OR @PgId IS NULL THROW 51381, 'Customer / partner group missing. Run script 02 first.', 1;
    IF @SrcId IS NULL OR @DstId IS NULL     THROW 51382, 'Connectors missing. Run script 03 first.', 1;

    BEGIN TRAN;

    -- an existing route gets these parties / connectors / group; its Status, schedule and JobID are left alone
    IF EXISTS (SELECT 1 FROM dbo.Routes WHERE Name = @Name)
    BEGIN
        UPDATE dbo.Routes
           SET TypeId = 80, SourcePartyId = @eSyncMateId, DestinationPartyId = @CustomerId,
               SourceConnectorId = @SrcId, DestinationConnectorId = @DstId, MapId = 0, PartyGroupId = @PgId,
               RouteGroup = 'Inventory', CustomerName = @ErpCustomerID, ModifiedDate = GETDATE(), ModifiedBy = 1
         WHERE Name = @Name;

        PRINT 'Updated route ' + @Name + ' (Status / schedule / JobID left as they are)';
    END
    ELSE
    BEGIN
        INSERT INTO dbo.Routes
            (Id, TypeId, Status, SourcePartyId, DestinationPartyId, SourceConnectorId, DestinationConnectorId, MapId, PartyGroupId,
             CreatedDate, CreatedBy, FrequencyType, StartDate, EndDate, RepeatCount, WeekDays, OnDay, ExecutionTime, JobID,
             Name, RouteGroup, CustomerName)
        SELECT ISNULL(MAX(Id), 0) + 1, 80, 'In-Active', @eSyncMateId, @CustomerId, @SrcId, @DstId, 0, @PgId,
               GETDATE(), 1, 'Minutely', CAST(GETDATE() AS DATE), NULL, @RepeatMinutes, '', '', '', NULL,
               @Name, 'Inventory', @ErpCustomerID
          FROM dbo.Routes WITH (UPDLOCK, HOLDLOCK);

        PRINT 'Created route ' + @Name;
    END

    COMMIT TRAN;

    SELECT r.Id, r.Name, r.TypeId, r.Status, r.CustomerName, sc.Name AS SourceConnector, dc.Name AS DestinationConnector,
           r.FrequencyType, r.RepeatCount
      FROM dbo.Routes r
      JOIN dbo.Connectors sc ON sc.Id = r.SourceConnectorId
      JOIN dbo.Connectors dc ON dc.Id = r.DestinationConnectorId
     WHERE r.Name = @Name;

    -- VW_Routes uses INNER JOINs: the route must show up here too
    SELECT Id, Name FROM dbo.VW_Routes WHERE Name = @Name;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO

/*==============================================================================
  POST-STEPS
  1. Setup > Connectors: open "<P> - WHSW Inventory Upload", fill the Client secret if blank,
     Save (encrypts it). Open "<P> - WHSW Inventory Get Data", check the connection string, Save.
  2. Make sure SCSInventoryFeed has rows for the customer (ERP Full / Differential feed, or test rows)
     with Status UPDATED or NEW.
  3. Flow / Routes > Test Run the route. Route log line:
     "Shopify inventory for [<customer>] batch [...]: N item(s), N location quantities in N call(s), N SYNCED, N ERROR."
  4. Activate from the UI (Setup > Routes or the Flow), never with an UPDATE - that leaves JobID NULL.
  5. Deploy BOTH Processor and RouteWorker (routes run in RouteWorker.exe).
==============================================================================*/
