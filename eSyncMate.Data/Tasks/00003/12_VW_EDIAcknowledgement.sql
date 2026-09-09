-- ============================================================================
-- Task 00003 - EDI & API Integration with BizMate EU - W3-19 (E11)
-- Run in: ESYNCMATE_EU  (also safe in ESYNCMATE_TEST)
-- Run AFTER: 01 (EDILedger, EDILedgerLink)
--
-- VW_EDIAcknowledgement - the correlation, made readable.
--
-- W2-10 and W2-12 built the resolution: an inbound 997 or 824 is matched to the
-- outbound message it answers, the two ledger rows are linked, and the verdict is
-- written onto the row that was answered. All of that is true and none of it is
-- visible - EDILedgerLink is two bigints and a type, and reading it means joining
-- EDILedger to itself twice and knowing which side is which.
--
-- This is that join, once, with the columns named from the reader's point of view
-- rather than the table's: what answered, what it answered, what it said.
--
-- It is a NEW view rather than columns on VW_EDITrace. That view is the published
-- trace contract - fourteen fields, in order, agreed with BizMate - and adding to
-- it would be changing a contract to save writing a join.
--
-- Idempotent - CREATE OR ALTER. Safe to re-run.
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

CREATE OR ALTER VIEW dbo.VW_EDIAcknowledgement
AS
SELECT
    -- The link itself
    linkId                  = l.Id,
    linkType                = l.LinkType,          -- Acknowledges | Rejects | Responds
    resolvedBy              = l.ResolvedBy,        -- Automatic | Manual
    resolvedOn              = l.ResolvedOn,        -- which key matched, in words
    detail                  = l.Detail,            -- what the partner actually said
    linkedAt                = l.CreatedDate,

    -- What answered: the inbound 997 or 824
    answerLedgerId          = a.Id,
    answerDocumentType      = a.DocumentType,
    answerPartnerControlNo  = a.PartnerControlNo,
    answerReceivedAt        = a.ReceivedAt,
    answerReference         = a.TransmissionReference,

    -- What it answered: the outbound document
    documentLedgerId        = d.Id,
    documentType            = d.DocumentType,
    partnerId               = d.PartnerId,
    interchangeControlNo    = d.InterchangeControlNo,   -- what the partner quoted back
    partnerControlNo        = d.PartnerControlNo,       -- BizMate's own number for it
    bizmateMessageId        = d.BizMateMessageId,
    transmissionReference   = d.TransmissionReference,
    deliveredToPartnerAt    = d.DeliveredToPartnerAt,
    acknowledgedAt          = d.AcknowledgedAt,
    outcome                 = d.Outcome,
    documentErrorDetail     = d.ErrorDetail,

    -- The one question an operator actually asks of this table
    needsResending          = CASE WHEN l.LinkType = 'Rejects' THEN 1 ELSE 0 END
FROM dbo.EDILedgerLink AS l
INNER JOIN dbo.EDILedger AS a ON a.Id = l.FromLedgerId
INNER JOIN dbo.EDILedger AS d ON d.Id = l.ToLedgerId;
GO

-- ---------------------------------------------------------------------------
-- Verify
-- ---------------------------------------------------------------------------
SELECT 'View' AS Item, CASE WHEN OBJECT_ID('dbo.VW_EDIAcknowledgement') IS NULL THEN 'MISSING' ELSE 'present' END AS Value
UNION ALL
SELECT 'Rows', CAST(COUNT(*) AS varchar(10)) FROM dbo.VW_EDIAcknowledgement
UNION ALL
SELECT 'Needing a resend', CAST(COUNT(*) AS varchar(10)) FROM dbo.VW_EDIAcknowledgement WHERE needsResending = 1;
GO

SET NOEXEC OFF;
GO
