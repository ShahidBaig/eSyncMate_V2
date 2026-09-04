-- EDILedgerLink - one document's relationship to another (W2-10, W2-12, W3-19).
--
-- Several requirements are all the same shape: a document arrives that is *about* a document we
-- already hold. A 997 acknowledges an outbound interchange. An 824 rejects a specific document and
-- BizMate moves the original to RejectedByPartner from that link. An 860 changes an 850. Rather
-- than adding a nullable "the other one" column to EDILedger per case, the relationship is a row.
--
-- FromLedgerId is the arriving document; ToLedgerId is the one it refers to. Both are ledger rows,
-- so an 824 that cannot be resolved to its target is itself still a first-class record with no
-- link - which is the visible failure W2-12 wants, not a dropped document.

CREATE TABLE [dbo].[EDILedgerLink] (
    [Id]            BIGINT IDENTITY(1,1) NOT NULL,
    [FromLedgerId]  BIGINT               NOT NULL,   -- the arriving document
    [ToLedgerId]    BIGINT               NOT NULL,   -- the document it refers to
    [LinkType]      VARCHAR(20)          NOT NULL,

    -- How the link was established, so a human-corrected link is distinguishable from one the
    -- correlator found on its own. Both are valid; only one of them is evidence the rules work.
    [ResolvedBy]    VARCHAR(20)          NOT NULL CONSTRAINT DF_EDILedgerLink_ResolvedBy DEFAULT 'Automatic',
    [ResolvedOn]    NVARCHAR(100)        NULL,       -- which key matched, e.g. 'ISA control number'
    [Detail]        NVARCHAR(MAX)        NULL,       -- partner status codes and free text on an 824

    [CreatedDate]   DATETIME2(3)         NOT NULL CONSTRAINT DF_EDILedgerLink_CreatedDate DEFAULT SYSUTCDATETIME(),
    [CreatedBy]     INT                  NOT NULL,

    CONSTRAINT PK_EDILedgerLink PRIMARY KEY CLUSTERED ([Id] ASC),

    CONSTRAINT FK_EDILedgerLink_From FOREIGN KEY ([FromLedgerId]) REFERENCES [dbo].[EDILedger] ([Id]),
    CONSTRAINT FK_EDILedgerLink_To   FOREIGN KEY ([ToLedgerId])   REFERENCES [dbo].[EDILedger] ([Id]),

    CONSTRAINT CK_EDILedgerLink_Type CHECK
        ([LinkType] IN ('Acknowledges','Rejects','Changes','Responds','Replaces','Invoices')),

    CONSTRAINT CK_EDILedgerLink_ResolvedBy CHECK ([ResolvedBy] IN ('Automatic','Manual')),

    -- A document cannot be about itself.
    CONSTRAINT CK_EDILedgerLink_NotSelf CHECK ([FromLedgerId] <> [ToLedgerId])
);
GO

-- One link of a given type between the same two documents. Re-running a correlator must not
-- accumulate duplicates.
CREATE UNIQUE NONCLUSTERED INDEX UX_EDILedgerLink_Edge
    ON [dbo].[EDILedgerLink] ([FromLedgerId] ASC, [ToLedgerId] ASC, [LinkType] ASC);
GO

-- "What is this document's history" - the reverse lookup, which is the one the trace and the
-- worklists actually ask.
CREATE NONCLUSTERED INDEX IX_EDILedgerLink_To
    ON [dbo].[EDILedgerLink] ([ToLedgerId] ASC, [LinkType] ASC)
    INCLUDE ([FromLedgerId], [ResolvedBy], [CreatedDate]);
GO
