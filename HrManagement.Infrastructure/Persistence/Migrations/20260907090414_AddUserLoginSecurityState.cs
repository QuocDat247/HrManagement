using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HrManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLoginSecurityState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserLoginSecurityStates",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FailedLoginCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LockoutEndUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLoginSecurityStates", x => x.AccountId);
                    table.ForeignKey(
                        name: "FK_UserLoginSecurityStates_UserAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserLoginSecurityStates");
        }
    }
}
