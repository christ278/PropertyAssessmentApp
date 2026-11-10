using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class EnsureAllSystemModulesAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- 1. Ensure all baseline system modules exist in [dbo].[SystemModule]
DECLARE @Modules TABLE (
    [ModuleCode] NVARCHAR(50),
    [ModuleName] NVARCHAR(100),
    [Category] NVARCHAR(50),
    [ControllerName] NVARCHAR(50),
    [ActionName] NVARCHAR(50),
    [Icon] NVARCHAR(50),
    [SortOrder] INT,
    [IsActive] BIT
);

INSERT INTO @Modules ([ModuleCode], [ModuleName], [Category], [ControllerName], [ActionName], [Icon], [SortOrder], [IsActive])
VALUES
    ('DASHBOARD_USER',      'Staff Dashboard (Ordinary Users)',         'Dashboards',                  'Dashboard',                'Staff',          'layout-dashboard', 1,  1),
    ('DASHBOARD_HEAD',      'Assessor Head Dashboard',                  'Dashboards',                  'Dashboard',                'AssessorHead',   'gauge',            2,  1),
    ('DASHBOARD_ADMIN',     'System Owner Dashboard (Super Admin)',     'Dashboards',                  'Dashboard',                'SystemOwner',    'server-cog',       3,  1),
    ('ANALYTICS',           'Executive Analytics & Reports',            'Main Menu',                   'Analytics',                'Index',          'trending-up',      4,  1),
    ('POOL_TRANSACTIONS',   'Pool Transactions (Shared Queue)',         'Transaction Workflows',       'Workflow',                 'Pool',           'inbox',            5,  1),
    ('MY_TRANSACTIONS',     'My Transactions (Claimed Workspace)',      'Transaction Workflows',       'Workflow',                 'MyTransactions', 'user-check',       6,  1),
    ('INSPECTION',          'Field Inspection Requests',                'FAAS & Transaction Requests', 'RequestForInspection',     'Index',          'clipboard-check',  10, 1),
    ('PROPERTY_ASSESSMENT', 'Property Assessment Requests',             'FAAS & Transaction Requests', 'PropertyAssessmentRequest', 'Index',          'building',         11, 1),
    ('ASSESSMENT_FAAS',     'Real Property Appraisal & Tax Dec (FAAS)', 'FAAS & Transaction Requests', 'Assessment',                'Index',          'file-spreadsheet', 12, 1),
    ('ASSESSMENT_PRINT',    'Print Tax Faas (Records Printout)', 'FAAS & Transaction Requests', 'Assessment',                'PrintPdf',       'printer',          13, 1),
    ('TAX_MAPPING',         'GIS Spatial & Tax Mapping',                'GIS & Spatial Mapping',       'TaxMapping',               'Index',          'map-pin',          14, 1),
    ('TAX_DECLARATION',     'Tax Declaration Certification Requests',   'FAAS & Transaction Requests', 'TaxDeclarationRequest',     'Index',          'file-text',        15, 1),
    ('LAND_HOLDINGS',       'Certificate of Land Holdings',             'FAAS & Transaction Requests', 'LandHoldingRequest',       'Index',          'map',              16, 1),
    ('ORDER_PAYMENT',       'Order of Payment Billing (OP)',            'Billing & Fee Collections',   'OrderPayment',             'Index',          'receipt',          20, 1),
    ('OWNER_REGISTRATION',  'Taxpayer Directory & Master Records',      'Master Records',              'Owner',                    'Index',          'users',            21, 1),
    ('DATA_TRANSFER',       'TD Data Transfer Utility',                 'File Maintenance',            'DataTransferUtility',      'Index',          'arrow-right-left', 25, 1),
    ('FILE_LOCATION',       'File Storage & Network Setup',             'System Administration',       'FileLocation',             'Index',          'hard-drive',       30, 1),
    ('WORKFLOW_SETTINGS',   'Workflow Pipeline & Approvals',            'System Administration',       'Workflow',                 'Settings',       'git-pull-request', 31, 1),
    ('USER_MANAGEMENT',     'User Staff Accounts',                      'System Administration',       'UserAccount',              'Index',          'shield-check',     40, 1),
    ('USER_ROLES',          'User Roles Directory',                     'System Administration',       'UserRole',                 'Index',          'user-cog',         41, 1),
    ('ROLE_PERMISSIONS',    'Dynamic Form Permissions',                 'System Administration',       'UserRole',                 'Permissions',    'shield-alert',     42, 1),
    ('REPORT_DIRECTORY',    'RPT Reports & Certifications Directory',   'Reports & Certifications',    'RPTReport',                'Index',          'file-text',        50, 1),
    ('REPORT_1',            'Tax Decleration Recods Certification',     'Reports & Certifications',    'RPTReport',                'ViewReport/1',   'file-check',       51, 1),
    ('REPORT_2',            'Certificate of NO Improvement',            'Reports & Certifications',    'RPTReport',                'ViewReport/2',   'file-minus',       52, 1),
    ('REPORT_3',            'Assessment of Tax Declaration',            'Reports & Certifications',    'RPTReport',                'ViewReport/3',   'calculator',       53, 1),
    ('REPORT_4',            'Certificate of Improvement',               'Reports & Certifications',    'RPTReport',                'ViewReport/4',   'building',         54, 1),
    ('REPORT_5',            'Certificate of no land holding',           'Reports & Certifications',    'RPTReport',                'ViewReport/5',   'file-x',           55, 1),
    ('REPORT_6',            'All property Holding(BIR Certification)',  'Reports & Certifications',    'RPTReport',                'ViewReport/6',   'landmark',         56, 1),
    ('REPORT_7',            'Agricultural Lands (DAR Certification)',   'Reports & Certifications',    'RPTReport',                'ViewReport/7',   'trees',            57, 1);

MERGE [dbo].[SystemModule] AS Target
USING @Modules AS Source
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
    INSERT ([ModuleCode], [ModuleName], [Category], [ControllerName], [ActionName], [Icon], [SortOrder], [IsActive])
    VALUES (Source.[ModuleCode], Source.[ModuleName], Source.[Category], Source.[ControllerName], Source.[ActionName], Source.[Icon], Source.[SortOrder], Source.[IsActive]);

-- 2. Grant default full permissions to the System Administrator role for all active modules
DECLARE @AdminRoleId UNIQUEIDENTIFIER;
SELECT TOP 1 @AdminRoleId = [Id] FROM [dbo].[UserRoleSettings] WHERE [RoleName] = 'System Administrator';

IF @AdminRoleId IS NOT NULL
BEGIN
    INSERT INTO [dbo].[RoleModulePermission] ([RoleId], [ModuleId], [CanView], [CanCreate], [CanEdit], [CanDelete], [CanApprove], [UpdatedDate])
    SELECT @AdminRoleId, sm.[ModuleId], 1, 1, 1, 1, 1, GETDATE()
    FROM [dbo].[SystemModule] sm
    WHERE sm.[IsActive] = 1
      AND NOT EXISTS (
          SELECT 1 FROM [dbo].[RoleModulePermission] rmp
          WHERE rmp.[RoleId] = @AdminRoleId AND rmp.[ModuleId] = sm.[ModuleId]
      );
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Non-destructive rollback: only remove modules added if explicitly required
        }
    }
}
