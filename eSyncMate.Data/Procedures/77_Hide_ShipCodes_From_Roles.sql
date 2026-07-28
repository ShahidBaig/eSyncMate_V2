/*==============================================================================
  Script : 77_Hide_ShipCodes_From_Roles.sql
  Date   : 2026-07-23
  Purpose: Hide the "ShipCodes Mapping" menu from the Role Management permission
           grid (and from role-based navigation).

           Mechanism: the Role grid endpoint (RoleController.GetMenus) selects
             WHERE IsActive = 1 AND (IsHidden = 0 OR IsHidden IS NULL)
           so setting IsHidden = 1 removes it from that grid. VW_UserMenus
           (role-based part) also excludes IsHidden menus, so it drops out of
           role-driven nav too. IsActive stays 1, so it remains assignable via
           DIRECT per-user grants (getHiddenMenus) — same treatment as the other
           Setup menus (Connectors, Maps, Routes, Route Types, Partner Groups).

  Idempotent: safe to run multiple times.
==============================================================================*/

SET NOCOUNT ON;

UPDATE dbo.Menus
SET IsHidden = 1
WHERE Route = 'edi/shipCodes'
  AND (IsHidden = 0 OR IsHidden IS NULL);

PRINT CONCAT('Rows hidden: ', @@ROWCOUNT);

-- Verify
SELECT Id, Name, Route, IsActive, IsHidden
FROM dbo.Menus
WHERE Route = 'edi/shipCodes';
GO
