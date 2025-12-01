using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.Migrations;

/// <inheritdoc />
public partial class KeyUsedForStringConversion : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder
            .AlterColumn<string>(
                name: "KeyUsedFor",
                table: "UserSymmetricKeys",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int"
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder
            .AlterColumn<string>(
                name: "KeyUsedFor",
                table: "UserAsymmetricKeys",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int"
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder
            .AlterColumn<string>(
                name: "KeyUsedFor",
                table: "AccountSymmetricKeys",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int"
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder
            .AlterColumn<string>(
                name: "KeyUsedFor",
                table: "AccountAsymmetricKeys",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int"
            )
            .Annotation("MySql:CharSet", "utf8mb4");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder
            .AlterColumn<int>(
                name: "KeyUsedFor",
                table: "UserSymmetricKeys",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)"
            )
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder
            .AlterColumn<int>(
                name: "KeyUsedFor",
                table: "UserAsymmetricKeys",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)"
            )
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder
            .AlterColumn<int>(
                name: "KeyUsedFor",
                table: "AccountSymmetricKeys",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)"
            )
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder
            .AlterColumn<int>(
                name: "KeyUsedFor",
                table: "AccountAsymmetricKeys",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)"
            )
            .OldAnnotation("MySql:CharSet", "utf8mb4");
    }
}
