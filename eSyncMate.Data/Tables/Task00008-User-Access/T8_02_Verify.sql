/*==============================================================================
  T8 02 - Task 00008: verify the MustChangePassword column (READ ONLY)
==============================================================================*/

SET NOCOUNT ON;

SELECT 'Columns' AS Grid,
       CASE WHEN COL_LENGTH('dbo.Users', 'MustChangePassword') IS NULL THEN 'MISSING' ELSE 'OK' END AS [Users.MustChangePassword],
       CASE WHEN COL_LENGTH('dbo.VW_Users', 'MustChangePassword') IS NULL THEN 'MISSING' ELSE 'OK' END AS [VW_Users.MustChangePassword];

IF COL_LENGTH('dbo.VW_Users', 'MustChangePassword') IS NOT NULL
    EXEC sp_executesql N'
        SELECT ''Users'' AS Grid, Id, USERID, Status, MustChangePassword
          FROM dbo.VW_Users
         ORDER BY Id;';
GO
