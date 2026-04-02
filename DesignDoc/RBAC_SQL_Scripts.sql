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

-- 5. UserRoles Table (user-to-role mapping)
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
INNER JOIN [dbo].[Menus] mn ON rm.MenuId = mn.Id AND mn.IsActive = 1
INNER JOIN [dbo].[Modules] m ON mn.ModuleId = m.Id AND m.IsActive = 1;
GO


-- ============================================
-- SCRIPT 3: Clear existing seed data (order matters - child tables first)
-- ============================================

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
    (2, 'Order Management',   'module.orderManagement',  'shopping_cart',        2, 1, GETDATE(), 0),
    (3, 'Product Management', 'module.productManagement','inventory_2',          3, 1, GETDATE(), 0),
    (4, 'Setup',              'module.setup',            'settings',             4, 1, GETDATE(), 0),
    (5, 'Exceptions',         'module.exceptions',       'error',                5, 1, GETDATE(), 0),
    (6, 'Admin',              'module.admin',            'admin_panel_settings', 6, 1, GETDATE(), 0),
    (7, 'Alerts',             'module.alerts',           'notifications',        7, 1, GETDATE(), 0);
GO


-- ============================================
-- SCRIPT 5: Seed Menus
-- Only ESYNCMATE client menus + shared menus
-- (GECKOTECH and SURGIMAC menus removed)
-- ============================================

INSERT INTO [dbo].[Menus] ([Id], [ModuleId], [Name], [TranslationKey], [Route], [Icon], [IsExternalLink], [ExternalUrl], [SortOrder], [Company], [IsActive], [CreatedDate], [CreatedBy])
VALUES
    -- Dashboard (ModuleId=1)
    (1,  1, 'Dashboard',       'nav.dashboard',       'edi/dashboard',     'dashboard',    0, NULL, 1, 'ESYNCMATE', 1, GETDATE(), 0),

    -- Order Management (ModuleId=2)
    (2,  2, 'Orders',          'nav.orders',          'edi/all-orders',    'list_alt',     0, NULL, 1, 'ESYNCMATE', 1, GETDATE(), 0),
    (3,  2, 'Customers',       'nav.customers',       'edi/customers',     'people',       0, NULL, 2, 'ESYNCMATE', 1, GETDATE(), 0),

    -- Product Management (ModuleId=3)
    (4,  3, 'Customer Product Catalog', 'nav.customerProductCatalog', 'edi/customerProductCatalog', 'category',      0, NULL, 1, 'ESYNCMATE', 1, GETDATE(), 0),
    (5,  3, 'Product Upload Prices',    'nav.productUploadPrices',    'edi/productuploadprices',    'upload',        0, NULL, 2, 'ESYNCMATE', 1, GETDATE(), 0),
    (6,  3, 'Product Prices',           'nav.productPrices',          'edi/productPrices',          'attach_money',  0, NULL, 3, 'ESYNCMATE', 1, GETDATE(), 0),
    (7,  3, 'Inventory',                'nav.inventory',              'edi/inventory',              'inventory',     0, NULL, 4, 'ESYNCMATE', 1, GETDATE(), 0),

    -- Setup (ModuleId=4)
    (8,  4, 'Connectors',          'nav.connectors',        'edi/connectors',    'power',        0, NULL, 1, 'ESYNCMATE', 1, GETDATE(), 0),
    (9,  4, 'Maps',                'nav.maps',              'edi/maps',          'map',          0, NULL, 2, 'ESYNCMATE', 1, GETDATE(), 0),
    (10, 4, 'Partner Groups',      'nav.partnerGroups',     'edi/partnergroups', 'group_work',   0, NULL, 3, 'ESYNCMATE', 1, GETDATE(), 0),
    (11, 4, 'Routes',              'nav.routes',            'edi/routes',        'alt_route',    0, NULL, 4, 'ESYNCMATE', 1, GETDATE(), 0),
    (12, 4, 'Route Types',         'nav.routeTypes',        'edi/routeTypes',    'merge_type',   0, NULL, 5, 'ESYNCMATE', 1, GETDATE(), 0),
    (13, 4, 'Flows',               'nav.flows',             'edi/flows',         'account_tree', 0, NULL, 6, 'ESYNCMATE', 1, GETDATE(), 0),
    (14, 4, 'Hangfire Dashboard',  'nav.hangfireDashboard', 'hangfire/dashboard','dashboard',    1, NULL, 7, 'ESYNCMATE', 1, GETDATE(), 0),

    -- Exceptions (ModuleId=5)
    (15, 5, 'Route Exceptions', 'nav.routeExceptions', 'edi/routeExceptions', 'warning', 0, NULL, 1, 'ESYNCMATE', 1, GETDATE(), 0),

    -- Admin (ModuleId=6)
    (16, 6, 'User Management',  'nav.userManagement',  'edi/users',           'manage_accounts', 0, NULL, 1, 'ESYNCMATE', 1, GETDATE(), 0),
    (17, 6, 'Role Management',  'nav.roleManagement',  'edi/roles',           'security',        0, NULL, 2, 'ESYNCMATE', 1, GETDATE(), 0),

    -- Alerts (ModuleId=7)
    (18, 7, 'Alert Configuration', 'nav.alertConfiguration', 'edi/alertConfiguration', 'notifications_active', 0, NULL, 1, 'ESYNCMATE', 1, GETDATE(), 0);
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

-- Admin: all menus except Role Management (Id=17)
INSERT INTO [dbo].[RoleMenus] ([Id], [RoleId], [MenuId], [CanView], [CanAdd], [CanEdit], [CanDelete], [CreatedDate], [CreatedBy])
SELECT @RMId + ROW_NUMBER() OVER (ORDER BY Id) - 1, 2, Id, 1, 1, 1, 1, GETDATE(), 0 FROM [dbo].[Menus] WHERE Id NOT IN (17);

SELECT @RMId = MAX(Id) + 1 FROM [dbo].[RoleMenus];

-- Operator: operational menus only (Dashboard, Orders, Products, Exceptions, Alerts — no Setup, no Admin)
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
