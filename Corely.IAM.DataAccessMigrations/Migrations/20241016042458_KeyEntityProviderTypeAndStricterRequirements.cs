using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.Migrations;

/// <inheritdoc />
public partial class KeyEntityProviderTypeAndStricterRequirements : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder
            .AddColumn<string>(
                name: "ProviderTypeCode",
                table: "UserSymmetricKeys",
                type: "longtext",
                nullable: false
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        // CUSTOM: Reorder columns
        migrationBuilder.Sql(
            "ALTER TABLE `UserSymmetricKeys` MODIFY COLUMN `KeyUsedFor` int NOT NULL DEFAULT 0 AFTER `UserId`"
        );
        migrationBuilder.Sql(
            "ALTER TABLE `UserSymmetricKeys` MODIFY COLUMN `ProviderTypeCode` longtext NOT NULL AFTER `KeyUsedFor`"
        );
        // END CUSTOM

        migrationBuilder
            .AddColumn<string>(
                name: "ProviderTypeCode",
                table: "UserAsymmetricKeys",
                type: "longtext",
                nullable: false
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        // CUSTOM: Reorder columns
        migrationBuilder.Sql(
            "ALTER TABLE `UserAsymmetricKeys` MODIFY COLUMN `KeyUsedFor` int NOT NULL DEFAULT 0 AFTER `UserId`"
        );
        migrationBuilder.Sql(
            "ALTER TABLE `UserAsymmetricKeys` MODIFY COLUMN `ProviderTypeCode` longtext NOT NULL AFTER `KeyUsedFor`"
        );
        // END CUSTOM

        migrationBuilder
            .AddColumn<string>(
                name: "ProviderTypeCode",
                table: "AccountSymmetricKeys",
                type: "longtext",
                nullable: false
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        // CUSTOM: Reorder columns
        migrationBuilder.Sql(
            "ALTER TABLE `AccountSymmetricKeys` MODIFY COLUMN `KeyUsedFor` int NOT NULL DEFAULT 0 AFTER `AccountId`"
        );
        migrationBuilder.Sql(
            "ALTER TABLE `AccountSymmetricKeys` MODIFY COLUMN `ProviderTypeCode` longtext NOT NULL AFTER `KeyUsedFor`"
        );
        // END CUSTOM

        migrationBuilder
            .AddColumn<string>(
                name: "ProviderTypeCode",
                table: "AccountAsymmetricKeys",
                type: "longtext",
                nullable: false
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        // CUSTOM: Reorder columns
        migrationBuilder.Sql(
            "ALTER TABLE `AccountAsymmetricKeys` MODIFY COLUMN `KeyUsedFor` int NOT NULL DEFAULT 0 AFTER `AccountId`"
        );
        migrationBuilder.Sql(
            "ALTER TABLE `AccountAsymmetricKeys` MODIFY COLUMN `ProviderTypeCode` longtext NOT NULL AFTER `KeyUsedFor`"
        );
        // END CUSTOM
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ProviderTypeCode", table: "UserSymmetricKeys");

        migrationBuilder.DropColumn(name: "ProviderTypeCode", table: "UserAsymmetricKeys");

        migrationBuilder.DropColumn(name: "ProviderTypeCode", table: "AccountSymmetricKeys");

        migrationBuilder.DropColumn(name: "ProviderTypeCode", table: "AccountAsymmetricKeys");
    }
}
