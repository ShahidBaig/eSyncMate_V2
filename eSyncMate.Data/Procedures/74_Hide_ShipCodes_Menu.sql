/* ------------------------------------------------------------------
   74 — Hide the "ShipCodes Mapping" interface

   Deactivates the menu so it no longer shows in the sidebar OR in Role
   Management (menu list is driven by Menus.IsActive). The RoleMenus grants
   are left in place, so re-enabling is a one-liner.

   Re-enable:  UPDATE dbo.Menus SET IsActive = 1, IsHidden = 0 WHERE Route = 'edi/shipCodes';
   ------------------------------------------------------------------ */

UPDATE dbo.Menus
SET IsActive = 1,
    IsHidden = 0
WHERE Route = 'edi/shipCodes';

-- Verify
SELECT Id, Name, Route, IsActive, IsHidden FROM dbo.Menus WHERE Route = 'edi/shipCodes';
