using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class AddOfficeScheduleAndStageEstimatedMinutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EstimatedMinutes",
                table: "WorkflowSequence",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "OfficeHolidays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HolidayDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsRecurringYearly = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficeHolidays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OfficeScheduleSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkStartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    WorkEndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    HasBreakTime = table.Column<bool>(type: "bit", nullable: false),
                    BreakStartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    BreakEndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    WorkMonday = table.Column<bool>(type: "bit", nullable: false),
                    WorkTuesday = table.Column<bool>(type: "bit", nullable: false),
                    WorkWednesday = table.Column<bool>(type: "bit", nullable: false),
                    WorkThursday = table.Column<bool>(type: "bit", nullable: false),
                    WorkFriday = table.Column<bool>(type: "bit", nullable: false),
                    WorkSaturday = table.Column<bool>(type: "bit", nullable: false),
                    WorkSunday = table.Column<bool>(type: "bit", nullable: false),
                    PauseWhenDeferred = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficeScheduleSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfficeHolidays");

            migrationBuilder.DropTable(
                name: "OfficeScheduleSettings");

            migrationBuilder.DropColumn(
                name: "EstimatedMinutes",
                table: "WorkflowSequence");
        }
    }
}
