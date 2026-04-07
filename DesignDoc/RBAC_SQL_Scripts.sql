-- ============================================
-- RBAC (Role-Based Access Control) SQL Scripts
-- Database: ESYNCMATE_TEST (V2)
-- Execute scripts in order (1 through 7)
--
-- IMPORTANT: Tables use manual Id generation via
-- GetMax() in the ORM - NO IDENTITY columns.
--
-- Company column on Menus:
--   NULL or '' = shared across ALL companies
--   'SURGIMAC' = only visible for SURGIMAC users
--   'ESYNCMATE,REPAINTSTUDIOS' = visible for both
--   'GECKOTECH' = only visible for GECKOTECH users
-- ============================================
--DROP TABLE  [dbo].[UserMenus];
--DROP TABLE [dbo].[UserRoles];
--DROP TABLE [dbo].[RoleMenus];
--DROP TABLE [dbo].[Menus];
--DROP TABLE [dbo].[Modules];
--DROP TABLE [dbo].[Roles];
--GO
-- ============================================
-- SCRIPT 1: Create Tables (NO IDENTITY - ORM uses GetMax())
-- ============================================

-- 1. Roles Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Roles')
BEGIN
    CREATE TABLE [dbo].[Roles] (
        [Id]          INT           NOT NULL PRIMARY KEY,
        [Name]        NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [IsActive]    BIT           NOT NULL DEFAULT 1,
        [CreatedDate] DATETIME      NOT NULL DEFAULT GETDATE(),
        [CreatedBy]   INT           NOT NULL DEFAULT 0
    );
END
GO

-- 2. Modules Table (top-level menu groups)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Modules')
BEGIN
    CREATE TABLE [dbo].[Modules] (
        [Id]             INT           NOT NULL PRIMARY KEY,
        [Name]           NVARCHAR(100) NOT NULL,
        [TranslationKey] NVARCHAR(200) NULL,
        [Icon]           NVARCHAR(100) NULL,
        [SortOrder]      INT           NOT NULL DEFAULT 0,
        [IsActive]       BIT           NOT NULL DEFAULT 1,
        [CreatedDate]    DATETIME      NOT NULL DEFAULT GETDATE(),
        [CreatedBy]      INT           NOT NULL DEFAULT 0
    );
END
GO

-- 3. Menus Table (individual menu items)
--    Company column: NULL/empty = all companies, otherwise comma-separated company names
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Menus')
BEGIN
    CREATE TABLE [dbo].[Menus] (
        [Id]             INT           NOT NULL PRIMARY KEY,
        [ModuleId]       INT           NOT NULL,
        [Name]           NVARCHAR(100) NOT NULL,
        [TranslationKey] NVARCHAR(200) NULL,
        [Route]          NVARCHAR(200) NULL,
        [Icon]           NVARCHAR(100) NULL,
        [IsExternalLink] BIT           NOT NULL DEFAULT 0,
        [ExternalUrl]    NVARCHAR(500) NULL,
        [SortOrder]      INT           NOT NULL DEFAULT 0,
        [Company]        NVARCHAR(500) NULL,
        [IsHidden]       BIT           NOT NULL DEFAULT 0,
        [IsActive]       BIT           NOT NULL DEFAULT 1,
        [CreatedDate]    DATETIME      NOT NULL DEFAULT GETDATE(),
        [CreatedBy]      INT           NOT NULL DEFAULT 0,
        CONSTRAINT [FK_Menus_Modules] FOREIGN KEY ([ModuleId]) REFERENCES [dbo].[Modules]([Id])
    );
END
GO

-- 4. RoleMenus Table (role-to-menu permission mapping)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RoleMenus')
BEGIN
    CREATE TABLE [dbo].[RoleMenus] (
        [Id]          INT      NOT NULL PRIMARY KEY,
        [RoleId]      INT      NOT NULL,
        [MenuId]      INT      NOT NULL,
        [CanView]     BIT      NOT NULL DEFAULT 1,
        [CanAdd]      BIT      NOT NULL DEFAULT 0,
        [CanEdit]     BIT      NOT NULL DEFAULT 0,
        [CanDelete]   BIT      NOT NULL DEFAULT 0,
        [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
        [CreatedBy]   INT      NOT NULL DEFAULT 0,
        CONSTRAINT [FK_RoleMenus_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([Id]),
        CONSTRAINT [FK_RoleMenus_Menus] FOREIGN KEY ([MenuId]) REFERENCES [dbo].[Menus]([Id]),
        CONSTRAINT [UQ_RoleMenus_RoleMenu] UNIQUE ([RoleId], [MenuId])
    );
END
GO

-- 5. UserMenus Table (direct user-to-menu assignment — bypasses roles)
--    Used to grant hidden menus to specific users without making them role-assignable
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserMenus')
BEGIN
    CREATE TABLE [dbo].[UserMenus] (
        [Id]          INT      NOT NULL PRIMARY KEY,
        [UserId]      INT      NOT NULL,
        [MenuId]      INT      NOT NULL,
        [CanView]     BIT      NOT NULL DEFAULT 1,
        [CanAdd]      BIT      NOT NULL DEFAULT 0,
        [CanEdit]     BIT      NOT NULL DEFAULT 0,
        [CanDelete]   BIT      NOT NULL DEFAULT 0,
        [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
        [CreatedBy]   INT      NOT NULL DEFAULT 0,
        CONSTRAINT [FK_UserMenus_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_UserMenus_Menus] FOREIGN KEY ([MenuId]) REFERENCES [dbo].[Menus]([Id]),
        CONSTRAINT [UQ_UserMenus_UserMenu] UNIQUE ([UserId], [MenuId])
    );
END
GO

-- 6. UserRoles Table (user-to-role mapping)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserRoles')
BEGIN
    CREATE TABLE [dbo].[UserRoles] (
        [Id]          INT      NOT NULL PRIMARY KEY,
        [UserId]      INT      NOT NULL,
        [RoleId]      INT      NOT NULL,
        [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
        [CreatedBy]   INT      NOT NULL DEFAULT 0,
        CONSTRAINT [FK_UserRoles_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_UserRoles_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([Id]),
        CONSTRAINT [UQ_UserRoles_UserRole] UNIQUE ([UserId], [RoleId])
    );
END
GO


-- ============================================
-- SCRIPT 2: Create View (includes Company column for filtering)
-- ============================================

IF EXISTS (SELECT * FROM sys.views WHERE name = 'VW_UserMenus')
    DROP VIEW [dbo].[VW_UserMenus];
GO

CREATE VIEW [dbo].[VW_UserMenus]
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
    rm.CanDelete
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
    um.CanDelete
FROM [dbo].[UserMenus] um
INNER JOIN [dbo].[Menus] mn ON um.MenuId = mn.Id AND mn.IsActive = 1
INNER JOIN [dbo].[Modules] m ON mn.ModuleId = m.Id AND m.IsActive = 1
WHERE um.CanView = 1;
GO


-- ============================================
-- SCRIPT 3: Clear existing seed data (order matters - child tables first)
-- ============================================

DELETE FROM [dbo].[UserMenus];
DELETE FROM [dbo].[UserRoles];
DELETE FROM [dbo].[RoleMenus];
DELETE FROM [dbo].[Menus];
DELETE FROM [dbo].[Modules];
DELETE FROM [dbo].[Roles];
GO


-- ============================================
-- SCRIPT 4: Seed Roles
-- ============================================

INSERT INTO [dbo].[Roles] ([Id], [Name], [Description], [IsActive], [CreatedDate], [CreatedBy])
VALUES
    (1, 'SuperAdmin', 'Full access to all modules, menus, and actions', 1, GETDATE(), 0),
    (2, 'Admin',      'Access to most menus, can manage users',        1, GETDATE(), 0),
    (3, 'Operator',   'Access to operational menus (orders, inventory)', 1, GETDATE(), 0),
    (4, 'Viewer',     'Read-only access to reports and data views',    1, GETDATE(), 0);
GO


-- ============================================
-- SCRIPT 4: Seed Modules
-- ============================================

INSERT INTO [dbo].[Modules] ([Id], [Name], [TranslationKey], [Icon], [SortOrder], [IsActive], [CreatedDate], [CreatedBy])
VALUES
    (1, 'Dashboard',          'module.dashboard',        'dashboard',            1, 1, GETDATE(), 0),
    (2, 'Orders',             'module.orders',           'shopping_cart',        2, 1, GETDATE(), 0),
    (3, 'Products',           'module.products',         'inventory_2',          3, 1, GETDATE(), 0),
    (4, 'Setup',              'module.setup',            'settings',             4, 1, GETDATE(), 0),
    (5, 'Logs',               'module.logs',             'description',          5, 1, GETDATE(), 0),
    (6, 'Admin',              'module.admin',            'admin_panel_settings', 6, 1, GETDATE(), 0);
GO


-- ============================================
-- SCRIPT 5: Seed Menus
-- Only ESYNCMATE client menus + shared menus
-- (GECKOTECH and SURGIMAC menus removed)
-- ============================================

-- IsHidden: 0 = visible (shown in nav + role assignment UI)
--           1 = hidden  (not shown anywhere, can be re-enabled by setting to 0)

INSERT INTO [dbo].[Menus] ([Id], [ModuleId], [Name], [TranslationKey], [Route], [Icon], [IsExternalLink], [ExternalUrl], [SortOrder], [Company], [IsHidden], [IsActive], [CreatedDate], [CreatedBy])
VALUES
    -- Dashboard (ModuleId=1)
    (1,  1, 'Dashboard',       'nav.dashboard',       'edi/dashboard',     'dashboard',    0, NULL, 1, 'ESYNCMATE', 0, 1, GETDATE(), 0),

    -- Order Management (ModuleId=2)
    (2,  2, 'Orders',          'nav.orders',          'edi/all-orders',    'list_alt',     0, NULL, 1, 'ESYNCMATE', 0, 1, GETDATE(), 0),

    -- Product Management (ModuleId=3)
    (3,  3, 'Customer Product Catalog', 'nav.customerProductCatalog', 'edi/customerProductCatalog', 'category',      0, NULL, 1, 'ESYNCMATE', 0, 1, GETDATE(), 0),
    (4,  3, 'Product Upload Prices',    'nav.productUploadPrices',    'edi/productuploadprices',    'upload',        0, NULL, 2, 'ESYNCMATE', 0, 1, GETDATE(), 0),
    (5,  3, 'Product Prices',           'nav.productPrices',          'edi/productPrices',          'attach_money',  0, NULL, 3, 'ESYNCMATE', 0, 1, GETDATE(), 0),
    (6,  3, 'Inventory Feed Summary',   'nav.inventory',              'edi/inventory',              'inventory',     0, NULL, 4, 'ESYNCMATE', 0, 1, GETDATE(), 0),

    -- Setup (ModuleId=4) — Most setup menus hidden by default; Flows, Alerts, Customers visible
    (7,  4, 'Customers',           'nav.customers',         'edi/customers',     'people',       0, NULL, 1, 'ESYNCMATE', 0, 1, GETDATE(), 0),
    (8,  4, 'Connectors',          'nav.connectors',        'edi/connectors',    'power',        0, NULL, 2, 'ESYNCMATE', 1, 1, GETDATE(), 0),
    (9,  4, 'Maps',                'nav.maps',              'edi/maps',          'map',          0, NULL, 3, 'ESYNCMATE', 1, 1, GETDATE(), 0),
    (10, 4, 'Partner Groups',      'nav.partnerGroups',     'edi/partnergroups', 'group_work',   0, NULL, 4, 'ESYNCMATE', 1, 1, GETDATE(), 0),
    (11, 4, 'Routes',              'nav.routes',            'edi/routes',        'alt_route',    0, NULL, 5, 'ESYNCMATE', 1, 1, GETDATE(), 0),
    (12, 4, 'Route Types',         'nav.routeTypes',        'edi/routeTypes',    'merge_type',   0, NULL, 6, 'ESYNCMATE', 1, 1, GETDATE(), 0),
    (13, 4, 'Flows',               'nav.flows',             'edi/flows',         'account_tree', 0, NULL, 7, 'ESYNCMATE', 0, 1, GETDATE(), 0),
    (14, 4, 'Hangfire Dashboard',  'nav.hangfireDashboard', 'hangfire/dashboard','dashboard',    1, NULL, 8, 'ESYNCMATE', 1, 1, GETDATE(), 0),
    (15, 4, 'Alerts',              'nav.alertConfiguration', 'edi/alertConfiguration', 'notifications_active', 0, NULL, 8, 'ESYNCMATE', 0, 1, GETDATE(), 0),

    -- Exceptions (ModuleId=5)
    (16, 5, 'Exceptions', 'nav.routeExceptions', 'edi/routeExceptions', 'warning', 0, NULL, 1, 'ESYNCMATE', 0, 1, GETDATE(), 0),

    -- Admin (ModuleId=6)
    (17, 6, 'User Management',  'nav.userManagement',  'edi/users',           'manage_accounts', 0, NULL, 1, 'ESYNCMATE', 0, 1, GETDATE(), 0),
    (18, 6, 'Role Management',  'nav.roleManagement',  'edi/roles',           'security',        0, NULL, 2, 'ESYNCMATE', 0, 1, GETDATE(), 0);
GO


-- ============================================
-- SCRIPT 6: Seed RoleMenus (with manual Ids)
-- SuperAdmin (1) -> All menus, full CRUD
-- Admin (2) -> All except Role Management
-- Operator (3) -> Operational menus, no admin/setup
-- Viewer (4) -> View-only operational
--
-- NOTE: Company filtering happens at query time,
-- so all roles get all menus assigned - the backend
-- filters by Company when building the menu tree.
-- ============================================

DECLARE @RMId INT = 1;

-- SuperAdmin: all menus, full permissions
INSERT INTO [dbo].[RoleMenus] ([Id], [RoleId], [MenuId], [CanView], [CanAdd], [CanEdit], [CanDelete], [CreatedDate], [CreatedBy])
SELECT @RMId + ROW_NUMBER() OVER (ORDER BY Id) - 1, 1, Id, 1, 1, 1, 1, GETDATE(), 0 FROM [dbo].[Menus];

SELECT @RMId = MAX(Id) + 1 FROM [dbo].[RoleMenus];

-- Admin: all menus except Role Management (Id=18)
INSERT INTO [dbo].[RoleMenus] ([Id], [RoleId], [MenuId], [CanView], [CanAdd], [CanEdit], [CanDelete], [CreatedDate], [CreatedBy])
SELECT @RMId + ROW_NUMBER() OVER (ORDER BY Id) - 1, 2, Id, 1, 1, 1, 1, GETDATE(), 0 FROM [dbo].[Menus] WHERE Id NOT IN (18);

SELECT @RMId = MAX(Id) + 1 FROM [dbo].[RoleMenus];

-- Operator: operational menus only (Dashboard, Orders, Products, Exceptions — no Setup, no Admin)
INSERT INTO [dbo].[RoleMenus] ([Id], [RoleId], [MenuId], [CanView], [CanAdd], [CanEdit], [CanDelete], [CreatedDate], [CreatedBy])
SELECT @RMId + ROW_NUMBER() OVER (ORDER BY Id) - 1, 3, Id, 1, 1, 1, 0, GETDATE(), 0 FROM [dbo].[Menus] WHERE ModuleId IN (1,2,3,5,7);

SELECT @RMId = MAX(Id) + 1 FROM [dbo].[RoleMenus];

-- Viewer: view-only access to Dashboard, Orders, Products
INSERT INTO [dbo].[RoleMenus] ([Id], [RoleId], [MenuId], [CanView], [CanAdd], [CanEdit], [CanDelete], [CreatedDate], [CreatedBy])
SELECT @RMId + ROW_NUMBER() OVER (ORDER BY Id) - 1, 4, Id, 1, 0, 0, 0, GETDATE(), 0 FROM [dbo].[Menus] WHERE ModuleId IN (1,2,3);
GO


-- ============================================
-- SCRIPT 7: Migrate Existing Users to UserRoles
-- ADMIN -> SuperAdmin (1)
-- WRITER -> Operator (3)
-- READER -> Viewer (4)
-- ============================================

DECLARE @URId INT = 1;

INSERT INTO [dbo].[UserRoles] ([Id], [UserId], [RoleId], [CreatedDate], [CreatedBy])
SELECT
    @URId + ROW_NUMBER() OVER (ORDER BY u.Id) - 1,
    u.Id,
    CASE
        WHEN UPPER(u.UserType) = 'ADMIN'  THEN 1
        WHEN UPPER(u.UserType) = 'WRITER' THEN 3
        WHEN UPPER(u.UserType) = 'READER' THEN 4
        ELSE 4
    END,
    GETDATE(),
    0
FROM [dbo].[Users] u
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[UserRoles] ur WHERE ur.UserId = u.Id
);
GO


-- ============================================
-- SCRIPT 8: Alter VW_Users to include RoleName
-- This view is used by the Users grid to display
-- the role name alongside user data.
-- ============================================

IF EXISTS (SELECT * FROM sys.views WHERE name = 'VW_Users')
    DROP VIEW [dbo].[VW_Users];
GO

CREATE VIEW [dbo].[VW_Users] AS
 SELECT
      U.[Id], U.[FirstName], U.[LastName], U.[Email], U.[Mobile], U.[Password],
      U.[Status], U.[CreatedDate], U.[CreatedBy], U.[UserType], U.[Company],
      U.[CustomerName], U.[IsSetupAllowed], U.[USERID],
      ISNULL(R.[Name], '') AS RoleName
  FROM Users U WITH (NOLOCK)
  LEFT JOIN UserRoles UR WITH (NOLOCK) ON U.Id = UR.UserId
  LEFT JOIN Roles R WITH (NOLOCK) ON UR.RoleId = R.Id
  WHERE U.[Status] != 'DELETED'
GO


-- ============================================
-- SCRIPT 9: Add IsHidden column to existing Menus table
-- Run this if Menus table already exists without IsHidden
-- ============================================

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Menus') AND name = 'IsHidden')
BEGIN
    ALTER TABLE [dbo].[Menus] ADD [IsHidden] BIT NOT NULL DEFAULT 0;
END
GO

-- Set hidden flag on specific menus (Connectors, Maps, Partner Groups, Routes, Route Types, Hangfire Dashboard)
UPDATE [dbo].[Menus] SET [IsHidden] = 1 WHERE [Route] IN ('edi/connectors', 'edi/maps', 'edi/partnergroups', 'edi/routes', 'edi/routeTypes', 'hangfire/dashboard');
GO

-- Move Alert Configuration from Alerts module to Setup module (if Alerts module exists)
UPDATE [dbo].[Menus] SET [ModuleId] = (SELECT TOP 1 Id FROM [dbo].[Modules] WHERE [Name] = 'Setup')
WHERE [Route] = 'edi/alertConfiguration' AND [ModuleId] = (SELECT TOP 1 Id FROM [dbo].[Modules] WHERE [Name] = 'Alerts');
GO

-- Deactivate Alerts module (no longer needed)
UPDATE [dbo].[Modules] SET [IsActive] = 0 WHERE [Name] = 'Alerts';
GO


-- ============================================
-- SCRIPT 10: Grant hidden menus to specific users
--
-- Hidden menus (IsHidden=1) are invisible in roles.
-- To show them to a specific user, insert into UserMenus:
--
-- Example: Grant Connectors + Routes to user with Id=1
-- ============================================

 DECLARE @UMId INT = (SELECT ISNULL(MAX(Id), 0) + 1 FROM [dbo].[UserMenus]);
 INSERT INTO [dbo].[UserMenus] ([Id], [UserId], [MenuId], [CanView], [CanAdd], [CanEdit], [CanDelete], [CreatedDate], [CreatedBy])
 VALUES
   (@UMId,     1, 8,  1, 1, 1, 1, GETDATE(), 0),  -- Connectors (MenuId=8)
   (@UMId + 1, 1, 9,  1, 1, 1, 1, GETDATE(), 0),  -- Maps (MenuId=9)
   (@UMId + 2, 1, 10, 1, 1, 1, 1, GETDATE(), 0),  -- Partner Groups (MenuId=10)
   (@UMId + 3, 1, 11, 1, 1, 1, 1, GETDATE(), 0),  -- Routes (MenuId=11)
   (@UMId + 4, 1, 12, 1, 1, 1, 1, GETDATE(), 0),  -- Route Types (MenuId=12)
   (@UMId + 5, 1, 14, 1, 1, 1, 1, GETDATE(), 0);  -- Hangfire Dashboard (MenuId=14)
--
-- To revoke: DELETE FROM [dbo].[UserMenus] WHERE UserId = 1 AND MenuId = 8;


--SELECT * FROM UserMenus


GO
ALTER VIEW [dbo].[VW_BatchWiseInventory] AS
SELECT  DISTINCT INV.*, [Data].BatchID, [Data].Id
FROM SCSInventoryFeed INV WITH (NOLOCK)
	INNER JOIN SCSInventoryFeedData [Data] WITH (NOLOCK) ON INV.CustomerID = [Data].CustomerId AND INV.ItemId = [Data].ItemId
GO

