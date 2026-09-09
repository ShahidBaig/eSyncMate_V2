-- ============================================================================
-- Task 00003 - EDI & API Integration with BizMate EU - W3-04 (E2)
-- Run in: ESYNCMATE_EU  (also safe in ESYNCMATE_TEST)
--
-- Declares what this instance's BizMate credential is for: BizMate_UsageIndicator.
--
-- T on a non-production instance, P on production. An inbound interchange whose
-- ISA15 disagrees is refused at the boundary, before it is registered with
-- BizMate and before any order is created.
--
-- The reason it has to be enforced here is that nobody else does. `testIndicator`
-- is read by none of BizMate's inbound handlers, so a document marked T that
-- arrives on a production credential becomes a REAL order, with real stock
-- committed against it and a real ASN owed on it. ISA15 is the one place the
-- partner states intent, and eSyncMate is the only thing between that statement
-- and an order.
--
-- Leaving the setting absent turns the check off, which is deliberate for an
-- instance nobody has classified yet - but it is not a state to stay in. An unset
-- value accepts every document whatever it claims to be.
--
-- Set to T here because this is the EU NON-PRODUCTION instance. **On production,
-- run this with P.** Getting it backwards is worse than not running it at all:
-- it would refuse the real traffic and accept the test traffic.
--
-- Idempotent - updates in place if it already exists. Safe to re-run.
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

DECLARE @Tag   NVARCHAR(200) = N'BizMate_UsageIndicator';
DECLARE @Value NVARCHAR(100) = N'T';        -- P on the production instance

IF EXISTS (SELECT 1 FROM dbo.ApplicationSettings WHERE TagName = @Tag)
BEGIN
    UPDATE dbo.ApplicationSettings SET TagValue = @Value WHERE TagName = @Tag;

    PRINT 'ApplicationSettings: ' + @Tag + ' set to ' + @Value;
END
ELSE
BEGIN
    INSERT INTO dbo.ApplicationSettings (TagName, TagValue, CreatedDate, CreatedUser)
    VALUES (@Tag, @Value, GETDATE(), 1);

    PRINT 'ApplicationSettings: added ' + @Tag + ' = ' + @Value;
END
GO

-- What is actually arriving, so a wrong value shows up here rather than as a
-- morning of refused documents.
SELECT 'Setting' AS Item, TagValue AS Value FROM dbo.ApplicationSettings WHERE TagName = 'BizMate_UsageIndicator'
UNION ALL
SELECT 'Inbound interchanges carrying ISA15=' + ISNULL(ISAUsageIndicator, '(none)'), CAST(COUNT(*) AS varchar(10))
  FROM dbo.InboundEDIInfo
 GROUP BY ISAUsageIndicator;
GO

SET NOEXEC OFF;
GO
