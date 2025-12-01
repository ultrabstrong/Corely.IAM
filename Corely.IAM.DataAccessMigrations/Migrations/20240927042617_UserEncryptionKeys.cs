using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.Migrations;

/// <inheritdoc />
public partial class UserEncryptionKeys : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "AsymmetricKeyUserId",
            table: "Users",
            type: "int",
            nullable: true
        );

        migrationBuilder.AddColumn<int>(
            name: "SymmetricKeyUserId",
            table: "Users",
            type: "int",
            nullable: true
        );

        migrationBuilder
            .CreateTable(
                name: "UserAsymmetricKeys",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    PublicKey = table
                        .Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PrivateKey = table
                        .Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedUtc = table.Column<DateTime>(
                        type: "TIMESTAMP",
                        nullable: false,
                        defaultValueSql: "(UTC_TIMESTAMP)"
                    ),
                    ModifiedUtc = table.Column<DateTime>(
                        type: "TIMESTAMP",
                        nullable: false,
                        defaultValueSql: "(UTC_TIMESTAMP)"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAsymmetricKeys", x => x.UserId);
                }
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder
            .CreateTable(
                name: "UserSymmetricKeys",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Key = table
                        .Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedUtc = table.Column<DateTime>(
                        type: "TIMESTAMP",
                        nullable: false,
                        defaultValueSql: "(UTC_TIMESTAMP)"
                    ),
                    ModifiedUtc = table.Column<DateTime>(
                        type: "TIMESTAMP",
                        nullable: false,
                        defaultValueSql: "(UTC_TIMESTAMP)"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSymmetricKeys", x => x.UserId);
                }
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_Users_AsymmetricKeyUserId",
            table: "Users",
            column: "AsymmetricKeyUserId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_Users_SymmetricKeyUserId",
            table: "Users",
            column: "SymmetricKeyUserId"
        );

        migrationBuilder.AddForeignKey(
            name: "FK_Users_UserAsymmetricKeys_AsymmetricKeyUserId",
            table: "Users",
            column: "AsymmetricKeyUserId",
            principalTable: "UserAsymmetricKeys",
            principalColumn: "UserId"
        );

        migrationBuilder.AddForeignKey(
            name: "FK_Users_UserSymmetricKeys_SymmetricKeyUserId",
            table: "Users",
            column: "SymmetricKeyUserId",
            principalTable: "UserSymmetricKeys",
            principalColumn: "UserId"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Users_UserAsymmetricKeys_AsymmetricKeyUserId",
            table: "Users"
        );

        migrationBuilder.DropForeignKey(
            name: "FK_Users_UserSymmetricKeys_SymmetricKeyUserId",
            table: "Users"
        );

        migrationBuilder.DropTable(name: "UserAsymmetricKeys");

        migrationBuilder.DropTable(name: "UserSymmetricKeys");

        migrationBuilder.DropIndex(name: "IX_Users_AsymmetricKeyUserId", table: "Users");

        migrationBuilder.DropIndex(name: "IX_Users_SymmetricKeyUserId", table: "Users");

        migrationBuilder.DropColumn(name: "AsymmetricKeyUserId", table: "Users");

        migrationBuilder.DropColumn(name: "SymmetricKeyUserId", table: "Users");
    }
}
