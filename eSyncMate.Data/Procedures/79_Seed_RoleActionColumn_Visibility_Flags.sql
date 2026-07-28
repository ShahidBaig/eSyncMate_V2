/*==============================================================================
  Script : 79_Seed_RoleActionColumn_Visibility_Flags.sql
  Date   : 2026-07-23
  Purpose: Config flags that control whether the Resubmit / Re-Transmit permission
           COLUMNS appear in the Role Management grid — WITHOUT a redeployment.

           The Role grid reads these live (GET api/Role/getActionColumnVisibility).
           Default '0' = hidden (matches current state). To turn a column ON later,
           just run:
             UPDATE ApplicationSettings SET TagValue = '1'
             WHERE TagName = 'ShowResubmitActionInRoles';   -- or ShowReTransmitActionInRoles
           then refresh the Role Management screen. No build/deploy needed.

  Idempotent: safe to run multiple times (won't overwrite an existing value).
==============================================================================*/

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM dbo.ApplicationSettings WHERE TagName = 'ShowResubmitActionInRoles')
    INSERT INTO dbo.ApplicationSettings (TagName, TagValue, CreatedDate, CreatedUser)
    VALUES ('ShowResubmitActionInRoles', '0', GETDATE(), 1);

IF NOT EXISTS (SELECT 1 FROM dbo.ApplicationSettings WHERE TagName = 'ShowReTransmitActionInRoles')
    INSERT INTO dbo.ApplicationSettings (TagName, TagValue, CreatedDate, CreatedUser)
    VALUES ('ShowReTransmitActionInRoles', '0', GETDATE(), 1);

-- Verify
SELECT TagName, TagValue FROM dbo.ApplicationSettings
WHERE TagName IN ('ShowResubmitActionInRoles', 'ShowReTransmitActionInRoles');
GO
