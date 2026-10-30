using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class AddBuildingDepreciationScheduleTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BuildingDepreciationSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GeneralRevisionCycle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BuildingAgeLabel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AgeLowerBound = table.Column<int>(type: "int", nullable: false),
                    AgeUpperBound = table.Column<int>(type: "int", nullable: false),
                    ClassificationCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StructuralType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EstimatedEconomicLifeYears = table.Column<int>(type: "int", nullable: false),
                    EstimatedResidualValuePercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DepreciationPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EffectiveDepreciationPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsResidualFloorCap = table.Column<bool>(type: "bit", nullable: false),
                    RevisionYear = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildingDepreciationSchedules", x => x.Id);
                });

            migrationBuilder.AddColumn<int>(
                name: "BuildingDepreciationScheduleId",
                table: "TDNBuildings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeneralRevisionCycle",
                table: "TDNBuildings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedEconomicLifeYears",
                table: "TDNBuildings",
                type: "int",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedResidualValuePercent",
                table: "TDNBuildings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 25m);

            migrationBuilder.InsertData(
                table: "BuildingDepreciationSchedules",
                columns: new[] { "Id", "AgeLowerBound", "AgeUpperBound", "BuildingAgeLabel", "ClassificationCode", "DepreciationPercent", "EffectiveDepreciationPercent", "EstimatedEconomicLifeYears", "EstimatedResidualValuePercent", "GeneralRevisionCycle", "IsActive", "IsResidualFloorCap", "RevisionYear", "SortOrder", "StructuralType" },
                values: new object[,]
                {
                    { 1, 1, 1, "1", "I", 6.7m, 6.7m, 15, 10m, "1st 3 yrs of GR", true, false, 2024, 1, "Type I" },
                    { 2, 1, 1, "1", "II-A", 5.0m, 5.0m, 20, 15m, "1st 3 yrs of GR", true, false, 2024, 2, "Type II" },
                    { 3, 1, 1, "1", "II-B", 5.0m, 5.0m, 18, 12m, "1st 3 yrs of GR", true, false, 2024, 3, "Type II" },
                    { 4, 1, 1, "1", "III-A", 3.3m, 3.3m, 30, 25m, "1st 3 yrs of GR", true, false, 2024, 4, "Type III" },
                    { 5, 1, 1, "1", "III-B", 4.0m, 4.0m, 25, 20m, "1st 3 yrs of GR", true, false, 2024, 5, "Type III" },
                    { 6, 1, 1, "1", "IV-A", 2.5m, 2.5m, 40, 35m, "1st 3 yrs of GR", true, false, 2024, 6, "Type IV" },
                    { 7, 1, 1, "1", "IV-B", 2.6m, 2.6m, 38, 33m, "1st 3 yrs of GR", true, false, 2024, 7, "Type IV" },
                    { 8, 1, 1, "1", "V-A", 2.0m, 2.0m, 50, 40m, "1st 3 yrs of GR", true, false, 2024, 8, "Type V" },
                    { 9, 1, 1, "1", "V-B", 2.2m, 2.2m, 45, 37m, "1st 3 yrs of GR", true, false, 2024, 9, "Type V" },
                    { 10, 2, 2, "2", "I", 13.3m, 13.3m, 15, 10m, "1st 3 yrs of GR", true, false, 2024, 10, "Type I" },
                    { 11, 2, 2, "2", "II-A", 10.0m, 10.0m, 20, 15m, "1st 3 yrs of GR", true, false, 2024, 11, "Type II" },
                    { 12, 2, 2, "2", "II-B", 11.1m, 11.1m, 18, 12m, "1st 3 yrs of GR", true, false, 2024, 12, "Type II" },
                    { 13, 2, 2, "2", "III-A", 6.7m, 6.7m, 30, 25m, "1st 3 yrs of GR", true, false, 2024, 13, "Type III" },
                    { 14, 2, 2, "2", "III-B", 8.0m, 8.0m, 25, 20m, "1st 3 yrs of GR", true, false, 2024, 14, "Type III" },
                    { 15, 2, 2, "2", "IV-A", 5.0m, 5.0m, 40, 35m, "1st 3 yrs of GR", true, false, 2024, 15, "Type IV" },
                    { 16, 2, 2, "2", "IV-B", 5.3m, 5.3m, 38, 33m, "1st 3 yrs of GR", true, false, 2024, 16, "Type IV" },
                    { 17, 2, 2, "2", "V-A", 4.0m, 4.0m, 50, 40m, "1st 3 yrs of GR", true, false, 2024, 17, "Type V" },
                    { 18, 2, 2, "2", "V-B", 4.4m, 4.4m, 45, 37m, "1st 3 yrs of GR", true, false, 2024, 18, "Type V" },
                    { 19, 3, 3, "3", "I", 20.0m, 20.0m, 15, 10m, "1st 3 yrs of GR", true, false, 2024, 19, "Type I" },
                    { 20, 3, 3, "3", "II-A", 15.0m, 15.0m, 20, 15m, "1st 3 yrs of GR", true, false, 2024, 20, "Type II" },
                    { 21, 3, 3, "3", "II-B", 16.7m, 16.7m, 18, 12m, "1st 3 yrs of GR", true, false, 2024, 21, "Type II" },
                    { 22, 3, 3, "3", "III-A", 10.0m, 10.0m, 30, 25m, "1st 3 yrs of GR", true, false, 2024, 22, "Type III" },
                    { 23, 3, 3, "3", "III-B", 12.0m, 12.0m, 25, 20m, "1st 3 yrs of GR", true, false, 2024, 23, "Type III" },
                    { 24, 3, 3, "3", "IV-A", 7.5m, 7.5m, 40, 35m, "1st 3 yrs of GR", true, false, 2024, 24, "Type IV" },
                    { 25, 3, 3, "3", "IV-B", 7.9m, 7.9m, 38, 33m, "1st 3 yrs of GR", true, false, 2024, 25, "Type IV" },
                    { 26, 3, 3, "3", "V-A", 6.0m, 6.0m, 50, 40m, "1st 3 yrs of GR", true, false, 2024, 26, "Type V" },
                    { 27, 3, 3, "3", "V-B", 6.7m, 6.7m, 45, 37m, "1st 3 yrs of GR", true, false, 2024, 27, "Type V" },
                    { 28, 4, 4, "4", "I", 26.7m, 26.7m, 15, 10m, "2nd 3 yrs of GR", true, false, 2024, 28, "Type I" },
                    { 29, 4, 4, "4", "II-A", 20.0m, 20.0m, 20, 15m, "2nd 3 yrs of GR", true, false, 2024, 29, "Type II" },
                    { 30, 4, 4, "4", "II-B", 22.2m, 22.2m, 18, 12m, "2nd 3 yrs of GR", true, false, 2024, 30, "Type II" },
                    { 31, 4, 4, "4", "III-A", 13.3m, 13.3m, 30, 25m, "2nd 3 yrs of GR", true, false, 2024, 31, "Type III" },
                    { 32, 4, 4, "4", "III-B", 16.0m, 16.0m, 25, 20m, "2nd 3 yrs of GR", true, false, 2024, 32, "Type III" },
                    { 33, 4, 4, "4", "IV-A", 10.0m, 10.0m, 40, 35m, "2nd 3 yrs of GR", true, false, 2024, 33, "Type IV" },
                    { 34, 4, 4, "4", "IV-B", 10.5m, 10.5m, 38, 33m, "2nd 3 yrs of GR", true, false, 2024, 34, "Type IV" },
                    { 35, 4, 4, "4", "V-A", 8.0m, 8.0m, 50, 40m, "2nd 3 yrs of GR", true, false, 2024, 35, "Type V" },
                    { 36, 4, 4, "4", "V-B", 8.9m, 8.9m, 45, 37m, "2nd 3 yrs of GR", true, false, 2024, 36, "Type V" },
                    { 37, 5, 5, "5", "I", 33.3m, 33.3m, 15, 10m, "2nd 3 yrs of GR", true, false, 2024, 37, "Type I" },
                    { 38, 5, 5, "5", "II-A", 25.0m, 25.0m, 20, 15m, "2nd 3 yrs of GR", true, false, 2024, 38, "Type II" },
                    { 39, 5, 5, "5", "II-B", 27.8m, 27.8m, 18, 12m, "2nd 3 yrs of GR", true, false, 2024, 39, "Type II" },
                    { 40, 5, 5, "5", "III-A", 16.7m, 16.7m, 30, 25m, "2nd 3 yrs of GR", true, false, 2024, 40, "Type III" },
                    { 41, 5, 5, "5", "III-B", 20.0m, 20.0m, 25, 20m, "2nd 3 yrs of GR", true, false, 2024, 41, "Type III" },
                    { 42, 5, 5, "5", "IV-A", 12.5m, 12.5m, 40, 35m, "2nd 3 yrs of GR", true, false, 2024, 42, "Type IV" },
                    { 43, 5, 5, "5", "IV-B", 13.2m, 13.2m, 38, 33m, "2nd 3 yrs of GR", true, false, 2024, 43, "Type IV" },
                    { 44, 5, 5, "5", "V-A", 10.0m, 10.0m, 50, 40m, "2nd 3 yrs of GR", true, false, 2024, 44, "Type V" },
                    { 45, 5, 5, "5", "V-B", 11.1m, 11.1m, 45, 37m, "2nd 3 yrs of GR", true, false, 2024, 45, "Type V" },
                    { 46, 6, 6, "6", "I", 40.0m, 40.0m, 15, 10m, "2nd 3 yrs of GR", true, false, 2024, 46, "Type I" },
                    { 47, 6, 6, "6", "II-A", 30.0m, 30.0m, 20, 15m, "2nd 3 yrs of GR", true, false, 2024, 47, "Type II" },
                    { 48, 6, 6, "6", "II-B", 33.3m, 33.3m, 18, 12m, "2nd 3 yrs of GR", true, false, 2024, 48, "Type II" },
                    { 49, 6, 6, "6", "III-A", 20.0m, 20.0m, 30, 25m, "2nd 3 yrs of GR", true, false, 2024, 49, "Type III" },
                    { 50, 6, 6, "6", "III-B", 24.0m, 24.0m, 25, 20m, "2nd 3 yrs of GR", true, false, 2024, 50, "Type III" },
                    { 51, 6, 6, "6", "IV-A", 15.0m, 15.0m, 40, 35m, "2nd 3 yrs of GR", true, false, 2024, 51, "Type IV" },
                    { 52, 6, 6, "6", "IV-B", 15.8m, 15.8m, 38, 33m, "2nd 3 yrs of GR", true, false, 2024, 52, "Type IV" },
                    { 53, 6, 6, "6", "V-A", 12.0m, 12.0m, 50, 40m, "2nd 3 yrs of GR", true, false, 2024, 53, "Type V" },
                    { 54, 6, 6, "6", "V-B", 13.3m, 13.3m, 45, 37m, "2nd 3 yrs of GR", true, false, 2024, 54, "Type V" },
                    { 55, 7, 7, "7", "I", 46.7m, 46.7m, 15, 10m, "3rd 3 yrs of GR", true, false, 2024, 55, "Type I" },
                    { 56, 7, 7, "7", "II-A", 35.0m, 35.0m, 20, 15m, "3rd 3 yrs of GR", true, false, 2024, 56, "Type II" },
                    { 57, 7, 7, "7", "II-B", 38.9m, 38.9m, 18, 12m, "3rd 3 yrs of GR", true, false, 2024, 57, "Type II" },
                    { 58, 7, 7, "7", "III-A", 23.3m, 23.3m, 30, 25m, "3rd 3 yrs of GR", true, false, 2024, 58, "Type III" },
                    { 59, 7, 7, "7", "III-B", 28.0m, 28.0m, 25, 20m, "3rd 3 yrs of GR", true, false, 2024, 59, "Type III" },
                    { 60, 7, 7, "7", "IV-A", 17.5m, 17.5m, 40, 35m, "3rd 3 yrs of GR", true, false, 2024, 60, "Type IV" },
                    { 61, 7, 7, "7", "IV-B", 18.4m, 18.4m, 38, 33m, "3rd 3 yrs of GR", true, false, 2024, 61, "Type IV" },
                    { 62, 7, 7, "7", "V-A", 14.0m, 14.0m, 50, 40m, "3rd 3 yrs of GR", true, false, 2024, 62, "Type V" },
                    { 63, 7, 7, "7", "V-B", 15.6m, 15.6m, 45, 37m, "3rd 3 yrs of GR", true, false, 2024, 63, "Type V" },
                    { 64, 8, 8, "8", "I", 53.3m, 53.3m, 15, 10m, "3rd 3 yrs of GR", true, false, 2024, 64, "Type I" },
                    { 65, 8, 8, "8", "II-A", 40.0m, 40.0m, 20, 15m, "3rd 3 yrs of GR", true, false, 2024, 65, "Type II" },
                    { 66, 8, 8, "8", "II-B", 44.4m, 44.4m, 18, 12m, "3rd 3 yrs of GR", true, false, 2024, 66, "Type II" },
                    { 67, 8, 8, "8", "III-A", 26.7m, 26.7m, 30, 25m, "3rd 3 yrs of GR", true, false, 2024, 67, "Type III" },
                    { 68, 8, 8, "8", "III-B", 32.0m, 32.0m, 25, 20m, "3rd 3 yrs of GR", true, false, 2024, 68, "Type III" },
                    { 69, 8, 8, "8", "IV-A", 20.0m, 20.0m, 40, 35m, "3rd 3 yrs of GR", true, false, 2024, 69, "Type IV" },
                    { 70, 8, 8, "8", "IV-B", 21.1m, 21.1m, 38, 33m, "3rd 3 yrs of GR", true, false, 2024, 70, "Type IV" },
                    { 71, 8, 8, "8", "V-A", 16.0m, 16.0m, 50, 40m, "3rd 3 yrs of GR", true, false, 2024, 71, "Type V" },
                    { 72, 8, 8, "8", "V-B", 17.8m, 17.8m, 45, 37m, "3rd 3 yrs of GR", true, false, 2024, 72, "Type V" },
                    { 73, 9, 9, "9", "I", 60.0m, 60.0m, 15, 10m, "3rd 3 yrs of GR", true, false, 2024, 73, "Type I" },
                    { 74, 9, 9, "9", "II-A", 45.0m, 45.0m, 20, 15m, "3rd 3 yrs of GR", true, false, 2024, 74, "Type II" },
                    { 75, 9, 9, "9", "II-B", 50.0m, 50.0m, 18, 12m, "3rd 3 yrs of GR", true, false, 2024, 75, "Type II" },
                    { 76, 9, 9, "9", "III-A", 30.0m, 30.0m, 30, 25m, "3rd 3 yrs of GR", true, false, 2024, 76, "Type III" },
                    { 77, 9, 9, "9", "III-B", 36.0m, 36.0m, 25, 20m, "3rd 3 yrs of GR", true, false, 2024, 77, "Type III" },
                    { 78, 9, 9, "9", "IV-A", 22.5m, 22.5m, 40, 35m, "3rd 3 yrs of GR", true, false, 2024, 78, "Type IV" },
                    { 79, 9, 9, "9", "IV-B", 23.7m, 23.7m, 38, 33m, "3rd 3 yrs of GR", true, false, 2024, 79, "Type IV" },
                    { 80, 9, 9, "9", "V-A", 18.0m, 18.0m, 50, 40m, "3rd 3 yrs of GR", true, false, 2024, 80, "Type V" },
                    { 81, 9, 9, "9", "V-B", 20.0m, 20.0m, 45, 37m, "3rd 3 yrs of GR", true, false, 2024, 81, "Type V" },
                    { 82, 10, 10, "10", "I", 66.7m, 66.7m, 15, 10m, "4th 3 yrs of GR", true, false, 2024, 82, "Type I" },
                    { 83, 10, 10, "10", "II-A", 50.0m, 50.0m, 20, 15m, "4th 3 yrs of GR", true, false, 2024, 83, "Type II" },
                    { 84, 10, 10, "10", "II-B", 55.6m, 55.6m, 18, 12m, "4th 3 yrs of GR", true, false, 2024, 84, "Type II" },
                    { 85, 10, 10, "10", "III-A", 33.3m, 33.3m, 30, 25m, "4th 3 yrs of GR", true, false, 2024, 85, "Type III" },
                    { 86, 10, 10, "10", "III-B", 40.0m, 40.0m, 25, 20m, "4th 3 yrs of GR", true, false, 2024, 86, "Type III" },
                    { 87, 10, 10, "10", "IV-A", 25.0m, 25.0m, 40, 35m, "4th 3 yrs of GR", true, false, 2024, 87, "Type IV" },
                    { 88, 10, 10, "10", "IV-B", 26.3m, 26.3m, 38, 33m, "4th 3 yrs of GR", true, false, 2024, 88, "Type IV" },
                    { 89, 10, 10, "10", "V-A", 20.0m, 20.0m, 50, 40m, "4th 3 yrs of GR", true, false, 2024, 89, "Type V" },
                    { 90, 10, 10, "10", "V-B", 22.2m, 22.2m, 45, 37m, "4th 3 yrs of GR", true, false, 2024, 90, "Type V" },
                    { 91, 11, 11, "11", "I", 73.3m, 73.3m, 15, 10m, "4th 3 yrs of GR", true, false, 2024, 91, "Type I" },
                    { 92, 11, 11, "11", "II-A", 55.0m, 55.0m, 20, 15m, "4th 3 yrs of GR", true, false, 2024, 92, "Type II" },
                    { 93, 11, 11, "11", "II-B", 61.1m, 61.1m, 18, 12m, "4th 3 yrs of GR", true, false, 2024, 93, "Type II" },
                    { 94, 11, 11, "11", "III-A", 36.7m, 36.7m, 30, 25m, "4th 3 yrs of GR", true, false, 2024, 94, "Type III" },
                    { 95, 11, 11, "11", "III-B", 44.0m, 44.0m, 25, 20m, "4th 3 yrs of GR", true, false, 2024, 95, "Type III" },
                    { 96, 11, 11, "11", "IV-A", 27.5m, 27.5m, 40, 35m, "4th 3 yrs of GR", true, false, 2024, 96, "Type IV" },
                    { 97, 11, 11, "11", "IV-B", 28.9m, 28.9m, 38, 33m, "4th 3 yrs of GR", true, false, 2024, 97, "Type IV" },
                    { 98, 11, 11, "11", "V-A", 22.0m, 22.0m, 50, 40m, "4th 3 yrs of GR", true, false, 2024, 98, "Type V" },
                    { 99, 11, 11, "11", "V-B", 24.4m, 24.4m, 45, 37m, "4th 3 yrs of GR", true, false, 2024, 99, "Type V" },
                    { 100, 12, 12, "12", "I", 80.0m, 80.0m, 15, 10m, "4th 3 yrs of GR", true, false, 2024, 100, "Type I" },
                    { 101, 12, 12, "12", "II-A", 60.0m, 60.0m, 20, 15m, "4th 3 yrs of GR", true, false, 2024, 101, "Type II" },
                    { 102, 12, 12, "12", "II-B", 66.7m, 66.7m, 18, 12m, "4th 3 yrs of GR", true, false, 2024, 102, "Type II" },
                    { 103, 12, 12, "12", "III-A", 40.0m, 40.0m, 30, 25m, "4th 3 yrs of GR", true, false, 2024, 103, "Type III" },
                    { 104, 12, 12, "12", "III-B", 48.0m, 48.0m, 25, 20m, "4th 3 yrs of GR", true, false, 2024, 104, "Type III" },
                    { 105, 12, 12, "12", "IV-A", 30.0m, 30.0m, 40, 35m, "4th 3 yrs of GR", true, false, 2024, 105, "Type IV" },
                    { 106, 12, 12, "12", "IV-B", 31.6m, 31.6m, 38, 33m, "4th 3 yrs of GR", true, false, 2024, 106, "Type IV" },
                    { 107, 12, 12, "12", "V-A", 24.0m, 24.0m, 50, 40m, "4th 3 yrs of GR", true, false, 2024, 107, "Type V" },
                    { 108, 12, 12, "12", "V-B", 26.7m, 26.7m, 45, 37m, "4th 3 yrs of GR", true, false, 2024, 108, "Type V" },
                    { 109, 13, 13, "13", "I", 86.7m, 86.7m, 15, 10m, "5th 3 yrs of GR", true, false, 2024, 109, "Type I" },
                    { 110, 13, 13, "13", "II-A", 65.0m, 65.0m, 20, 15m, "5th 3 yrs of GR", true, false, 2024, 110, "Type II" },
                    { 111, 13, 13, "13", "II-B", 72.2m, 72.2m, 18, 12m, "5th 3 yrs of GR", true, false, 2024, 111, "Type II" },
                    { 112, 13, 13, "13", "III-A", 43.3m, 43.3m, 30, 25m, "5th 3 yrs of GR", true, false, 2024, 112, "Type III" },
                    { 113, 13, 13, "13", "III-B", 52.0m, 52.0m, 25, 20m, "5th 3 yrs of GR", true, false, 2024, 113, "Type III" },
                    { 114, 13, 13, "13", "IV-A", 32.5m, 32.5m, 40, 35m, "5th 3 yrs of GR", true, false, 2024, 114, "Type IV" },
                    { 115, 13, 13, "13", "IV-B", 34.2m, 34.2m, 38, 33m, "5th 3 yrs of GR", true, false, 2024, 115, "Type IV" },
                    { 116, 13, 13, "13", "V-A", 26.0m, 26.0m, 50, 40m, "5th 3 yrs of GR", true, false, 2024, 116, "Type V" },
                    { 117, 13, 13, "13", "V-B", 28.9m, 28.9m, 45, 37m, "5th 3 yrs of GR", true, false, 2024, 117, "Type V" },
                    { 118, 14, 14, "14", "I", 93.3m, 93.3m, 15, 10m, "5th 3 yrs of GR", true, false, 2024, 118, "Type I" },
                    { 119, 14, 14, "14", "II-A", 70.0m, 70.0m, 20, 15m, "5th 3 yrs of GR", true, false, 2024, 119, "Type II" },
                    { 120, 14, 14, "14", "II-B", 77.8m, 77.8m, 18, 12m, "5th 3 yrs of GR", true, false, 2024, 120, "Type II" },
                    { 121, 14, 14, "14", "III-A", 46.7m, 46.7m, 30, 25m, "5th 3 yrs of GR", true, false, 2024, 121, "Type III" },
                    { 122, 14, 14, "14", "III-B", 56.0m, 56.0m, 25, 20m, "5th 3 yrs of GR", true, false, 2024, 122, "Type III" },
                    { 123, 14, 14, "14", "IV-A", 35.0m, 35.0m, 40, 35m, "5th 3 yrs of GR", true, false, 2024, 123, "Type IV" },
                    { 124, 14, 14, "14", "IV-B", 36.8m, 36.8m, 38, 33m, "5th 3 yrs of GR", true, false, 2024, 124, "Type IV" },
                    { 125, 14, 14, "14", "V-A", 28.0m, 28.0m, 50, 40m, "5th 3 yrs of GR", true, false, 2024, 125, "Type V" },
                    { 126, 14, 14, "14", "V-B", 31.1m, 31.1m, 45, 37m, "5th 3 yrs of GR", true, false, 2024, 126, "Type V" },
                    { 127, 15, 15, "15", "I", null, 90m, 15, 10m, "5th 3 yrs of GR", true, true, 2024, 127, "Type I" },
                    { 128, 15, 15, "15", "II-A", 75.0m, 75.0m, 20, 15m, "5th 3 yrs of GR", true, false, 2024, 128, "Type II" },
                    { 129, 15, 15, "15", "II-B", 83.3m, 83.3m, 18, 12m, "5th 3 yrs of GR", true, false, 2024, 129, "Type II" },
                    { 130, 15, 15, "15", "III-A", 50.0m, 50.0m, 30, 25m, "5th 3 yrs of GR", true, false, 2024, 130, "Type III" },
                    { 131, 15, 15, "15", "III-B", 60.0m, 60.0m, 25, 20m, "5th 3 yrs of GR", true, false, 2024, 131, "Type III" },
                    { 132, 15, 15, "15", "IV-A", 37.5m, 37.5m, 40, 35m, "5th 3 yrs of GR", true, false, 2024, 132, "Type IV" },
                    { 133, 15, 15, "15", "IV-B", 39.5m, 39.5m, 38, 33m, "5th 3 yrs of GR", true, false, 2024, 133, "Type IV" },
                    { 134, 15, 15, "15", "V-A", 30.0m, 30.0m, 50, 40m, "5th 3 yrs of GR", true, false, 2024, 134, "Type V" },
                    { 135, 15, 15, "15", "V-B", 33.3m, 33.3m, 45, 37m, "5th 3 yrs of GR", true, false, 2024, 135, "Type V" },
                    { 136, 16, 16, "16", "I", null, 90m, 15, 10m, "6th 3 yrs of GR", true, true, 2024, 136, "Type I" },
                    { 137, 16, 16, "16", "II-A", 80.0m, 80.0m, 20, 15m, "6th 3 yrs of GR", true, false, 2024, 137, "Type II" },
                    { 138, 16, 16, "16", "II-B", 88.9m, 88.9m, 18, 12m, "6th 3 yrs of GR", true, false, 2024, 138, "Type II" },
                    { 139, 16, 16, "16", "III-A", 53.3m, 53.3m, 30, 25m, "6th 3 yrs of GR", true, false, 2024, 139, "Type III" },
                    { 140, 16, 16, "16", "III-B", 64.0m, 64.0m, 25, 20m, "6th 3 yrs of GR", true, false, 2024, 140, "Type III" },
                    { 141, 16, 16, "16", "IV-A", 40.0m, 40.0m, 40, 35m, "6th 3 yrs of GR", true, false, 2024, 141, "Type IV" },
                    { 142, 16, 16, "16", "IV-B", 42.1m, 42.1m, 38, 33m, "6th 3 yrs of GR", true, false, 2024, 142, "Type IV" },
                    { 143, 16, 16, "16", "V-A", 32.0m, 32.0m, 50, 40m, "6th 3 yrs of GR", true, false, 2024, 143, "Type V" },
                    { 144, 16, 16, "16", "V-B", 35.6m, 35.6m, 45, 37m, "6th 3 yrs of GR", true, false, 2024, 144, "Type V" },
                    { 145, 17, 17, "17", "I", null, 90m, 15, 10m, "6th 3 yrs of GR", true, true, 2024, 145, "Type I" },
                    { 146, 17, 17, "17", "II-A", 85.0m, 85.0m, 20, 15m, "6th 3 yrs of GR", true, false, 2024, 146, "Type II" },
                    { 147, 17, 17, "17", "II-B", null, 88m, 18, 12m, "6th 3 yrs of GR", true, true, 2024, 147, "Type II" },
                    { 148, 17, 17, "17", "III-A", 56.7m, 56.7m, 30, 25m, "6th 3 yrs of GR", true, false, 2024, 148, "Type III" },
                    { 149, 17, 17, "17", "III-B", 68.0m, 68.0m, 25, 20m, "6th 3 yrs of GR", true, false, 2024, 149, "Type III" },
                    { 150, 17, 17, "17", "IV-A", 42.5m, 42.5m, 40, 35m, "6th 3 yrs of GR", true, false, 2024, 150, "Type IV" },
                    { 151, 17, 17, "17", "IV-B", 44.7m, 44.7m, 38, 33m, "6th 3 yrs of GR", true, false, 2024, 151, "Type IV" },
                    { 152, 17, 17, "17", "V-A", 34.0m, 34.0m, 50, 40m, "6th 3 yrs of GR", true, false, 2024, 152, "Type V" },
                    { 153, 17, 17, "17", "V-B", 37.8m, 37.8m, 45, 37m, "6th 3 yrs of GR", true, false, 2024, 153, "Type V" },
                    { 154, 18, 20, "18–20", "I", null, 90m, 15, 10m, "6th 3 yrs of GR", true, true, 2024, 154, "Type I" },
                    { 155, 18, 20, "18–20", "II-A", null, 85m, 20, 15m, "6th 3 yrs of GR", true, true, 2024, 155, "Type II" },
                    { 156, 18, 20, "18–20", "II-B", null, 88m, 18, 12m, "6th 3 yrs of GR", true, true, 2024, 156, "Type II" },
                    { 157, 18, 20, "18–20", "III-A", 66.7m, 66.7m, 30, 25m, "6th 3 yrs of GR", true, false, 2024, 157, "Type III" },
                    { 158, 18, 20, "18–20", "III-B", 80.0m, 80.0m, 25, 20m, "6th 3 yrs of GR", true, false, 2024, 158, "Type III" },
                    { 159, 18, 20, "18–20", "IV-A", 50.0m, 50.0m, 40, 35m, "6th 3 yrs of GR", true, false, 2024, 159, "Type IV" },
                    { 160, 18, 20, "18–20", "IV-B", 52.6m, 52.6m, 38, 33m, "6th 3 yrs of GR", true, false, 2024, 160, "Type IV" },
                    { 161, 18, 20, "18–20", "V-A", 40.0m, 40.0m, 50, 40m, "6th 3 yrs of GR", true, false, 2024, 161, "Type V" },
                    { 162, 18, 20, "18–20", "V-B", 44.4m, 44.4m, 45, 37m, "6th 3 yrs of GR", true, false, 2024, 162, "Type V" },
                    { 163, 21, 23, "21–23", "I", null, 90m, 15, 10m, "After 20 yrs GR", true, true, 2024, 163, "Type I" },
                    { 164, 21, 23, "21–23", "II-A", null, 85m, 20, 15m, "After 20 yrs GR", true, true, 2024, 164, "Type II" },
                    { 165, 21, 23, "21–23", "II-B", null, 88m, 18, 12m, "After 20 yrs GR", true, true, 2024, 165, "Type II" },
                    { 166, 21, 23, "21–23", "III-A", 76.7m, 76.7m, 30, 25m, "After 20 yrs GR", true, false, 2024, 166, "Type III" },
                    { 167, 21, 23, "21–23", "III-B", 76.5m, 76.5m, 25, 20m, "After 20 yrs GR", true, false, 2024, 167, "Type III" },
                    { 168, 21, 23, "21–23", "IV-A", 57.5m, 57.5m, 40, 35m, "After 20 yrs GR", true, false, 2024, 168, "Type IV" },
                    { 169, 21, 23, "21–23", "IV-B", 60.5m, 60.5m, 38, 33m, "After 20 yrs GR", true, false, 2024, 169, "Type IV" },
                    { 170, 21, 23, "21–23", "V-A", 46.0m, 46.0m, 50, 40m, "After 20 yrs GR", true, false, 2024, 170, "Type V" },
                    { 171, 21, 23, "21–23", "V-B", 51.1m, 51.1m, 45, 37m, "After 20 yrs GR", true, false, 2024, 171, "Type V" },
                    { 172, 24, 26, "24–26", "I", null, 90m, 15, 10m, "After 20 yrs GR", true, true, 2024, 172, "Type I" },
                    { 173, 24, 26, "24–26", "II-A", null, 85m, 20, 15m, "After 20 yrs GR", true, true, 2024, 173, "Type II" },
                    { 174, 24, 26, "24–26", "II-B", null, 88m, 18, 12m, "After 20 yrs GR", true, true, 2024, 174, "Type II" },
                    { 175, 24, 26, "24–26", "III-A", null, 75m, 30, 25m, "After 20 yrs GR", true, true, 2024, 175, "Type III" },
                    { 176, 24, 26, "24–26", "III-B", 80.0m, 80.0m, 25, 20m, "After 20 yrs GR", true, false, 2024, 176, "Type III" },
                    { 177, 24, 26, "24–26", "IV-A", 65.0m, 65.0m, 40, 35m, "After 20 yrs GR", true, false, 2024, 177, "Type IV" },
                    { 178, 24, 26, "24–26", "IV-B", 68.4m, 68.4m, 38, 33m, "After 20 yrs GR", true, false, 2024, 178, "Type IV" },
                    { 179, 24, 26, "24–26", "V-A", 52.0m, 52.0m, 50, 40m, "After 20 yrs GR", true, false, 2024, 179, "Type V" },
                    { 180, 24, 26, "24–26", "V-B", 57.8m, 57.8m, 45, 37m, "After 20 yrs GR", true, false, 2024, 180, "Type V" },
                    { 181, 27, 30, "27–30", "I", null, 90m, 15, 10m, "After 20 yrs GR", true, true, 2024, 181, "Type I" },
                    { 182, 27, 30, "27–30", "II-A", null, 85m, 20, 15m, "After 20 yrs GR", true, true, 2024, 182, "Type II" },
                    { 183, 27, 30, "27–30", "II-B", null, 88m, 18, 12m, "After 20 yrs GR", true, true, 2024, 183, "Type II" },
                    { 184, 27, 30, "27–30", "III-A", null, 75m, 30, 25m, "After 20 yrs GR", true, true, 2024, 184, "Type III" },
                    { 185, 27, 30, "27–30", "III-B", null, 80m, 25, 20m, "After 20 yrs GR", true, true, 2024, 185, "Type III" },
                    { 186, 27, 30, "27–30", "IV-A", null, 65m, 40, 35m, "After 20 yrs GR", true, true, 2024, 186, "Type IV" },
                    { 187, 27, 30, "27–30", "IV-B", null, 67m, 38, 33m, "After 20 yrs GR", true, true, 2024, 187, "Type IV" },
                    { 188, 27, 30, "27–30", "V-A", 60.0m, 60.0m, 50, 40m, "After 20 yrs GR", true, false, 2024, 188, "Type V" },
                    { 189, 27, 30, "27–30", "V-B", 64.4m, 64.4m, 45, 37m, "After 20 yrs GR", true, false, 2024, 189, "Type V" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BuildingDepreciationSchedules_ClassificationCode_AgeLowerBound_AgeUpperBound",
                table: "BuildingDepreciationSchedules",
                columns: new[] { "ClassificationCode", "AgeLowerBound", "AgeUpperBound" });

            migrationBuilder.CreateIndex(
                name: "IX_TDNBuildings_BuildingDepreciationScheduleId",
                table: "TDNBuildings",
                column: "BuildingDepreciationScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_TDNBuildings_BuildingDepreciationSchedules_BuildingDepreciationScheduleId",
                table: "TDNBuildings",
                column: "BuildingDepreciationScheduleId",
                principalTable: "BuildingDepreciationSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TDNBuildings_BuildingDepreciationSchedules_BuildingDepreciationScheduleId",
                table: "TDNBuildings");

            migrationBuilder.DropIndex(
                name: "IX_TDNBuildings_BuildingDepreciationScheduleId",
                table: "TDNBuildings");

            migrationBuilder.DropColumn(
                name: "BuildingDepreciationScheduleId",
                table: "TDNBuildings");

            migrationBuilder.DropColumn(
                name: "GeneralRevisionCycle",
                table: "TDNBuildings");

            migrationBuilder.DropColumn(
                name: "EstimatedEconomicLifeYears",
                table: "TDNBuildings");

            migrationBuilder.DropColumn(
                name: "EstimatedResidualValuePercent",
                table: "TDNBuildings");

            migrationBuilder.DropTable(
                name: "BuildingDepreciationSchedules");
        }
    }
}
