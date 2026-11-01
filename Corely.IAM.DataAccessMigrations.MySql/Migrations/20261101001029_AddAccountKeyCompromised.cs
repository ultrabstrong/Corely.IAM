using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountKeyCompromised : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompromisedUtc",
                table: "AccountSymmetricKeys",
                type: "datetime(6)",
                nullable: true
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "CompromisedUtc",
                table: "AccountAsymmetricKeys",
                type: "datetime(6)",
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CompromisedUtc", table: "AccountSymmetricKeys");

            migrationBuilder.DropColumn(name: "CompromisedUtc", table: "AccountAsymmetricKeys");
        }
    }
}
