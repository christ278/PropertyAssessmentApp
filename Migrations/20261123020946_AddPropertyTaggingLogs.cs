using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyTaggingLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PropertyTaggingLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxDeclarationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxDeclarationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PropertyIndexNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OwnerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TagType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PerformedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyTaggingLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PropertyTaggingLogs");
        }
    }
}
