using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.Migrations;

/// <inheritdoc />
public partial class AddUserAuthTokenTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder
            .CreateTable(
                name: "UserAuthTokens",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation(
                            "MySql:ValueGenerationStrategy",
                            MySqlValueGenerationStrategy.IdentityColumn
                        ),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    Jti = table
                        .Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IssuedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RevokedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(
                        type: "TIMESTAMP",
                        nullable: false,
                        defaultValueSql: "(UTC_TIMESTAMP)"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAuthTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAuthTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_UserAuthTokens_ExpiresUtc",
            table: "UserAuthTokens",
            column: "ExpiresUtc"
        );

        migrationBuilder.CreateIndex(
            name: "IX_UserAuthTokens_Jti",
            table: "UserAuthTokens",
            column: "Jti",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_UserAuthTokens_UserId",
            table: "UserAuthTokens",
            column: "UserId"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "UserAuthTokens");
    }
}
