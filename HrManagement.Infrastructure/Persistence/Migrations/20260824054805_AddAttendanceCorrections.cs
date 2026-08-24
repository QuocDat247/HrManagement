using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HrManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceCorrections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AttendanceRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AffectedEventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Revision = table.Column<int>(type: "INTEGER", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    BeforeEventType = table.Column<int>(type: "INTEGER", nullable: true),
                    BeforeOccurredAtUtc = table.Column<long>(type: "INTEGER", nullable: true),
                    AfterEventType = table.Column<int>(type: "INTEGER", nullable: true),
                    AfterOccurredAtUtc = table.Column<long>(type: "INTEGER", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CorrectedAtUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    ActorUserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ActorUsername = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceCorrections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceCorrections_AttendanceRecords_AttendanceRecordId",
                        column: x => x.AttendanceRecordId,
                        principalTable: "AttendanceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceCorrections_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceCorrections_Employee_CorrectedAtUtc",
                table: "AttendanceCorrections",
                columns: new[] { "EmployeeId", "CorrectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceCorrections_Record_Event_Revision",
                table: "AttendanceCorrections",
                columns: new[] { "AttendanceRecordId", "AffectedEventId", "Revision" });

            migrationBuilder.CreateIndex(
                name: "UX_AttendanceCorrections_Record_Revision",
                table: "AttendanceCorrections",
                columns: new[] { "AttendanceRecordId", "Revision" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceCorrections");
        }
    }
}
