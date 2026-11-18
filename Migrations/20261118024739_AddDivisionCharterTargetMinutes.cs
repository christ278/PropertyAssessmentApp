using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class AddDivisionCharterTargetMinutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CharterTargetMinutes",
                table: "WorkflowDivisionSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CharterTargetMinutes",
                table: "WorkflowDivisionSettings");
        }
    }
}
