-- ============================================================================
-- Task 00003 - EDI & API Integration with BizMate EU - F-39
-- Run in: ESYNCMATE_EU  (also safe in ESYNCMATE_TEST)
-- Run AFTER: the F-39 code fix is deployed - not before.
--
-- Retires the duplicate outbound ledger rows the pre-fix builds left behind.
--
-- Until F-39, BizMateOutboundPipeline inserted a new EDILedger row on every
-- attempt at a document instead of returning to the row that document already
-- had. BizMate re-serves anything not yet Delivered, so each pass minted a
-- fresh row: 18 ASNs refused by the ciphertext guard produced 18 more Failed
-- rows every five minutes, and the 40 documents that eventually delivered left
-- their earlier Pending rows behind as orphans.
--
-- The code fix stops the growth. This retires what already accumulated, so the
-- ledger says one row per document and "how many are pending" can be answered
-- by counting.
--
-- ROW OF RECORD, per (PartnerId, BizMateMessageId) on Direction='Out':
--   the delivered row if the document ever went out; otherwise the lowest id,
--   which is the row the fixed code now reuses.
--
-- Nothing that carries evidence is touched. A row is retired only when it has
-- no artifact, no link, no OutboundEDI/InboundEDI row, and was never delivered
-- or acknowledged - i.e. only rows that record an attempt that produced
-- nothing. Delivered rows, and the artifacts hanging off them, are the audit
-- trail and stay.
--
-- REPORTS ONLY unless @Commit = 1. Read the report first.
-- Idempotent: a second run finds nothing left to do.
-- ============================================================================
-- sqlcmd connects with QUOTED_IDENTIFIER OFF, and EDILedger carries indexes that refuse a
-- DELETE under that setting. Set it here rather than relying on the client (the report ran
-- fine and the DELETE then failed with Msg 1934, which reads like a permissions problem and
-- is not one).
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_WARNINGS ON;
SET NOCOUNT ON;

DECLARE @Commit BIT = 0;   -- set to 1 to actually retire the rows

IF DB_NAME() NOT LIKE 'ESYNCMATE%'
BEGIN
    RAISERROR('Wrong database context (%s). Run this script in ESYNCMATE_EU.', 16, 1, @@SERVERNAME);
    SET NOEXEC ON;
END
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_WARNINGS ON;

DECLARE @Commit BIT = 0;   -- keep in step with the value above

IF OBJECT_ID('tempdb..#Redundant') IS NOT NULL DROP TABLE #Redundant;

;WITH RowOfRecord AS
(
    SELECT
        PartnerId,
        BizMateMessageId,
        KeepId = COALESCE(
                     MIN(CASE WHEN DeliveredToPartnerAt IS NOT NULL THEN Id END),
                     MIN(Id))
    FROM dbo.EDILedger
    WHERE Direction = 'Out'
      AND BizMateMessageId IS NOT NULL
    GROUP BY PartnerId, BizMateMessageId
)
SELECT l.Id, l.PartnerId, l.BizMateMessageId, l.DocumentType, l.Outcome, l.ReceivedAt, r.KeepId
INTO #Redundant
FROM dbo.EDILedger AS l
INNER JOIN RowOfRecord AS r
        ON r.PartnerId        = l.PartnerId
       AND r.BizMateMessageId = l.BizMateMessageId
WHERE l.Direction = 'Out'
  AND l.Id <> r.KeepId
  -- never a row that carries evidence of something having happened
  AND l.DeliveredToPartnerAt IS NULL
  AND l.AcknowledgedAt       IS NULL
  AND l.OutboundEDIId        IS NULL
  AND l.InboundEDIId         IS NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.EDILedgerArtifact a WHERE a.LedgerId    = l.Id)
  AND NOT EXISTS (SELECT 1 FROM dbo.EDILedgerLink     k WHERE k.FromLedgerId = l.Id OR k.ToLedgerId = l.Id);

PRINT '--- F-39 reconciliation ---';

SELECT DocumentType, Outcome, Redundant = COUNT(*), Documents = COUNT(DISTINCT BizMateMessageId)
FROM #Redundant
GROUP BY DocumentType, Outcome
ORDER BY DocumentType, Outcome;

SELECT TotalRedundantRows = COUNT(*), DocumentsAffected = COUNT(DISTINCT BizMateMessageId) FROM #Redundant;

SELECT OutboundRowsAfter = (SELECT COUNT(*) FROM dbo.EDILedger WHERE Direction = 'Out')
                         - (SELECT COUNT(*) FROM #Redundant),
       DistinctDocuments  = (SELECT COUNT(DISTINCT BizMateMessageId) FROM dbo.EDILedger WHERE Direction = 'Out');

IF @Commit = 1
BEGIN
    BEGIN TRANSACTION;

    DELETE l
    FROM dbo.EDILedger AS l
    INNER JOIN #Redundant AS r ON r.Id = l.Id;

    PRINT 'Retired ' + CAST(@@ROWCOUNT AS VARCHAR(20)) + ' redundant outbound ledger row(s).';

    COMMIT TRANSACTION;
END
ELSE
BEGIN
    PRINT 'Report only - nothing was changed. Set @Commit = 1 (both declarations) to retire the rows above.';
END

DROP TABLE #Redundant;
GO

SET NOEXEC OFF;
GO
