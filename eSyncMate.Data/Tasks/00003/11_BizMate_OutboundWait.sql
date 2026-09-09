-- ============================================================================
-- Task 00003 - EDI & API Integration with BizMate EU - W1-09 (E9, E23)
-- Run in: ESYNCMATE_EU  (also safe in ESYNCMATE_TEST)
--
-- Turns on the outbox long poll: BizMate_OutboundWaitSeconds.
--
-- The outbound route asks BizMate what is staged and is answered immediately.
-- With a five-minute schedule that makes an ASN's latency "up to five minutes"
-- no matter how quickly BizMate stages it. With a wait, BizMate holds the
-- request open and answers the moment something appears, so the latency becomes
-- about as long as they take to stage it.
--
-- Measured against this instance before choosing the value: an empty outbox
-- answered in 0.02s with no wait and 25.02s with wait=25, so the gateway does
-- honour it rather than ignoring the parameter.
--
-- 25 rather than the contract's maximum of 30: the route holds its execution
-- lock for the duration, and leaving a few seconds of headroom under the
-- server's own ceiling avoids racing its timeout. Set it to 0 to go back to
-- ask-and-be-told; the code clamps anything above 30.
--
-- The alternative mode is the signed webhook (W1-05), which would be lower
-- latency still but needs an endpoint BizMate can reach and a signature to
-- verify on every push. Long poll costs nothing and needs nobody.
--
-- Idempotent - updates in place if the setting already exists. Safe to re-run.
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

DECLARE @Tag   NVARCHAR(200) = N'BizMate_OutboundWaitSeconds';
DECLARE @Value NVARCHAR(100) = N'25';

IF EXISTS (SELECT 1 FROM dbo.ApplicationSettings WHERE TagName = @Tag)
BEGIN
    UPDATE dbo.ApplicationSettings
       SET TagValue = @Value
     WHERE TagName = @Tag;

    PRINT 'ApplicationSettings: ' + @Tag + ' set to ' + @Value;
END
ELSE
BEGIN
    INSERT INTO dbo.ApplicationSettings (TagName, TagValue, CreatedDate, CreatedUser)
    VALUES (@Tag, @Value, GETDATE(), 1);

    PRINT 'ApplicationSettings: added ' + @Tag + ' = ' + @Value;
END
GO

SELECT TagName, TagValue FROM dbo.ApplicationSettings WHERE TagName = 'BizMate_OutboundWaitSeconds';
GO

SET NOEXEC OFF;
GO
