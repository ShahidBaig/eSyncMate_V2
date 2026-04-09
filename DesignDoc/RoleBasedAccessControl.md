# Role-Based Access Control (RBAC) Implementation

## Overview

This document describes the complete RBAC system implemented in eSyncMate V2, ported from V1. The system controls which menus, modules, and actions each user can access based on their assigned role.

---

## Database Schema

### Tables

| Table | Purpose |
|-------|---------|
| `Roles` | Role definitions (SuperAdmin, Admin, Operator, Viewer) |
| `Modules` | Top-level menu groups (Orders, Setup, Admin, etc.) |
| `Menus` | Individual menu items with routes, icons, and company filtering |
| `RoleMenus` | Role-to-menu permission mapping (canView/canAdd/canEdit/canDelete) |
| `UserRoles` | User-to-role assignment (one role per user) |

### View

**`VW_UserMenus`** - Joins all 5 tables to produce a flat result set of user permissions:
```sql
SELECT ur.UserId, r.RoleName, m.ModuleId, m.ModuleName, mn.MenuId, mn.MenuName,
       mn.Route, mn.Company, rm.CanView, rm.CanAdd, rm.CanEdit, rm.CanDelete
FROM UserRoles ur
JOIN Roles r ON ur.RoleId = r.Id AND r.IsActive = 1
JOIN RoleMenus rm ON r.Id = rm.RoleId AND rm.CanView = 1
JOIN Menus mn ON rm.MenuId = mn.Id AND mn.IsActive = 1 AND (mn.IsHidden = 0 OR mn.IsHidden IS NULL)
JOIN Modules m ON mn.ModuleId = m.Id AND m.IsActive = 1
```

### Hidden Menus (`IsHidden` Flag)

The `Menus.IsHidden` column controls menu visibility across the entire system:

| IsHidden | Behavior |
|----------|----------|
| `0` (default) | Menu is visible in navigation and role assignment UI |
| `1` | Menu is completely hidden — not shown in nav, not assignable in roles |

**Currently Hidden Menus:**
| Menu | Route | Reason |
|------|-------|--------|
| Connectors | `edi/connectors` | Internal setup — not needed for client users |
| Maps | `edi/maps` | Internal setup — not needed for client users |
| Partner Groups | `edi/partnergroups` | Internal setup — not needed for client users |
| Routes | `edi/routes` | Internal setup — not needed for client users |
| Route Types | `edi/routeTypes` | Internal setup — not needed for client users |
| Hangfire Dashboard | `hangfire/dashboard` | System admin tool — not for end users |

**How it works:**
- Hidden menus are **permanently excluded** from the Role Management UI (`getMenus` filters `IsHidden = 0`)
- Hidden menus are **excluded** from `VW_UserMenus` role-based query
- Hidden menus can be **granted to specific users** via the `UserMenus` table (direct assignment)
- `VW_UserMenus` uses a `UNION` of role-based menus + direct user-menu assignments

### Direct User Menu Assignment (`UserMenus` Table)

For granting hidden menus to specific users without making them role-configurable:

| Table | Purpose |
|-------|---------|
| `UserMenus` | Direct user-to-menu mapping that bypasses roles entirely |

**Columns:** `Id`, `UserId`, `MenuId`, `CanView`, `CanAdd`, `CanEdit`, `CanDelete`, `CreatedDate`, `CreatedBy`

**How to grant hidden menus to a specific user:**
```sql
-- Grant Connectors (MenuId=8) to user with Id=1
DECLARE @Id INT = (SELECT ISNULL(MAX(Id), 0) + 1 FROM [dbo].[UserMenus]);
INSERT INTO [dbo].[UserMenus] (Id, UserId, MenuId, CanView, CanAdd, CanEdit, CanDelete, CreatedDate, CreatedBy)
VALUES (@Id, 1, 8, 1, 1, 1, 1, GETDATE(), 0);

-- Revoke it
DELETE FROM [dbo].[UserMenus] WHERE UserId = 1 AND MenuId = 8;
```

**API Endpoints for direct user-menu management:**

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/Role/getHiddenMenus` | GET | List all hidden menus (for admin UI) |
| `/api/Role/getUserMenusDirect?userId=X` | GET | Get direct menu assignments for a user |
| `/api/Role/saveUserMenusDirect` | POST | Save direct menu assignments for a user |

**Security architecture:**
```
                    ┌─────────────────────┐
                    │   Role Management   │
                    │   (Roles UI)        │
                    │                     │
                    │  Only sees menus    │
                    │  where IsHidden=0   │
                    └─────────┬───────────┘
                              │
              ┌───────────────┴───────────────┐
              │         VW_UserMenus          │
              │          (UNION)              │
              ├───────────────┬───────────────┤
              │  Role-Based   │  Direct User  │
              │  (IsHidden=0) │  (UserMenus)  │
              │  via Roles +  │  Any menu,    │
              │  RoleMenus    │  per user     │
              └───────────────┴───────────────┘
```

### Company-Scoped Menus

The `Menus.Company` column controls which companies see each menu:

| Company Value | Behavior |
|---------------|----------|
| `NULL` or `''` | Shared across ALL companies |
| `'SURGIMAC'` | Only visible for SURGIMAC users |
| `'ESYNCMATE,REPAINTSTUDIOS'` | Visible for both companies |
| `'GECKOTECH'` | Only visible for GECKOTECH users |

Filtering happens at query time in `GetUserMenuTree()`:
```sql
WHERE UserId = @userId AND (Company IS NULL OR Company = '' OR Company LIKE '%@company%')
```

### Important: No IDENTITY Columns

All tables use manual ID generation via the ORM's `GetMax()` method. This is the standard pattern across the entire eSyncMate codebase.

---

## Seeded Roles

| Id | Name | Description | Permissions |
|----|------|-------------|-------------|
| 1 | SuperAdmin | Full access to all modules | All menus, full CRUD |
| 2 | Admin | Access to most menus | All menus except Role Management |
| 3 | Operator | Operational menus | Orders, Products, Exceptions, Setup (no delete) |
| 4 | Viewer | Read-only access | Orders, Purchases, Products (view only) |

---

## Seeded Modules

| Id | Name | Icon | Sort |
|----|------|------|------|
| 1 | Dashboard | dashboard | 1 |
| 2 | Orders | shopping_cart | 2 |
| 3 | Products | inventory_2 | 3 |
| 4 | Setup | settings | 4 |
| 5 | Logs | description | 5 |
| 6 | Admin | admin_panel_settings | 6 |

> **Note:** Alerts module was removed. Alert Configuration is now under Setup.

---

## Route-to-Menu Mapping

| Route | Menu Name | Module | Company |
|-------|-----------|--------|---------|
| `edi/dashboard` | Dashboard | Dashboard | ESYNCMATE,REPAINTSTUDIOS |
| `edi/all-orders` | Orders | Orders | ESYNCMATE,REPAINTSTUDIOS |
| `edi/customers` | Customers | Setup | ESYNCMATE,REPAINTSTUDIOS / GECKOTECH |
| `edi/purchaseOrder` | Purchase Orders | Purchases | SURGIMAC |
| `edi/carrier` | Carrier Load Tender | Carrier Load Management | GECKOTECH |
| `edi/ediFileCounter` | EDI File Counter | Carrier Load Management | GECKOTECH |
| `edi/customerProductCatalog` | Customer Product Catalog | Products | ESYNCMATE |
| `edi/productuploadprices` | Product Upload Prices | Products | ESYNCMATE |
| `edi/productPrices` | Product Prices | Products | ESYNCMATE |
| `edi/inventory` | Inventory Feed Summary | Products | ESYNCMATE |
| `edi/invFeedFromNDC` | Inv Feed From NDC | Products | SURGIMAC |
| `edi/sipmentFromNdc` | Shipment From NDC | Products | SURGIMAC |
| `edi/salesInvoiceNdc` | Sales Invoice NDC | Products | SURGIMAC |
| `edi/purchaseOrdersTracking` | PO Tracking | Products | SURGIMAC |
| `edi/connectors` | Connectors | Setup | Shared |
| `edi/maps` | Maps | Setup | Shared |
| `edi/partnergroups` | Partner Groups | Setup | Shared |
| `edi/routes` | Routes | Setup | Shared |
| `edi/routeTypes` | Route Types | Setup | Shared |
| `edi/flows` | Flows | Setup | Shared |
| `edi/routeExceptions` | Exceptions | Logs | Shared |
| `edi/users` | User Management | Admin | Shared |
| `edi/roles` | Role Management | Admin | Shared |
| `edi/alertConfiguration` | Alerts | Setup | Shared |

---

## Backend API

### Authentication Controller Changes

**Login endpoint** (`GET /Login`) now returns menu tree in response:
```json
{
  "token": "jwt-token-string",
  "message": "Success",
  "menus": {
    "roleName": "SuperAdmin",
    "modules": [
      {
        "moduleId": 1,
        "moduleName": "Dashboard",
        "moduleTranslationKey": "module.dashboard",
        "moduleIcon": "dashboard",
        "moduleSortOrder": 1,
        "menuItems": [
          {
            "menuId": 1,
            "menuName": "Dashboard",
            "menuTranslationKey": "nav.dashboard",
            "route": "edi/dashboard",
            "menuIcon": "dashboard",
            "canView": true,
            "canAdd": true,
            "canEdit": true,
            "canDelete": true
          }
        ]
      }
    ]
  }
}
```

**RegisterUser endpoint** (`POST /registerUser`) now accepts `roleId` in the request body and automatically creates a `UserRoles` entry.

### Role Controller Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/Role/getRoles` | GET | List all roles |
| `/api/Role/saveRole` | POST | Create or update a role |
| `/api/Role/deleteRole` | POST | Delete a role |
| `/api/Role/getModules` | GET | List all active modules |
| `/api/Role/getMenus` | GET | List all active menus |
| `/api/Role/getRoleMenus?roleId=X` | GET | Get menu permissions for a role |
| `/api/Role/saveRoleMenus` | POST | Save menu permissions for a role |
| `/api/Role/getUserRole?userId=X` | GET | Get role assigned to a user |
| `/api/Role/saveUserRole` | POST | Assign a role to a user |
| `/api/Role/getUserMenus?userId=X&company=Y` | GET | Get full menu tree for a user |

### Backend Entity Files

| File | Key Features |
|------|-------------|
| `eSyncMate.DB/Entities/Roles.cs` | Standard CRUD (GetList, SaveNew, Modify, Delete) |
| `eSyncMate.DB/Entities/Modules.cs` | Standard CRUD |
| `eSyncMate.DB/Entities/Menus.cs` | Standard CRUD, includes Company field |
| `eSyncMate.DB/Entities/RoleMenus.cs` | `GetViewList()` queries VW_UserMenus, `DeleteByRoleId()` for bulk delete |
| `eSyncMate.DB/Entities/UserRoles.cs` | `DeleteByUserId()` for role reassignment |

---

## Frontend Implementation

### Data Flow

```
Login → API returns token + menus
  → saveToken(token) to localStorage
  → saveUserMenus(menus) to localStorage
  → Navigate to first allowed route

Page Load → getUserMenus() from localStorage
  → Header renders module navigation dropdowns
  → Sidebar renders expandable module groups
  → Guard checks route permissions
```

### Key Frontend Files

| File | Purpose |
|------|---------|
| `models/models.ts` | RBAC interfaces (UserMenuItem, UserMenuModule, UserMenuResponse, etc.) |
| `services/api.service.ts` | Menu localStorage methods + Role API calls |
| `authorization.guard.ts` | Route protection with RBAC checking |
| `login/login.component.ts` | Saves menus from login response, menu-based navigation |
| `page-header/page-header.component.ts` | Dynamic module navigation from stored menus |
| `side-nav/side-nav.component.ts` | Dynamic sidebar from stored menus |
| `role-management/role-management.component.ts` | Admin UI for managing roles and menu permissions |

### Permission Checking in Components

```typescript
// Check if user can perform action on current route
const permissions = this.api.getMenuPermissions('edi/users');
if (permissions) {
  this.canAdd = permissions.canAdd;
  this.canEdit = permissions.canEdit;
  this.canDelete = permissions.canDelete;
}

// Check admin status
get isAdminUser(): boolean {
  const userMenus = this.api.getUserMenus();
  if (userMenus?.modules) {
    for (const mod of userMenus.modules) {
      for (const item of mod.menuItems) {
        if (item.route === 'edi/users' || item.route === 'edi/roles') return true;
      }
    }
  }
  return false;
}
```

### Authorization Guard

The `AuthorizationGuard` protects routes with this logic:
1. Not logged in → redirect to login
2. No menus stored → allow access (backward compatibility)
3. Route in user's menus with `canView: true` → allow
4. Route not permitted → redirect to first allowed route
5. No allowed routes → allow access (backward compat)

### Role Management UI

Two-tab interface at `/edi/roles`:
- **Tab 1 (Roles)**: CRUD table for role definitions with name, description, active status
- **Tab 2 (Menu Assignments)**: Accordion per module with checkboxes for View/Add/Edit/Delete per menu item. "Select All" toggle per module.

---

## User Migration

Existing users are migrated to UserRoles based on their UserType:
- `ADMIN` → SuperAdmin (RoleId: 1)
- `WRITER` → Operator (RoleId: 3)
- `READER` → Viewer (RoleId: 4)

See Script 7 in `RBAC_SQL_Scripts.sql`.

---

## Backward Compatibility

- Users without a `UserRoles` entry will see no menus in the header/sidebar but will NOT be locked out
- The authorization guard falls back to "allow access" when no menus are stored
- Company-based navigation fallback remains in the login component
- `isAdminUser` falls back to checking `userType === 'ADMIN'` if menu data is unavailable

---

**Last Updated**: 2026-04-01
