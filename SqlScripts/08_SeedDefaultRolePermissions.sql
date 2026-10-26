-- =========================================================================
-- SCRIPT: 08_SeedDefaultRolePermissions.sql
-- DESCRIPTION: Sets up default module permissions in [dbo].[RoleModulePermission]:
--              1. Automatically ensures [dbo].[SystemModule] and [dbo].[UserRoleSettings]
--                 contain all required baseline modules and roles.
--              2. By default, EVERY active role has access to:
--                 - Pool Transactions (POOL_TRANSACTIONS)
--                 - My Transactions (MY_TRANSACTIONS)
--              3. Division 1 Receiving role gets:
--                 - RPA: Property Assessment Requests (PROPERTY_ASSESSMENT)
--                 - RFI: Field Inspection Requests (INSPECTION)
--              4. Division 2 Receiving role gets:
--                 - Tax Mapping (TAX_MAPPING)
--              5. Division 3 Receiving role gets:
--                 - Tax Declaration (TAX_DECLARATION)
--                 - Land Holding (LAND_HOLDINGS)
--              6. System Administrator gets all active system modules.
-- DATABASE: FairTrackDB
-- =========================================================================
-- USE []; -- Run against active connected database
GO

SET NOCOUNT ON;

PRINT '=========================================================================';
PRINT 'Starting Default Role & Module Permissions Seeding';
PRINT '=========================================================================';

-- -----------------------------------------------------------------------------
-- Pre-step A: Ensure SystemModule Table & Core Modules Exist
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'SystemModule')
BEGIN
    CREATE TABLE [dbo].[SystemModule] (
        [ModuleId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ModuleCode] NVARCHAR(50) NOT NULL UNIQUE,
        [ModuleName] NVARCHAR(100) NOT NULL,
        [Category] NVARCHAR(50) NULL,
        [ControllerName] NVARCHAR(50) NULL,
        [ActionName] NVARCHAR(50) NULL,
        [Icon] NVARCHAR(50) NULL,
        [SortOrder] INT NOT NULL DEFAULT 0,
        [IsActive] BIT NOT NULL DEFAULT 1
    );
    PRINT 'Created table [dbo].[SystemModule].';
END
GO

-- Upsert baseline modules if not yet populated
IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemModule] WHERE [ModuleCode] IN ('POOL_TRANSACTIONS', 'MY_TRANSACTIONS'))
BEGIN
    PRINT 'System modules missing; auto-seeding baseline modules into SystemModule...';
    
    SET IDENTITY_INSERT [dbo].[SystemModule] ON;

    DECLARE @InitialModules TABLE (
        [ModuleId] INT, [ModuleCode] NVARCHAR(50), [ModuleName] NVARCHAR(100),
        [Category] NVARCHAR(50), [ControllerName] NVARCHAR(50), [ActionName] NVARCHAR(50),
        [Icon] NVARCHAR(50), [SortOrder] INT, [IsActive] BIT
    );

    INSERT INTO @InitialModules ([ModuleId], [ModuleCode], [ModuleName], [Category], [ControllerName], [ActionName], [Icon], [SortOrder], [IsActive])
    VALUES
        (1,  'DASHBOARD',           'Executive Dashboard',                      'Main Menu',                   'Home',                     'Index',          'layout-dashboard', 1,  1),
        (17, 'POOL_TRANSACTIONS',   'Pool Transactions (Shared Queue)',         'Transaction Workflows',       'Workflow',                 'Pool',           'inbox',            1,  1),
        (2,  'ANALYTICS',           'Executive Analytics & Reports',            'Main Menu',                   'Analytics',                'Index',          'trending-up',      2,  1),
        (18, 'MY_TRANSACTIONS',     'My Transactions (Claimed Workspace)',      'Transaction Workflows',       'Workflow',                 'MyTransactions', 'user-check',       2,  1),
        (3,  'INSPECTION',          'Field Inspection Requests',                'FAAS & Transaction Requests', 'RequestForInspection',     'Index',          'clipboard-check',  10, 1),
        (4,  'PROPERTY_ASSESSMENT', 'Property Assessment Requests',             'FAAS & Transaction Requests', 'PropertyAssessmentRequest', 'Index',          'building',         11, 1),
        (5,  'ASSESSMENT_FAAS',     'Real Property Appraisal & Tax Dec (FAAS)', 'FAAS & Transaction Requests', 'Assessment',                'Index',          'file-spreadsheet', 12, 1),
        (6,  'TAX_MAPPING',         'GIS Spatial & Tax Mapping',                'GIS & Spatial Mapping',       'TaxMapping',               'Index',          'map-pin',          13, 1),
        (7,  'TAX_DECLARATION',     'Tax Declaration Certification Requests',   'FAAS & Transaction Requests', 'TaxDeclarationRequest',     'Index',          'file-text',        14, 1),
        (8,  'LAND_HOLDINGS',       'Certificate of Land Holdings',             'FAAS & Transaction Requests', 'LandHoldingRequest',       'Index',          'map',              15, 1),
        (9,  'ORDER_PAYMENT',       'Order of Payment Billing (OP)',            'Billing & Fee Collections',   'OrderPayment',             'Index',          'receipt',          20, 1),
        (10, 'OWNER_REGISTRATION',  'Taxpayer Directory & Master Records',      'Master Records',              'Owner',                    'Index',          'users',            21, 1),
        (11, 'FILE_LOCATION',       'File Storage & Network Setup',             'System Administration',       'FileLocation',             'Index',          'hard-drive',       30, 1),
        (12, 'WORKFLOW_SETTINGS',   'Workflow Pipeline & Approvals',            'System Administration',       'Workflow',                 'Settings',       'git-pull-request', 31, 1),
        (13, 'USER_MANAGEMENT',     'User Staff Accounts',                      'System Administration',       'UserAccount',              'Index',          'shield-check',     40, 1),
        (14, 'USER_ROLES',          'User Roles Directory',                     'System Administration',       'UserRole',                 'Index',          'user-cog',         41, 1),
        (15, 'ROLE_PERMISSIONS',    'Dynamic Form Permissions',                 'System Administration',       'UserRole',                 'Permissions',    'shield-alert',     42, 1),
        (16, 'DATA_TRANSFER',       'TD Data Transfer Utility',                 'File Maintenance',            'DataTransferUtility',      'Index',          'arrow-right-left', 50, 1);

    MERGE [dbo].[SystemModule] AS Target
    USING @InitialModules AS Source
    ON Target.[ModuleCode] = Source.[ModuleCode]
    WHEN MATCHED THEN
        UPDATE SET Target.[ModuleName] = Source.[ModuleName], Target.[ControllerName] = Source.[ControllerName], Target.[ActionName] = Source.[ActionName]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([ModuleId], [ModuleCode], [ModuleName], [Category], [ControllerName], [ActionName], [Icon], [SortOrder], [IsActive])
        VALUES (Source.[ModuleId], Source.[ModuleCode], Source.[ModuleName], Source.[Category], Source.[ControllerName], Source.[ActionName], Source.[Icon], Source.[SortOrder], Source.[IsActive]);

    SET IDENTITY_INSERT [dbo].[SystemModule] OFF;
    PRINT 'Baseline SystemModule records inserted successfully.';
END
GO

-- -----------------------------------------------------------------------------
-- Pre-step B: Ensure UserRoleSettings Table & Baseline Roles Exist
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'UserRoleSettings')
BEGIN
    CREATE TABLE [dbo].[UserRoleSettings] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [RoleName] NVARCHAR(150) NOT NULL,
        [TransactionStatusId] INT NULL,
        [Division] INT NULL,
        [Description] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [UpdatedDate] DATETIME2 NOT NULL DEFAULT GETDATE()
    );
    PRINT 'Created table [dbo].[UserRoleSettings].';
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRoleSettings] WHERE [RoleName] = 'System Administrator')
BEGIN
    INSERT INTO [dbo].[UserRoleSettings] ([Id], [RoleName], [TransactionStatusId], [Division], [Description], [IsActive], [CreatedDate], [UpdatedDate])
    VALUES ('50147D2F-24FE-4FA9-B270-9F217F58D333', 'System Administrator', NULL, NULL, 'Full administrative access, user accounts, and system configuration.', 1, GETDATE(), GETDATE());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRoleSettings] WHERE [Division] = 1 AND [TransactionStatusId] = 1)
BEGIN
    INSERT INTO [dbo].[UserRoleSettings] ([Id], [RoleName], [TransactionStatusId], [Division], [Description], [IsActive], [CreatedDate], [UpdatedDate])
    VALUES ('5266D996-43CD-490F-95FE-CED504C44AB1', 'Receiving', 1, 1, 'Document intake, requirements checking, and transaction tracking initiation.', 1, GETDATE(), GETDATE());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRoleSettings] WHERE [Division] = 2 AND [TransactionStatusId] = 15)
BEGIN
    INSERT INTO [dbo].[UserRoleSettings] ([Id], [RoleName], [TransactionStatusId], [Division], [Description], [IsActive], [CreatedDate], [UpdatedDate])
    VALUES ('F8122558-287D-4309-8BB8-ED5563E52CEE', 'Receiving (GIS)', 15, 2, 'GIS mapping request intake, survey plan verification, and section indexing.', 1, GETDATE(), GETDATE());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRoleSettings] WHERE [Division] = 3 AND [TransactionStatusId] = 25)
BEGIN
    INSERT INTO [dbo].[UserRoleSettings] ([Id], [RoleName], [TransactionStatusId], [Division], [Description], [IsActive], [CreatedDate], [UpdatedDate])
    VALUES ('E91ECBD7-EDB5-4C31-BB91-8A8830A5B12C', 'Receiving (Records)', 25, 3, 'Intake for Land Holding and Tax Declaration certificates and requests.', 1, GETDATE(), GETDATE());
END
GO

-- -----------------------------------------------------------------------------
-- Core Step: Apply Role Module Permissions
-- -----------------------------------------------------------------------------
BEGIN TRANSACTION;

BEGIN TRY
    -- 1. Resolve Module IDs dynamically from [dbo].[SystemModule]
    DECLARE @PoolModuleId INT,
            @MyTransModuleId INT,
            @InspectionModuleId INT,
            @PropAssessmentModuleId INT,
            @TaxMappingModuleId INT,
            @TaxDecModuleId INT,
            @LandHoldingModuleId INT;

    SELECT @PoolModuleId = ModuleId FROM [dbo].[SystemModule] WHERE ModuleCode = 'POOL_TRANSACTIONS';
    SELECT @MyTransModuleId = ModuleId FROM [dbo].[SystemModule] WHERE ModuleCode = 'MY_TRANSACTIONS';
    SELECT @InspectionModuleId = ModuleId FROM [dbo].[SystemModule] WHERE ModuleCode = 'INSPECTION';
    SELECT @PropAssessmentModuleId = ModuleId FROM [dbo].[SystemModule] WHERE ModuleCode = 'PROPERTY_ASSESSMENT';
    SELECT @TaxMappingModuleId = ModuleId FROM [dbo].[SystemModule] WHERE ModuleCode = 'TAX_MAPPING';
    SELECT @TaxDecModuleId = ModuleId FROM [dbo].[SystemModule] WHERE ModuleCode = 'TAX_DECLARATION';
    SELECT @LandHoldingModuleId = ModuleId FROM [dbo].[SystemModule] WHERE ModuleCode = 'LAND_HOLDINGS';

    PRINT 'Resolved target System Modules successfully.';

    -- 2. Resolve Receiving Role IDs dynamically from [dbo].[UserRoleSettings]
    DECLARE @Div1ReceivingRoleId UNIQUEIDENTIFIER,
            @Div2ReceivingRoleId UNIQUEIDENTIFIER,
            @Div3ReceivingRoleId UNIQUEIDENTIFIER,
            @AdminRoleId UNIQUEIDENTIFIER;

    -- Division 1 Receiving (TransactionStatusId = 1 / Receiving)
    SELECT TOP 1 @Div1ReceivingRoleId = Id 
    FROM [dbo].[UserRoleSettings] 
    WHERE Division = 1 AND (TransactionStatusId = 1 OR RoleName = 'Receiving');

    -- Division 2 Receiving (TransactionStatusId = 15 / Receiving (GIS))
    SELECT TOP 1 @Div2ReceivingRoleId = Id 
    FROM [dbo].[UserRoleSettings] 
    WHERE Division = 2 AND (TransactionStatusId = 15 OR RoleName LIKE '%Receiving%');

    -- Division 3 Receiving (TransactionStatusId = 25 / Receiving (Records))
    SELECT TOP 1 @Div3ReceivingRoleId = Id 
    FROM [dbo].[UserRoleSettings] 
    WHERE Division = 3 AND (TransactionStatusId = 25 OR RoleName LIKE '%Receiving%');

    -- System Administrator
    SELECT TOP 1 @AdminRoleId = Id 
    FROM [dbo].[UserRoleSettings] 
    WHERE RoleName = 'System Administrator' OR Id = '50147D2F-24FE-4FA9-B270-9F217F58D333';

    PRINT 'Resolved Receiving and Admin role IDs successfully.';

    -- 3. Reset non-admin permissions to clean baseline
    PRINT 'Cleaning existing permissions for non-admin roles...';
    
    DELETE FROM [dbo].[RoleModulePermission]
    WHERE RoleId <> @AdminRoleId;

    -- 4. Grant EVERY Role default access to POOL_TRANSACTIONS & MY_TRANSACTIONS
    PRINT 'Granting Pool Transactions & My Transactions to ALL active roles...';

    IF @PoolModuleId IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[RoleModulePermission] (RoleId, ModuleId, CanView, CanCreate, CanEdit, CanDelete, CanApprove, UpdatedBy, UpdatedDate)
        SELECT Id, @PoolModuleId, 1, 1, 1, 1, 1, 'DefaultRoleAccessScript', GETDATE()
        FROM [dbo].[UserRoleSettings]
        WHERE IsActive = 1 AND Id <> @AdminRoleId;
    END

    IF @MyTransModuleId IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[RoleModulePermission] (RoleId, ModuleId, CanView, CanCreate, CanEdit, CanDelete, CanApprove, UpdatedBy, UpdatedDate)
        SELECT Id, @MyTransModuleId, 1, 1, 1, 1, 1, 'DefaultRoleAccessScript', GETDATE()
        FROM [dbo].[UserRoleSettings]
        WHERE IsActive = 1 AND Id <> @AdminRoleId;
    END

    PRINT 'Default Pool & My Transactions access granted to all roles.';

    -- 5. Division 1 Receiving: Additional access to RPA (PROPERTY_ASSESSMENT) and RFI (INSPECTION)
    IF @Div1ReceivingRoleId IS NOT NULL
    BEGIN
        PRINT 'Configuring Division 1 Receiving additional permissions (RPA & RFI)...';

        IF @PropAssessmentModuleId IS NOT NULL
        BEGIN
            INSERT INTO [dbo].[RoleModulePermission] (RoleId, ModuleId, CanView, CanCreate, CanEdit, CanDelete, CanApprove, UpdatedBy, UpdatedDate)
            VALUES (@Div1ReceivingRoleId, @PropAssessmentModuleId, 1, 1, 1, 1, 1, 'DefaultRoleAccessScript', GETDATE());
        END

        IF @InspectionModuleId IS NOT NULL
        BEGIN
            INSERT INTO [dbo].[RoleModulePermission] (RoleId, ModuleId, CanView, CanCreate, CanEdit, CanDelete, CanApprove, UpdatedBy, UpdatedDate)
            VALUES (@Div1ReceivingRoleId, @InspectionModuleId, 1, 1, 1, 1, 1, 'DefaultRoleAccessScript', GETDATE());
        END

        PRINT 'Division 1 Receiving permissions configured.';
    END

    -- 6. Division 2 Receiving: Additional access to Tax Mapping (TAX_MAPPING)
    IF @Div2ReceivingRoleId IS NOT NULL AND @TaxMappingModuleId IS NOT NULL
    BEGIN
        PRINT 'Configuring Division 2 Receiving additional permissions (Tax Mapping)...';

        INSERT INTO [dbo].[RoleModulePermission] (RoleId, ModuleId, CanView, CanCreate, CanEdit, CanDelete, CanApprove, UpdatedBy, UpdatedDate)
        VALUES (@Div2ReceivingRoleId, @TaxMappingModuleId, 1, 1, 1, 1, 1, 'DefaultRoleAccessScript', GETDATE());

        PRINT 'Division 2 Receiving permissions configured.';
    END

    -- 7. Division 3 Receiving: Additional access to Tax Dec (TAX_DECLARATION) and Land Holding (LAND_HOLDINGS)
    IF @Div3ReceivingRoleId IS NOT NULL
    BEGIN
        PRINT 'Configuring Division 3 Receiving additional permissions (Tax Dec & Land Holding)...';

        IF @TaxDecModuleId IS NOT NULL
        BEGIN
            INSERT INTO [dbo].[RoleModulePermission] (RoleId, ModuleId, CanView, CanCreate, CanEdit, CanDelete, CanApprove, UpdatedBy, UpdatedDate)
            VALUES (@Div3ReceivingRoleId, @TaxDecModuleId, 1, 1, 1, 1, 1, 'DefaultRoleAccessScript', GETDATE());
        END

        IF @LandHoldingModuleId IS NOT NULL
        BEGIN
            INSERT INTO [dbo].[RoleModulePermission] (RoleId, ModuleId, CanView, CanCreate, CanEdit, CanDelete, CanApprove, UpdatedBy, UpdatedDate)
            VALUES (@Div3ReceivingRoleId, @LandHoldingModuleId, 1, 1, 1, 1, 1, 'DefaultRoleAccessScript', GETDATE());
        END

        PRINT 'Division 3 Receiving permissions configured.';
    END

    -- 8. Ensure System Administrator has access to all active system modules
    IF @AdminRoleId IS NOT NULL
    BEGIN
        PRINT 'Configuring System Administrator full access to all active modules...';

        MERGE [dbo].[RoleModulePermission] AS target
        USING (
            SELECT @AdminRoleId AS RoleId, ModuleId
            FROM [dbo].[SystemModule]
            WHERE IsActive = 1
        ) AS source
        ON target.RoleId = source.RoleId AND target.ModuleId = source.ModuleId
        WHEN MATCHED THEN
            UPDATE SET CanView = 1, CanCreate = 1, CanEdit = 1, CanDelete = 1, CanApprove = 1,
                       UpdatedBy = 'DefaultRoleAccessScript', UpdatedDate = GETDATE()
        WHEN NOT MATCHED THEN
            INSERT (RoleId, ModuleId, CanView, CanCreate, CanEdit, CanDelete, CanApprove, UpdatedBy, UpdatedDate)
            VALUES (source.RoleId, source.ModuleId, 1, 1, 1, 1, 1, 'DefaultRoleAccessScript', GETDATE());

        PRINT 'System Administrator permissions configured.';
    END

    COMMIT TRANSACTION;
    PRINT '=========================================================================';
    PRINT 'Default role permissions applied successfully.';
    PRINT '=========================================================================';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END

    PRINT 'ERROR during script execution: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
GO

-- -----------------------------------------------------------------------------
-- Verification Query
-- -----------------------------------------------------------------------------
SELECT 
    ISNULL(CAST(r.Division AS NVARCHAR(10)), 'ADMIN') AS [Div],
    r.RoleName AS [Role Name],
    m.ModuleCode AS [Module Code],
    m.ModuleName AS [Module Title],
    p.CanView,
    p.CanCreate,
    p.CanEdit,
    p.CanDelete,
    p.CanApprove
FROM [dbo].[RoleModulePermission] p
INNER JOIN [dbo].[UserRoleSettings] r ON p.RoleId = r.Id
INNER JOIN [dbo].[SystemModule] m ON p.ModuleId = m.ModuleId
WHERE r.RoleName LIKE '%Receiv%' OR r.RoleName = 'System Administrator'
ORDER BY r.Division, r.RoleName, m.SortOrder;
GO
