using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTaxMappingPurposeAndRemarks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF COL_LENGTH('TaxMappingRequests', 'Purpose') IS NOT NULL
                BEGIN
                    ALTER TABLE [TaxMappingRequests] DROP COLUMN [Purpose];
                END;

                IF COL_LENGTH('TaxMappingRequests', 'Remarks') IS NOT NULL
                BEGIN
                    ALTER TABLE [TaxMappingRequests] DROP COLUMN [Remarks];
                END;

                IF COL_LENGTH('UserRoleSettings', 'TransactionStatusId') IS NULL
                BEGIN
                    ALTER TABLE [UserRoleSettings] ADD [TransactionStatusId] int NULL;
                END;

                IF COL_LENGTH('UserAccounts', 'DivisionId') IS NULL
                BEGIN
                    ALTER TABLE [UserAccounts] ADD [DivisionId] int NULL;
                END;

                IF COL_LENGTH('UserAccounts', 'WorkflowRoleId') IS NULL
                BEGIN
                    ALTER TABLE [UserAccounts] ADD [WorkflowRoleId] int NULL;
                END;

                IF COL_LENGTH('UserAccounts', 'WorkflowRoleName') IS NULL
                BEGIN
                    ALTER TABLE [UserAccounts] ADD [WorkflowRoleName] nvarchar(max) NULL;
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransactionStatusId",
                table: "UserRoleSettings");

            migrationBuilder.DropColumn(
                name: "DivisionId",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "WorkflowRoleId",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "WorkflowRoleName",
                table: "UserAccounts");

            migrationBuilder.AddColumn<int>(
                name: "Purpose",
                table: "TaxMappingRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "TaxMappingRequests",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
