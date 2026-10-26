using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class AddBuildingClassificationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BuildingClassifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassificationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClassificationName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StructuralTypeDefault = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultUnitConstructionCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildingClassifications", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "BuildingClassifications",
                columns: new[] { "Id", "ClassificationCode", "ClassificationName", "DefaultUnitConstructionCost", "Description", "IsActive", "SortOrder", "StructuralTypeDefault" },
                values: new object[,]
                {
                    { 1, "I-A", "Class A - Reinforced Concrete", 15000m, "Reinforced Concrete Frame (RC Columns, Beams, Slabs & Walls)", true, 1, "Type I" },
                    { 2, "I-B", "Class B - Semi-Concrete / Hollow Blocks", 12500m, "Reinforced Concrete Columns & Beams with CHB Exterior Walls", true, 2, "Type II" },
                    { 3, "II-A", "Class C - Reinforced Concrete & Wood", 9800m, "Concrete Ground Floor with Timber / Wood Upper Floors", true, 3, "Type III" },
                    { 4, "III-A", "Class D - Wood / Timber Frame", 6500m, "Wood Columns, Girders, Timber Framing & Wood Flooring", true, 4, "Type IV" },
                    { 5, "IV-A", "Class E - Light Materials / Mixed", 3500m, "Light G.I. Sheets, Bamboo, Sawali, Salvaged Materials", true, 5, "Type V" },
                    { 6, "RES-01", "Residential - One Family Dwelling", 14000m, "Single Family Detached Residential Building", true, 6, "Type I" },
                    { 7, "RES-02", "Residential - Two Family Dwelling", 13500m, "Duplex Residential Unit", true, 7, "Type I" },
                    { 8, "RES-03", "Residential - Multi-Family / Apartment", 14500m, "Apartment Building, Rowhouse, Accessoria", true, 8, "Type I" },
                    { 9, "RES-04", "Residential - Condominium Unit", 22000m, "High-Rise / Mid-Rise Residential Condominium", true, 9, "Type I" },
                    { 10, "COM-01", "Commercial - Retail / Shopping Center", 18000m, "Retail Stores, Shopping Mall, Supermarket", true, 10, "Type I" },
                    { 11, "COM-02", "Commercial - Office Building / Bank", 20000m, "Office Tower, Financial Center, Bank Building", true, 11, "Type I" },
                    { 12, "COM-03", "Commercial - Hotel / Motel / Inn", 24000m, "Hospitality Building, Hotel, Resort Accommodation", true, 12, "Type I" },
                    { 13, "IND-01", "Industrial - Factory / Manufacturing Plant", 16000m, "Industrial Production and Manufacturing Complex", true, 13, "Type III" },
                    { 14, "IND-02", "Industrial - Warehouse / Storage Facility", 11000m, "Storage Bodega, Logistics Center, Cold Storage", true, 14, "Type III" },
                    { 15, "INS-01", "Institutional - School / University", 15500m, "Educational Classrooms, Campus Laboratories", true, 15, "Type I" },
                    { 16, "INS-02", "Institutional - Hospital / Medical Clinic", 25000m, "Hospital, Clinical Laboratory, Health Facility", true, 16, "Type I" },
                    { 17, "SPE-01", "Special - Gymnasium / Coliseum / Arena", 21000m, "Sports Coliseum, Cultural Theater, Auditorium", true, 17, "Type I" },
                    { 18, "AGR-01", "Agricultural - Farm Shed / Livestock Facility", 4500m, "Poultry House, Piggery Pen, Farm Machinery Shed", true, 18, "Type IV" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BuildingClassifications");
        }
    }
}
