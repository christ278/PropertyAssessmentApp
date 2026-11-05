using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerIdToTaxDeclarationsAndRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- 1. TaxDeclarations
IF COL_LENGTH(N'[dbo].[TaxDeclarations]', 'OwnerId') IS NULL
BEGIN
    ALTER TABLE [dbo].[TaxDeclarations] ADD [OwnerId] UNIQUEIDENTIFIER NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TaxDeclarations_OwnerId' AND object_id = OBJECT_ID(N'[dbo].[TaxDeclarations]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TaxDeclarations_OwnerId] ON [dbo].[TaxDeclarations] ([OwnerId]);
END

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TaxDeclarations_TaxpayerOwners_OwnerId' AND parent_object_id = OBJECT_ID(N'[dbo].[TaxDeclarations]'))
BEGIN
    ALTER TABLE [dbo].[TaxDeclarations] WITH CHECK ADD CONSTRAINT [FK_TaxDeclarations_TaxpayerOwners_OwnerId]
        FOREIGN KEY ([OwnerId]) REFERENCES [dbo].[TaxpayerOwners] ([Id]) ON DELETE SET NULL;
END

-- 2. PropertyAssessmentRequests
IF COL_LENGTH(N'[dbo].[PropertyAssessmentRequests]', 'OwnerId') IS NULL
BEGIN
    ALTER TABLE [dbo].[PropertyAssessmentRequests] ADD [OwnerId] UNIQUEIDENTIFIER NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PropertyAssessmentRequests_OwnerId' AND object_id = OBJECT_ID(N'[dbo].[PropertyAssessmentRequests]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PropertyAssessmentRequests_OwnerId] ON [dbo].[PropertyAssessmentRequests] ([OwnerId]);
END

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PropertyAssessmentRequests_TaxpayerOwners_OwnerId' AND parent_object_id = OBJECT_ID(N'[dbo].[PropertyAssessmentRequests]'))
BEGIN
    ALTER TABLE [dbo].[PropertyAssessmentRequests] WITH CHECK ADD CONSTRAINT [FK_PropertyAssessmentRequests_TaxpayerOwners_OwnerId]
        FOREIGN KEY ([OwnerId]) REFERENCES [dbo].[TaxpayerOwners] ([Id]) ON DELETE SET NULL;
END

-- 3. RequestForInspections
IF COL_LENGTH(N'[dbo].[RequestForInspections]', 'OwnerId') IS NULL
BEGIN
    ALTER TABLE [dbo].[RequestForInspections] ADD [OwnerId] UNIQUEIDENTIFIER NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RequestForInspections_OwnerId' AND object_id = OBJECT_ID(N'[dbo].[RequestForInspections]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_RequestForInspections_OwnerId] ON [dbo].[RequestForInspections] ([OwnerId]);
END

-- 4. TaxMappingRequests
IF COL_LENGTH(N'[dbo].[TaxMappingRequests]', 'OwnerId') IS NULL
BEGIN
    ALTER TABLE [dbo].[TaxMappingRequests] ADD [OwnerId] UNIQUEIDENTIFIER NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TaxMappingRequests_OwnerId' AND object_id = OBJECT_ID(N'[dbo].[TaxMappingRequests]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TaxMappingRequests_OwnerId] ON [dbo].[TaxMappingRequests] ([OwnerId]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TaxDeclarations_TaxpayerOwners_OwnerId' AND parent_object_id = OBJECT_ID(N'[dbo].[TaxDeclarations]'))
    ALTER TABLE [dbo].[TaxDeclarations] DROP CONSTRAINT [FK_TaxDeclarations_TaxpayerOwners_OwnerId];

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TaxDeclarations_OwnerId' AND object_id = OBJECT_ID(N'[dbo].[TaxDeclarations]'))
    DROP INDEX [IX_TaxDeclarations_OwnerId] ON [dbo].[TaxDeclarations];

IF COL_LENGTH(N'[dbo].[TaxDeclarations]', 'OwnerId') IS NOT NULL
    ALTER TABLE [dbo].[TaxDeclarations] DROP COLUMN [OwnerId];
");
        }
    }
}
