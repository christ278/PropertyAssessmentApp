using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class AddLandAppraisalPrerequisitesAndTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ARPNo",
                table: "TDNLands",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CadastralSurveyNo",
                table: "TDNLands",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Eremarks",
                table: "TDNLands",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LandAdjustmentsJson",
                table: "TDNLands",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LandAppraisalDetailsJson",
                table: "TDNLands",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LandImprovementsJson",
                table: "TDNLands",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LandPropertyAssessmentsJson",
                table: "TDNLands",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nremarks",
                table: "TDNLands",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParcelNo",
                table: "TDNLands",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Purok",
                table: "TDNLands",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SectionNo",
                table: "TDNLands",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sremarks",
                table: "TDNLands",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Wremarks",
                table: "TDNLands",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "refActualUse",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActualUseCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActualUseName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsTaxExempt = table.Column<bool>(type: "bit", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refActualUse", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "refAdjustment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Distance = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AdjustmentPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RevisionYear = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refAdjustment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "refClassification",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassificationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClassificationName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Ranking = table.Column<int>(type: "int", nullable: true),
                    Prefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refClassification", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "refImprovementType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TypeDescription = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BaseValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AssessmentLevel = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RevisionYear = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refImprovementType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "refLandAssessmentLevel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActualUseId = table.Column<int>(type: "int", nullable: true),
                    ActualUseName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AssessmentLevel = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RevisionYear = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refLandAssessmentLevel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "refSubClassification",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassificationId = table.Column<int>(type: "int", nullable: false),
                    SubClass = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UnitValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RevisionYear = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refSubClassification", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TDNLandBaseMarkets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TDNLandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNo = table.Column<int>(type: "int", nullable: false),
                    ClassificationId = table.Column<int>(type: "int", nullable: true),
                    ClassificationName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BaseMarketValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AdjustmentFactorId = table.Column<int>(type: "int", nullable: true),
                    AdjustmentFactor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PercentAdjustment = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ValueAdjustment = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AdjustedMarketValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TDNLandBaseMarkets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TDNLandBaseMarkets_TDNLands_TDNLandId",
                        column: x => x.TDNLandId,
                        principalTable: "TDNLands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TDNLandDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TDNLandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNo = table.Column<int>(type: "int", nullable: false),
                    ClassificationId = table.Column<int>(type: "int", nullable: true),
                    ClassificationName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubClassId = table.Column<int>(type: "int", nullable: true),
                    SubClassName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActualUseId = table.Column<int>(type: "int", nullable: true),
                    ActualUseName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AreaSQM = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AreaHAS = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 2, nullable: false),
                    UnitValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MarketValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsTaxable = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TDNLandDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TDNLandDetails_TDNLands_TDNLandId",
                        column: x => x.TDNLandId,
                        principalTable: "TDNLands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TDNLandImprovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TDNLandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNo = table.Column<int>(type: "int", nullable: false),
                    TypeId = table.Column<int>(type: "int", nullable: true),
                    ProductClass = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AreaPlanted = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsFruitBearing = table.Column<bool>(type: "bit", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AgeYears = table.Column<int>(type: "int", nullable: true),
                    UnitValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MarketValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AssessmentLevel = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AssessedValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsTaxable = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TDNLandImprovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TDNLandImprovements_TDNLands_TDNLandId",
                        column: x => x.TDNLandId,
                        principalTable: "TDNLands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TDNLandPropertyAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TDNLandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNo = table.Column<int>(type: "int", nullable: false),
                    Kinds = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActualUseId = table.Column<int>(type: "int", nullable: true),
                    ActualUseName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MarketValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AssessmentLevelPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AssessmentValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TDNLandPropertyAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TDNLandPropertyAssessments_TDNLands_TDNLandId",
                        column: x => x.TDNLandId,
                        principalTable: "TDNLands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "refActualUse",
                columns: new[] { "Id", "ActualUseCode", "ActualUseName", "IsActive", "IsTaxExempt", "Rank" },
                values: new object[,]
                {
                    { 1, "RES", "RESIDENTIAL", true, false, 1 },
                    { 2, "AGR", "AGRICULTURAL", true, false, 2 },
                    { 3, "COM", "COMMERCIAL", true, false, 3 },
                    { 4, "IND", "INDUSTRIAL", true, false, 4 },
                    { 5, "MIN", "MINERAL", true, false, 5 },
                    { 6, "CUL", "SPECIAL - CULTURAL", true, false, 7 },
                    { 7, "HOS", "SPECIAL - HOSPITAL", true, false, 8 },
                    { 8, "SCI", "SPECIAL - SCIENTIFIC", true, false, 9 },
                    { 9, "GOV", "GOVERNMENT", true, true, 10 },
                    { 10, "REL", "RELIGIOUS", true, true, 11 },
                    { 11, "CHAR", "CHARITABLE", true, true, 12 },
                    { 12, "EDUC", "EDUCATIONAL", true, true, 13 }
                });

            migrationBuilder.InsertData(
                table: "refAdjustment",
                columns: new[] { "Id", "AdjustmentPercent", "Description", "Distance", "IsActive", "RevisionYear" },
                values: new object[,]
                {
                    { 1, 10.00m, "Corner Influence (Residential)", null, true, 2011 },
                    { 2, 25.00m, "Corner Influence (Commercial)", null, true, 2011 },
                    { 3, 0.00m, "Provincial & National Highway Frontage", null, true, 2011 },
                    { 4, -2.00m, "All-Weather Gravel Road", "0-1 km", true, 2011 },
                    { 5, -6.00m, "Dirt Road", "Over 1-3 km", true, 2011 },
                    { 6, -10.00m, "Interior Lot / No Road Outlet", null, true, 2011 },
                    { 7, -10.00m, "Topography: 16-30 degrees slope", null, true, 2011 },
                    { 8, -20.00m, "Topography: 31-45 degrees slope", null, true, 2011 },
                    { 9, -30.00m, "Sunken / Lowland / Subject to Inundation", null, true, 2011 },
                    { 10, -20.00m, "Cost of Filling / Stripping (20m depth)", "20m", true, 2011 },
                    { 11, -10.00m, "Slightly Eroded Land", null, true, 2011 }
                });

            migrationBuilder.InsertData(
                table: "refClassification",
                columns: new[] { "Id", "ClassificationCode", "ClassificationName", "IsActive", "Prefix", "Ranking", "SortOrder" },
                values: new object[,]
                {
                    { 1, "RES", "RESIDENTIAL", true, "RES", 1, 1 },
                    { 4, "COM", "COMMERCIAL", true, "COM", 3, 2 },
                    { 6, "IND", "INDUSTRIAL", true, "IND", 4, 3 },
                    { 10, "CUL", "SPECIAL - CULTURAL", true, "CUL", 7, 5 },
                    { 11, "HOS", "SPECIAL - HOSPITAL", true, "HOS", 8, 6 },
                    { 12, "SCI", "SPECIAL - SCIENTIFIC", true, "SCI", 9, 7 },
                    { 48, "AGR", "AGRICULTURAL", true, "AGR", 2, 4 },
                    { 49, "MIN", "MINERAL", true, "MIN", 5, 8 },
                    { 50, "TIM", "TIMBERLAND", true, "TIM", 6, 9 }
                });

            migrationBuilder.InsertData(
                table: "refImprovementType",
                columns: new[] { "Id", "AssessmentLevel", "BaseValue", "IsActive", "Kind", "RevisionYear", "TypeCode", "TypeDescription" },
                values: new object[,]
                {
                    { 1, 40.00m, 120.00m, true, "FRUIT BEARING", 2011, "CCT", "COCONUT TREE" },
                    { 2, 40.00m, 350.00m, true, "FRUIT BEARING", 2011, "MGO", "MANGO TREE" },
                    { 3, 40.00m, 35.00m, true, "FRUIT BEARING", 2011, "BNN", "BANANA CLUMPS" },
                    { 4, 40.00m, 45.00m, true, "PLANT", 2011, "BMO", "BAMBOO CLUMPS" },
                    { 5, 40.00m, 150.00m, true, "FRUIT BEARING", 2011, "AVD", "AVOCADO TREE" },
                    { 6, 40.00m, 50.00m, true, "FRUIT BEARING", 2011, "COFT", "COFFEE TREE" },
                    { 7, 40.00m, 60.00m, true, "FRUIT BEARING", 2011, "CAT", "CACAO TREE" },
                    { 8, 40.00m, 400.00m, true, "FRUIT BEARING", 2011, "DB", "DURIAN (BEARING)" },
                    { 9, 40.00m, 180.00m, true, "FOREST TREE", 2011, "GF", "GEMILINA TREE" },
                    { 10, 40.00m, 250.00m, true, "FOREST TREE", 2011, "MB", "MAHOGANY (BIG)" },
                    { 11, 40.00m, 500.00m, true, "HARDWOOD", 2011, "NR", "NARRA TREE" },
                    { 12, 40.00m, 650.00m, true, "IMPROVEMENT", 2011, "FENCE", "CONCRETE FENCE / PERIMETER WALL" },
                    { 13, 40.00m, 250.00m, true, "IMPROVEMENT", 2011, "PAVE", "CONCRETE PAVEMENT / DRIVEWAY" }
                });

            migrationBuilder.InsertData(
                table: "refLandAssessmentLevel",
                columns: new[] { "Id", "ActualUseId", "ActualUseName", "AssessmentLevel", "IsActive", "RevisionYear" },
                values: new object[,]
                {
                    { 1, 1, "RESIDENTIAL", 20.00m, true, 2011 },
                    { 2, 2, "AGRICULTURAL", 40.00m, true, 2011 },
                    { 3, 3, "COMMERCIAL", 50.00m, true, 2011 },
                    { 4, 4, "INDUSTRIAL", 50.00m, true, 2011 },
                    { 5, 5, "MINERAL", 50.00m, true, 2011 },
                    { 6, 6, "SPECIAL - CULTURAL", 10.00m, true, 2011 },
                    { 7, 7, "SPECIAL - HOSPITAL", 10.00m, true, 2011 },
                    { 8, 8, "SPECIAL - SCIENTIFIC", 10.00m, true, 2011 },
                    { 9, 9, "GOVERNMENT", 0.00m, true, 2011 }
                });

            migrationBuilder.InsertData(
                table: "refSubClassification",
                columns: new[] { "Id", "ClassificationId", "IsActive", "RevisionYear", "SubClass", "UnitValue" },
                values: new object[,]
                {
                    { 1, 1, true, 2011, "1st Class Residential", 520.00m },
                    { 2, 1, true, 2011, "2nd Class Residential", 468.00m },
                    { 3, 1, true, 2011, "3rd Class Residential", 410.80m },
                    { 4, 1, true, 2011, "4th Class Residential", 351.00m },
                    { 5, 1, true, 2011, "5th Class Residential", 286.00m },
                    { 6, 1, true, 2011, "6th Class Residential", 234.00m },
                    { 7, 1, true, 2011, "7th Class Residential", 163.80m },
                    { 8, 1, true, 2011, "8th Class Residential", 117.00m },
                    { 9, 4, true, 2011, "1st Class Commercial", 3120.00m },
                    { 10, 4, true, 2011, "2nd Class Commercial", 2522.00m },
                    { 11, 4, true, 2011, "3rd Class Commercial", 1924.00m },
                    { 12, 4, true, 2011, "4th Class Commercial", 1378.00m },
                    { 13, 4, true, 2011, "5th Class Commercial", 1222.00m },
                    { 14, 4, true, 2011, "6th Class Commercial", 988.00m },
                    { 15, 6, true, 2011, "1st Class Industrial", 910.00m },
                    { 16, 6, true, 2011, "2nd Class Industrial", 806.00m },
                    { 17, 6, true, 2011, "3rd Class Industrial", 676.00m },
                    { 18, 6, true, 2011, "4th Class Industrial", 546.00m },
                    { 19, 6, true, 2011, "5th Class Industrial", 416.00m },
                    { 20, 48, true, 2011, "Riceland Irrigated (1st Class)", 150000.00m },
                    { 21, 48, true, 2011, "Riceland Unirrigated (2nd Class)", 130000.00m },
                    { 22, 48, true, 2011, "Cornland (1st Class)", 110000.00m },
                    { 23, 48, true, 2011, "Coconut Land", 100000.00m },
                    { 24, 48, true, 2011, "Banana Plantation", 120000.00m },
                    { 25, 48, true, 2011, "Fishpond / Aquaculture", 160000.00m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TDNLandBaseMarkets_TDNLandId",
                table: "TDNLandBaseMarkets",
                column: "TDNLandId");

            migrationBuilder.CreateIndex(
                name: "IX_TDNLandDetails_TDNLandId",
                table: "TDNLandDetails",
                column: "TDNLandId");

            migrationBuilder.CreateIndex(
                name: "IX_TDNLandImprovements_TDNLandId",
                table: "TDNLandImprovements",
                column: "TDNLandId");

            migrationBuilder.CreateIndex(
                name: "IX_TDNLandPropertyAssessments_TDNLandId",
                table: "TDNLandPropertyAssessments",
                column: "TDNLandId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "refActualUse");

            migrationBuilder.DropTable(
                name: "refAdjustment");

            migrationBuilder.DropTable(
                name: "refClassification");

            migrationBuilder.DropTable(
                name: "refImprovementType");

            migrationBuilder.DropTable(
                name: "refLandAssessmentLevel");

            migrationBuilder.DropTable(
                name: "refSubClassification");

            migrationBuilder.DropTable(
                name: "TDNLandBaseMarkets");

            migrationBuilder.DropTable(
                name: "TDNLandDetails");

            migrationBuilder.DropTable(
                name: "TDNLandImprovements");

            migrationBuilder.DropTable(
                name: "TDNLandPropertyAssessments");

            migrationBuilder.DropColumn(
                name: "ARPNo",
                table: "TDNLands");

            migrationBuilder.DropColumn(
                name: "CadastralSurveyNo",
                table: "TDNLands");

            migrationBuilder.DropColumn(
                name: "Eremarks",
                table: "TDNLands");

            migrationBuilder.DropColumn(
                name: "LandAdjustmentsJson",
                table: "TDNLands");

            migrationBuilder.DropColumn(
                name: "LandAppraisalDetailsJson",
                table: "TDNLands");

            migrationBuilder.DropColumn(
                name: "LandImprovementsJson",
                table: "TDNLands");

            migrationBuilder.DropColumn(
                name: "LandPropertyAssessmentsJson",
                table: "TDNLands");

            migrationBuilder.DropColumn(
                name: "Nremarks",
                table: "TDNLands");

            migrationBuilder.DropColumn(
                name: "ParcelNo",
                table: "TDNLands");

            migrationBuilder.DropColumn(
                name: "Purok",
                table: "TDNLands");

            migrationBuilder.DropColumn(
                name: "SectionNo",
                table: "TDNLands");

            migrationBuilder.DropColumn(
                name: "Sremarks",
                table: "TDNLands");

            migrationBuilder.DropColumn(
                name: "Wremarks",
                table: "TDNLands");
        }
    }
}
