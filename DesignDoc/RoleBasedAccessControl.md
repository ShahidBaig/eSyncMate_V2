# Role-Based Access Control (RBAC) Implementation

## Overview

This document describes the complete RBAC system implemented in eSyncMate V2, ported from V1. The system controls which menus, modules, and actions each user can access based on their assigned role.

---

## Database Schema

### Tables

| Table | Purpose |
|-------|---------|
| `Roles` | Role definitions (SuperAdmin, Admin, Operator, Viewer) |
| `Modules` | Top-level menu groups (Order Management, Setup, Admin, etc.) |
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
JOIN Menus mn ON rm.MenuId = mn.Id AND mn.IsActive = 1
JOIN Modules m ON mn.ModuleId = m.Id AND m.IsActive = 1
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
| 3 | Operator | Operational menus | Orders, Purchases, Products, Exceptions, Alerts (no delete) |
| 4 | Viewer | Read-only access | Orders, Purchases, Products (view only) |

---

## Seeded Modules

| Id | Name | Icon | Sort |
|----|------|------|------|
| 1 | Dashboard | dashboard | 1 |
| 2 | Order Management | shopping_cart | 2 |
| 3 | Purchases | receipt_long | 3 |
| 4 | Carrier Load Management | local_shipping | 4 |
| 5 | Product Management | inventory_2 | 5 |
| 6 | Setup | settings | 6 |
| 7 | Exceptions | error | 7 |
| 8 | Admin | admin_panel_settings | 8 |
| 9 | Alerts | notifications | 9 |

---

## Route-to-Menu Mapping

| Route | Menu Name | Module | Company |
|-------|-----------|--------|---------|
| `edi/dashboard` | Dashboard | Dashboard | ESYNCMATE,REPAINTSTUDIOS |
| `edi/all-orders` | Orders | Order Management | ESYNCMATE,REPAINTSTUDIOS |
| `edi/customers` | Customers | Order Management / Carrier Load | ESYNCMATE,REPAINTSTUDIOS / GECKOTECH |
| `edi/purchaseOrder` | Purchase Orders | Purchases | SURGIMAC |
| `edi/carrier` | Carrier Load Tender | Carrier Load Management | GECKOTECH |
| `edi/ediFileCounter` | EDI File Counter | Carrier Load Management | GECKOTECH |
| `edi/customerProductCatalog` | Customer Product Catalog | Product Management | ESYNCMATE |
| `edi/productuploadprices` | Product Upload Prices | Product Management | ESYNCMATE |
| `edi/productPrices` | Product Prices | Product Management | ESYNCMATE |
| `edi/inventory` | Inventory | Product Management | ESYNCMATE |
| `edi/invFeedFromNDC` | Inv Feed From NDC | Product Management | SURGIMAC |
| `edi/sipmentFromNdc` | Shipment From NDC | Product Management | SURGIMAC |
| `edi/salesInvoiceNdc` | Sales Invoice NDC | Product Management | SURGIMAC |
| `edi/purchaseOrdersTracking` | PO Tracking | Product Management | SURGIMAC |
| `edi/connectors` | Connectors | Setup | Shared |
| `edi/maps` | Maps | Setup | Shared |
| `edi/partnergroups` | Partner Groups | Setup | Shared |
| `edi/routes` | Routes | Setup | Shared |
| `edi/routeTypes` | Route Types | Setup | Shared |
| `edi/flows` | Flows | Setup | Shared |
| `edi/routeExceptions` | Route Exceptions | Exceptions | Shared |
| `edi/users` | User Management | Admin | Shared |
| `edi/roles` | Role Management | Admin | Shared |
| `edi/alertConfiguration` | Alert Configuration | Alerts | Shared |

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
