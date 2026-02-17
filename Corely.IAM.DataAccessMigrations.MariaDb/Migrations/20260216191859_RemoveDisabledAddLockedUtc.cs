using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.MariaDb.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDisabledAddLockedUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Disabled", table: "Users");

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUtc",
                table: "Users",
                type: "datetime(6)",
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "LockedUtc", table: "Users");

            migrationBuilder.AddColumn<bool>(
                name: "Disabled",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false
            );
        }
    }
}
