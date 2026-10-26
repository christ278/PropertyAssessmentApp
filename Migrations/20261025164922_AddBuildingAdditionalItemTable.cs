using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class AddBuildingAdditionalItemTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BuildingAdditionalItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdditionalItemDescription = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    PercentUnitValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CostPerSQM = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExcessSQM = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExcessPercentage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RevisionYear = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildingAdditionalItems", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "BuildingAdditionalItems",
                columns: new[] { "Id", "AdditionalItemDescription", "CostPerSQM", "ExcessPercentage", "ExcessSQM", "IsActive", "PercentUnitValue", "RevisionYear", "SortOrder" },
                values: new object[,]
                {
                    { 1, "CARPORT", 0.00m, 0.00m, 0.00m, true, 30.00m, 2011, 1 },
                    { 2, "MEZZANINE", 0.00m, 0.00m, 0.00m, true, 60.00m, 2011, 2 },
                    { 3, "PORCH", 0.00m, 0.00m, 0.00m, true, 40.00m, 2011, 3 },
                    { 4, "BALCONY", 0.00m, 0.00m, 0.00m, true, 45.00m, 2011, 4 },
                    { 5, "GARAGE", 0.00m, 0.00m, 0.00m, true, 45.00m, 2011, 5 },
                    { 6, "TERRACE : COVERED", 0.00m, 0.00m, 0.00m, true, 40.00m, 2011, 6 },
                    { 7, "TERRACE : OPEN", 0.00m, 0.00m, 0.00m, true, 20.00m, 2011, 7 },
                    { 8, "DECK ROOF : COVERED", 0.00m, 0.00m, 0.00m, true, 40.00m, 2011, 8 },
                    { 9, "DECK ROOF : OPEN", 0.00m, 0.00m, 0.00m, true, 20.00m, 2011, 9 },
                    { 10, "BASEMENT : RESIDENTIAL", 0.00m, 0.00m, 0.00m, true, 70.00m, 2011, 10 },
                    { 11, "PAVEMENTS : TENNIS COURT", 75.00m, 0.00m, 0.00m, true, 0.00m, 2011, 11 },
                    { 12, "PAVEMENTS : CONCRETE 5 CM. THICK", 75.00m, 0.00m, 0.00m, true, 0.00m, 2011, 12 },
                    { 13, "PAVEMENTS : CONCRETE 10 CM. THICK", 150.00m, 0.00m, 0.00m, true, 0.00m, 2011, 13 },
                    { 14, "PAVEMENTS :CONCRETE 15 CM. THICK", 225.00m, 0.00m, 0.00m, true, 0.00m, 2011, 14 },
                    { 15, "PAVEMENTS : CONCRETE 20 CM. THICK", 300.00m, 0.00m, 0.00m, true, 0.00m, 2011, 15 },
                    { 16, "PAVEMENTS : ASPHALT 1 COURSE", 120.00m, 0.00m, 0.00m, true, 0.00m, 2011, 16 },
                    { 17, "PAVEMENTS : ASPHALT 2 COURSE", 150.00m, 0.00m, 0.00m, true, 0.00m, 2011, 17 },
                    { 18, "PAVEMENTS : ASPHALT 3 COURSE", 180.00m, 0.00m, 0.00m, true, 0.00m, 2011, 18 },
                    { 19, "FLOOR FINISHES : GRANITE TILES", 1000.00m, 0.00m, 0.00m, true, 0.00m, 2011, 19 },
                    { 20, "FLOOR FINISHES : MARBLE SLABS", 700.00m, 0.00m, 0.00m, true, 0.00m, 2011, 20 },
                    { 21, "FLOOR FINISHES : MARBLE TILES", 600.00m, 0.00m, 0.00m, true, 0.00m, 2011, 21 },
                    { 22, "FLOOR FINISHES : CERAMIC TILES", 600.00m, 0.00m, 0.00m, true, 0.00m, 2011, 22 },
                    { 23, "FLOOR FINISHES : CRAZY CUT MARBLES", 600.00m, 0.00m, 0.00m, true, 0.00m, 2011, 23 },
                    { 24, "FLOOR FINISHES : GRANOLITHIC", 400.00m, 0.00m, 0.00m, true, 0.00m, 2011, 24 },
                    { 25, "FLOOR FINISHES : NARRA", 600.00m, 0.00m, 0.00m, true, 0.00m, 2011, 25 },
                    { 26, "FLOOR FINISHES : YAKAL", 500.00m, 0.00m, 0.00m, true, 0.00m, 2011, 26 },
                    { 27, "FLOOR FINISHES : NARRA/FANCY WOOD TILES", 400.00m, 0.00m, 0.00m, true, 0.00m, 2011, 27 },
                    { 28, "FLOOR FINISHES : ORDINARY WOOD TILES", 400.00m, 0.00m, 0.00m, true, 0.00m, 2011, 28 },
                    { 29, "FLOOR FINISHES : VINYL TILES", 300.00m, 0.00m, 0.00m, true, 0.00m, 2011, 29 },
                    { 30, "FLOOR FINISHES : WASHOUT PEEBLES", 100.00m, 0.00m, 0.00m, true, 0.00m, 2011, 30 },
                    { 31, "FLOOR FINISHES : UNGLAZED TILES", 300.00m, 0.00m, 0.00m, true, 0.00m, 2011, 31 },
                    { 33, "WALLING : DOUBLE WALLING (ORDINARY PLYWOOD)", 200.00m, 0.00m, 0.00m, true, 0.00m, 2011, 33 },
                    { 34, "WALLING : DOUBLE WALLING (NARRA PANELING)", 300.00m, 0.00m, 0.00m, true, 0.00m, 2011, 34 },
                    { 35, "WALLING : GLAZED WHITE TILES", 400.00m, 0.00m, 0.00m, true, 0.00m, 2011, 35 },
                    { 36, "WALLING : GLAZED COLORED TILES", 600.00m, 0.00m, 0.00m, true, 0.00m, 2011, 36 },
                    { 37, "WALLING : FANCY TILES", 200.00m, 0.00m, 0.00m, true, 0.00m, 2011, 37 },
                    { 38, "WALLING : SYNTHETIC RUBLE", 100.00m, 0.00m, 0.00m, true, 0.00m, 2011, 38 },
                    { 39, "WALLING : BRICKS", 100.00m, 0.00m, 0.00m, true, 0.00m, 2011, 39 },
                    { 40, "SPECIAL PANELS : GLASS WITH WOODEN FRAMES", 400.00m, 0.00m, 0.00m, true, 0.00m, 2011, 40 },
                    { 41, "SPECIAL PANELS : GLASS WITH ALUMINUM FRAMES", 600.00m, 0.00m, 0.00m, true, 0.00m, 2011, 41 },
                    { 42, "CEILINGS BELOW CONCRETE FLOOR : ORDINARY PLYWOOD", 200.00m, 0.00m, 0.00m, true, 0.00m, 2011, 42 },
                    { 43, "CEILINGS BELOW CONCRETE FLOOR : LUMINOUS CEILING", 500.00m, 0.00m, 0.00m, true, 0.00m, 2011, 43 },
                    { 44, "CEILINGS BELOW CONCRETE FLOOR : ACOUSTIC", 400.00m, 0.00m, 0.00m, true, 0.00m, 2011, 44 },
                    { 45, "CEILINGS BELOW CONCRETE FLOOR : SPECIAL FINISH", 300.00m, 0.00m, 0.00m, true, 0.00m, 2011, 45 },
                    { 46, "GATES, GRILLS AND WINDOW GRILLS : STEEL GATES W/GRILLS (SPECIAL DESIGNS)", 1000.00m, 0.00m, 0.00m, true, 0.00m, 2011, 46 },
                    { 47, "GATES, GRILLS AND WINDOW GRILLS : STEEL GRILLS W/GRILLS (W/O DESIGNS ORDINARY)", 600.00m, 0.00m, 0.00m, true, 0.00m, 2011, 47 },
                    { 48, "GATES, GRILLS AND WINDOW GRILLS : WINDOW GRILLS", 400.00m, 0.00m, 0.00m, true, 0.00m, 2011, 48 },
                    { 49, "EXCESS HEIGHTS : RESIDENTIAL AND COMMERCIAL IN THE EXCESS OF 3 METERS", 0.00m, 0.00m, 0.00m, true, 10.00m, 2011, 49 },
                    { 50, "EXCESS HEIGHTS : BODEGA AND FACTORY IN THE EXCESS OF 4.5 METERS", 0.00m, 0.00m, 0.00m, true, 10.00m, 2011, 50 },
                    { 51, "EXTRA T % B - ORDINARY FINISH", 10000.00m, 0.00m, 0.00m, true, 0.00m, 2011, 51 },
                    { 52, "PAINTING", 0.00m, 0.00m, 0.00m, true, 10.00m, 2011, 52 },
                    { 53, "SECOND HAND MATERIALS", 0.00m, 0.00m, 0.00m, true, -10.00m, 2011, 53 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BuildingAdditionalItems");
        }
    }
}
