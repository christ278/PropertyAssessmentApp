using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyTagging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PropertyTaggings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxDeclarationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxDeclarationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PropertyIndexNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OwnerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TagType = table.Column<int>(type: "int", nullable: false),
                    PartyName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TagDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PerDocNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PageNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BookNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SeriesOf = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PostedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PostedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReasonForCancellation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DateCancelled = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyTaggings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyTaggings_TaxDeclarations_TaxDeclarationId",
                        column: x => x.TaxDeclarationId,
                        principalTable: "TaxDeclarations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyTaggings_TaxDeclarationId",
                table: "PropertyTaggings",
                column: "TaxDeclarationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PropertyTaggings");
        }
    }
}
