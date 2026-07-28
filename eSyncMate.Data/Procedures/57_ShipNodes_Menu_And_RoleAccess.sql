/* ------------------------------------------------------------------
   57 — Setup > Ship Nodes: menu entry + role access

   The sidebar is data-driven (Menus + RoleMenus feed GET api/Role/getUserMenus),
   so the screen only appears once these rows exist.

   Setup module = ModuleId 4.
   NOTE: Id is an explicit, NON-IDENTITY key in BOTH Menus and RoleMenus — it must be supplied.
   Re-runnable: every insert is guarded.
   ------------------------------------------------------------------ */

DECLARE @MenuId INT;

IF NOT EXISTS (SELECT 1 FROM dbo.Menus WHERE Route = 'edi/shipNodes')
BEGIN
    SET @MenuId = (SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.Menus);

    -- ApiPrefix is what PermissionMiddleware matches the request path against
    INSERT INTO dbo.Menus (Id, ModuleId, Name, TranslationKey, Route, Icon, IsExternalLink, ExternalUrl, SortOrder, Company, IsHidden, IsActive, CreatedDate, CreatedBy, ApiPrefix)
    VALUES (@MenuId, 4, 'Ship Nodes', 'nav.shipNodes', 'edi/shipNodes', 'hub', 0, NULL, 9, 'ESYNCMATE', 0, 1, GETDATE(), 0, 'api/ShipNodes');
END
ELSE
BEGIN
    SET @MenuId = (SELECT Id FROM dbo.Menus WHERE Route = 'edi/shipNodes');
END

-- Full access for the admin roles (same grants Route Types has: RoleId 1 and 2)
IF NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE MenuId = @MenuId AND RoleId = 1)
BEGIN
    INSERT INTO dbo.RoleMenus (Id, RoleId, MenuId, CanView, CanAdd, CanEdit, CanDelete, CreatedDate, CreatedBy)
    VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.RoleMenus), 1, @MenuId, 1, 1, 1, 1, GETDATE(), 0);
END

IF NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE MenuId = @MenuId AND RoleId = 2)
BEGIN
    INSERT INTO dbo.RoleMenus (Id, RoleId, MenuId, CanView, CanAdd, CanEdit, CanDelete, CreatedDate, CreatedBy)
    VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.RoleMenus), 2, @MenuId, 1, 1, 1, 1, GETDATE(), 0);
END

-- Verify
SELECT m.Id AS MenuId, m.Name, m.Route, m.Icon, m.SortOrder, m.IsHidden, m.IsActive,
       rm.Id AS RoleMenuId, rm.RoleId, rm.CanView, rm.CanAdd, rm.CanEdit, rm.CanDelete
FROM dbo.Menus m
LEFT JOIN dbo.RoleMenus rm ON rm.MenuId = m.Id
WHERE m.Route = 'edi/shipNodes';

/* ---- Rollback ----
DELETE FROM dbo.RoleMenus WHERE MenuId = (SELECT Id FROM dbo.Menus WHERE Route = 'edi/shipNodes');
DELETE FROM dbo.Menus WHERE Route = 'edi/shipNodes';
*/
