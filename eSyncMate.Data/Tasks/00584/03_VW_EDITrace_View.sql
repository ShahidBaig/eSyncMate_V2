-- ============================================================================
-- Task 00584 - EDI & API Integration with BizMate EU - W2-13 / W2-16
-- Run in: ESYNCMATE_EU  (also safe in ESYNCMATE_TEST)
-- Run AFTER: 01 (the view reads EDILedger)
--
-- The ESyncMateTraceRecord shape, exactly as the contract fixes it.
-- Contract: M1-Contracts/esyncmate-trace-api.md, version 1.0 (2026-09-03).
--
-- This view exists for two reasons. It is what the trace read API projects, so
-- the API is a thin authenticated read rather than a second definition of the
-- record. And it is, on its own, the agreed fallback: the contract states that a
-- read-only view carrying exactly these column names and this vocabulary is
-- acceptable in place of the HTTP endpoint, with BizMate swapping its client for
-- a table reader behind the same interface.
--
-- Keeping the column names identical to the JSON field names is what makes that
-- swap free. DO NOT "correct" them to the PascalCase used elsewhere in this
-- database - the lowerCamelCase is the contract.
--
-- handedOffAt is one field in the contract but two facts in the ledger, because
-- inbound and outbound hand off to different places: inbound it is when
-- POST /inbound returned 200, outbound it is when the transmission left
-- eSyncMate for the partner. The CASE below is that mapping and is the only
-- place it should live.
--
-- Idempotent - CREATE OR ALTER. Safe to re-run.
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
GO

CREATE OR ALTER VIEW dbo.VW_EDITrace AS
SELECT
    L.TransmissionReference                     AS transmissionReference,
    L.PartnerId                                 AS partnerId,
    L.PartnerControlNo                          AS partnerControlNo,
    L.Direction                                 AS direction,
    L.DocumentType                              AS documentType,
    L.MapName                                   AS mapName,
    L.MapVersion                                AS mapVersion,
    L.ReceivedAt                                AS receivedAt,
    L.TranslatedAt                              AS translatedAt,
    L.Outcome                                   AS outcome,
    L.ErrorDetail                               AS errorDetail,
    CASE L.Direction
        WHEN 'In'  THEN L.HandedToBizMateAt
        WHEN 'Out' THEN L.DeliveredToPartnerAt
    END                                         AS handedOffAt,
    L.BizMateMessageId                          AS bizmateMessageId,
    L.RawArtifactRef                            AS rawArtifactRef
FROM
    dbo.EDILedger L WITH (NOLOCK);
GO

PRINT 'Created or updated dbo.VW_EDITrace.';
PRINT 'Script 03 complete.';
GO
