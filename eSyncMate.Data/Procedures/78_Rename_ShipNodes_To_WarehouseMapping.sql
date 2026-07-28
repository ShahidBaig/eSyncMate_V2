/*==============================================================================
  Script : 78_Rename_ShipNodes_To_WarehouseMapping.sql
  Date   : 2026-07-23
  Purpose: Rename the "Ship Nodes" menu to "Warehouse Mapping".

           Menus.Name drives the Role Management grid label (menu.menuName) and
           acts as the fallback display name. The nav + page title come from the
           translation key nav.shipNodes, whose value was updated to
           "Warehouse Mapping" (en) / "Mapeo de almacenes" (es) in the UI.
           Route and TranslationKey are left unchanged (internal identifiers).

  Idempotent: safe to run multiple times.
==============================================================================*/

SET NOCOUNT ON;

UPDATE dbo.Menus
SET Name = 'Warehouse Mapping'
WHERE Route = 'edi/shipNodes'
  AND Name <> 'Warehouse Mapping';

PRINT CONCAT('Rows renamed: ', @@ROWCOUNT);

-- Verify
SELECT Id, Name, Route, TranslationKey FROM dbo.Menus WHERE Route = 'edi/shipNodes';
GO
