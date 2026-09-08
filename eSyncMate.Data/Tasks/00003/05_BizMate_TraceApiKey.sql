-- ============================================================================
-- Task 00003 - EDI & API Integration with BizMate EU - W2-13 / E17
-- Run in: ESYNCMATE_EU  (also safe in ESYNCMATE_TEST)
-- Run AFTER: nothing (ApplicationSettings already exists)
--
-- Issues the static bearer key for the trace read API. This is the one BizMate
-- secret that travels in the OTHER direction: BizMate issues us a credential
-- trio, and we issue them this key, which they hold in their own configuration
-- as ESyncMate:TraceApiKey. One key per environment.
--
-- TraceController.IsAuthorised reads it through CommonUtils.BizMate_TraceApiKey,
-- which LoadFromDatabase fills from the tag written here. While the tag is
-- absent the endpoint is CLOSED, not open, and BizMate simply renders the hop
-- as "eSyncMate record not connected" - the documented, harmless state.
--
-- The key is generated HERE, by the server, and is never written into this
-- script, a repository, a ticket or a chat. Read it back once with the SELECT
-- at the bottom to hand to BizMate out of band, the same way they handed us
-- the credential trio.
--
-- CRYPT_GEN_RANDOM(32) is a cryptographic PRNG - 256 bits, rendered as 64 hex
-- characters behind an environment-naming prefix so a key found loose in a
-- config file identifies itself. The comparison in TraceController is
-- constant-time (CryptographicOperations.FixedTimeEquals), so key length is the
-- only thing standing between the endpoint and a guess.
--
-- Idempotent - an existing key is left alone, never regenerated. Re-running
-- will NOT rotate. To rotate deliberately, see the block at the bottom.
-- ============================================================================
SET NOCOUNT ON;

IF DB_NAME() NOT LIKE 'ESYNCMATE%'
BEGIN
    RAISERROR('Wrong database context (%s). Run this script in ESYNCMATE_EU.', 16, 1, @@SERVERNAME);
    RETURN;
END

IF OBJECT_ID(N'dbo.ApplicationSettings', N'U') IS NULL
BEGIN
    RAISERROR('dbo.ApplicationSettings is missing. This is the wrong database.', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM dbo.ApplicationSettings WHERE TagName = 'BizMate_TraceApiKey')
BEGIN
    DECLARE @Key NVARCHAR(500) =
        'esm_trace_' +
        CASE WHEN DB_NAME() = 'ESYNCMATE_EU' THEN 'eu_nonprod' ELSE LOWER(DB_NAME()) END +
        '_' + LOWER(CONVERT(varchar(64), CRYPT_GEN_RANDOM(32), 2));

    INSERT INTO dbo.ApplicationSettings (TagName, TagValue, CreatedDate, CreatedUser)
    VALUES ('BizMate_TraceApiKey', @Key, GETUTCDATE(), 1);

    PRINT 'ApplicationSettings: BizMate_TraceApiKey issued (256-bit).';
END
ELSE
BEGIN
    PRINT 'ApplicationSettings: BizMate_TraceApiKey already present - left unchanged.';
END
GO

-- ---------------------------------------------------------------------------
-- Verify. Shows the key masked; it is never printed in full by this script.
-- ---------------------------------------------------------------------------
SELECT  TagName,
        LEN(TagValue)                              AS KeyLength,
        LEFT(TagValue, 22) + '...' + RIGHT(TagValue, 4) AS Masked,
        CreatedDate
FROM    dbo.ApplicationSettings
WHERE   TagName = 'BizMate_TraceApiKey';
GO

-- ---------------------------------------------------------------------------
-- TO HAND THE KEY TO BIZMATE - run this one statement, copy the value out of
-- band (not through a repository, a ticket or a chat), then clear your results
-- pane. BizMate sets it as ESyncMate:TraceApiKey alongside ESyncMateTraceUrl.
--
--   SELECT TagValue FROM dbo.ApplicationSettings WHERE TagName = 'BizMate_TraceApiKey';
--
-- TO ROTATE - deliberate act, not a re-run. The old key stops working the
-- moment the Processor restarts, so tell BizMate before running it.
--
--   UPDATE dbo.ApplicationSettings
--      SET TagValue = 'esm_trace_eu_nonprod_' + LOWER(CONVERT(varchar(64), CRYPT_GEN_RANDOM(32), 2))
--    WHERE TagName = 'BizMate_TraceApiKey';
--
-- NOTE: CommonUtils.LoadFromDatabase reads ApplicationSettings ONCE at start-up.
-- Restart the Processor after issuing or rotating, or the key will not be live.
-- ---------------------------------------------------------------------------
