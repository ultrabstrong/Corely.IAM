using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.MsSql.Migrations
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
                type: "datetime2",
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
                type: "bit",
                nullable: false,
                defaultValue: false
            );
        }
    }
}
