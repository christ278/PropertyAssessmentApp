using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSmsSystemModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemModule] WHERE [ModuleCode] = 'SMS_NOTIFICATIONS')
BEGIN
    INSERT INTO [dbo].[SystemModule] ([ModuleCode], [ModuleName], [Category], [ControllerName], [ActionName], [Icon], [SortOrder], [IsActive])
    VALUES ('SMS_NOTIFICATIONS', 'SMS Delivery Logs & PhilSMS Gateway', 'Communication & Master Records', 'Sms', 'Index', 'message-square', 22, 1);
END

-- Grant default permission to System Administrator and City Assessor Head
DECLARE @SmsModuleId INT;
SELECT TOP 1 @SmsModuleId = [ModuleId] FROM [dbo].[SystemModule] WHERE [ModuleCode] = 'SMS_NOTIFICATIONS';

IF @SmsModuleId IS NOT NULL
BEGIN
    INSERT INTO [dbo].[RoleModulePermission] ([RoleId], [ModuleId], [CanView], [CanCreate], [CanEdit], [CanDelete], [CanApprove], [UpdatedDate])
    SELECT r.[Id], @SmsModuleId, 1, 1, 1, 1, 1, GETDATE()
    FROM [dbo].[UserRoleSettings] r
    WHERE (r.[RoleName] = 'System Administrator' OR r.[RoleName] = 'City Assessor Head')
      AND NOT EXISTS (
          SELECT 1 FROM [dbo].[RoleModulePermission] rmp
          WHERE rmp.[RoleId] = r.[Id] AND rmp.[ModuleId] = @SmsModuleId
      );
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @SmsModuleId INT;
SELECT TOP 1 @SmsModuleId = [ModuleId] FROM [dbo].[SystemModule] WHERE [ModuleCode] = 'SMS_NOTIFICATIONS';
IF @SmsModuleId IS NOT NULL
BEGIN
    DELETE FROM [dbo].[RoleModulePermission] WHERE [ModuleId] = @SmsModuleId;
    DELETE FROM [dbo].[SystemModule] WHERE [ModuleId] = @SmsModuleId;
END
");
        }
    }
}
