# eSyncMate V2 - Application Flow Document

## End-to-End Process Guide

**Version:** 2.0
**Date:** April 2026
**Prepared for:** Client Review & QA Deployment

---

## 1. Application Overview

eSyncMate is an enterprise B2B Electronic Data Interchange (EDI) management platform designed for the home furnishing industry. The platform automates order processing, product catalog management, inventory tracking, and pricing operations across multiple retail partners.

### Key Capabilities
- EDI document processing (850 Purchase Orders, 855 Acknowledgments, 856 ASN, 810 Invoices)
- Product catalog and pricing management with bulk upload
- Real-time inventory feed tracking and batch processing
- Automated data flows between trading partners
- Role-based access control with granular permissions
- Multi-language support (English / Spanish)
- Real-time notifications and alert configuration

### Supported Retail Partners
| Partner Code | Retailer |
|-------------|----------|
| AMA1005 | Amazon |
| KNO8068 | Knot |
| LOW2221MP | Lowe's |
| MAC0149M | Macy's |
| MIC1300MP | Michaels |
| TAR6266P | Target |
| WAL4001MP | Walmart |

---

## 2. Login & Authentication Flow

### Step 1: User Login
- User navigates to the login page
- Enters **User ID** and **Password** (8-15 characters)
- Selects language preference (English / Spanish)
- Clicks **Login** button

### Step 2: Server Authentication
- Backend validates credentials against the database
- On success, generates a **JWT token** (120-minute expiry)
- Fetches user's **menu tree** based on their assigned role and company
- Returns token + menu tree to the frontend

### Step 3: Session Initialization
- JWT token stored in browser localStorage
- Menu tree stored for navigation rendering
- User redirected to their first permitted page (e.g., Dashboard)

### Step 4: Session Security
- All API requests include the JWT token in the Authorization header
- Sessions expire after **120 minutes** of inactivity
- On expiry, user is redirected to login with a session expiry message
- Logout clears all stored tokens and menu data

### Login Page Features
- Password field (hidden by default)
- Language selector (English / Spanish)
- Progress bar animation during login
- Clear button to reset form fields
- Links to Terms of Use and Privacy Policy
- Session expiry notification on re-login

---

## 3. Navigation & Module Structure

After login, the user sees a **top navigation bar** with module dropdowns. Each module contains menu items based on the user's role permissions.

### Module Overview

| Module | Icon | Description |
|--------|------|-------------|
| **Dashboard** | dashboard | Real-time KPIs and statistics |
| **Orders** | shopping_cart | Order management and EDI processing |
| **Products** | inventory_2 | Product catalog, pricing, and inventory |
| **Setup** | settings | Flows, alert configuration |
| **Logs** | description | Exception tracking and error logs |
| **Admin** | admin_panel_settings | User and role management |

### Menu Items by Module

#### Dashboard
| Menu | Description |
|------|-------------|
| Dashboard | Order statistics, inventory stats, customer breakdown |

#### Orders
| Menu | Description |
|------|-------------|
| Orders | View, search, and process orders with EDI actions |
| Customers | Manage customer master data and alert configuration |

#### Products
| Menu | Description |
|------|-------------|
| Customer Product Catalog | Upload and manage product data per customer |
| Product Promotions | Bulk upload promotional pricing |
| Product Prices | View/manage pricing with Excel export |
| Inventory Feed Summary | Track inventory import batches |

#### Setup
| Menu | Description |
|------|-------------|
| Flows | Configure automated partner data exchange flows |
| Alert Configuration | Set up event-based alerts and notifications |

#### Logs
| Menu | Description |
|------|-------------|
| Exceptions | Track data processing errors and route exceptions |

#### Admin
| Menu | Description |
|------|-------------|
| Users | Create, edit, delete user accounts with role assignment |
| Role Management | Configure roles and menu-level permissions |

---

## 4. Dashboard

### Purpose
Provides a real-time overview of system activity and KPIs.

### What Users See
- **Order Statistics**: Total orders, orders by customer, orders by status
- **Inventory Statistics**: Full feed and differential feed counts, batch processing status
- **Customer Breakdown**: Partner-wise order and inventory summary
- **Auto-Refresh**: Dashboard refreshes every 15 minutes

### Status Color Coding
| Status | Meaning | Color |
|--------|---------|-------|
| NEW | Order received | Blue |
| SYNCED | Sent to ERP | Sky Blue |
| SHIPPED | ASN generated | Green |
| INVOICED | Invoice sent | Purple |
| ERROR | Processing failed | Red |
| INPROGRESS | Currently processing | Light Blue |

---

## 5. Orders Management

### Purpose
Central hub for managing purchase orders received from retail partners.

### End-to-End Order Flow

```
Retailer sends EDI 850 (Purchase Order)
    |
    v
eSyncMate receives and creates order (Status: NEW)
    |
    v
Order synced to ERP system (Status: SYNCED)
    |
    v
ERP processes order (Status: PROCESSED)
    |
    v
Generate ASN - EDI 856 (Status: ASNGEN/ASNMARK)
    |
    v
Ship order and send ASN to retailer (Status: SHIPPED)
    |
    v
Generate Invoice - EDI 810 (Status: INVOICED)
    |
    v
Order complete (Status: COMPLETE/FINISHED)
```

### Available Actions (Based on Permissions)

| Action | Description | When Available |
|--------|-------------|---------------|
| View Details | View complete order information | Always |
| Sync Order | Send order to ERP | NEW or SYNCERROR status |
| Send ACK (855) | Acknowledge receipt to retailer | After sync |
| Generate ASN (856) | Create shipment notification | After processing |
| Mark for ASN | Flag order for ASN generation | After processing |
| Create Invoice | Generate invoice record | After shipment |
| Create 810 | Send invoice EDI to retailer | After invoice |
| Re-Process | Retry failed orders | ERROR/ASNERROR/ACKERROR |
| Process for Shipment | Trigger shipment workflow | After sync |

### Search Capabilities
- Order ID, Customer PO Number, ERP SO Number
- Date range (From/To)
- Status (with searchable dropdown)
- Customer Name (with searchable dropdown)

### Server-Side Pagination
- Default: 10 records per page
- Options: 5, 10, 25, 100
- First/Last page buttons

---

## 6. Customer Management

### Purpose
Maintain customer/trading partner master data.

### Features
- **Search** by ID, Name, ERP Customer ID, ISA Customer ID, Marketplace, Date
- **Add Customer** - Create new trading partner record
- **Edit Customer** - Modify customer details (ERP IDs, ISA IDs, marketplace)
- **Alert Configuration** - Set up per-customer alert rules

### Customer Data Fields
- Name, ERP Customer ID, ISA Customer ID
- ISA 810 Receiver ID, ISA 856 Receiver ID
- Marketplace, Created Date

---

## 7. Product Catalog Management

### Purpose
Upload, manage, and track product data for each customer/retailer.

### End-to-End Product Flow

```
Select ERP Customer ID and Item Type
    |
    v
Download sample CSV template
    |
    v
Fill in product data (SKU, pricing, UPC, etc.)
    |
    v
Upload CSV/Excel file
    |
    v
System processes and validates data (Status: NEW)
    |
    v
Products approved and pricing synced (Status: APPROVED/APPROVED_PR)
    |
    v
Products synced with retailer (Status: SYNCED)
```

### Three Card Layout
1. **Upload Section** - Customer selection, item type filter, sample download, file upload, rejected CSV download
2. **Search & Actions** - Search by various fields + Download/Clear Errors buttons
3. **Data Grid** - Product listing with status, pricing, and edit actions

### Collapse Feature
- Arrow toggle to hide/show upload and search sections
- Maximizes grid space for data review

---

## 8. Product Pricing

### Purpose
Manage product prices with bulk upload and Excel export capabilities.

### Features
- Upload pricing files (CSV/Excel)
- Search by ERP Customer ID, Item ID, Status, Date
- **Process Product Prices** - Trigger pricing sync
- **Export to Excel** - Download current view
- Status tracking with colored pill badges

---

## 9. Inventory Feed Summary

### Purpose
Track inventory data import batches from various sources.

### Features
- Search by Item ID, Customer, Route Type, Date range, Status
- View batch details with **Get Batch Wise Inventory** action
- Batch detail dialog with server-side pagination and item search
- File viewer for individual inventory records

### Inventory Batch Statuses
| Status | Meaning |
|--------|---------|
| PROCESSING | Batch currently being imported |
| COMPLETED | Batch successfully processed |
| ERROR | Batch processing failed |

---

## 10. Flows Management

### Purpose
Configure automated data exchange flows between trading partners.

### Features
- Create, edit, and delete flows
- Partner/customer assignment
- Flow status tracking
- Flow detail configuration with route steps

---

## 11. Alert Configuration

### Purpose
Set up event-driven alerts for system monitoring.

### Features
- Create alert rules with name and query
- Assign alerts to customers
- Edit and delete alert configurations

---

## 12. Exception Logs

### Purpose
Monitor and investigate data processing errors.

### Features
- Search by Route Name, Message, Date range, Status
- View error details and stack traces
- Download error files for investigation
- Status filtering (Active / In-Active)
- Server-side pagination for large log volumes

---

## 13. User Management

### Purpose
Administer user accounts with role-based access.

### User Creation Flow

```
Admin clicks "Add User"
    |
    v
Fill in: User ID, Name, Email, Phone, Password
    |
    v
Select Role (SuperAdmin, Admin, Operator, Viewer)
    |
    v
Select Status (Active / Blocked)
    |
    v
Assign Customer(s) for data filtering
    |
    v
Click "Create Account"
    |
    v
System creates user + assigns role via UserRoles table
```

### User Grid Features
- Search by ID, User ID, Name, Email, Status, Date
- **Role column** - Shows assigned role name
- **Edit User** - Modify details, change role, update password
- **Delete User** - Soft delete with confirmation dialog
- Server-side pagination

---

## 14. Role-Based Access Control (RBAC)

### Overview
eSyncMate uses a comprehensive RBAC system that controls which modules, menus, and actions each user can access.

### Role Hierarchy

| Role | Access Level | Permissions |
|------|-------------|-------------|
| **SuperAdmin** | Full system access | All menus, all CRUD operations |
| **Admin** | Management access | All menus except Role Management, full CRUD |
| **Operator** | Operational access | Orders, Products, Logs; Add/Edit (no Delete) |
| **Viewer** | Read-only access | Orders, Products; View only |

### Permission Levels (Per Menu Item)

| Permission | Controls |
|-----------|----------|
| **canView** | Menu visibility in navigation + route access |
| **canAdd** | "Add" / "Create" buttons shown |
| **canEdit** | "Edit" buttons and edit actions shown |
| **canDelete** | "Delete" buttons shown |

### Role Management Interface

**Tab 1: Roles**
- Create new roles with name, description, and active status
- Edit existing roles
- Delete roles

**Tab 2: Menu Assignments**
- Select a role to configure
- Module-grouped accordion with checkboxes
- Per-menu permissions: View, Add, Edit, Delete
- "Select All" toggle per module
- Save button applies all changes

### How Permissions Flow

```
User logs in
    |
    v
Backend queries VW_UserMenus (role-based + direct user menus)
    |
    v
Menu tree returned with canView/canAdd/canEdit/canDelete per item
    |
    v
Frontend stores in localStorage
    |
    v
Navigation renders only permitted modules/menus
    |
    v
AuthorizationGuard blocks unauthorized routes
    |
    v
Each component checks permissions for Add/Edit/Delete buttons
```

### Hidden Menus System

Certain internal menus are permanently hidden from the Role Management UI:
- Connectors, Maps, Partner Groups, Routes, Route Types, Hangfire Dashboard

These menus:
- Never appear in the Roles interface
- Cannot be assigned through roles
- Can only be granted to specific users via direct database assignment

### Direct User Menu Assignment

For granting hidden menus to specific users:
```
UserMenus table: UserId + MenuId + Permissions
    |
    v
VW_UserMenus UNION includes direct assignments
    |
    v
User sees hidden menus in their navigation
```

---

## 15. Registration (Add User)

### Purpose
Create new user accounts with role assignment.

### Form Fields
- User ID (unique identifier)
- First Name, Last Name
- Email, Phone Number
- Password + Confirm Password (8-15 characters)
- **Role** (dropdown - SuperAdmin, Admin, Operator, Viewer)
- Status (Active / Blocked)
- Customer Assignment (multi-select)

### Process
1. Admin fills in user details
2. Selects a role from the dropdown
3. Assigns customer access
4. Clicks "Create Account"
5. Backend creates user record + UserRoles entry
6. New user can immediately log in

---

## 16. Notifications (ESYNCMATE)

### Purpose
Real-time event notifications for order and processing status changes.

### Features
- Bell icon in header with unread count badge
- Notification dropdown with status icons
- Color-coded: Green (Completed), Red (Failed), Orange (Running)
- Time-ago display (Just now, 5m ago, 2h ago, 3d ago)
- Mark individual or all notifications as read
- Server time synchronization for accurate timestamps

---

## 17. Data Grid Features (All Interfaces)

### Common Features Across All Grids
- **Server-side pagination** - Only requested page loaded from database
- **Progress bar** - Non-blocking loading indicator above table
- **Blue-gray header** - Professional table header styling
- **Alternating rows** - Striped background for readability
- **Hover highlight** - Light blue row highlight on hover
- **Status pills** - Modern colored pill badges for status values
- **Action buttons** - Icon buttons with tooltips (Edit, Delete, View)
- **First/Last page buttons** - Quick navigation in paginator

### Page Size Options
- 5, 10 (default), 25, 100 records per page

---

## 18. File Upload & Export

### Upload Capabilities
| Interface | File Types | Purpose |
|-----------|-----------|---------|
| Product Catalog | CSV, XLS, XLSX | Product data import |
| Product Promotions | CSV, XLS, XLSX | Promotional pricing |
| Product Prices | CSV, XLS, XLSX | Price updates |
| Process 850 | EDI 850 format | Purchase order import |

### Export Capabilities
| Interface | Export Type | Purpose |
|-----------|-----------|---------|
| Product Prices | Excel (XLSX) | Price data export |
| Product Catalog | CSV | Rejected items download |
| Product Catalog | CSV | Sample template download |
| Product Catalog | CSV | Items data download |

---

## 19. Security Features

| Feature | Implementation |
|---------|--------------|
| Authentication | JWT tokens with 120-minute expiry |
| Authorization | Route guards (AuthorizationGuard) |
| Password Security | Encrypted storage, 8-15 char requirement |
| Session Management | Auto-expiry, inactivity detection |
| RBAC | 4-level permission model (View/Add/Edit/Delete) |
| Data Isolation | Company-based menu filtering |
| Audit Trail | All changes logged with user and timestamp |
| Hidden Menus | Internal tools hidden from role assignment |

---

## 20. Terms of Use & Privacy Policy

### Accessible From
- Login page footer links
- Opens in new browser tab

### Terms of Use Covers
- Acceptable use policy
- EDI compliance requirements
- File upload responsibilities
- Third-party integration terms
- Intellectual property
- Session and security policies

### Privacy Policy Covers
- Data collection (account, business, system data)
- Data usage and processing
- Security measures (encryption, RBAC, audit logging)
- Third-party data sharing (trading partners, ShipStation, Amazon)
- Data retention policies
- User rights and contact information
- Cookie and local storage usage

---

## 21. System Architecture Summary

```
+-------------------+     +-------------------+     +-------------------+
|   Angular 18 UI   |---->|   .NET Backend     |---->|   SQL Server DB   |
|   (Port 5015)     |     |   (Port 5000)      |     |                   |
+-------------------+     +-------------------+     +-------------------+
        |                         |                         |
        |                         |                    +----+----+
   localStorage              JWT Auth              Tables | Views
   - access_token             RBAC                 - Users
   - user_menus               API                  - Roles
   - tokenExpiry                                   - Modules
                                                   - Menus
                                                   - RoleMenus
                                                   - UserRoles
                                                   - UserMenus
                                                   - Orders
                                                   - Customers
                                                   - Inventory
                                                   - ...
```

---

## 22. Glossary

| Term | Definition |
|------|-----------|
| **EDI** | Electronic Data Interchange - standardized electronic communication |
| **EDI 850** | Purchase Order document |
| **EDI 855** | Purchase Order Acknowledgment |
| **EDI 856** | Advance Shipment Notification (ASN) |
| **EDI 810** | Invoice document |
| **ERP** | Enterprise Resource Planning system (e.g., SPARS) |
| **RBAC** | Role-Based Access Control |
| **JWT** | JSON Web Token - authentication mechanism |
| **ISA** | Interchange Control Header - EDI partner identifier |
| **ASN** | Advance Shipment Notification |
| **SKU** | Stock Keeping Unit |
| **UPC** | Universal Product Code |
| **ATS** | Available To Sell (inventory) |
| **NDC** | National Distribution Center |

---

**Document End**

*This document covers the complete end-to-end application flow of eSyncMate V2. For technical implementation details, refer to the separate RoleBasedAccessControl.md and UI_Implementation.md documents in the DesignDoc folder.*
