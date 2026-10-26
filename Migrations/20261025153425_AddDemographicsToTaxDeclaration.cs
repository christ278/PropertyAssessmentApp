using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class AddDemographicsToTaxDeclaration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdministratorCivilStatus",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdministratorEmail",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdministratorGender",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerCivilStatus",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdministratorCivilStatus",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "AdministratorEmail",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "AdministratorGender",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "OwnerCivilStatus",
                table: "TaxDeclarations");
        }
    }
}
