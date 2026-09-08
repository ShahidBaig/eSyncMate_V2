-- ============================================================================
-- Task 00003 - EDI & API Integration with BizMate EU - W1-18
-- Run in: ESYNCMATE_EU  (also safe in ESYNCMATE_TEST)
-- Run AFTER: 06 (this reuses the partner and connectors that script creates)
--
-- The outbound half of the loop, as a route: RouteType 601 "BizMate - Send EDI"
-- (BizMateOutboundEDIRoute), which collects what BizMate has staged for a
-- partner, renders each document and writes it to the partner's transfer.
--
-- The connectors are the mirror of route 600:
--
--   Source       BizMate - EDI Bridge   (credentials come from ApplicationSettings,
--                                        never from the connector row)
--   Destination  the partner's folder, whose Url is the outbound side
--
-- The one partner connector serves both directions - BaseUrl is what route 600
-- reads, Url is what route 601 writes - so it is renamed here from
-- "BELL-D12 - Inbound Folder" to "BELL-D12 - EDI Folder", in place, keeping its
-- id so route 141 is unaffected.
--
-- Note the route is created Active but with no Hangfire job: add it to a Flow
-- to schedule it, or fire it once with POST api/flows/testRun/{routeId}.
--
-- Idempotent - every row guarded by its natural key. Safe to re-run.
-- ============================================================================
SET NOCOUNT ON;

IF DB_NAME() NOT LIKE 'ESYNCMATE%'
BEGIN
    RAISERROR('Wrong database context (%s). Run this script in ESYNCMATE_EU.', 16, 1, @@SERVERNAME);
    RETURN;
END
GO

-- ---------------------------------------------------------------------------
-- The partner connector serves both directions; name it for that
-- ---------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM dbo.Connectors WHERE Name = 'BELL-D12 - Inbound Folder')
BEGIN
    UPDATE dbo.Connectors
       SET Name = 'BELL-D12 - EDI Folder', ModifiedDate = SYSUTCDATETIME(), ModifiedBy = 1
     WHERE Name = 'BELL-D12 - Inbound Folder';

    PRINT 'Connectors: renamed BELL-D12 - Inbound Folder -> BELL-D12 - EDI Folder (it serves both directions)';
END
GO

-- ---------------------------------------------------------------------------
-- RouteType 601
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.RouteTypes WHERE Id = 601)
BEGIN
    INSERT INTO dbo.RouteTypes (Id, Name, Description, CreatedDate, CreatedBy)
    VALUES (601, 'BizMate - Send EDI',
            'Collects the documents BizMate has staged for a partner, renders each into the partner''s format, writes it to the partner''s transfer and confirms delivery back to BizMate. Recovers anything left Fetched but unconfirmed first.',
            GETDATE(), 1);
    PRINT 'RouteTypes: added 601 BizMate - Send EDI';
END
ELSE
    PRINT 'RouteTypes: 601 already present.';
GO

-- ---------------------------------------------------------------------------
-- The route
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Routes WHERE Name = 'BELL-D12 - Send BizMate documents')
BEGIN
    INSERT INTO dbo.Routes (Id, TypeId, Status, SourcePartyId, DestinationPartyId, SourceConnectorId, DestinationConnectorId,
                            MapId, PartyGroupId, CreatedDate, CreatedBy, FrequencyType, StartDate, EndDate, RepeatCount,
                            WeekDays, OnDay, ExecutionTime, JobID, Name, RouteGroup, CustomerName)
    VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.Routes),
            601, 'Active',
            -- mirror of route 600: BizMate is the source, the partner the destination
            (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = 'BIZMATE'),
            (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = 'BELL-D12'),
            (SELECT Id FROM dbo.Connectors WHERE Name = 'BizMate - EDI Bridge'),
            (SELECT Id FROM dbo.Connectors WHERE Name = 'BELL-D12 - EDI Folder'),
            (SELECT Id FROM dbo.Maps WHERE Name = '850'),   -- VW_Routes LEFT JOINs Maps; any valid id is fine
            (SELECT Id FROM dbo.PartnerGroups WHERE Description = 'BELL-D12 - BizMate'),
            GETDATE(), 1, 'Minutely', GETDATE(), DATEADD(year, 3, GETDATE()), 5,
            '', '', '', NULL, 'BELL-D12 - Send BizMate documents', 'BizMate', 'BELL-D12');

    PRINT 'Routes: added BELL-D12 - Send BizMate documents (Active, no Hangfire job yet)';
END
ELSE
    PRINT 'Routes: BELL-D12 - Send BizMate documents already present.';
GO

-- ---------------------------------------------------------------------------
-- Add it to the existing flow so it can be test-run from the Flows screen
-- ---------------------------------------------------------------------------
DECLARE @FlowId  BIGINT = (SELECT Id FROM dbo.Flows WHERE Title = 'BizMate EU - Inbound EDI');
DECLARE @RouteId INT    = (SELECT Id FROM dbo.Routes WHERE Name = 'BELL-D12 - Send BizMate documents');

IF @FlowId IS NOT NULL AND @RouteId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.FlowDetails WHERE FlowId = @FlowId AND RouteId = @RouteId)
BEGIN
    INSERT INTO dbo.FlowDetails (FlowId, RouteId, Status, In_Out, FrequencyType, StartDate, EndDate,
                                 RepeatCount, WeekDays, OnDay, ExecutionTime, CreatedDate, CreatedBy)
    VALUES (@FlowId, @RouteId, 'Active', 'Out', 'Minutely', GETDATE(), DATEADD(year, 3, GETDATE()),
            5, '', '', '', GETDATE(), 1);

    PRINT 'FlowDetails: route added to the BizMate EU flow';
END
GO

-- ---------------------------------------------------------------------------
-- Verify
-- ---------------------------------------------------------------------------
SELECT 'RouteType' AS Item, CAST(Id AS varchar(10)) + ' ' + Name AS Value FROM dbo.RouteTypes WHERE Id = 601
UNION ALL SELECT 'Route', CAST(Id AS varchar(10)) + ' ' + Name + ' [' + Status + ']' FROM dbo.Routes WHERE Name = 'BELL-D12 - Send BizMate documents'
UNION ALL SELECT 'Connector', Name FROM dbo.Connectors WHERE Name IN ('BELL-D12 - EDI Folder', 'BizMate - EDI Bridge')
UNION ALL SELECT 'In VW_Routes', CASE WHEN EXISTS (SELECT 1 FROM dbo.VW_Routes WHERE Name = 'BELL-D12 - Send BizMate documents')
                                      THEN 'visible' ELSE 'NOT VISIBLE - a foreign key is missing' END
UNION ALL SELECT 'In flow', CAST(COUNT(*) AS varchar(10)) + ' route(s) in BizMate EU - Inbound EDI'
            FROM dbo.FlowDetails fd JOIN dbo.Flows f ON f.Id = fd.FlowId WHERE f.Title = 'BizMate EU - Inbound EDI';
GO
