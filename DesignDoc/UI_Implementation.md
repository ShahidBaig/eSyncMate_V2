# UI Implementation Documentation

## Overview

This document describes the UI overhaul applied to eSyncMate V2, porting the modern UI patterns from V1. The changes transform the application from a basic Material toolbar layout to a professional enterprise B2B SaaS interface with dynamic, role-based navigation.

---

## Login Screen

### Layout
- **Two-column design**: Login card on the left (38% width), promotional cards on the right
- **Responsive**: Promo column hides below 900px, login becomes full-width

### Components
- **Left Column**: Login form with User ID, Password (with visibility toggle), Language selector, Login/Cancel buttons, Terms of Use footer
- **Right Column**: Two promo cards - "Seamless EDI Integration" (teal gradient) and "Multi-Platform Support" (light neutral)

### Authentication Flow
1. User enters credentials and submits
2. Backend returns JWT token + menu tree (modules/menus with permissions)
3. Token stored in `localStorage.access_token`
4. Menu tree stored in `localStorage.user_menus`
5. Navigation to first available menu route from user's permissions
6. Fallback: company-based navigation if no menus available

### Color Palette
| Token | Value | Usage |
|-------|-------|-------|
| Primary | `#2563eb` | Links, focus states |
| Primary Dark | `#1d4ed8` | Hover states |
| Teal | `#0d9488` | Promo card gradient |
| Gray-600 | `#475569` | Login button |
| Gray-800 | `#1e293b` | Text |

### Files
- `UI/src/app/login/login.component.ts`
- `UI/src/app/login/login.component.html`
- `UI/src/app/login/login.component.scss`

---

## Post-Login Layout

### Structure
```
+--------------------------------------------------+
| Page Header (sticky top, z-index: 100)           |
| [Logo] [Module Nav Dropdowns] [Search][Bell][User]|
+--------------------------------------------------+
| Page Title Bar (current page name)               |
+--------+-----------------------------------------+
| Sidebar| Main Content Area                       |
| (side  | (router-outlet)                         |
|  nav)  |                                         |
+--------+-----------------------------------------+
```

### App Component
- Uses Angular Material `mat-sidenav-container` with `mode="side"` sidebar
- Sidebar and header conditionally rendered via `isLoggedIn` getter
- Background color: `#f8fafc`

### Files
- `UI/src/app/app.component.ts`
- `UI/src/app/app.component.html`
- `UI/src/app/app.component.scss`

---

## Page Header (Navbar)

### Features
1. **Logo & Branding** (left): eSyncMate logo with text
2. **Module Navigation** (center): Horizontal module buttons with hover dropdowns showing menu items
3. **Search** (right): Expandable search bar filtering across all menus by name/module/route
4. **Notification Bell** (right, ESYNCMATE only): Real-time notification polling with unread count badge
5. **Language Selector** (right): English/Spanish toggle via mat-menu
6. **User Profile** (right): Avatar with initials, name, dropdown with Change Password, Add User, Logout

### Dynamic Navigation
- Modules and menus loaded from `localStorage.user_menus` (set during login)
- Active module/menu highlighted based on current route
- `isAdminUser` computed by checking if user's menus include `edi/users` or `edi/roles`
- Page title bar below header shows current page name from menu metadata

### Notification System (preserved from V2)
- `NotificationService` polls for notifications (ESYNCMATE company only)
- Bell icon with red unread count badge (max "9+")
- Dropdown with notification items showing status icons, route name, message, time-ago

### Files
- `UI/src/app/page-header/page-header.component.ts`
- `UI/src/app/page-header/page-header.component.html`
- `UI/src/app/page-header/page-header.component.scss`

---

## Sidebar Navigation

### Features
1. **Logo** at top with border separator
2. **Search bar**: Filters menus by name/module/route, shows search results view
3. **Module groups**: Expandable/collapsible module headers with icons
4. **Menu items**: Dot indicators for inactive items, highlighted blue for active route
5. **Auto-expansion**: Active module automatically expanded on page load
6. **Custom scrollbar**: Thin 3px scrollbar for navigation overflow

### Dynamic Data
- `modules: UserMenuModule[]` loaded from `api.getUserMenus()` (localStorage)
- Only items with `canView: true` are rendered
- `expandedModules: Set<number>` tracks expanded state
- Route tracking via `NavigationEnd` subscription

### Files
- `UI/src/app/side-nav/side-nav.component.ts`
- `UI/src/app/side-nav/side-nav.component.html`
- `UI/src/app/side-nav/side-nav.component.scss`

---

## Design System

### Typography
- **Font Family**: `'Poppins', 'Segoe UI', sans-serif`
- **Header**: 56px height
- **Module titles**: 18px, uppercase, weight 600
- **Menu items**: 24px (sidebar), 13px (dropdowns)

### Color Tokens (consistent across all components)
```scss
$primary:       #2563eb;
$primary-dark:  #1d4ed8;
$primary-light: #eff6ff;
$gray-50:       #f8fafc;
$gray-100:      #f1f5f9;
$gray-200:      #e2e8f0;
$gray-300:      #cbd5e1;
$gray-400:      #94a3b8;
$gray-500:      #64748b;
$gray-600:      #475569;
$gray-700:      #334155;
$gray-800:      #1e293b;
$gray-900:      #0f172a;
```

### Responsive Breakpoints
- `900px`: Login promo column hidden, login becomes single column
- Sidebar uses flex column with overflow-y auto for smaller screens

---

## Key Architectural Decisions

1. **Dynamic menus from backend**: No hardcoded menu items in frontend. All menus come from the RBAC system via the login response.
2. **localStorage caching**: Menu tree cached in `user_menus` key to avoid re-fetching on every page load.
3. **Backward compatibility**: If no menus are stored (user without role), company-based fallback navigation is used.
4. **Notification preservation**: V2's notification system was merged into V1's header design rather than replaced.
5. **Standalone components**: All new/modified components use Angular's standalone component pattern.

---

**Last Updated**: 2026-04-01
