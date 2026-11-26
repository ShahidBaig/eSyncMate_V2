/*==============================================================================
  T8 01 - Task 00008: "Require password change at next login"

  What it changes:
    1. dbo.Users: new column MustChangePassword BIT NOT NULL DEFAULT 0 (existing users get 0 = not forced).
    2. dbo.VW_Users: the column is added to the view. The LIVE view definition is read and changed in one place
       (after U.[MFASecret]); every other part of the view stays exactly as it is.

  Run it BEFORE deploying the API of Task 00008: the API writes this column when a user is added or edited.
  Safe to run more than once (a step that is already done is skipped).

  @Apply = 0 (default) DRY RUN: shows what would change.

  Stops (nothing changed):
    52101 dbo.Users or dbo.VW_Users not found
    52102 the view does not contain U.[MFASecret] exactly once (change the view by hand: add U.[MustChangePassword])
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Apply BIT = 0;   -- 0 = dry run, 1 = change

BEGIN TRY
    IF OBJECT_ID('dbo.Users', 'U') IS NULL OR OBJECT_ID('dbo.VW_Users', 'V') IS NULL
        THROW 52101, 'dbo.Users or dbo.VW_Users not found. Nothing changed.', 1;

    DECLARE @HasColumn BIT = CASE WHEN COL_LENGTH('dbo.Users', 'MustChangePassword') IS NULL THEN 0 ELSE 1 END;
    DECLARE @View NVARCHAR(MAX) = OBJECT_DEFINITION(OBJECT_ID('dbo.VW_Users'));
    DECLARE @ViewHasIt BIT = CASE WHEN CHARINDEX('MustChangePassword', @View) > 0 THEN 1 ELSE 0 END;
    DECLARE @Needle NVARCHAR(50) = N'U.[MFASecret]';
    DECLARE @Hits INT = (LEN(@View) - LEN(REPLACE(@View, @Needle, N''))) / LEN(@Needle);

    SELECT 'Plan' AS Grid,
           CASE WHEN @HasColumn = 1 THEN 'already there - skipped' ELSE 'will be added (BIT NOT NULL DEFAULT 0)' END AS [Users.MustChangePassword],
           CASE WHEN @ViewHasIt = 1 THEN 'already there - skipped'
                WHEN @Hits = 1 THEN 'will be added after U.[MFASecret]'
                ELSE 'STOP: U.[MFASecret] found ' + CAST(@Hits AS VARCHAR(10)) + ' time(s)' END AS [VW_Users];

    IF @ViewHasIt = 0 AND @Hits <> 1
        THROW 52102, 'VW_Users does not contain U.[MFASecret] exactly once. Nothing changed - add U.[MustChangePassword] to the view by hand.', 1;

    IF @Apply = 0
    BEGIN
        PRINT 'DRY RUN: nothing changed. Set @Apply = 1 to apply.';
        RETURN;
    END

    IF @HasColumn = 0
    BEGIN
        ALTER TABLE dbo.Users ADD MustChangePassword BIT NOT NULL
            CONSTRAINT DF_Users_MustChangePassword DEFAULT (0);
        PRINT 'Users.MustChangePassword added.';
    END

    IF @ViewHasIt = 0
    BEGIN
        SET @View = REPLACE(@View, @Needle, @Needle + N', U.[MustChangePassword]');

        -- CREATE VIEW -> ALTER VIEW (only the first keyword)
        DECLARE @Create INT = PATINDEX('%CREATE%VIEW%', @View);
        DECLARE @Alter  INT = PATINDEX('%ALTER%VIEW%', @View);
        IF @Create > 0 AND (@Alter = 0 OR @Create < @Alter)
            SET @View = STUFF(@View, @Create, LEN('CREATE'), 'ALTER');

        EXEC sp_executesql @View;
        PRINT 'VW_Users altered.';
    END

    SELECT 'After' AS Grid,
           CASE WHEN COL_LENGTH('dbo.Users', 'MustChangePassword') IS NULL THEN 'MISSING' ELSE 'OK' END AS [Users column],
           CASE WHEN COL_LENGTH('dbo.VW_Users', 'MustChangePassword') IS NULL THEN 'MISSING' ELSE 'OK' END AS [View column];
END TRY
BEGIN CATCH
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
