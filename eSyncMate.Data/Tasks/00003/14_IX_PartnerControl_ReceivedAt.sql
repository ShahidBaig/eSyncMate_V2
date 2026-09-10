-- ============================================================================
-- Task 00003 - EDI & API Integration with BizMate EU - W2-14 (E17)
-- Run in: ESYNCMATE_EU  (also safe in ESYNCMATE_TEST)
-- Run AFTER: 01 (EDILedger and its indexes)
--
-- Takes the sort out of trace lookup 2.
--
-- The trace contract promises an answer inside five seconds, and the second
-- lookup - by (partnerId, partnerControlNo) - has to return the NEWEST match,
-- because a partner reuses a control number across years and the current document
-- is the one being traced.
--
-- IX_EDILedger_PartnerControl is keyed on (PartnerId, PartnerControlNo) and
-- carries ReceivedAt as an INCLUDED column. Included columns are stored, not
-- ordered, so `ORDER BY receivedAt DESC` cannot be answered from the index:
-- measured on the live instance, the plan is
--
--     Top -> Sort(TOP 1, ORDER BY ReceivedAt DESC) -> Index Seek + key lookup
--
-- The sort is free today because a control number matches about one row. It stops
-- being free exactly when the item's own premise comes true - a partner reusing a
-- control number, and years of history behind it - and a TopN Sort over a growing
-- duplicate set is the shape of thing that quietly eats a five-second budget.
--
-- Moving ReceivedAt from the include list into the key, descending, means the
-- first row the seek meets is the answer. Everything else stays included, so the
-- lookup is otherwise unchanged.
--
-- NOT changed: the key lookup on the clustered index. Both trace lookups do one,
-- for ErrorDetail, which is NVARCHAR(MAX) - including a MAX column would bloat
-- every leaf page of the index to save one lookup per query. The lookup is
-- correct; what was wrong was the code comment calling these covering seeks, and
-- that is fixed in TraceController rather than here.
--
-- DROP_EXISTING keeps the name and rebuilds in one operation. Idempotent: the
-- guard below skips the rebuild once ReceivedAt is a key column.
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

IF EXISTS (
    SELECT 1
      FROM sys.indexes i
      JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
      JOIN sys.columns c        ON c.object_id  = ic.object_id AND c.column_id = ic.column_id
     WHERE i.object_id = OBJECT_ID('dbo.EDILedger')
       AND i.name      = 'IX_EDILedger_PartnerControl'
       AND c.name      = 'ReceivedAt'
       AND ic.is_included_column = 0)
BEGIN
    PRINT 'IX_EDILedger_PartnerControl already keys on ReceivedAt - nothing to do.';
END
ELSE
BEGIN
    CREATE NONCLUSTERED INDEX IX_EDILedger_PartnerControl
        ON dbo.EDILedger ([PartnerId] ASC, [PartnerControlNo] ASC, [ReceivedAt] DESC)
        INCLUDE ([TransmissionReference], [Direction], [DocumentType], [MapName], [MapVersion],
                 [TranslatedAt], [Outcome], [HandedToBizMateAt], [DeliveredToPartnerAt],
                 [BizMateMessageId], [RawArtifactRef])
        WITH (DROP_EXISTING = ON);

    PRINT 'IX_EDILedger_PartnerControl rebuilt with ReceivedAt DESC as a key column.';
END
GO

-- ---------------------------------------------------------------------------
-- Verify
-- ---------------------------------------------------------------------------
SELECT
    KeyColumns = STUFF((SELECT ', ' + c.name + CASE WHEN ic.is_descending_key = 1 THEN ' DESC' ELSE '' END
                          FROM sys.index_columns ic
                          JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                         WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0
                         ORDER BY ic.key_ordinal FOR XML PATH('')), 1, 2, ''),
    IncludedCount = (SELECT COUNT(*) FROM sys.index_columns ic
                      WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 1)
  FROM sys.indexes i
 WHERE i.object_id = OBJECT_ID('dbo.EDILedger')
   AND i.name = 'IX_EDILedger_PartnerControl';
GO

SET NOEXEC OFF;
GO
