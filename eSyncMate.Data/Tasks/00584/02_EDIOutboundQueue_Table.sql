-- ============================================================================
-- Task 00584 - EDI & API Integration with BizMate EU - W1-14
-- Run in: ESYNCMATE_EU  (also safe in ESYNCMATE_TEST)
-- Run AFTER: 01 (the queue references EDILedger)
--
-- Durable store-and-forward for calls to BizMate. The requirement is explicit
-- that a BizMate restart must never lose a document; this table makes that true
-- of our own restart as well, because the call is persisted BEFORE it is
-- attempted and there is no in-memory state a crash could drop.
--
-- What is queued is the CALL, not the document. A document already has its
-- ledger row; this holds what still has to be said to BizMate about it, in a
-- form that replays verbatim after any interruption.
--
-- The three commitments made in answer to EQ-11, implemented here and in the
-- indexes below:
--
--   Ordering  FIFO within a partner. A call is not attempted while an older one
--             for the same partner is outstanding, so a document cannot
--             overtake its own predecessor. The claim enforces this, not a
--             convention a caller could forget.
--   Fairness  No ordering across partners, deliberately. One partner's backlog
--             must not starve the others, so the drain round-robins.
--   Shedding  Never. The queue blocks rather than discards. An accepted call is
--             delivered or visibly failed. Pressure is handled by refusing
--             ACCEPTANCE at a stated ceiling, so the refusal is a record rather
--             than a document that quietly disappeared.
--
-- Idempotent - every object guarded. Safe to re-run.
-- ============================================================================
SET NOCOUNT ON;

IF DB_NAME() NOT LIKE 'ESYNCMATE%'
BEGIN
    RAISERROR('Wrong database context (%s). Run this script in ESYNCMATE_EU.', 16, 1, @@SERVERNAME);
    RETURN;
END

IF OBJECT_ID(N'dbo.EDILedger', N'U') IS NULL
BEGIN
    RAISERROR('dbo.EDILedger does not exist. Run 01_EDI_Ledger_Tables.sql first.', 16, 1);
    RETURN;
END

IF OBJECT_ID(N'dbo.EDIOutboundQueue', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EDIOutboundQueue (
        Id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EDIOutboundQueue PRIMARY KEY CLUSTERED,

        -- Nullable because not every call is about a document: a partner-state
        -- report is about a partner.
        LedgerId         BIGINT        NULL CONSTRAINT FK_EDIOutboundQueue_Ledger REFERENCES dbo.EDILedger (Id),

        -- The FIFO scope. Every ordering guarantee here is per-partner, nothing more.
        PartnerId        NVARCHAR(50)  NOT NULL,

        -- Enough to replay the call exactly as first built, after any interruption.
        Operation        VARCHAR(30)   NOT NULL CONSTRAINT CK_EDIOutboundQueue_Operation
                                       CHECK (Operation IN ('RegisterRaw','PostInbound','PostAck',
                                                            'MarkFetched','MarkDelivered','PartnerState')),
        HttpMethod       VARCHAR(10)   NOT NULL,
        PublicPath       NVARCHAR(400) NOT NULL,   -- what gets signed; never the query string
        UrlPathWithQuery NVARCHAR(600) NULL,       -- what gets requested, when they differ
        Scope            NVARCHAR(200) NOT NULL,   -- least authority this call needs (W1-03)
        Payload          NVARCHAR(MAX) NULL,       -- the request body, verbatim

        -- One per logical document exchange, stable across retries, so every
        -- attempt carries the same join key into BizMate's Document Trace (X-04).
        CorrelationId    NVARCHAR(50)  NOT NULL,

        Status           VARCHAR(12)   NOT NULL CONSTRAINT DF_EDIOutboundQueue_Status DEFAULT 'Pending'
                                       CONSTRAINT CK_EDIOutboundQueue_Status
                                       CHECK (Status IN ('Pending','InFlight','Succeeded','Failed')),
        AttemptCount     INT           NOT NULL CONSTRAINT DF_EDIOutboundQueue_Attempts DEFAULT 0,
        NextAttemptAt    DATETIME2(3)  NOT NULL CONSTRAINT DF_EDIOutboundQueue_NextAttempt DEFAULT SYSUTCDATETIME(),
        LastAttemptAt    DATETIME2(3)  NULL,
        LastError        NVARCHAR(MAX) NULL,
        CompletedAt      DATETIME2(3)  NULL,

        -- Set when a claim is taken, so a drain interrupted mid-flight can be
        -- reclaimed rather than left InFlight forever by a process that is no
        -- longer running - which would block that partner permanently, because
        -- the FIFO rule will not step over an in-flight row.
        ClaimedAt        DATETIME2(3)  NULL,
        ClaimedBy        NVARCHAR(100) NULL,

        CreatedDate      DATETIME2(3)  NOT NULL CONSTRAINT DF_EDIOutboundQueue_CreatedDate DEFAULT SYSUTCDATETIME(),
        CreatedBy        INT           NOT NULL,
        ModifiedDate     DATETIME2(3)  NULL,
        ModifiedBy       INT           NULL,

        -- A call that stopped trying must say why. Same reasoning as the ledger:
        -- an operator needs the reason, and "it failed" is not one.
        CONSTRAINT CK_EDIOutboundQueue_FailedReason
            CHECK (Status <> 'Failed' OR LastError IS NOT NULL)
    );

    PRINT 'Created dbo.EDIOutboundQueue';
END
ELSE
    PRINT 'dbo.EDIOutboundQueue already exists.';
GO

-- The drain query, and the only index the hot path uses. Ordered by
-- (PartnerId, Id) because Id is the arrival order and FIFO within a partner is
-- the guarantee. Filtered because settled rows are the overwhelming majority
-- and must not be paged through to find work.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EDIOutboundQueue_Drain' AND object_id = OBJECT_ID(N'dbo.EDIOutboundQueue'))
    CREATE NONCLUSTERED INDEX IX_EDIOutboundQueue_Drain
        ON dbo.EDIOutboundQueue (PartnerId ASC, NextAttemptAt ASC, Id ASC)
        INCLUDE (Operation, AttemptCount, Status)
        WHERE Status IN ('Pending','InFlight');

-- Reclaiming a claim abandoned by a process that died mid-flight.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EDIOutboundQueue_Stale' AND object_id = OBJECT_ID(N'dbo.EDIOutboundQueue'))
    CREATE NONCLUSTERED INDEX IX_EDIOutboundQueue_Stale
        ON dbo.EDIOutboundQueue (ClaimedAt ASC)
        INCLUDE (PartnerId, Id)
        WHERE Status = 'InFlight';

-- Everything still owed about one document.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EDIOutboundQueue_Ledger' AND object_id = OBJECT_ID(N'dbo.EDIOutboundQueue'))
    CREATE NONCLUSTERED INDEX IX_EDIOutboundQueue_Ledger
        ON dbo.EDIOutboundQueue (LedgerId ASC)
        WHERE LedgerId IS NOT NULL;

PRINT 'dbo.EDIOutboundQueue indexes verified.';
PRINT 'Script 02 complete.';
GO
