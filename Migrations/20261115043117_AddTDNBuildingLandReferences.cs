using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class AddTDNBuildingLandReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LandReferencesJson",
                table: "TDNBuildings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TDNBuildingLandReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TDNBuildingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LandTaxDeclarationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LandTdNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LandPin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LandOwnerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LotNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TDNBuildingLandReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TDNBuildingLandReferences_TDNBuildings_TDNBuildingId",
                        column: x => x.TDNBuildingId,
                        principalTable: "TDNBuildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TDNBuildingLandReferences_TaxDeclarations_LandTaxDeclarationId",
                        column: x => x.LandTaxDeclarationId,
                        principalTable: "TaxDeclarations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TDNBuildingLandReferences_LandTaxDeclarationId",
                table: "TDNBuildingLandReferences",
                column: "LandTaxDeclarationId");

            migrationBuilder.CreateIndex(
                name: "IX_TDNBuildingLandReferences_TDNBuildingId",
                table: "TDNBuildingLandReferences",
                column: "TDNBuildingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TDNBuildingLandReferences");

            migrationBuilder.DropColumn(
                name: "LandReferencesJson",
                table: "TDNBuildings");
        }
    }
}
