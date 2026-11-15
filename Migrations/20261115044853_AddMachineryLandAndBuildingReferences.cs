using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class AddMachineryLandAndBuildingReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BuildingTaxDeclarationId",
                table: "TDNMachineries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LandTaxDeclarationId",
                table: "TDNMachineries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TDNMachineries_BuildingTaxDeclarationId",
                table: "TDNMachineries",
                column: "BuildingTaxDeclarationId");

            migrationBuilder.CreateIndex(
                name: "IX_TDNMachineries_LandTaxDeclarationId",
                table: "TDNMachineries",
                column: "LandTaxDeclarationId");

            migrationBuilder.AddForeignKey(
                name: "FK_TDNMachineries_TaxDeclarations_BuildingTaxDeclarationId",
                table: "TDNMachineries",
                column: "BuildingTaxDeclarationId",
                principalTable: "TaxDeclarations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TDNMachineries_TaxDeclarations_LandTaxDeclarationId",
                table: "TDNMachineries",
                column: "LandTaxDeclarationId",
                principalTable: "TaxDeclarations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TDNMachineries_TaxDeclarations_BuildingTaxDeclarationId",
                table: "TDNMachineries");

            migrationBuilder.DropForeignKey(
                name: "FK_TDNMachineries_TaxDeclarations_LandTaxDeclarationId",
                table: "TDNMachineries");

            migrationBuilder.DropIndex(
                name: "IX_TDNMachineries_BuildingTaxDeclarationId",
                table: "TDNMachineries");

            migrationBuilder.DropIndex(
                name: "IX_TDNMachineries_LandTaxDeclarationId",
                table: "TDNMachineries");

            migrationBuilder.DropColumn(
                name: "BuildingTaxDeclarationId",
                table: "TDNMachineries");

            migrationBuilder.DropColumn(
                name: "LandTaxDeclarationId",
                table: "TDNMachineries");
        }
    }
}
