-- EDILedgerArtifact - the thing that actually crossed the wire (W2-03, requirement E10).
--
-- Separate from EDILedger because artifacts are large, are written once and never updated, and
-- age out on a different schedule from the record that describes them (EQ-09: 24 months live,
-- 7 years cold archive).
--
-- Content is VARBINARY, not NVARCHAR. The hash is taken over the raw bytes exactly as sent or
-- received, and a byte-order mark, a CRLF that should have been an LF, or a re-encoding would all
-- change that hash. Storing text and re-encoding on the way out would make the hash unreproducible,
-- which defeats the point of holding the artifact at all. ContentEncoding says how to render it.
--
-- Every version is kept. The existing VW_OrderData shows only the most recent artifact per order
-- and type, so a re-sent 856 hides its predecessor from the interface; a dispute promise phrased
-- as "we sent you that order on the 4th" needs the one from the 4th, not the latest one.
--
-- For a flat file under AD-01 the artifact is the FILE, never the rows it stages into.

CREATE TABLE [dbo].[EDILedgerArtifact] (
    [Id]              BIGINT IDENTITY(1,1) NOT NULL,
    [LedgerId]        BIGINT               NOT NULL,

    -- Which representation this is. A single document can carry several: what arrived, what we
    -- produced, and the canonical payload we handed to BizMate.
    [Stage]           VARCHAR(20)          NOT NULL,   -- AsReceived | AsSent | Canonical | Rendered
    [FormatLabel]     VARCHAR(12)          NOT NULL,   -- the mechanism vocabulary, plus JSON
    [ContentEncoding] VARCHAR(20)          NOT NULL CONSTRAINT DF_EDILedgerArtifact_Encoding DEFAULT 'utf-8',

    [ContentHash]     CHAR(64)             NOT NULL,   -- lowercase hex SHA-256 of Content
    [SizeBytes]       BIGINT               NOT NULL,
    [Content]         VARBINARY(MAX)       NULL,       -- null once offloaded to ExternalRef
    [ExternalRef]     NVARCHAR(400)        NULL,       -- cold storage location after offload

    -- Retention (EQ-09). ExpiresAt is when this row leaves the live store for the archive; it is
    -- set on insert from the retention policy rather than computed by a sweeper guessing intent.
    [ExpiresAt]       DATETIME2(3)         NULL,
    [ArchivedAt]      DATETIME2(3)         NULL,

    [CreatedDate]     DATETIME2(3)         NOT NULL CONSTRAINT DF_EDILedgerArtifact_CreatedDate DEFAULT SYSUTCDATETIME(),
    [CreatedBy]       INT                  NOT NULL,

    CONSTRAINT PK_EDILedgerArtifact PRIMARY KEY CLUSTERED ([Id] ASC),

    CONSTRAINT FK_EDILedgerArtifact_Ledger FOREIGN KEY ([LedgerId])
        REFERENCES [dbo].[EDILedger] ([Id]),

    CONSTRAINT CK_EDILedgerArtifact_Stage CHECK ([Stage] IN ('AsReceived','AsSent','Canonical','Rendered')),

    -- The contract's Format vocabulary exactly (openapi.yaml). Note there is no PartnerAPI value
    -- anywhere in M1, despite the work plan assuming one - see EQ-15.
    CONSTRAINT CK_EDILedgerArtifact_Format CHECK
        ([FormatLabel] IN ('X12','EDIFACT','JSON','CSV','FixedWidth','XML','DBMap')),

    -- The bytes live here or somewhere else, but they exist somewhere. An artifact row with
    -- neither is a record of nothing.
    CONSTRAINT CK_EDILedgerArtifact_Content CHECK ([Content] IS NOT NULL OR [ExternalRef] IS NOT NULL)
);
GO

CREATE NONCLUSTERED INDEX IX_EDILedgerArtifact_Ledger
    ON [dbo].[EDILedgerArtifact] ([LedgerId] ASC, [Stage] ASC, [CreatedDate] DESC);
GO

-- Duplicate detection and "have we seen this exact file before" without reading the bytes.
CREATE NONCLUSTERED INDEX IX_EDILedgerArtifact_ContentHash
    ON [dbo].[EDILedgerArtifact] ([ContentHash] ASC);
GO

-- The retention sweeper's working set: what is due to leave the live store.
CREATE NONCLUSTERED INDEX IX_EDILedgerArtifact_DueForArchive
    ON [dbo].[EDILedgerArtifact] ([ExpiresAt] ASC)
    INCLUDE ([LedgerId], [SizeBytes])
    WHERE [ArchivedAt] IS NULL AND [ExpiresAt] IS NOT NULL;
GO
