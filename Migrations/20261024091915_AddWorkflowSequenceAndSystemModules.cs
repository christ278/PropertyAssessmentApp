using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowSequenceAndSystemModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SystemModule')
BEGIN
    CREATE TABLE [dbo].[SystemModule] (
        [ModuleId] INT IDENTITY(1,1) NOT NULL,
        [ModuleCode] NVARCHAR(50) NOT NULL,
        [ModuleName] NVARCHAR(100) NOT NULL,
        [Category] NVARCHAR(50) NULL,
        [ControllerName] NVARCHAR(50) NULL,
        [ActionName] NVARCHAR(50) NULL,
        [Icon] NVARCHAR(50) NULL,
        [SortOrder] INT NOT NULL DEFAULT 0,
        [IsActive] BIT NOT NULL DEFAULT 1,
        CONSTRAINT [PK_SystemModule] PRIMARY KEY CLUSTERED ([ModuleId] ASC)
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TransactionStatus')
BEGIN
    CREATE TABLE [dbo].[TransactionStatus] (
        [TransactionStatusId] INT NOT NULL,
        [Name] NVARCHAR(150) NOT NULL,
        [IsComplete] BIT NOT NULL DEFAULT 0,
        [DivisionId] INT NOT NULL DEFAULT 1,
        CONSTRAINT [PK_TransactionStatus] PRIMARY KEY CLUSTERED ([TransactionStatusId] ASC)
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WorkflowSequence')
BEGIN
    CREATE TABLE [dbo].[WorkflowSequence] (
        [SequenceId] INT IDENTITY(1,1) NOT NULL,
        [TransactionTypeId] INT NOT NULL,
        [CurrentStatusId] INT NOT NULL,
        [NextStatusId] INT NULL,
        [IsApproval] BIT NOT NULL DEFAULT 0,
        CONSTRAINT [PK_WorkflowSequence] PRIMARY KEY CLUSTERED ([SequenceId] ASC)
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RoleModulePermission')
BEGIN
    CREATE TABLE [dbo].[RoleModulePermission] (
        [PermissionId] INT IDENTITY(1,1) NOT NULL,
        [RoleId] UNIQUEIDENTIFIER NOT NULL,
        [ModuleId] INT NOT NULL,
        [CanView] BIT NOT NULL DEFAULT 0,
        [CanCreate] BIT NOT NULL DEFAULT 0,
        [CanEdit] BIT NOT NULL DEFAULT 0,
        [CanDelete] BIT NOT NULL DEFAULT 0,
        [CanApprove] BIT NOT NULL DEFAULT 0,
        [UpdatedBy] NVARCHAR(MAX) NULL,
        [UpdatedDate] DATETIME2 NULL,
        CONSTRAINT [PK_RoleModulePermission] PRIMARY KEY CLUSTERED ([PermissionId] ASC),
        CONSTRAINT [FK_RoleModulePermission_SystemModule_ModuleId] FOREIGN KEY ([ModuleId]) REFERENCES [dbo].[SystemModule] ([ModuleId]) ON DELETE CASCADE
    );
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleModulePermission");

            migrationBuilder.DropTable(
                name: "TransactionStatus");

            migrationBuilder.DropTable(
                name: "WorkflowSequence");

            migrationBuilder.DropTable(
                name: "SystemModule");
        }
    }
}
