-- VW_EDITrace - the ESyncMateTraceRecord shape, exactly as the contract fixes it.
--
-- Contract: M1-Contracts/esyncmate-trace-api.md, contract version 1.0 (2026-09-03).
--
-- This view exists for two reasons. It is what the trace read API (W2-13) projects, so the API is
-- a thin authenticated read rather than a second definition of the record. And it is, on its own,
-- the agreed fallback (W2-16, D-17): the contract states that a read-only view carrying exactly
-- these column names and this vocabulary is acceptable in place of the HTTP endpoint, with BizMate
-- swapping its client for a table reader behind the same interface. Keeping the column names
-- identical to the JSON field names is what makes that swap free, so do not "correct" them to the
-- PascalCase used elsewhere in this database.
--
-- handedOffAt is one field in the contract but two facts in the ledger, because inbound and
-- outbound hand off to different places: inbound it is when POST /inbound returned 200, outbound
-- it is when the transmission left eSyncMate for the partner. The CASE below is that mapping and
-- is the only place it should live.
--
-- Both lookups the contract names are covered seeks against UX_EDILedger_TransmissionReference
-- and IX_EDILedger_PartnerControl, which carry every column below in their INCLUDE lists so the
-- 5-second budget is met without touching the base table.

CREATE VIEW [dbo].[VW_EDITrace] AS
SELECT
    L.[TransmissionReference]                       AS [transmissionReference],
    L.[PartnerId]                                   AS [partnerId],
    L.[PartnerControlNo]                            AS [partnerControlNo],
    L.[Direction]                                   AS [direction],
    L.[DocumentType]                                AS [documentType],
    L.[MapName]                                     AS [mapName],
    L.[MapVersion]                                  AS [mapVersion],
    L.[ReceivedAt]                                  AS [receivedAt],
    L.[TranslatedAt]                                AS [translatedAt],
    L.[Outcome]                                     AS [outcome],
    L.[ErrorDetail]                                 AS [errorDetail],
    CASE L.[Direction]
        WHEN 'In'  THEN L.[HandedToBizMateAt]
        WHEN 'Out' THEN L.[DeliveredToPartnerAt]
    END                                             AS [handedOffAt],
    L.[BizMateMessageId]                            AS [bizmateMessageId],
    L.[RawArtifactRef]                              AS [rawArtifactRef]
FROM
    [dbo].[EDILedger] L WITH (NOLOCK);
GO
