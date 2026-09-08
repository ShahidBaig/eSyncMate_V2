-- ============================================================================
-- Task 00003 - EDI & API Integration with BizMate EU - W2-01 / W2-03 / W2-10
-- Run in: ESYNCMATE_EU  (also safe in ESYNCMATE_TEST)
--
-- The per-document ledger. Replaces route-execution logging as the unit of
-- record, which is the mechanism E10, E11, E12, E13, E17 and E18 all become
-- features of.
--
-- Two things here are deliberate and must not be "tidied" later:
--
--   OrderId is NULLABLE. The ledger covers every carrier and every document,
--   and a document need not have become an order - a malformed 850, an 824, a
--   997, an 846 feed - to be recorded.
--
--   The raw X12 artifact is NOT copied here (AD-02, 2026-09-08). eSyncMate
--   already retains every interchange in InboundEDI, with the ISA and GS
--   identity in InboundEDIInfo, and what it sends in OutboundEDI. The ledger
--   links to those rows; EDILedgerArtifact is kept for the flat-file and
--   DB-map carriers and for the as-sent and canonical stages.
--
--   Direction is from BIZMATE's point of view, not eSyncMate's, because that is
--   the vocabulary the trace contract fixes: 'In' is partner -> BizMate.
--
-- The invariant the service enforces on top of this schema: LEDGER FIRST - the
-- row and the registered artifact both exist before anything is translated, so
-- a document that dies still dies visibly.
--
-- CONVERGENT, not just idempotent. Safe to re-run, and safe to run over an
-- earlier revision of these tables: it adds InterchangeControlNo, widens the
-- mechanism vocabulary to PartnerAPI, makes BizMateDuplicate NOT NULL, and adds
-- the AD-02 links to InboundEDI and OutboundEDI plus BizMateRawFileRef if an
-- earlier version is already deployed.
-- ============================================================================
SET NOCOUNT ON;

IF DB_NAME() NOT LIKE 'ESYNCMATE%'
BEGIN
    RAISERROR('Wrong database context (%s). Run this script in ESYNCMATE_EU.', 16, 1, @@SERVERNAME);
    RETURN;
END

-- ---------------------------------------------------------------------------
-- EDILedger - one row per document, both directions, every carrier
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.EDILedger', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EDILedger (
        Id                    BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EDILedger PRIMARY KEY CLUSTERED,

        -- Identity linkage (W2-06). BizMate's keys are echoed, never invented here.
        TransmissionReference NVARCHAR(100) NOT NULL,
        PartnerId             NVARCHAR(50)  NOT NULL,
        PartnerControlNo      NVARCHAR(50)  NULL,
        InterchangeControlNo  NVARCHAR(50)  NULL,
        CustomerNo            NVARCHAR(50)  NULL,
        CorrelationId         NVARCHAR(50)  NOT NULL,
        BizMateMessageId      BIGINT        NULL,
        BizMateDuplicate      BIT           NOT NULL CONSTRAINT DF_EDILedger_Duplicate DEFAULT 0,

        -- Classification
        Direction             VARCHAR(3)    NOT NULL,
        DocumentType          VARCHAR(10)   NOT NULL,
        Family                VARCHAR(12)   NULL,
        Format                VARCHAR(12)   NOT NULL,
        Mechanism             VARCHAR(10)   NOT NULL,
        Channel               VARCHAR(3)    NOT NULL CONSTRAINT DF_EDILedger_Channel DEFAULT 'EDI',
        Provenance            VARCHAR(20)   NOT NULL,

        -- Translation
        MapName               NVARCHAR(100) NULL,
        MapVersion            NVARCHAR(20)  NULL,
        Outcome               VARCHAR(12)   NOT NULL,
        ErrorDetail           NVARCHAR(MAX) NULL,

        -- Hop timestamps (W2-07), all UTC
        ReceivedAt            DATETIME2(3)  NOT NULL,
        TranslatedAt          DATETIME2(3)  NULL,
        HandedToBizMateAt     DATETIME2(3)  NULL,
        DeliveredToPartnerAt  DATETIME2(3)  NULL,
        AcknowledgedAt        DATETIME2(3)  NULL,

        RawArtifactRef        NVARCHAR(400) NULL,

        -- All nullable on purpose - see the header note.
        OrderId               INT           NULL,
        RouteId               INT           NULL,
        CustomerId            INT           NULL,

        CreatedDate           DATETIME2(3)  NOT NULL CONSTRAINT DF_EDILedger_CreatedDate DEFAULT SYSUTCDATETIME(),
        CreatedBy             INT           NOT NULL,
        ModifiedDate          DATETIME2(3)  NULL,
        ModifiedBy            INT           NULL
    );

    PRINT 'Created dbo.EDILedger';
END
ELSE
    PRINT 'dbo.EDILedger already exists - converging below.';
GO

-- --- Column added after the first revision (W2-09 / EQ-01) ------------------
-- PartnerControlNo and the control number that actually crosses the wire are
-- two different facts. On an outbound interchange the second is OURS, because
-- eSyncMate assigns its own ISA and GS control numbers rather than borrowing
-- BizMate's. An inbound 997 quotes THIS number, so it is what W2-10 correlates
-- on and it cannot share a column with the echoed BizMate value.
IF COL_LENGTH('dbo.EDILedger', 'InterchangeControlNo') IS NULL
BEGIN
    ALTER TABLE dbo.EDILedger ADD InterchangeControlNo NVARCHAR(50) NULL;
    PRINT 'Added dbo.EDILedger.InterchangeControlNo';
END
GO

-- --- BizMateDuplicate: NULL in the first revision, NOT NULL now -------------
-- It is only meaningful once HandedToBizMateAt is set, and that column already
-- says whether we posted at all, so a third "unknown" state carried nothing.
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID(N'dbo.EDILedger') AND name = 'BizMateDuplicate' AND is_nullable = 1)
BEGIN
    UPDATE dbo.EDILedger SET BizMateDuplicate = 0 WHERE BizMateDuplicate IS NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_EDILedger_Duplicate')
        ALTER TABLE dbo.EDILedger ADD CONSTRAINT DF_EDILedger_Duplicate DEFAULT 0 FOR BizMateDuplicate;

    ALTER TABLE dbo.EDILedger ALTER COLUMN BizMateDuplicate BIT NOT NULL;

    PRINT 'dbo.EDILedger.BizMateDuplicate is now NOT NULL';
END
GO

-- --- Check constraints -----------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_EDILedger_Direction')
    ALTER TABLE dbo.EDILedger ADD CONSTRAINT CK_EDILedger_Direction CHECK (Direction IN ('In','Out'));

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_EDILedger_Outcome')
    ALTER TABLE dbo.EDILedger ADD CONSTRAINT CK_EDILedger_Outcome
        CHECK (Outcome IN ('Pending','Translated','Failed','Rejected'));

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_EDILedger_Family')
    ALTER TABLE dbo.EDILedger ADD CONSTRAINT CK_EDILedger_Family
        CHECK (Family IS NULL OR Family IN ('Order','Consignment'));

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_EDILedger_Format')
    ALTER TABLE dbo.EDILedger ADD CONSTRAINT CK_EDILedger_Format
        CHECK (Format IN ('X12','EDIFACT','JSON','CSV','FixedWidth','XML','DBMap'));

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_EDILedger_Channel')
    ALTER TABLE dbo.EDILedger ADD CONSTRAINT CK_EDILedger_Channel CHECK (Channel IN ('EDI','API'));

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_EDILedger_Provenance')
    ALTER TABLE dbo.EDILedger ADD CONSTRAINT CK_EDILedger_Provenance
        CHECK (Provenance IN ('Wire','PreProcessor','ExternalWriter','Outbox','Marketplace','Doorway'));

-- A document that did not translate must say why (W2-08). This is the whole
-- difference between "we never received it" and "it arrived and broke, here is
-- why" expressed as a constraint, so a silent failure cannot reach the table.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_EDILedger_ErrorDetail')
    ALTER TABLE dbo.EDILedger ADD CONSTRAINT CK_EDILedger_ErrorDetail
        CHECK (Outcome IN ('Pending','Translated') OR ErrorDetail IS NOT NULL);
GO

-- --- Mechanism vocabulary: widen to PartnerAPI (D-31) ----------------------
-- BizMate added PartnerAPI in their task script 40, AFTER the M1 contract was
-- frozen, so openapi.yaml 1.0 still enumerates three values while their shipped
-- database allows four. Follow the database.
--
-- The guard reads sys.check_constraints.definition, which is the NORMALISED
-- text, and uses CHARINDEX rather than LIKE - square brackets in a LIKE pattern
-- are a character class, which is not what is wanted here.
DECLARE @MechDefn NVARCHAR(MAX);

SELECT @MechDefn = definition
FROM sys.check_constraints
WHERE name = 'CK_EDILedger_Mechanism' AND parent_object_id = OBJECT_ID(N'dbo.EDILedger');

IF @MechDefn IS NULL
BEGIN
    ALTER TABLE dbo.EDILedger ADD CONSTRAINT CK_EDILedger_Mechanism
        CHECK (Mechanism IN ('RawEDI','FlatFile','DBMap','PartnerAPI'));
    PRINT 'Added CK_EDILedger_Mechanism (4 values).';
END
ELSE IF CHARINDEX('PartnerAPI', @MechDefn) = 0
BEGIN
    ALTER TABLE dbo.EDILedger DROP CONSTRAINT CK_EDILedger_Mechanism;
    ALTER TABLE dbo.EDILedger ADD CONSTRAINT CK_EDILedger_Mechanism
        CHECK (Mechanism IN ('RawEDI','FlatFile','DBMap','PartnerAPI'));
    PRINT 'Widened CK_EDILedger_Mechanism to allow PartnerAPI.';
END
ELSE
    PRINT 'CK_EDILedger_Mechanism already allows PartnerAPI - no change.';
GO

-- --- Format drives Mechanism, with one deliberate exception ----------------
-- Deriving rather than accepting is what stops a flat file that AD-01 staged
-- through the DB-map tables being relabelled DBMap and misreporting the partner
-- relationship on BizMate's boards (ER-07). With this constraint the relabel is
-- not a rule anybody has to remember - the row will not insert.
--
-- PartnerAPI is the exception: it is NOT derivable from an artifact format,
-- because a marketplace document arrives as JSON like any other API push, so
-- eSyncMate declares it on the inbound call.
DECLARE @DerivDefn NVARCHAR(MAX);

SELECT @DerivDefn = definition
FROM sys.check_constraints
WHERE name = 'CK_EDILedger_MechanismDerivation' AND parent_object_id = OBJECT_ID(N'dbo.EDILedger');

IF @DerivDefn IS NOT NULL AND CHARINDEX('PartnerAPI', @DerivDefn) = 0
BEGIN
    ALTER TABLE dbo.EDILedger DROP CONSTRAINT CK_EDILedger_MechanismDerivation;
    SET @DerivDefn = NULL;
    PRINT 'Dropped the pre-PartnerAPI CK_EDILedger_MechanismDerivation.';
END

IF @DerivDefn IS NULL
BEGIN
    ALTER TABLE dbo.EDILedger ADD CONSTRAINT CK_EDILedger_MechanismDerivation CHECK (
           Mechanism = 'PartnerAPI'
        OR (Format IN ('X12','EDIFACT')          AND Mechanism = 'RawEDI')
        OR (Format IN ('CSV','FixedWidth','XML') AND Mechanism = 'FlatFile')
        OR (Format = 'DBMap'                     AND Mechanism = 'DBMap'));
    PRINT 'Added CK_EDILedger_MechanismDerivation.';
END
ELSE
    PRINT 'CK_EDILedger_MechanismDerivation already current - no change.';
GO

-- --- AD-02 (2026-09-08): link to the raw X12 rows instead of copying them ---
-- eSyncMate already retains every X12 interchange in InboundEDI (with the ISA
-- and GS identity per interchange in InboundEDIInfo) and what it sends in
-- OutboundEDI; Orders.InboundEDIId points TO that record. F-3 was wrong for
-- X12 (F-14). The ledger therefore links to those rows, and EDILedgerArtifact
-- is kept for the flat-file and DB-map carriers and for the as-sent and
-- canonical stages no existing table holds.
--
-- BizMateRawFileRef: the rawFileRef BizMate returns from POST /raw. The trace
-- contract defines rawArtifactRef as OUR reference ("BizMate does not
-- dereference it"); the first revision stored BizMate's there (F-21).
IF COL_LENGTH('dbo.EDILedger', 'InboundEDIId') IS NULL
BEGIN
    ALTER TABLE dbo.EDILedger ADD InboundEDIId INT NULL;
    PRINT 'Added dbo.EDILedger.InboundEDIId (AD-02)';
END

IF COL_LENGTH('dbo.EDILedger', 'OutboundEDIId') IS NULL
BEGIN
    ALTER TABLE dbo.EDILedger ADD OutboundEDIId INT NULL;
    PRINT 'Added dbo.EDILedger.OutboundEDIId (AD-02)';
END

IF COL_LENGTH('dbo.EDILedger', 'BizMateRawFileRef') IS NULL
BEGIN
    ALTER TABLE dbo.EDILedger ADD BizMateRawFileRef BIGINT NULL;
    PRINT 'Added dbo.EDILedger.BizMateRawFileRef (F-21)';
END
GO

-- Real foreign keys where the target tables exist (they do in ESYNCMATE_EU).
-- Guarded so the script still runs in a database that never carried the X12
-- tables.
IF OBJECT_ID(N'dbo.InboundEDI', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EDILedger_InboundEDI')
    ALTER TABLE dbo.EDILedger ADD CONSTRAINT FK_EDILedger_InboundEDI
        FOREIGN KEY (InboundEDIId) REFERENCES dbo.InboundEDI (Id);

IF OBJECT_ID(N'dbo.OutboundEDI', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EDILedger_OutboundEDI')
    ALTER TABLE dbo.EDILedger ADD CONSTRAINT FK_EDILedger_OutboundEDI
        FOREIGN KEY (OutboundEDIId) REFERENCES dbo.OutboundEDI (Id);

-- An inbound record cannot point at something we sent, nor an outbound one at
-- something we received.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_EDILedger_RawLinkDirection')
    ALTER TABLE dbo.EDILedger ADD CONSTRAINT CK_EDILedger_RawLinkDirection CHECK (
           (Direction = 'In'  AND OutboundEDIId IS NULL)
        OR (Direction = 'Out' AND InboundEDIId  IS NULL));

PRINT 'AD-02 raw-row links verified.';
GO

-- --- Indexes ---------------------------------------------------------------
-- Both trace lookups carry every column the trace record needs in their INCLUDE
-- lists, so each is a covering seek and the contract's 5-second budget is met
-- without touching the base table (W2-14).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_EDILedger_TransmissionReference' AND object_id = OBJECT_ID(N'dbo.EDILedger'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_EDILedger_TransmissionReference
        ON dbo.EDILedger (TransmissionReference ASC)
        INCLUDE (PartnerId, PartnerControlNo, Direction, DocumentType, MapName, MapVersion,
                 ReceivedAt, TranslatedAt, Outcome, HandedToBizMateAt, DeliveredToPartnerAt,
                 BizMateMessageId, RawArtifactRef);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EDILedger_PartnerControl' AND object_id = OBJECT_ID(N'dbo.EDILedger'))
    CREATE NONCLUSTERED INDEX IX_EDILedger_PartnerControl
        ON dbo.EDILedger (PartnerId ASC, PartnerControlNo ASC)
        INCLUDE (TransmissionReference, Direction, DocumentType, MapName, MapVersion,
                 ReceivedAt, TranslatedAt, Outcome, HandedToBizMateAt, DeliveredToPartnerAt,
                 BizMateMessageId, RawArtifactRef);

-- An inbound 997 or CONTRL quotes the interchange or group control number of
-- the transmission it acknowledges. This is the seek that resolves it (W2-10).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EDILedger_InterchangeControlNo' AND object_id = OBJECT_ID(N'dbo.EDILedger'))
    CREATE NONCLUSTERED INDEX IX_EDILedger_InterchangeControlNo
        ON dbo.EDILedger (PartnerId ASC, InterchangeControlNo ASC)
        INCLUDE (TransmissionReference, DocumentType, DeliveredToPartnerAt, AcknowledgedAt)
        WHERE InterchangeControlNo IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EDILedger_BizMateMessageId' AND object_id = OBJECT_ID(N'dbo.EDILedger'))
    CREATE NONCLUSTERED INDEX IX_EDILedger_BizMateMessageId
        ON dbo.EDILedger (BizMateMessageId ASC)
        WHERE BizMateMessageId IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EDILedger_CorrelationId' AND object_id = OBJECT_ID(N'dbo.EDILedger'))
    CREATE NONCLUSTERED INDEX IX_EDILedger_CorrelationId
        ON dbo.EDILedger (CorrelationId ASC);

-- Worklists. Filtered so the index stays small as the table grows: the
-- overwhelming majority of rows settle to Translated and are never scanned.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EDILedger_Open' AND object_id = OBJECT_ID(N'dbo.EDILedger'))
    CREATE NONCLUSTERED INDEX IX_EDILedger_Open
        ON dbo.EDILedger (Outcome ASC, ReceivedAt ASC)
        INCLUDE (PartnerId, DocumentType, Direction, Mechanism, ErrorDetail)
        WHERE Outcome IN ('Pending','Failed','Rejected');

-- Acknowledgement ageing (W2-11). Branching on mechanism matters because only
-- RawEDI ever receives a 997; FlatFile, DBMap and PartnerAPI acknowledge at the
-- API confirmation and must never be aged waiting for one that cannot arrive.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EDILedger_AwaitingAck' AND object_id = OBJECT_ID(N'dbo.EDILedger'))
    CREATE NONCLUSTERED INDEX IX_EDILedger_AwaitingAck
        ON dbo.EDILedger (Mechanism ASC, DeliveredToPartnerAt ASC)
        INCLUDE (PartnerId, PartnerControlNo, DocumentType)
        WHERE AcknowledgedAt IS NULL AND Direction = 'Out';

-- AD-02: from a raw row back to its ledger record - what the test loop's
-- verifier (X-13) and the order screens ask when they hold an InboundEDI or
-- OutboundEDI id.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EDILedger_InboundEDI' AND object_id = OBJECT_ID(N'dbo.EDILedger'))
    CREATE NONCLUSTERED INDEX IX_EDILedger_InboundEDI
        ON dbo.EDILedger (InboundEDIId ASC)
        INCLUDE (TransmissionReference, DocumentType, Outcome)
        WHERE InboundEDIId IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EDILedger_OutboundEDI' AND object_id = OBJECT_ID(N'dbo.EDILedger'))
    CREATE NONCLUSTERED INDEX IX_EDILedger_OutboundEDI
        ON dbo.EDILedger (OutboundEDIId ASC)
        INCLUDE (TransmissionReference, DocumentType, Outcome)
        WHERE OutboundEDIId IS NOT NULL;

PRINT 'dbo.EDILedger indexes verified.';
GO

-- ---------------------------------------------------------------------------
-- EDILedgerArtifact - the thing that actually crossed the wire (W2-03, E10)
-- ---------------------------------------------------------------------------
-- Content is VARBINARY, not NVARCHAR. The hash is over the raw bytes exactly as
-- sent or received, and a byte-order mark, a CRLF that should have been an LF,
-- or a re-encoding would each change it. Storing text and re-encoding on the way
-- out makes the hash unreproducible, which defeats holding the artifact at all.
--
-- Every version is kept. VW_OrderData shows only the most recent artifact per
-- order and type, so a re-sent 856 hides its predecessor; a dispute phrased as
-- "we sent you that order on the 4th" needs the one from the 4th.
IF OBJECT_ID(N'dbo.EDILedgerArtifact', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EDILedgerArtifact (
        Id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EDILedgerArtifact PRIMARY KEY CLUSTERED,
        LedgerId        BIGINT         NOT NULL CONSTRAINT FK_EDILedgerArtifact_Ledger REFERENCES dbo.EDILedger (Id),

        Stage           VARCHAR(20)    NOT NULL CONSTRAINT CK_EDILedgerArtifact_Stage
                                       CHECK (Stage IN ('AsReceived','AsSent','Canonical','Rendered')),
        -- PartnerAPI is a MECHANISM, not a format: a marketplace artifact is JSON.
        FormatLabel     VARCHAR(12)    NOT NULL CONSTRAINT CK_EDILedgerArtifact_Format
                                       CHECK (FormatLabel IN ('X12','EDIFACT','JSON','CSV','FixedWidth','XML','DBMap')),
        ContentEncoding VARCHAR(20)    NOT NULL CONSTRAINT DF_EDILedgerArtifact_Encoding DEFAULT 'utf-8',

        ContentHash     CHAR(64)       NOT NULL,
        SizeBytes       BIGINT         NOT NULL,
        Content         VARBINARY(MAX) NULL,
        ExternalRef     NVARCHAR(400)  NULL,

        -- EQ-09: 24 months live, 7 years cold archive. ExpiresAt is set on insert
        -- from the policy rather than computed later by a sweeper guessing intent.
        ExpiresAt       DATETIME2(3)   NULL,
        ArchivedAt      DATETIME2(3)   NULL,

        CreatedDate     DATETIME2(3)   NOT NULL CONSTRAINT DF_EDILedgerArtifact_CreatedDate DEFAULT SYSUTCDATETIME(),
        CreatedBy       INT            NOT NULL,

        -- The bytes live here or somewhere else, but they exist somewhere. An
        -- artifact row with neither is a record of nothing.
        CONSTRAINT CK_EDILedgerArtifact_Content CHECK (Content IS NOT NULL OR ExternalRef IS NOT NULL)
    );

    PRINT 'Created dbo.EDILedgerArtifact';
END
ELSE
    PRINT 'dbo.EDILedgerArtifact already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EDILedgerArtifact_Ledger' AND object_id = OBJECT_ID(N'dbo.EDILedgerArtifact'))
    CREATE NONCLUSTERED INDEX IX_EDILedgerArtifact_Ledger
        ON dbo.EDILedgerArtifact (LedgerId ASC, Stage ASC, CreatedDate DESC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EDILedgerArtifact_ContentHash' AND object_id = OBJECT_ID(N'dbo.EDILedgerArtifact'))
    CREATE NONCLUSTERED INDEX IX_EDILedgerArtifact_ContentHash
        ON dbo.EDILedgerArtifact (ContentHash ASC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EDILedgerArtifact_DueForArchive' AND object_id = OBJECT_ID(N'dbo.EDILedgerArtifact'))
    CREATE NONCLUSTERED INDEX IX_EDILedgerArtifact_DueForArchive
        ON dbo.EDILedgerArtifact (ExpiresAt ASC)
        INCLUDE (LedgerId, SizeBytes)
        WHERE ArchivedAt IS NULL AND ExpiresAt IS NOT NULL;

PRINT 'dbo.EDILedgerArtifact indexes verified.';
GO

-- ---------------------------------------------------------------------------
-- EDILedgerLink - one document's relationship to another (W2-10, W2-12, W3-19)
-- ---------------------------------------------------------------------------
-- A 997 acknowledges an outbound interchange, an 824 rejects a specific
-- document, an 860 changes an 850. All the same shape, so the relationship is a
-- row rather than a nullable column per case. An arriving document that cannot
-- be resolved to its target is still a first-class ledger record with no link,
-- which is the visible failure W2-12 wants rather than a dropped document.
IF OBJECT_ID(N'dbo.EDILedgerLink', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EDILedgerLink (
        Id           BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EDILedgerLink PRIMARY KEY CLUSTERED,
        FromLedgerId BIGINT        NOT NULL CONSTRAINT FK_EDILedgerLink_From REFERENCES dbo.EDILedger (Id),
        ToLedgerId   BIGINT        NOT NULL CONSTRAINT FK_EDILedgerLink_To   REFERENCES dbo.EDILedger (Id),
        LinkType     VARCHAR(20)   NOT NULL CONSTRAINT CK_EDILedgerLink_Type
                                   CHECK (LinkType IN ('Acknowledges','Rejects','Changes','Responds','Replaces','Invoices')),

        -- Distinguishes a human-corrected link from one the correlator found on
        -- its own. Both are valid; only one is evidence the rules work.
        ResolvedBy   VARCHAR(20)   NOT NULL CONSTRAINT DF_EDILedgerLink_ResolvedBy DEFAULT 'Automatic'
                                   CONSTRAINT CK_EDILedgerLink_ResolvedBy CHECK (ResolvedBy IN ('Automatic','Manual')),
        ResolvedOn   NVARCHAR(100) NULL,
        Detail       NVARCHAR(MAX) NULL,

        CreatedDate  DATETIME2(3)  NOT NULL CONSTRAINT DF_EDILedgerLink_CreatedDate DEFAULT SYSUTCDATETIME(),
        CreatedBy    INT           NOT NULL,

        CONSTRAINT CK_EDILedgerLink_NotSelf CHECK (FromLedgerId <> ToLedgerId)
    );

    PRINT 'Created dbo.EDILedgerLink';
END
ELSE
    PRINT 'dbo.EDILedgerLink already exists.';
GO

-- One link of a given type between the same two documents: re-running a
-- correlator must not accumulate duplicates.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_EDILedgerLink_Edge' AND object_id = OBJECT_ID(N'dbo.EDILedgerLink'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_EDILedgerLink_Edge
        ON dbo.EDILedgerLink (FromLedgerId ASC, ToLedgerId ASC, LinkType ASC);

-- "What is this document's history" - the reverse lookup, which is the one the
-- trace and the worklists actually ask.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EDILedgerLink_To' AND object_id = OBJECT_ID(N'dbo.EDILedgerLink'))
    CREATE NONCLUSTERED INDEX IX_EDILedgerLink_To
        ON dbo.EDILedgerLink (ToLedgerId ASC, LinkType ASC)
        INCLUDE (FromLedgerId, ResolvedBy, CreatedDate);

PRINT 'dbo.EDILedgerLink indexes verified.';
PRINT 'Script 01 complete.';
GO
