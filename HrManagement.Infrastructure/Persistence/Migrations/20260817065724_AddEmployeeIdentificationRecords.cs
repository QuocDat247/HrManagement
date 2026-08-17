using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HrManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeIdentificationRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeIdentificationRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    DocumentNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    IssuingAuthority = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PlaceOfIssue = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IssuingCountry = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeIdentificationRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeIdentificationRecords_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeIdentificationRecords_EmployeeId_Type",
                table: "EmployeeIdentificationRecords",
                columns: new[] { "EmployeeId", "Type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeIdentificationRecords");
        }
    }
}
