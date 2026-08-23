using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HrManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceExpectationSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExpectationSource",
                table: "AttendanceRecords",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ExpectationSourceId",
                table: "AttendanceRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExpectationSourceName",
                table: "AttendanceRecords",
                type: "TEXT",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectationSource",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "ExpectationSourceId",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "ExpectationSourceName",
                table: "AttendanceRecords");
        }
    }
}
