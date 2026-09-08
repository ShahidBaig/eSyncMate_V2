-- ============================================================================
-- Task 00003 - EDI & API Integration with BizMate EU - deployment verification
-- Run in: ESYNCMATE_EU  (also safe in ESYNCMATE_TEST)
-- Run AFTER: 01, 02, 03
--
-- Reports what is actually deployed against what scripts 01 to 03 should have
-- produced, and says plainly whether the database matches the code. Reads only -
-- it changes nothing, so it is safe to run at any time, including in production.
--
-- Run this after the others rather than trusting their PRINT output: a script
-- that was interrupted part way still prints most of its progress messages.
-- ============================================================================
SET NOCOUNT ON;

DECLARE @Expected TABLE (Kind VARCHAR(12), ObjectName SYSNAME, ItemName SYSNAME);

INSERT INTO @Expected (Kind, ObjectName, ItemName) VALUES
    ('Table',  'EDILedger',          'EDILedger'),
    ('Table',  'EDILedgerArtifact',  'EDILedgerArtifact'),
    ('Table',  'EDILedgerLink',      'EDILedgerLink'),
    ('Table',  'EDIOutboundQueue',   'EDIOutboundQueue'),
    ('View',   'VW_EDITrace',        'VW_EDITrace'),

    ('Column', 'EDILedger',          'InterchangeControlNo'),
    ('Column', 'EDILedger',          'Format'),
    ('Column', 'EDILedger',          'Mechanism'),
    ('Column', 'EDILedger',          'Channel'),
    ('Column', 'EDILedger',          'Provenance'),

    ('Index',  'EDILedger',          'UX_EDILedger_TransmissionReference'),
    ('Index',  'EDILedger',          'IX_EDILedger_PartnerControl'),
    ('Index',  'EDILedger',          'IX_EDILedger_InterchangeControlNo'),
    ('Index',  'EDILedger',          'IX_EDILedger_BizMateMessageId'),
    ('Index',  'EDILedger',          'IX_EDILedger_CorrelationId'),
    ('Index',  'EDILedger',          'IX_EDILedger_Open'),
    ('Index',  'EDILedger',          'IX_EDILedger_AwaitingAck'),
    ('Index',  'EDILedgerArtifact',  'IX_EDILedgerArtifact_Ledger'),
    ('Index',  'EDILedgerArtifact',  'IX_EDILedgerArtifact_ContentHash'),
    ('Index',  'EDILedgerArtifact',  'IX_EDILedgerArtifact_DueForArchive'),
    ('Index',  'EDILedgerLink',      'UX_EDILedgerLink_Edge'),
    ('Index',  'EDILedgerLink',      'IX_EDILedgerLink_To'),
    ('Index',  'EDIOutboundQueue',   'IX_EDIOutboundQueue_Drain'),
    ('Index',  'EDIOutboundQueue',   'IX_EDIOutboundQueue_Stale'),
    ('Index',  'EDIOutboundQueue',   'IX_EDIOutboundQueue_Ledger'),

    ('Check',  'EDILedger',          'CK_EDILedger_Direction'),
    ('Check',  'EDILedger',          'CK_EDILedger_Outcome'),
    ('Check',  'EDILedger',          'CK_EDILedger_Family'),
    ('Check',  'EDILedger',          'CK_EDILedger_Format'),
    ('Check',  'EDILedger',          'CK_EDILedger_Mechanism'),
    ('Check',  'EDILedger',          'CK_EDILedger_Channel'),
    ('Check',  'EDILedger',          'CK_EDILedger_Provenance'),
    ('Check',  'EDILedger',          'CK_EDILedger_ErrorDetail'),
    ('Check',  'EDILedger',          'CK_EDILedger_MechanismDerivation'),
    ('Check',  'EDILedgerArtifact',  'CK_EDILedgerArtifact_Stage'),
    ('Check',  'EDILedgerArtifact',  'CK_EDILedgerArtifact_Format'),
    ('Check',  'EDILedgerArtifact',  'CK_EDILedgerArtifact_Content'),
    ('Check',  'EDILedgerLink',      'CK_EDILedgerLink_Type'),
    ('Check',  'EDILedgerLink',      'CK_EDILedgerLink_ResolvedBy'),
    ('Check',  'EDILedgerLink',      'CK_EDILedgerLink_NotSelf'),
    ('Check',  'EDIOutboundQueue',   'CK_EDIOutboundQueue_Status'),
    ('Check',  'EDIOutboundQueue',   'CK_EDIOutboundQueue_Operation'),
    ('Check',  'EDIOutboundQueue',   'CK_EDIOutboundQueue_FailedReason');

SELECT
    e.Kind,
    e.ObjectName,
    e.ItemName,
    CASE WHEN
        (e.Kind = 'Table'  AND OBJECT_ID(N'dbo.' + e.ItemName, N'U') IS NOT NULL)
     OR (e.Kind = 'View'   AND OBJECT_ID(N'dbo.' + e.ItemName, N'V') IS NOT NULL)
     OR (e.Kind = 'Column' AND COL_LENGTH('dbo.' + e.ObjectName, e.ItemName) IS NOT NULL)
     OR (e.Kind = 'Index'  AND EXISTS (SELECT 1 FROM sys.indexes
                                       WHERE name = e.ItemName AND object_id = OBJECT_ID(N'dbo.' + e.ObjectName)))
     OR (e.Kind = 'Check'  AND EXISTS (SELECT 1 FROM sys.check_constraints
                                       WHERE name = e.ItemName AND parent_object_id = OBJECT_ID(N'dbo.' + e.ObjectName)))
    THEN 'OK' ELSE 'MISSING' END AS State
FROM @Expected e
ORDER BY
    CASE e.Kind WHEN 'Table' THEN 1 WHEN 'View' THEN 2 WHEN 'Column' THEN 3 WHEN 'Index' THEN 4 ELSE 5 END,
    e.ObjectName, e.ItemName;

-- --- The two vocabulary corrections, checked by content not just presence ---
PRINT '';
PRINT 'Vocabulary checks:';

DECLARE @Mech NVARCHAR(MAX), @Deriv NVARCHAR(MAX);

SELECT @Mech = definition FROM sys.check_constraints
WHERE name = 'CK_EDILedger_Mechanism' AND parent_object_id = OBJECT_ID(N'dbo.EDILedger');

SELECT @Deriv = definition FROM sys.check_constraints
WHERE name = 'CK_EDILedger_MechanismDerivation' AND parent_object_id = OBJECT_ID(N'dbo.EDILedger');

-- CHARINDEX, not LIKE: square brackets in a LIKE pattern are a character class,
-- and the normalised constraint definition is full of them.
IF @Mech IS NULL
    PRINT '  MISSING  CK_EDILedger_Mechanism';
ELSE IF CHARINDEX('PartnerAPI', @Mech) > 0
    PRINT '  OK       CK_EDILedger_Mechanism allows PartnerAPI (D-31)';
ELSE
    PRINT '  STALE    CK_EDILedger_Mechanism does NOT allow PartnerAPI - re-run script 01';

IF @Deriv IS NULL
    PRINT '  MISSING  CK_EDILedger_MechanismDerivation';
ELSE IF CHARINDEX('PartnerAPI', @Deriv) > 0
    PRINT '  OK       CK_EDILedger_MechanismDerivation exempts PartnerAPI';
ELSE
    PRINT '  STALE    CK_EDILedger_MechanismDerivation predates PartnerAPI - re-run script 01';

IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID(N'dbo.EDILedger') AND name = 'BizMateDuplicate' AND is_nullable = 1)
    PRINT '  STALE    EDILedger.BizMateDuplicate is still NULLable - re-run script 01';
ELSE IF COL_LENGTH('dbo.EDILedger', 'BizMateDuplicate') IS NOT NULL
    PRINT '  OK       EDILedger.BizMateDuplicate is NOT NULL';

IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID(N'dbo.EDILedger') AND name = 'OrderId' AND is_nullable = 0)
    PRINT '  WRONG    EDILedger.OrderId is NOT NULL. It must stay nullable - the ledger is';
ELSE
    PRINT '  OK       EDILedger.OrderId is nullable (the ledger is order-independent)';

-- --- The trace view must carry the contract's 14 field names ---------------
PRINT '';
PRINT 'Trace contract field names:';

DECLARE @Missing INT = 0;

SELECT @Missing = COUNT(*)
FROM (VALUES ('transmissionReference'),('partnerId'),('partnerControlNo'),('direction'),
             ('documentType'),('mapName'),('mapVersion'),('receivedAt'),('translatedAt'),
             ('outcome'),('errorDetail'),('handedOffAt'),('bizmateMessageId'),('rawArtifactRef')) AS f(Name)
WHERE NOT EXISTS (SELECT 1 FROM sys.columns
                  WHERE object_id = OBJECT_ID(N'dbo.VW_EDITrace') AND name = f.Name COLLATE DATABASE_DEFAULT);

IF OBJECT_ID(N'dbo.VW_EDITrace', N'V') IS NULL
    PRINT '  MISSING  dbo.VW_EDITrace';
ELSE IF @Missing = 0
    PRINT '  OK       all 14 ESyncMateTraceRecord field names present';
ELSE
    PRINT '  WRONG    ' + CAST(@Missing AS VARCHAR(10)) + ' contract field name(s) missing from VW_EDITrace';

PRINT '';
PRINT 'Verification complete. Any MISSING, STALE or WRONG row above needs the named script re-run.';
GO
