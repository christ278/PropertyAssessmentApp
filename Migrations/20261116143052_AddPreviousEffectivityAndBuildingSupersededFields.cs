using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPreviousEffectivityAndBuildingSupersededFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreviousEffectivity",
                table: "TDNMachineries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousEffectivity",
                table: "TDNLands",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PreviousAssessedValue",
                table: "TDNBuildings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PreviousEffectivity",
                table: "TDNBuildings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PreviousMarketValue",
                table: "TDNBuildings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PreviousOwner",
                table: "TDNBuildings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousPin",
                table: "TDNBuildings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousTdNo",
                table: "TDNBuildings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrevAssessedValue",
                table: "PropertyAssessmentRequests",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PrevEffectivity",
                table: "PropertyAssessmentRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrevMarketValue",
                table: "PropertyAssessmentRequests",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreviousEffectivity",
                table: "TDNMachineries");

            migrationBuilder.DropColumn(
                name: "PreviousEffectivity",
                table: "TDNLands");

            migrationBuilder.DropColumn(
                name: "PreviousAssessedValue",
                table: "TDNBuildings");

            migrationBuilder.DropColumn(
                name: "PreviousEffectivity",
                table: "TDNBuildings");

            migrationBuilder.DropColumn(
                name: "PreviousMarketValue",
                table: "TDNBuildings");

            migrationBuilder.DropColumn(
                name: "PreviousOwner",
                table: "TDNBuildings");

            migrationBuilder.DropColumn(
                name: "PreviousPin",
                table: "TDNBuildings");

            migrationBuilder.DropColumn(
                name: "PreviousTdNo",
                table: "TDNBuildings");

            migrationBuilder.DropColumn(
                name: "PrevAssessedValue",
                table: "PropertyAssessmentRequests");

            migrationBuilder.DropColumn(
                name: "PrevEffectivity",
                table: "PropertyAssessmentRequests");

            migrationBuilder.DropColumn(
                name: "PrevMarketValue",
                table: "PropertyAssessmentRequests");
        }
    }
}
