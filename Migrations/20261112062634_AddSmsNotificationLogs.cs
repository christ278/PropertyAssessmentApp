using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSmsNotificationLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SmsNotificationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RecipientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SenderId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    GatewayResponse = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsNotificationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SmsNotificationLogs_TaxpayerOwners_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "TaxpayerOwners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SmsNotificationLogs_OwnerId",
                table: "SmsNotificationLogs",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SmsNotificationLogs");
        }
    }
}
