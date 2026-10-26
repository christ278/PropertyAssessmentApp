using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPartitionColumnsAndDataTransferLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF COL_LENGTH('PropertyAssessmentRequests', 'GeneratedPartitionsJson') IS NULL
                BEGIN
                    ALTER TABLE [PropertyAssessmentRequests] ADD [GeneratedPartitionsJson] nvarchar(max) NULL;
                END;

                IF COL_LENGTH('PropertyAssessmentRequests', 'SelectedRefPropertyIdsJson') IS NULL
                BEGIN
                    ALTER TABLE [PropertyAssessmentRequests] ADD [SelectedRefPropertyIdsJson] nvarchar(max) NULL;
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DataTransferLogs')
                BEGIN
                    CREATE TABLE [DataTransferLogs] (
                        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                        [TaxDeclarationNumber] nvarchar(100) NOT NULL,
                        [PropertyIndexNumber] nvarchar(100) NOT NULL,
                        [OwnerName] nvarchar(250) NOT NULL,
                        [PropertyKind] nvarchar(50) NOT NULL,
                        [MarketValue] decimal(18,2) NOT NULL,
                        [AssessedValue] decimal(18,2) NOT NULL,
                        [MigratedBy] nvarchar(150) NOT NULL,
                        [MigratedByRole] nvarchar(100) NULL,
                        [MigratedAt] datetime2 NOT NULL,
                        [Status] nvarchar(50) NOT NULL,
                        [Remarks] nvarchar(max) NULL
                    );
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataTransferLogs");

            migrationBuilder.DropColumn(
                name: "GeneratedPartitionsJson",
                table: "PropertyAssessmentRequests");

            migrationBuilder.DropColumn(
                name: "SelectedRefPropertyIdsJson",
                table: "PropertyAssessmentRequests");
        }
    }
}
