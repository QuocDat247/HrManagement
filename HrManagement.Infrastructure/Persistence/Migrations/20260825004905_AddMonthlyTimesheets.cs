using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HrManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyTimesheets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TimesheetPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ClosedAtUtc = table.Column<long>(type: "INTEGER", nullable: true),
                    ClosedByUserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ClosedByUsername = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimesheetPeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyTimesheetDaySnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TimesheetPeriodId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AttendanceRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    IsWorkingDay = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExpectedPlannedMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    WorkedMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    LateMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    EarlyLeaveMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    CorrectionRevision = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyTimesheetDaySnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyTimesheetDaySnapshots_AttendanceRecords_AttendanceRecordId",
                        column: x => x.AttendanceRecordId,
                        principalTable: "AttendanceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonthlyTimesheetDaySnapshots_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonthlyTimesheetDaySnapshots_TimesheetPeriods_TimesheetPeriodId",
                        column: x => x.TimesheetPeriodId,
                        principalTable: "TimesheetPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyTimesheetDaySnapshots_AttendanceRecordId",
                table: "MonthlyTimesheetDaySnapshots",
                column: "AttendanceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyTimesheetSnapshots_Employee_Date",
                table: "MonthlyTimesheetDaySnapshots",
                columns: new[] { "EmployeeId", "WorkDate" });

            migrationBuilder.CreateIndex(
                name: "UX_MonthlyTimesheetSnapshots_Period_Employee_Date",
                table: "MonthlyTimesheetDaySnapshots",
                columns: new[] { "TimesheetPeriodId", "EmployeeId", "WorkDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_TimesheetPeriods_Year_Month",
                table: "TimesheetPeriods",
                columns: new[] { "Year", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonthlyTimesheetDaySnapshots");

            migrationBuilder.DropTable(
                name: "TimesheetPeriods");
        }
    }
}
