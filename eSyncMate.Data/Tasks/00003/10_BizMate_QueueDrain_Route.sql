-- ============================================================================
-- Task 00003 - EDI & API Integration with BizMate EU - W1-19 (W1-14, E22)
-- Run in: ESYNCMATE_EU  (also safe in ESYNCMATE_TEST)
-- Run AFTER: 07 (this reuses the partner group and the BizMate bridge connector)
--
-- The store-and-forward queue's drain, as a route: RouteType 602
-- "BizMate - Drain queue" (BizMateQueueDrainRoute).
--
-- It was a Hangfire recurring job registered from Program.cs. As a route it takes
-- the same execution lock as everything else, writes RouteLog where operators
-- already look, appears in VW_Routes and the Flow interface, and can be test-run.
-- As a bare recurring job it was invisible to all of that and its only voice was
-- Console.WriteLine into a service window nobody reads.
--
-- One route, not one per partner. Fairness across partners is the drain's own
-- business - round-robin, oldest waiting first - and a route per partner would
-- hand that decision to the scheduler, which knows nothing about it.
--
-- It needs no destination connector. The route works the QUEUE, and the queue
-- already knows every partner and every call in it; the source connector is the
-- BizMate bridge only because VW_Routes INNER JOINs Connectors and a route with
-- no connector would vanish from the list (see the note on VW_Routes in
-- CLAUDE.md).
--
-- The old recurring job is removed by the code at start-up
-- (BizMateQueueDrainJob.Unregister), so an upgraded instance does not drain the
-- same queue on two schedules. Nothing to do here for that.
--
-- Idempotent - every row guarded by its natural key. Safe to re-run.
-- ============================================================================
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_WARNINGS ON;
SET NOCOUNT ON;

IF DB_NAME() NOT LIKE 'ESYNCMATE%'
BEGIN
    RAISERROR('Wrong database context (%s). Run this script in ESYNCMATE_EU.', 16, 1, @@SERVERNAME);
    SET NOEXEC ON;
END
GO

-- ---------------------------------------------------------------------------
-- RouteType 602
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.RouteTypes WHERE Id = 602)
BEGIN
    INSERT INTO dbo.RouteTypes (Id, Name, Description, CreatedDate, CreatedBy)
    VALUES (602, 'BizMate - Drain queue',
            'Retries the calls held in the BizMate store-and-forward queue, oldest waiting first and round-robin across partners, and reports a backed-up partner to BizMate as QueueBacklog. Nothing else in the pipeline retries: a route that hands a call to the queue is finished with it.',
            GETDATE(), 1);
    PRINT 'RouteTypes: added 602 BizMate - Drain queue';
END
ELSE
    PRINT 'RouteTypes: 602 already present.';
GO

-- ---------------------------------------------------------------------------
-- The route
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Routes WHERE Name = 'BizMate - Drain outbound queue')
BEGIN
    INSERT INTO dbo.Routes (Id, TypeId, Status, SourcePartyId, DestinationPartyId, SourceConnectorId, DestinationConnectorId,
                            MapId, PartyGroupId, CreatedDate, CreatedBy, FrequencyType, StartDate, EndDate, RepeatCount,
                            WeekDays, OnDay, ExecutionTime, JobID, Name, RouteGroup, CustomerName)
    VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.Routes),
            602, 'Active',
            -- Both sides are BizMate: the queue holds calls TO BizMate, and no
            -- partner transfer is touched by this route at all.
            (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = 'BIZMATE'),
            (SELECT Id FROM dbo.Customers WHERE ERPCustomerID = 'BIZMATE'),
            (SELECT Id FROM dbo.Connectors WHERE Name = 'BizMate - EDI Bridge'),
            (SELECT Id FROM dbo.Connectors WHERE Name = 'BizMate - EDI Bridge'),
            (SELECT Id FROM dbo.Maps WHERE Name = '850'),   -- VW_Routes LEFT JOINs Maps; any valid id is fine
            (SELECT Id FROM dbo.PartnerGroups WHERE Description = 'BELL-D12 - BizMate'),
            GETDATE(), 1, 'Minutely', GETDATE(), DATEADD(year, 3, GETDATE()), 5,
            '', '', '', NULL, 'BizMate - Drain outbound queue', 'BizMate', 'BELL-D12');

    PRINT 'Routes: added BizMate - Drain outbound queue (Active, no Hangfire job yet)';
END
ELSE
    PRINT 'Routes: BizMate - Drain outbound queue already present.';
GO

-- ---------------------------------------------------------------------------
-- Add it to the existing flow so it can be test-run from the Flows screen
-- ---------------------------------------------------------------------------
DECLARE @FlowId  BIGINT = (SELECT Id FROM dbo.Flows WHERE Title = 'BizMate EU - Inbound EDI');
DECLARE @RouteId INT    = (SELECT Id FROM dbo.Routes WHERE Name = 'BizMate - Drain outbound queue');

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
SELECT 'RouteType' AS Item, CAST(Id AS varchar(10)) + ' ' + Name AS Value FROM dbo.RouteTypes WHERE Id = 602
UNION ALL
SELECT 'Route', CAST(Id AS varchar(10)) + ' ' + Name + ' (' + Status + ')' FROM dbo.Routes WHERE TypeId = 602
UNION ALL
SELECT 'In VW_Routes', CAST(COUNT(*) AS varchar(10)) FROM dbo.VW_Routes WHERE TypeId = 602;
GO

SET NOEXEC OFF;
GO
