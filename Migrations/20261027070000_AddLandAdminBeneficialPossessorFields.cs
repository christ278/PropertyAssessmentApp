using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PropertyAssessmentApp.Data;

#nullable disable

namespace PropertyAssessmentApp.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261027070000_AddLandAdminBeneficialPossessorFields")]
    public partial class AddLandAdminBeneficialPossessorFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add Administrator, Beneficial User, Possessor flags to TaxDeclarations
            migrationBuilder.AddColumn<bool>(
                name: "IsAdministrator",
                table: "TaxDeclarations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsBeneficialUser",
                table: "TaxDeclarations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPossessor",
                table: "TaxDeclarations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // 2. Add Administrator, Beneficial User, Possessor flags & demographics to TDNLands
            migrationBuilder.AddColumn<bool>(
                name: "IsAdministrator",
                table: "TDNLands",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsBeneficialUser",
                table: "TDNLands",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPossessor",
                table: "TDNLands",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AdministratorName",
                table: "TDNLands",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdministratorSex",
                table: "TDNLands",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdministratorAddress",
                table: "TDNLands",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdministratorTin",
                table: "TDNLands",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdministratorPhone",
                table: "TDNLands",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdministratorEmail",
                table: "TDNLands",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsAdministrator", table: "TaxDeclarations");
            migrationBuilder.DropColumn(name: "IsBeneficialUser", table: "TaxDeclarations");
            migrationBuilder.DropColumn(name: "IsPossessor", table: "TaxDeclarations");

            migrationBuilder.DropColumn(name: "IsAdministrator", table: "TDNLands");
            migrationBuilder.DropColumn(name: "IsBeneficialUser", table: "TDNLands");
            migrationBuilder.DropColumn(name: "IsPossessor", table: "TDNLands");
            migrationBuilder.DropColumn(name: "AdministratorName", table: "TDNLands");
            migrationBuilder.DropColumn(name: "AdministratorSex", table: "TDNLands");
            migrationBuilder.DropColumn(name: "AdministratorAddress", table: "TDNLands");
            migrationBuilder.DropColumn(name: "AdministratorTin", table: "TDNLands");
            migrationBuilder.DropColumn(name: "AdministratorPhone", table: "TDNLands");
            migrationBuilder.DropColumn(name: "AdministratorEmail", table: "TDNLands");
        }
    }
}
