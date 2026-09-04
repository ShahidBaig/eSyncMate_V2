-- EDILedger - the per-document record (W2-01, requirements E1, E10, E11, E12, E13, E17, E18).
--
-- One row per document, both directions, every carrier, created BEFORE the document is
-- translated or processed (W2-02). This replaces route-execution logging as the unit of record.
--
-- Two things about this table are deliberate and must not be "tidied" later:
--
--   OrderId is NULLABLE. Today's artifact store hangs off OrderData, whose OrderId is NOT NULL
--   with an inner join to Orders, so a document that never becomes an order - a malformed 850,
--   an unmappable partner code, an 824, a 997, a 846 feed, an 860 for a PO we do not hold - has
--   nowhere to be retained at all. Those are precisely the documents E10 and E12 exist to make
--   visible. The ledger is order-independent by construction.
--
--   Direction is from BIZMATE's point of view, not eSyncMate's, because that is the vocabulary
--   the trace contract fixes: 'In' is partner -> BizMate, 'Out' is BizMate -> partner.
--
-- Contract: M1-Contracts/esyncmate-trace-api.md (field names and vocabulary), auth-and-signing.md.

CREATE TABLE [dbo].[EDILedger] (
    [Id]                    BIGINT IDENTITY(1,1) NOT NULL,

    -- Identity linkage (W2-06). BizMate's keys are echoed here, never invented by eSyncMate.
    [TransmissionReference] NVARCHAR(100)        NOT NULL,   -- eSyncMate's own id, both directions
    [PartnerId]             NVARCHAR(50)         NOT NULL,
    -- Inbound: the partner's ISA/GS control number. Outbound: BizMate's own partnerControlNo,
    -- echoed back, which is what the trace contract asks for on an outbound record.
    [PartnerControlNo]      NVARCHAR(50)         NULL,

    -- The control number that actually crossed the wire in the envelope. On an outbound
    -- interchange this is OURS: EQ-01 settles that eSyncMate assigns its own ISA and GS control
    -- numbers rather than borrowing BizMate's, because eSyncMate already owns that monotonic
    -- sequence per partner and the parallel run would otherwise risk a collision the partner sees
    -- first. It is a separate fact from PartnerControlNo and cannot share the column: an inbound
    -- 997 arrives quoting THIS number, which is what W2-10 correlates on.
    [InterchangeControlNo]  NVARCHAR(50)         NULL,
    [CustomerNo]            NVARCHAR(50)         NULL,       -- BizMate customer the document belongs to
    [CorrelationId]         NVARCHAR(50)         NOT NULL,   -- X-Correlation-Id, join key in the trace
    [BizMateMessageId]      BIGINT               NULL,       -- messageId returned by BizMate
    -- BizMate's duplicate flag. True is a SUCCESS - BizMate already holds this document (W1-15).
    -- Not nullable: it is only meaningful once HandedToBizMateAt is set, and that column already
    -- says whether we posted at all, so a third "unknown" state would carry no information.
    [BizMateDuplicate]      BIT                  NOT NULL CONSTRAINT DF_EDILedger_Duplicate DEFAULT 0,

    -- Classification.
    --
    -- Format and Mechanism are two fields in the contract, not one, and Mechanism is DERIVED:
    -- X12/EDIFACT -> RawEDI, CSV/FixedWidth/XML -> FlatFile, DBMap -> DBMap (openapi.yaml, the
    -- Format schema). Format is the truth about the artifact; Mechanism is the coarse carrier
    -- BizMate keys artifact rendering, acknowledgement expectations, the duplicate key and board
    -- filtering off. CK_EDILedger_MechanismDerivation below makes the two unable to disagree.
    [Direction]             VARCHAR(3)           NOT NULL,   -- In | Out, from BizMate's point of view
    [DocumentType]          VARCHAR(10)          NOT NULL,   -- 850, 856, ... Document Catalog codes
    [Family]                VARCHAR(12)          NULL,       -- Order | Consignment (W2-05), never inferred
    [Format]                VARCHAR(12)          NOT NULL,   -- what the artifact actually is (E5, W2-04)
    [Mechanism]             VARCHAR(10)          NOT NULL,   -- derived from Format; never set independently
    [Channel]               VARCHAR(3)           NOT NULL CONSTRAINT DF_EDILedger_Channel DEFAULT 'EDI',
    [Provenance]            VARCHAR(20)          NOT NULL,   -- how the row reached us (W4-24, AD-01)

    -- Translation
    [MapName]               NVARCHAR(100)        NULL,
    [MapVersion]            NVARCHAR(20)         NULL,
    [Outcome]               VARCHAR(12)          NOT NULL,   -- Pending | Translated | Failed | Rejected
    [ErrorDetail]           NVARCHAR(MAX)        NULL,       -- required whenever Outcome <> Translated

    -- Hop timestamps (W2-07), all UTC. The trace contract's single handedOffAt is derived from
    -- these two per direction - see VW_EDITrace.
    [ReceivedAt]            DATETIME2(3)         NOT NULL,   -- inbound: arrival; outbound: collection
    [TranslatedAt]          DATETIME2(3)         NULL,
    [HandedToBizMateAt]     DATETIME2(3)         NULL,       -- inbound: POST /inbound returned 200
    [DeliveredToPartnerAt]  DATETIME2(3)         NULL,       -- outbound: left eSyncMate for the partner
    [AcknowledgedAt]        DATETIME2(3)         NULL,       -- 997/CONTRL closed, or API confirmation

    -- Artifact pointer (W2-03). The artifact rows themselves live in EDILedgerArtifact.
    [RawArtifactRef]        NVARCHAR(400)        NULL,

    -- Optional links into the operational tables. All nullable on purpose - see the note above.
    [OrderId]               INT                  NULL,
    [RouteId]               INT                  NULL,
    [CustomerId]            INT                  NULL,

    [CreatedDate]           DATETIME2(3)         NOT NULL CONSTRAINT DF_EDILedger_CreatedDate DEFAULT SYSUTCDATETIME(),
    [CreatedBy]             INT                  NOT NULL,
    [ModifiedDate]          DATETIME2(3)         NULL,
    [ModifiedBy]            INT                  NULL,

    CONSTRAINT PK_EDILedger PRIMARY KEY CLUSTERED ([Id] ASC),

    CONSTRAINT CK_EDILedger_Direction  CHECK ([Direction] IN ('In','Out')),
    CONSTRAINT CK_EDILedger_Outcome    CHECK ([Outcome]   IN ('Pending','Translated','Failed','Rejected')),
    CONSTRAINT CK_EDILedger_Family     CHECK ([Family]    IS NULL OR [Family] IN ('Order','Consignment')),
    CONSTRAINT CK_EDILedger_Format     CHECK ([Format]    IN ('X12','EDIFACT','JSON','CSV','FixedWidth','XML','DBMap')),
    CONSTRAINT CK_EDILedger_Mechanism  CHECK ([Mechanism] IN ('RawEDI','FlatFile','DBMap')),
    CONSTRAINT CK_EDILedger_Channel    CHECK ([Channel]   IN ('EDI','API')),
    CONSTRAINT CK_EDILedger_Provenance CHECK ([Provenance] IN ('Wire','PreProcessor','ExternalWriter','Outbox','Marketplace','Doorway')),

    -- The derivation the contract states, enforced rather than trusted.
    --
    -- This is the ER-07 guard made structural. Under AD-01 a CSV file is parsed and staged into
    -- the DB-map tables, so there is a standing temptation to relabel it DBMap because that is how
    -- it travelled internally. That would misreport the partner relationship on BizMate's boards.
    -- With this constraint the relabel is not a policy anybody has to remember - the row simply
    -- will not insert.
    --
    -- JSON has no mechanism mapping in the contract and is only reachable on the API channel; see
    -- EQ-15, which also asks where marketplace traffic sits now that PartnerAPI turns out not to
    -- exist in the M1 vocabulary. Tighten this branch once that is answered.
    CONSTRAINT CK_EDILedger_MechanismDerivation CHECK (
           ([Format] IN ('X12','EDIFACT')          AND [Mechanism] = 'RawEDI')
        OR ([Format] IN ('CSV','FixedWidth','XML') AND [Mechanism] = 'FlatFile')
        OR ([Format] = 'DBMap'                     AND [Mechanism] = 'DBMap')
        OR ([Format] = 'JSON'                      AND [Channel]   = 'API')
    ),

    -- A document that did not translate must say why (W2-08). "It arrived and broke, here is why"
    -- is the whole difference this table exists to make.
    CONSTRAINT CK_EDILedger_ErrorDetail CHECK ([Outcome] IN ('Pending','Translated') OR [ErrorDetail] IS NOT NULL)
);
GO

-- Trace lookup 1: by transmissionReference. Also the uniqueness guarantee for our own id.
CREATE UNIQUE NONCLUSTERED INDEX UX_EDILedger_TransmissionReference
    ON [dbo].[EDILedger] ([TransmissionReference] ASC)
    INCLUDE ([PartnerId], [PartnerControlNo], [Direction], [DocumentType], [MapName], [MapVersion],
             [ReceivedAt], [TranslatedAt], [Outcome], [HandedToBizMateAt], [DeliveredToPartnerAt],
             [BizMateMessageId], [RawArtifactRef]);
GO

-- Trace lookup 2: by (partnerId, partnerControlNo). Both indexes carry the whole trace record so
-- the 5-second budget is met from a covering seek and never a key lookup, let alone a scan (W2-14).
CREATE NONCLUSTERED INDEX IX_EDILedger_PartnerControl
    ON [dbo].[EDILedger] ([PartnerId] ASC, [PartnerControlNo] ASC)
    INCLUDE ([TransmissionReference], [Direction], [DocumentType], [MapName], [MapVersion],
             [ReceivedAt], [TranslatedAt], [Outcome], [HandedToBizMateAt], [DeliveredToPartnerAt],
             [BizMateMessageId], [RawArtifactRef]);
GO

-- W2-10: an inbound 997 or CONTRL quotes the interchange or group control number of the
-- transmission it acknowledges. This is the seek that resolves it back to the outbound row.
CREATE NONCLUSTERED INDEX IX_EDILedger_InterchangeControlNo
    ON [dbo].[EDILedger] ([PartnerId] ASC, [InterchangeControlNo] ASC)
    INCLUDE ([TransmissionReference], [DocumentType], [DeliveredToPartnerAt], [AcknowledgedAt])
    WHERE [InterchangeControlNo] IS NOT NULL;
GO

-- Correlating BizMate's reply back to our row, and the X-Correlation-Id join.
CREATE NONCLUSTERED INDEX IX_EDILedger_BizMateMessageId
    ON [dbo].[EDILedger] ([BizMateMessageId] ASC)
    WHERE [BizMateMessageId] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX IX_EDILedger_CorrelationId
    ON [dbo].[EDILedger] ([CorrelationId] ASC);
GO

-- Worklists: what is still open, and what failed. Filtered so the index stays small as the
-- table grows - the overwhelming majority of rows settle to Translated and are never scanned.
CREATE NONCLUSTERED INDEX IX_EDILedger_Open
    ON [dbo].[EDILedger] ([Outcome] ASC, [ReceivedAt] ASC)
    INCLUDE ([PartnerId], [DocumentType], [Direction], [Mechanism], [ErrorDetail])
    WHERE [Outcome] IN ('Pending','Failed','Rejected');
GO

-- Acknowledgement ageing (W2-11): what is delivered but not yet acknowledged. Branching on
-- mechanism matters because only RawEDI ever receives a 997; FlatFile and DBMap acknowledge at the
-- API confirmation on the inbound post and must never be aged waiting for one that cannot arrive.
CREATE NONCLUSTERED INDEX IX_EDILedger_AwaitingAck
    ON [dbo].[EDILedger] ([Mechanism] ASC, [DeliveredToPartnerAt] ASC)
    INCLUDE ([PartnerId], [PartnerControlNo], [DocumentType])
    WHERE [AcknowledgedAt] IS NULL AND [Direction] = 'Out';
GO
