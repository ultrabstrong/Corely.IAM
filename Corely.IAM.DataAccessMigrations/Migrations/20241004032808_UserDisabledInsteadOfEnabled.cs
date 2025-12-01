using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.Migrations;

/// <inheritdoc />
public partial class UserDisabledInsteadOfEnabled : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Enabled", table: "Users");

        migrationBuilder.AddColumn<bool>(
            name: "Disabled",
            table: "Users",
            type: "tinyint(1)",
            nullable: false,
            defaultValue: false
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Disabled", table: "Users");

        migrationBuilder.AddColumn<bool>(
            name: "Enabled",
            table: "Users",
            type: "tinyint(1)",
            nullable: false,
            defaultValue: true
        );
    }
}
