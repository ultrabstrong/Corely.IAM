using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.Migrations
{
    /// <inheritdoc />
    public partial class MakeModifiedUtcNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedUtc",
                table: "UserSymmetricKeys",
                type: "TIMESTAMP",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP",
                oldDefaultValueSql: "(UTC_TIMESTAMP)"
            );

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedUtc",
                table: "Users",
                type: "TIMESTAMP",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP",
                oldDefaultValueSql: "(UTC_TIMESTAMP)"
            );

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedUtc",
                table: "UserAsymmetricKeys",
                type: "TIMESTAMP",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP",
                oldDefaultValueSql: "(UTC_TIMESTAMP)"
            );

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedUtc",
                table: "Roles",
                type: "TIMESTAMP",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP",
                oldDefaultValueSql: "(UTC_TIMESTAMP)"
            );

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedUtc",
                table: "Permissions",
                type: "TIMESTAMP",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP",
                oldDefaultValueSql: "(UTC_TIMESTAMP)"
            );

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedUtc",
                table: "Groups",
                type: "TIMESTAMP",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP",
                oldDefaultValueSql: "(UTC_TIMESTAMP)"
            );

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedUtc",
                table: "BasicAuths",
                type: "TIMESTAMP",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP",
                oldDefaultValueSql: "(UTC_TIMESTAMP)"
            );

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedUtc",
                table: "AccountSymmetricKeys",
                type: "TIMESTAMP",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP",
                oldDefaultValueSql: "(UTC_TIMESTAMP)"
            );

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedUtc",
                table: "Accounts",
                type: "TIMESTAMP",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP",
                oldDefaultValueSql: "(UTC_TIMESTAMP)"
            );

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedUtc",
                table: "AccountAsymmetricKeys",
                type: "TIMESTAMP",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP",
                oldDefaultValueSql: "(UTC_TIMESTAMP)"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedUtc",
                table: "UserSymmetricKeys",
                type: "TIMESTAMP",
                nullable: false,
                defaultValueSql: "(UTC_TIMESTAMP)",
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedUtc",
                table: "Users",
                type: "TIMESTAMP",
                nullable: false,
                defaultValueSql: "(UTC_TIMESTAMP)",
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedUtc",
                table: "UserAsymmetricKeys",
                type: "TIMESTAMP",
                nullable: false,
                defaultValueSql: "(UTC_TIMESTAMP)",
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedUtc",
                table: "Roles",
                type: "TIMESTAMP",
                nullable: false,
                defaultValueSql: "(UTC_TIMESTAMP)",
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedUtc",
                table: "Permissions",
                type: "TIMESTAMP",
                nullable: false,
                defaultValueSql: "(UTC_TIMESTAMP)",
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedUtc",
                table: "Groups",
                type: "TIMESTAMP",
                nullable: false,
                defaultValueSql: "(UTC_TIMESTAMP)",
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedUtc",
                table: "BasicAuths",
                type: "TIMESTAMP",
                nullable: false,
                defaultValueSql: "(UTC_TIMESTAMP)",
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedUtc",
                table: "AccountSymmetricKeys",
                type: "TIMESTAMP",
                nullable: false,
                defaultValueSql: "(UTC_TIMESTAMP)",
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedUtc",
                table: "Accounts",
                type: "TIMESTAMP",
                nullable: false,
                defaultValueSql: "(UTC_TIMESTAMP)",
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedUtc",
                table: "AccountAsymmetricKeys",
                type: "TIMESTAMP",
                nullable: false,
                defaultValueSql: "(UTC_TIMESTAMP)",
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP",
                oldNullable: true
            );
        }
    }
}
