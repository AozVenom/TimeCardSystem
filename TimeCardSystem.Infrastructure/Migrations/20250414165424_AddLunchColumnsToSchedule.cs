using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeCardSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLunchColumnsToSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Schedules",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "Schedules",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "HasLunch",
                table: "Schedules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "LunchDuration",
                table: "Schedules",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 30, 0, 0));

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_ShiftStart",
                table: "Schedules",
                column: "ShiftStart");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Schedules_ShiftStart",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "HasLunch",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "LunchDuration",
                table: "Schedules");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Schedules",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "Schedules",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
