-- ============================================================================
-- Task 00003 - EDI & API Integration with BizMate EU - W3-01 / W3-02
-- Run in: ESYNCMATE_EU  (also safe in ESYNCMATE_TEST)
--
-- Makes the canonical 850 carry the PO type and the transaction purpose, which
-- it has never done.
--
-- The 850 Transformation map (Maps.Id = 12) resolves both fields through
-- Transformations.findMapTagValue against two static dictionaries:
--
--     poType  ->  BEG02 through orderTypeMap
--     status  ->  BEG01 through purposeMap
--
-- Nothing anywhere populates either dictionary. findMapTagValue returns an empty
-- string on a miss, and CanonicalValues.PruneEmpty then drops the property, so
-- BizMate has never been told the PO type or the purpose of a single order.
--
-- The visible consequence is that no order can be recognised as a dropship. The
-- W3-02 intake rule "a dropship ship-to needs address1 and country" is therefore
-- unreachable on real traffic: every order reads as non-dropship and gets the
-- stricter non-dropship rule applied instead.
--
-- The contract wants the X12 code itself, not a translation of it -
-- 850.schema.json says poType is "BEG02: SA stand-alone, DS drop ship, BK
-- blanket, RL release, KN purchase order" and purpose is "BEG01: 00 Original,
-- 05 Replace, 01 Cancellation, 06 Confirmation" - so the dictionary was never
-- the right mechanism for these two fields. They pass through instead.
--
-- SCOPE, deliberately narrow. This changes the CANONICAL payload only. It does
-- NOT touch the 850 DB Fields map (Maps.Id = 13), which carries the same broken
-- expression for its own "OrderType" field and writes to the order tables - that
-- one changes what is stored for an existing partner and is somebody's decision,
-- not a side effect of this script. Recorded, not fixed here.
--
-- Map 12 serves customers 3 (eSyncMate) and 15 (BELL-D12). Both currently get an
-- empty poType and status, so both gain a value rather than having one changed.
--
-- Idempotent - the replacement is a no-op once applied. Safe to re-run.
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

DECLARE @MapId INT = 12;

DECLARE @OldPoType NVARCHAR(400) =
    N'#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.findMapTagValue, #valueof($.Content[?(@.Name==''BEG'')].Content[1].E),orderTypeMap)';

DECLARE @NewPoType NVARCHAR(400) =
    N'#valueof($.Content[?(@.Name==''BEG'')].Content[1].E)';

DECLARE @OldStatus NVARCHAR(400) =
    N'#customfunction(eSyncMate.Processor,eSyncMate.Maps.Transformations.findMapTagValue, #valueof($.Content[?(@.Name==''BEG'')].Content[0].E),purposeMap)';

DECLARE @NewStatus NVARCHAR(400) =
    N'#valueof($.Content[?(@.Name==''BEG'')].Content[0].E)';

DECLARE @Map NVARCHAR(MAX) = (SELECT CAST(Map AS NVARCHAR(MAX)) FROM dbo.Maps WHERE Id = @MapId);

IF @Map IS NULL
BEGIN
    RAISERROR('Map %d was not found. Run the W7-11 seed (Tasks/00003/06) first.', 16, 1, @MapId);
    SET NOEXEC ON;
END

PRINT '--- before ---';
PRINT '  poType passes through : ' + CASE WHEN CHARINDEX('orderTypeMap', @Map) > 0 THEN 'no - still via orderTypeMap' ELSE 'yes' END;
PRINT '  status passes through : ' + CASE WHEN CHARINDEX('purposeMap', @Map) > 0 THEN 'no - still via purposeMap' ELSE 'yes' END;

DECLARE @Updated NVARCHAR(MAX) = REPLACE(REPLACE(@Map, @OldPoType, @NewPoType), @OldStatus, @NewStatus);

IF @Updated = @Map
BEGIN
    PRINT 'Nothing to change - the map already passes both values through.';
END
ELSE
BEGIN
    -- The map is JSON in a database column with nothing compiling it (F-1), so it
    -- is checked here rather than discovered broken at the next 850.
    IF ISJSON(@Updated) <> 1
    BEGIN
        RAISERROR('The rewritten map is not valid JSON. Nothing was changed.', 16, 1);
        SET NOEXEC ON;
    END

    BEGIN TRANSACTION;

    UPDATE dbo.Maps
       SET Map = @Updated,
           ModifiedDate = GETDATE(),
           ModifiedBy = 1
     WHERE Id = @MapId;

    PRINT 'Map ' + CAST(@MapId AS VARCHAR(10)) + ' updated: poType and status now carry BEG02 and BEG01.';

    COMMIT TRANSACTION;
END

DECLARE @After NVARCHAR(MAX) = (SELECT CAST(Map AS NVARCHAR(MAX)) FROM dbo.Maps WHERE Id = @MapId);

PRINT '--- after ---';
PRINT '  poType passes through : ' + CASE WHEN CHARINDEX('orderTypeMap', @After) > 0 THEN 'no - still via orderTypeMap' ELSE 'yes' END;
PRINT '  status passes through : ' + CASE WHEN CHARINDEX('purposeMap', @After) > 0 THEN 'no - still via purposeMap' ELSE 'yes' END;
PRINT '  valid JSON            : ' + CASE WHEN ISJSON(@After) = 1 THEN 'yes' ELSE 'NO' END;
GO

SET NOEXEC OFF;
GO
