-- =============================================================================
-- CITY ASSESSOR REAL PROPERTY TAX & FAAS ASSESSMENT MANAGEMENT SYSTEM
-- SCRIPT: 12_SeedDefaultModules.sql
-- DESCRIPTION: Seeds default system modules into [dbo].[SystemModule].
--              Ensures table existence, applies idempotent upsert (MERGE) 
--              with deterministic ModuleId values, and preserves sorting/routing.
-- DATABASE: FairTrackDB
-- =============================================================================
-- USE []; -- Run against active connected database
GO

SET NOCOUNT ON;

PRINT '=========================================================================';
PRINT 'Starting Default System Modules Seeding...';
PRINT '=========================================================================';

-- 1. Ensure SystemModule table exists
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

-- 2. Upsert default system modules using MERGE
BEGIN TRANSACTION;

BEGIN TRY
    -- Prepare source staging data
    DECLARE @SourceModules TABLE (
        [ModuleId] INT,
        [ModuleCode] NVARCHAR(50),
        [ModuleName] NVARCHAR(100),
        [Category] NVARCHAR(50),
        [ControllerName] NVARCHAR(50),
        [ActionName] NVARCHAR(50),
        [Icon] NVARCHAR(50),
        [SortOrder] INT,
        [IsActive] BIT
    );

    INSERT INTO @SourceModules ([ModuleId], [ModuleCode], [ModuleName], [Category], [ControllerName], [ActionName], [Icon], [SortOrder], [IsActive])
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

    -- Enable IDENTITY_INSERT so predetermined ModuleId values are retained
    SET IDENTITY_INSERT [dbo].[SystemModule] ON;

    MERGE [dbo].[SystemModule] AS Target
    USING @SourceModules AS Source
    ON Target.[ModuleCode] = Source.[ModuleCode]
    WHEN MATCHED THEN
        UPDATE SET 
            Target.[ModuleName]     = Source.[ModuleName],
            Target.[Category]       = Source.[Category],
            Target.[ControllerName] = Source.[ControllerName],
            Target.[ActionName]     = Source.[ActionName],
            Target.[Icon]           = Source.[Icon],
            Target.[SortOrder]      = Source.[SortOrder],
            Target.[IsActive]       = Source.[IsActive]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([ModuleId], [ModuleCode], [ModuleName], [Category], [ControllerName], [ActionName], [Icon], [SortOrder], [IsActive])
        VALUES (Source.[ModuleId], Source.[ModuleCode], Source.[ModuleName], Source.[Category], Source.[ControllerName], Source.[ActionName], Source.[Icon], Source.[SortOrder], Source.[IsActive]);

    SET IDENTITY_INSERT [dbo].[SystemModule] OFF;

    COMMIT TRANSACTION;
    PRINT 'System modules seeded successfully.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    IF OBJECTPROPERTY(OBJECT_ID('[dbo].[SystemModule]'), 'TableHasIdentity') = 1
        SET IDENTITY_INSERT [dbo].[SystemModule] OFF;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;
GO

-- 3. Verification output
PRINT '-------------------------------------------------------------------------';
PRINT 'Verification: Current System Modules:';
PRINT '-------------------------------------------------------------------------';
SELECT [ModuleId], [ModuleCode], [ModuleName], [Category], [ControllerName], [ActionName], [Icon], [SortOrder], [IsActive]
FROM [dbo].[SystemModule]
ORDER BY [SortOrder], [ModuleId];
GO
