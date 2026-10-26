using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCastBuildingFieldsToTaxDeclaration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdministratorAddress",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdministratorContactNo",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdministratorName",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdministratorTin",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssessmentBasis",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BackTaxesYearFrom",
                table: "TaxDeclarations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BackTaxesYearTo",
                table: "TaxDeclarations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseRcnValue",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BuildingAdditionalItemsJson",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BuildingDepreciatedMarketValue",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BuildingDepreciationAccumulated",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BuildingDepreciationPercent",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BuildingFloorBreakdownJson",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuildingFloorPlanSketchPath",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BuildingTotalAdditionsValue",
                table: "TaxDeclarations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CctDateRegistered",
                table: "TaxDeclarations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CctTitleNo",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateConstructed",
                table: "TaxDeclarations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAdministrator",
                table: "TaxDeclarations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCct",
                table: "TaxDeclarations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OwnerNameReflectedInCct",
                table: "TaxDeclarations",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdministratorAddress",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "AdministratorContactNo",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "AdministratorName",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "AdministratorTin",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "AssessmentBasis",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BackTaxesYearFrom",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BackTaxesYearTo",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BaseRcnValue",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BuildingAdditionalItemsJson",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BuildingDepreciatedMarketValue",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BuildingDepreciationAccumulated",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BuildingDepreciationPercent",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BuildingFloorBreakdownJson",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BuildingFloorPlanSketchPath",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "BuildingTotalAdditionsValue",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "CctDateRegistered",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "CctTitleNo",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "DateConstructed",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "IsAdministrator",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "IsCct",
                table: "TaxDeclarations");

            migrationBuilder.DropColumn(
                name: "OwnerNameReflectedInCct",
                table: "TaxDeclarations");
        }
    }
}
