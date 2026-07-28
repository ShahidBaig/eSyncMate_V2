/* ------------------------------------------------------------------
   58 — Ship Nodes: set Menus.ApiPrefix

   PermissionMiddleware matches a request path against Menus.ApiPrefix to decide which
   CanView/CanAdd/CanEdit/CanDelete flag applies. Script 57 created the menu without a
   prefix, so api/ShipNodes/* was never permission-checked — any authenticated user could
   call it regardless of their role.
   ------------------------------------------------------------------ */

UPDATE dbo.Menus
SET ApiPrefix = 'api/ShipNodes'
WHERE Route = 'edi/shipNodes' AND ISNULL(ApiPrefix, '') = '';
GO

-- Verify: every Setup menu should carry a prefix
SELECT m.Id, m.Name, m.Route, m.ApiPrefix, m.IsHidden, m.IsActive
FROM dbo.Menus m
WHERE m.ModuleId = 4
ORDER BY m.SortOrder;
