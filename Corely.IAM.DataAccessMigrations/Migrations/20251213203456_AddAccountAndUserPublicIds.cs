using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.Migrations;

/// <inheritdoc />
public partial class AddAccountAndUserPublicIds : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "PublicId",
            table: "Users",
            type: "char(36)",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
            collation: "ascii_general_ci"
        );

        migrationBuilder.AddColumn<Guid>(
            name: "PublicId",
            table: "Accounts",
            type: "char(36)",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
            collation: "ascii_general_ci"
        );

        migrationBuilder.CreateIndex(
            name: "IX_Users_PublicId",
            table: "Users",
            column: "PublicId",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_Accounts_PublicId",
            table: "Accounts",
            column: "PublicId",
            unique: true
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Users_PublicId", table: "Users");

        migrationBuilder.DropIndex(name: "IX_Accounts_PublicId", table: "Accounts");

        migrationBuilder.DropColumn(name: "PublicId", table: "Users");

        migrationBuilder.DropColumn(name: "PublicId", table: "Accounts");
    }
}
