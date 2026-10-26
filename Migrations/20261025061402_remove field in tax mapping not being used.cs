using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class removefieldintaxmappingnotbeingused : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "TaxMappingRequests");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "TaxMappingRequests");

            migrationBuilder.AddColumn<int>(
                name: "TransactionStatusId",
                table: "UserRoleSettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DivisionId",
                table: "UserAccounts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowRoleId",
                table: "UserAccounts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkflowRoleName",
                table: "UserAccounts",
                type: "nvarchar(max)",
                nullable: true);
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
