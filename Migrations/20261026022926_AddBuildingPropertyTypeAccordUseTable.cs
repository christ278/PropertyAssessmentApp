using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class AddBuildingPropertyTypeAccordUseTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BuildingPropertyTypeAccordUse",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertyTypeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildingPropertyTypeAccordUse", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "BuildingPropertyTypeAccordUse",
                columns: new[] { "Id", "Description", "IsActive", "PropertyTypeName", "SortOrder" },
                values: new object[,]
                {
                    { 1, null, true, "Townhouse", 1 },
                    { 2, null, true, "Industrial Condominium", 2 },
                    { 3, null, true, "Residential Condominium", 3 },
                    { 4, null, true, "Commercial Condominium", 4 },
                    { 5, null, true, "Other Type of Building and/or Structure", 5 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BuildingPropertyTypeAccordUse");
        }
    }
}
