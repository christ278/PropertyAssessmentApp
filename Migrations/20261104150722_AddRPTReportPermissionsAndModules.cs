using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class AddRPTReportPermissionsAndModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- 1. Insert REPORT_DIRECTORY
IF NOT EXISTS (SELECT 1 FROM [SystemModule] WHERE [ModuleCode] = 'REPORT_DIRECTORY')
BEGIN
    INSERT INTO [SystemModule] ([ModuleCode], [ModuleName], [Category], [ControllerName], [ActionName], [Icon], [SortOrder], [IsActive])
    VALUES ('REPORT_DIRECTORY', 'RPT Reports & Certifications Directory', 'Reports & Certifications', 'RPTReport', 'Index', 'file-text', 30, 1);
END

-- 2. Insert the 7 RPT Report modules matching RPTReports
IF NOT EXISTS (SELECT 1 FROM [SystemModule] WHERE [ModuleCode] = 'REPORT_1')
BEGIN
    INSERT INTO [SystemModule] ([ModuleCode], [ModuleName], [Category], [ControllerName], [ActionName], [Icon], [SortOrder], [IsActive])
    VALUES ('REPORT_1', 'Tax Decleration Recods Certification', 'Reports & Certifications', 'RPTReport', 'ViewReport/1', 'file-check', 31, 1);
END

IF NOT EXISTS (SELECT 1 FROM [SystemModule] WHERE [ModuleCode] = 'REPORT_2')
BEGIN
    INSERT INTO [SystemModule] ([ModuleCode], [ModuleName], [Category], [ControllerName], [ActionName], [Icon], [SortOrder], [IsActive])
    VALUES ('REPORT_2', 'Certificate of NO Improvement', 'Reports & Certifications', 'RPTReport', 'ViewReport/2', 'file-minus', 32, 1);
END

IF NOT EXISTS (SELECT 1 FROM [SystemModule] WHERE [ModuleCode] = 'REPORT_3')
BEGIN
    INSERT INTO [SystemModule] ([ModuleCode], [ModuleName], [Category], [ControllerName], [ActionName], [Icon], [SortOrder], [IsActive])
    VALUES ('REPORT_3', 'Assessment of Tax Declaration', 'Reports & Certifications', 'RPTReport', 'ViewReport/3', 'calculator', 33, 1);
END

IF NOT EXISTS (SELECT 1 FROM [SystemModule] WHERE [ModuleCode] = 'REPORT_4')
BEGIN
    INSERT INTO [SystemModule] ([ModuleCode], [ModuleName], [Category], [ControllerName], [ActionName], [Icon], [SortOrder], [IsActive])
    VALUES ('REPORT_4', 'Certificate of Improvement', 'Reports & Certifications', 'RPTReport', 'ViewReport/4', 'building', 34, 1);
END

IF NOT EXISTS (SELECT 1 FROM [SystemModule] WHERE [ModuleCode] = 'REPORT_5')
BEGIN
    INSERT INTO [SystemModule] ([ModuleCode], [ModuleName], [Category], [ControllerName], [ActionName], [Icon], [SortOrder], [IsActive])
    VALUES ('REPORT_5', 'Certificate of no land holding', 'Reports & Certifications', 'RPTReport', 'ViewReport/5', 'file-x', 35, 1);
END

IF NOT EXISTS (SELECT 1 FROM [SystemModule] WHERE [ModuleCode] = 'REPORT_6')
BEGIN
    INSERT INTO [SystemModule] ([ModuleCode], [ModuleName], [Category], [ControllerName], [ActionName], [Icon], [SortOrder], [IsActive])
    VALUES ('REPORT_6', 'All property Holding(BIR Certification)', 'Reports & Certifications', 'RPTReport', 'ViewReport/6', 'landmark', 36, 1);
END

IF NOT EXISTS (SELECT 1 FROM [SystemModule] WHERE [ModuleCode] = 'REPORT_7')
BEGIN
    INSERT INTO [SystemModule] ([ModuleCode], [ModuleName], [Category], [ControllerName], [ActionName], [Icon], [SortOrder], [IsActive])
    VALUES ('REPORT_7', 'Agricultural Lands (DAR Certification)', 'Reports & Certifications', 'RPTReport', 'ViewReport/7', 'trees', 37, 1);
END

-- 3. Grant default full permissions for System Administrator role
DECLARE @AdminRoleId UNIQUEIDENTIFIER;
SELECT TOP 1 @AdminRoleId = [Id] FROM [UserRoleSettings] WHERE [RoleName] = 'System Administrator';

IF @AdminRoleId IS NOT NULL
BEGIN
    INSERT INTO [RoleModulePermission] ([RoleId], [ModuleId], [CanView], [CanCreate], [CanEdit], [CanDelete], [CanApprove], [UpdatedDate])
    SELECT @AdminRoleId, sm.[ModuleId], 1, 1, 1, 1, 1, GETDATE()
    FROM [SystemModule] sm
    WHERE sm.[Category] = 'Reports & Certifications'
      AND NOT EXISTS (
          SELECT 1 FROM [RoleModulePermission] rmp
          WHERE rmp.[RoleId] = @AdminRoleId AND rmp.[ModuleId] = sm.[ModuleId]
      );
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM [RoleModulePermission] 
WHERE [ModuleId] IN (SELECT [ModuleId] FROM [SystemModule] WHERE [Category] = 'Reports & Certifications');

DELETE FROM [SystemModule] WHERE [Category] = 'Reports & Certifications';
");
        }
    }
}
