using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HrManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeOrganizationAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeOrganizationAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmploymentPeriodId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DepartmentCode = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    DepartmentName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PositionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PositionCode = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    PositionName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeOrganizationAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeOrganizationAssignments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeOrganizationAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeOrganizationAssignments_EmploymentPeriods_EmploymentPeriodId",
                        column: x => x.EmploymentPeriodId,
                        principalTable: "EmploymentPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeOrganizationAssignments_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOrganizationAssignments_DepartmentId",
                table: "EmployeeOrganizationAssignments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOrganizationAssignments_EmployeeId_StartDate",
                table: "EmployeeOrganizationAssignments",
                columns: new[] { "EmployeeId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOrganizationAssignments_EmploymentPeriodId",
                table: "EmployeeOrganizationAssignments",
                column: "EmploymentPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOrganizationAssignments_PositionId",
                table: "EmployeeOrganizationAssignments",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "UX_EmployeeOrganizationAssignments_EmployeeId_Open",
                table: "EmployeeOrganizationAssignments",
                column: "EmployeeId",
                unique: true,
                filter: "\"EndDate\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeOrganizationAssignments");
        }
    }
}
