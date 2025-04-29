using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeCardSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEmployeeIdUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Date",
                table: "TimeEntries",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "TimeEntries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EmployeeId",
                table: "TimeEntries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "TimeEntries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EmployeeId",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRate",
                table: "AspNetUsers",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Date",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "HourlyRate",
                table: "AspNetUsers");
        }
    }
}
