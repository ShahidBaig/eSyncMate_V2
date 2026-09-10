-- 15_EDIPartnerStateReport.sql
--
-- What eSyncMate has told BizMate on the partner-state feed (W2-17, E18).
--
-- Two reasons this exists rather than a static dictionary in the process.
--
-- The deciding one: RouteEngine:UseExternalProcess is true, so every route execution is a fresh
-- short-lived RouteWorker process. Any in-memory latch is born empty on every pass and dies with
-- the process - which is exactly what happened when MapDisabled was first built: it reported at
-- 17:55:07 and again at 18:00:09, five minutes later, having "remembered" nothing.
--
-- The one that would have justified it anyway: until now, a partner state we sent BizMate existed
-- only as a RouteLog line. Nothing could answer "what have we told them, and when" without reading
-- log text, which is the same argument the ledger itself rests on.
--
-- Append-only. The latest row per partner is the current state; the rows before it are the history
-- of how it got there. Only states BizMate actually accepted are written - a report that failed to
-- send is not a report, and must be tried again on the next pass.
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EDIPartnerStateReport')
BEGIN
    CREATE TABLE dbo.EDIPartnerStateReport
    (
        Id           BIGINT        IDENTITY(1,1) NOT NULL,
        PartnerId    VARCHAR(50)   NOT NULL,
        State        VARCHAR(20)   NOT NULL,
        Detail       VARCHAR(1000) NULL,
        ReportedAt   DATETIME      NOT NULL,
        CreatedDate  DATETIME      NOT NULL,
        CreatedBy    INT           NOT NULL,

        CONSTRAINT PK_EDIPartnerStateReport PRIMARY KEY CLUSTERED (Id),

        -- The same four the contract defines. A fifth state is a contract change, not a typo.
        CONSTRAINT CK_EDIPartnerStateReport_State
            CHECK (State IN ('Up','Down','MapDisabled','QueueBacklog'))
    );

    PRINT 'Created dbo.EDIPartnerStateReport.';
END
ELSE
BEGIN
    PRINT 'dbo.EDIPartnerStateReport already exists.';
END
GO

-- The only read this table serves: the latest state for one partner. Keyed so it is a seek to the
-- top of the partner's rows rather than a scan that grows with the history.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EDIPartnerStateReport_Latest')
BEGIN
    CREATE NONCLUSTERED INDEX IX_EDIPartnerStateReport_Latest
        ON dbo.EDIPartnerStateReport (PartnerId, Id DESC);

    PRINT 'Created IX_EDIPartnerStateReport_Latest.';
END
GO

SELECT TableName = 'EDIPartnerStateReport', Rows = COUNT(*) FROM dbo.EDIPartnerStateReport;
GO
