using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class AddRPTReportsTableAndSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RPTReports",
                columns: table => new
                {
                    RPTReportId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, defaultValue: "System"),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RPTReports", x => x.RPTReportId);
                });

            migrationBuilder.InsertData(
                table: "RPTReports",
                columns: new[] { "RPTReportId", "CreatedBy", "CreatedDate", "Description", "Title", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, "System", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Tax Declaration Records Certification", "Tax Decleration Recods Certification", null, null },
                    { 2, "System", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Certificate of No Improvement", "Certificate of NO Improvement", null, null },
                    { 3, "System", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Assessment of Tax Declaration", "Assessment of Tax Declaration", null, null },
                    { 4, "System", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Certificate of Improvement", "Certificate of Improvement", null, null },
                    { 5, "System", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Certificate of No Land Holding", "Certificate of no land holding", null, null },
                    { 6, "System", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "All Property Holding (BIR Certification)", "All property Holding(BIR Certification)", null, null },
                    { 7, "System", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Agricultural Lands (DAR Certification)", "Agricultural Lands (DAR Certification)", null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RPTReports");
        }
    }
}
