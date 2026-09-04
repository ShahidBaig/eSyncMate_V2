-- EDIOutboundQueue - durable store-and-forward for calls to BizMate (W1-14, requirement E22).
--
-- The requirement is explicit that a BizMate restart must never lose a document. Every call
-- eSyncMate makes to BizMate is persisted here before it is attempted, so the queue survives our
-- own restart as well as theirs: there is no in-memory state that a process crash could drop.
--
-- What is queued is the CALL, not the document. A document already has its ledger row (W2-01); this
-- table holds what still has to be said to BizMate about it, in a form that can be replayed
-- verbatim after any interruption.
--
-- The answers this table implements, from EQ-11:
--   Ordering  - FIFO within a partner. A call is not attempted while an older one for the same
--               partner is still outstanding, so a document cannot overtake its own predecessor.
--   Fairness  - no ordering across partners, deliberately. One partner's backlog must not starve
--               the others, so the drain is fair-scheduled across partners rather than global FIFO.
--   Shedding  - never. The queue blocks rather than discards. An accepted call is delivered or is
--               visibly failed; it is never silently dropped to relieve pressure. The ceiling is
--               enforced at the point of ACCEPTANCE instead, so the refusal is visible at the
--               boundary rather than being a document that quietly disappeared.

CREATE TABLE [dbo].[EDIOutboundQueue] (
    [Id]                BIGINT IDENTITY(1,1) NOT NULL,

    -- The document this call is about. Nullable because not every call has one - a partner-state
    -- report is about a partner, not a document.
    [LedgerId]          BIGINT               NULL,

    -- The FIFO scope. Every ordering guarantee in this table is per-partner and nothing more.
    [PartnerId]         NVARCHAR(50)         NOT NULL,

    -- Enough to replay the call exactly as it was first built, after any interruption.
    [Operation]         VARCHAR(30)          NOT NULL,
    [HttpMethod]        VARCHAR(10)          NOT NULL,
    [PublicPath]        NVARCHAR(400)        NOT NULL,   -- what gets signed; never the query string
    [UrlPathWithQuery]  NVARCHAR(600)        NULL,       -- what gets requested, when they differ
    [Scope]             NVARCHAR(200)        NOT NULL,   -- least authority this call needs (W1-03)
    [Payload]           NVARCHAR(MAX)        NULL,       -- the request body, verbatim

    -- One per logical document exchange and stable across retries, so every attempt of the same
    -- call carries the same join key into BizMate's Document Trace (X-04).
    [CorrelationId]     NVARCHAR(50)         NOT NULL,

    [Status]            VARCHAR(12)          NOT NULL CONSTRAINT DF_EDIOutboundQueue_Status DEFAULT 'Pending',
    [AttemptCount]      INT                  NOT NULL CONSTRAINT DF_EDIOutboundQueue_Attempts DEFAULT 0,
    [NextAttemptAt]     DATETIME2(3)         NOT NULL CONSTRAINT DF_EDIOutboundQueue_NextAttempt DEFAULT SYSUTCDATETIME(),
    [LastAttemptAt]     DATETIME2(3)         NULL,
    [LastError]         NVARCHAR(MAX)        NULL,
    [CompletedAt]       DATETIME2(3)         NULL,

    -- Set when a claim is taken, so a drain interrupted mid-flight can be reclaimed rather than
    -- left InFlight forever by a process that is no longer running.
    [ClaimedAt]         DATETIME2(3)         NULL,
    [ClaimedBy]         NVARCHAR(100)        NULL,

    [CreatedDate]       DATETIME2(3)         NOT NULL CONSTRAINT DF_EDIOutboundQueue_CreatedDate DEFAULT SYSUTCDATETIME(),
    [CreatedBy]         INT                  NOT NULL,
    [ModifiedDate]      DATETIME2(3)         NULL,
    [ModifiedBy]        INT                  NULL,

    CONSTRAINT PK_EDIOutboundQueue PRIMARY KEY CLUSTERED ([Id] ASC),

    CONSTRAINT FK_EDIOutboundQueue_Ledger FOREIGN KEY ([LedgerId])
        REFERENCES [dbo].[EDILedger] ([Id]),

    CONSTRAINT CK_EDIOutboundQueue_Status CHECK
        ([Status] IN ('Pending','InFlight','Succeeded','Failed')),

    CONSTRAINT CK_EDIOutboundQueue_Operation CHECK
        ([Operation] IN ('RegisterRaw','PostInbound','PostAck','MarkFetched','MarkDelivered','PartnerState')),

    -- A call that stopped trying must say why. Same reasoning as the ledger: an operator needs the
    -- reason, and "it failed" is not one.
    CONSTRAINT CK_EDIOutboundQueue_FailedReason CHECK
        ([Status] <> 'Failed' OR [LastError] IS NOT NULL)
);
GO

-- The drain query, and the only index the hot path uses. Ordered by (PartnerId, Id) because Id is
-- the arrival order and FIFO within a partner is the guarantee; filtered because the settled rows
-- are the overwhelming majority and must not be paged through to find work.
CREATE NONCLUSTERED INDEX IX_EDIOutboundQueue_Drain
    ON [dbo].[EDIOutboundQueue] ([PartnerId] ASC, [NextAttemptAt] ASC, [Id] ASC)
    INCLUDE ([Operation], [AttemptCount], [Status])
    WHERE [Status] IN ('Pending','InFlight');
GO

-- Reclaiming a claim abandoned by a process that died mid-flight.
CREATE NONCLUSTERED INDEX IX_EDIOutboundQueue_Stale
    ON [dbo].[EDIOutboundQueue] ([ClaimedAt] ASC)
    INCLUDE ([PartnerId], [Id])
    WHERE [Status] = 'InFlight';
GO

-- Everything still owed about one document, for the ledger's own view of its outstanding work.
CREATE NONCLUSTERED INDEX IX_EDIOutboundQueue_Ledger
    ON [dbo].[EDIOutboundQueue] ([LedgerId] ASC)
    WHERE [LedgerId] IS NOT NULL;
GO
