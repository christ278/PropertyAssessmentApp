using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class AddExecutiveAnalyticsRolePermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "OwnerEmail",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.Sql(@"
-- 1. Ensure DASHBOARD_ANALYTICS and ASSESSMENT_PRINT_TD exist in [dbo].[SystemModule]
IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemModule] WHERE [ModuleCode] = 'DASHBOARD_ANALYTICS')
BEGIN
    INSERT INTO [dbo].[SystemModule] ([ModuleCode], [ModuleName], [Category], [ControllerName], [ActionName], [Icon], [SortOrder], [IsActive])
    VALUES ('DASHBOARD_ANALYTICS', 'Executive Assessment Analytics', 'Dashboards', 'Dashboard', 'Analytics', 'bar-chart-3', 4, 1);
END
ELSE
BEGIN
    UPDATE [dbo].[SystemModule]
    SET [ModuleName] = 'Executive Assessment Analytics',
        [Category] = 'Dashboards',
        [ControllerName] = 'Dashboard',
        [ActionName] = 'Analytics',
        [Icon] = 'bar-chart-3',
        [SortOrder] = 4,
        [IsActive] = 1
    WHERE [ModuleCode] = 'DASHBOARD_ANALYTICS';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemModule] WHERE [ModuleCode] = 'ASSESSMENT_PRINT_TD')
BEGIN
    INSERT INTO [dbo].[SystemModule] ([ModuleCode], [ModuleName], [Category], [ControllerName], [ActionName], [Icon], [SortOrder], [IsActive])
    VALUES ('ASSESSMENT_PRINT_TD', 'Print Tax Declaration', 'FAAS & Transaction Requests', 'Assessment', 'PrintTaxDeclaration', 'file-check-2', 14, 1);
END

-- Deactivate obsolete ANALYTICS module code so only DASHBOARD_ANALYTICS appears in Role Permissions
UPDATE [dbo].[SystemModule]
SET [IsActive] = 0
WHERE [ModuleCode] = 'ANALYTICS';

-- 2. Ensure System Administrator role has full access
DECLARE @AdminRoleId UNIQUEIDENTIFIER;
SELECT TOP 1 @AdminRoleId = [Id] FROM [dbo].[UserRoleSettings] WHERE [RoleName] = 'System Administrator';

IF @AdminRoleId IS NOT NULL
BEGIN
    DECLARE @AnalyticsModId INT;
    SELECT TOP 1 @AnalyticsModId = [ModuleId] FROM [dbo].[SystemModule] WHERE [ModuleCode] = 'DASHBOARD_ANALYTICS';

    IF @AnalyticsModId IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM [dbo].[RoleModulePermission] WHERE [RoleId] = @AdminRoleId AND [ModuleId] = @AnalyticsModId
    )
    BEGIN
        INSERT INTO [dbo].[RoleModulePermission] ([RoleId], [ModuleId], [CanView], [CanCreate], [CanEdit], [CanDelete], [CanApprove], [UpdatedDate])
        VALUES (@AdminRoleId, @AnalyticsModId, 1, 1, 1, 1, 1, GETDATE());
    END

    DECLARE @PrintTdModId INT;
    SELECT TOP 1 @PrintTdModId = [ModuleId] FROM [dbo].[SystemModule] WHERE [ModuleCode] = 'ASSESSMENT_PRINT_TD';

    IF @PrintTdModId IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM [dbo].[RoleModulePermission] WHERE [RoleId] = @AdminRoleId AND [ModuleId] = @PrintTdModId
    )
    BEGIN
        INSERT INTO [dbo].[RoleModulePermission] ([RoleId], [ModuleId], [CanView], [CanCreate], [CanEdit], [CanDelete], [CanApprove], [UpdatedDate])
        VALUES (@AdminRoleId, @PrintTdModId, 1, 1, 1, 1, 1, GETDATE());
    END
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "OwnerEmail",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
