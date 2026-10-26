using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAssessmentApp.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionOwnershipAndQueues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedUserId",
                table: "DocumentForwardingLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedUserName",
                table: "DocumentForwardingLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsClaimed",
                table: "DocumentForwardingLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeferred",
                table: "DocumentForwardingLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedUserId",
                table: "DocumentForwardingLogs");

            migrationBuilder.DropColumn(
                name: "AssignedUserName",
                table: "DocumentForwardingLogs");

            migrationBuilder.DropColumn(
                name: "IsClaimed",
                table: "DocumentForwardingLogs");

            migrationBuilder.DropColumn(
                name: "IsDeferred",
                table: "DocumentForwardingLogs");
        }
    }
}
