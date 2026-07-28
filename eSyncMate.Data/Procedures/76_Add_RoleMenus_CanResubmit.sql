/*==============================================================================
  Script : 76_Add_RoleMenus_CanResubmit.sql
  Date   : 2026-07-23
  Purpose: Add TWO new granular role permissions, INDEPENDENT of CanEdit and of
           each other (client wants these controllable separately from Edit):
             - CanResubmit    -> Orders "Resubmit to ERP" action
             - CanReTransmit  -> Orders "Re-Transmit ASN" action

           Touches: RoleMenus + UserMenus (two new BIT columns), VW_UserMenus
           (both UNION halves), and Sp_GetUserPermissions (middleware SP).

  Idempotent: safe to run multiple times.
==============================================================================*/

SET NOCOUNT ON;
GO

/*---- 1. New permission columns on RoleMenus (default 0 = not granted) ----*/
IF COL_LENGTH('dbo.RoleMenus', 'CanResubmit') IS NULL
    ALTER TABLE dbo.RoleMenus
        ADD CanResubmit BIT NOT NULL
            CONSTRAINT DF_RoleMenus_CanResubmit DEFAULT (0);
GO
IF COL_LENGTH('dbo.RoleMenus', 'CanReTransmit') IS NULL
    ALTER TABLE dbo.RoleMenus
        ADD CanReTransmit BIT NOT NULL
            CONSTRAINT DF_RoleMenus_CanReTransmit DEFAULT (0);
GO

/*---- 2. Same columns on UserMenus (direct per-user grants for hidden menus) ----*/
IF COL_LENGTH('dbo.UserMenus', 'CanResubmit') IS NULL
    ALTER TABLE dbo.UserMenus
        ADD CanResubmit BIT NOT NULL
            CONSTRAINT DF_UserMenus_CanResubmit DEFAULT (0);
GO
IF COL_LENGTH('dbo.UserMenus', 'CanReTransmit') IS NULL
    ALTER TABLE dbo.UserMenus
        ADD CanReTransmit BIT NOT NULL
            CONSTRAINT DF_UserMenus_CanReTransmit DEFAULT (0);
GO

/*---- 3. VW_UserMenus — expose both new flags in BOTH UNION halves ----*/
CREATE OR ALTER VIEW [dbo].[VW_UserMenus]
AS
-- Part 1: Role-based menus (excludes hidden menus)
SELECT
    ur.UserId,
    r.Id AS RoleId,
    r.Name AS RoleName,
    m.Id AS ModuleId,
    m.Name AS ModuleName,
    m.TranslationKey AS ModuleTranslationKey,
    m.Icon AS ModuleIcon,
    m.SortOrder AS ModuleSortOrder,
    mn.Id AS MenuId,
    mn.Name AS MenuName,
    mn.TranslationKey AS MenuTranslationKey,
    mn.Route,
    mn.Icon AS MenuIcon,
    mn.IsExternalLink,
    mn.ExternalUrl,
    mn.SortOrder AS MenuSortOrder,
    mn.Company,
    rm.CanView,
    rm.CanAdd,
    rm.CanEdit,
    rm.CanDelete,
    rm.CanResubmit,
    rm.CanReTransmit
FROM [dbo].[UserRoles] ur
INNER JOIN [dbo].[Roles] r ON ur.RoleId = r.Id AND r.IsActive = 1
INNER JOIN [dbo].[RoleMenus] rm ON r.Id = rm.RoleId AND rm.CanView = 1
INNER JOIN [dbo].[Menus] mn ON rm.MenuId = mn.Id AND mn.IsActive = 1 AND (mn.IsHidden = 0 OR mn.IsHidden IS NULL)
INNER JOIN [dbo].[Modules] m ON mn.ModuleId = m.Id AND m.IsActive = 1

UNION

-- Part 2: Direct user-menu assignments (allows hidden menus for specific users)
SELECT
    um.UserId,
    0 AS RoleId,
    'Direct' AS RoleName,
    m.Id AS ModuleId,
    m.Name AS ModuleName,
    m.TranslationKey AS ModuleTranslationKey,
    m.Icon AS ModuleIcon,
    m.SortOrder AS ModuleSortOrder,
    mn.Id AS MenuId,
    mn.Name AS MenuName,
    mn.TranslationKey AS MenuTranslationKey,
    mn.Route,
    mn.Icon AS MenuIcon,
    mn.IsExternalLink,
    mn.ExternalUrl,
    mn.SortOrder AS MenuSortOrder,
    mn.Company,
    um.CanView,
    um.CanAdd,
    um.CanEdit,
    um.CanDelete,
    um.CanResubmit,
    um.CanReTransmit
FROM [dbo].[UserMenus] um
INNER JOIN [dbo].[Menus] mn ON um.MenuId = mn.Id AND mn.IsActive = 1
INNER JOIN [dbo].[Modules] m ON mn.ModuleId = m.Id AND m.IsActive = 1
WHERE um.CanView = 1;
GO

/*---- 4. Sp_GetUserPermissions — return both new flags for the middleware ----*/
CREATE OR ALTER PROCEDURE [dbo].[Sp_GetUserPermissions]
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        m.Id        AS MenuId,
        m.ApiPrefix,
        rm.CanView,
        rm.CanAdd,
        rm.CanEdit,
        rm.CanDelete,
        rm.CanResubmit,
        rm.CanReTransmit
    FROM UserRoles ur
    INNER JOIN RoleMenus rm ON ur.RoleId = rm.RoleId
    INNER JOIN Menus m      ON rm.MenuId  = m.Id
    WHERE ur.UserId  = @UserId
      AND m.IsActive  = 1
      AND m.ApiPrefix IS NOT NULL
      AND m.ApiPrefix <> '';
END
GO
