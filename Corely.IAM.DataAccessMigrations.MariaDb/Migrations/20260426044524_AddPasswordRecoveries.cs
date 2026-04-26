using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.MariaDb.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordRecoveries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PasswordRecoverys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SecretHash = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExpiresUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    InvalidatedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: false, defaultValueSql: "(UTC_TIMESTAMP)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordRecoverys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordRecoverys_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordRecoverys_ExpiresUtc",
                table: "PasswordRecoverys",
                column: "ExpiresUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordRecoverys_UserId",
                table: "PasswordRecoverys",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PasswordRecoverys");
        }
    }
}
