/*==============================================================================
  T8 99 - Task 00008: remove the MustChangePassword column again

  Takes U.[MustChangePassword] out of VW_Users, then drops the default constraint and the column.
  Deploy the API WITHOUT Task 00008 first: the Task 00008 API writes this column.
  @Apply = 0 (default) DRY RUN.
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Apply BIT = 0;   -- 0 = dry run, 1 = change

BEGIN TRY
    DECLARE @View NVARCHAR(MAX) = OBJECT_DEFINITION(OBJECT_ID('dbo.VW_Users'));
    DECLARE @Needle NVARCHAR(60) = N', U.[MustChangePassword]';

    SELECT 'Plan' AS Grid,
           CASE WHEN CHARINDEX(@Needle, @View) > 0 THEN 'will be removed from VW_Users' ELSE 'not in VW_Users' END AS [View],
           CASE WHEN COL_LENGTH('dbo.Users', 'MustChangePassword') IS NOT NULL THEN 'will be dropped' ELSE 'not there' END AS [Users column];

    IF @Apply = 0
    BEGIN
        PRINT 'DRY RUN: nothing changed. Set @Apply = 1 to revert.';
        RETURN;
    END

    IF CHARINDEX(@Needle, @View) > 0
    BEGIN
        SET @View = REPLACE(@View, @Needle, N'');

        DECLARE @Create INT = PATINDEX('%CREATE%VIEW%', @View);
        DECLARE @Alter  INT = PATINDEX('%ALTER%VIEW%', @View);
        IF @Create > 0 AND (@Alter = 0 OR @Create < @Alter)
            SET @View = STUFF(@View, @Create, LEN('CREATE'), 'ALTER');

        EXEC sp_executesql @View;
        PRINT 'VW_Users: MustChangePassword removed.';
    END

    IF OBJECT_ID('dbo.DF_Users_MustChangePassword', 'D') IS NOT NULL
        ALTER TABLE dbo.Users DROP CONSTRAINT DF_Users_MustChangePassword;

    IF COL_LENGTH('dbo.Users', 'MustChangePassword') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.Users DROP COLUMN MustChangePassword;
        PRINT 'Users.MustChangePassword dropped.';
    END
END TRY
BEGIN CATCH
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
