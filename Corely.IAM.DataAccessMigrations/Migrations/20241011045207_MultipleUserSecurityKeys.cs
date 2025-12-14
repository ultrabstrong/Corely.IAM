using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.Migrations;

/// <inheritdoc />
public partial class MultipleUserSecurityKeys : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // CUSTOM : Drop FK references
        migrationBuilder.DropForeignKey(
            name: "FK_UserSymmetricKeys_Users_UserId",
            table: "UserSymmetricKeys"
        );

        migrationBuilder.DropForeignKey(
            name: "FK_UserAsymmetricKeys_Users_UserId",
            table: "UserAsymmetricKeys"
        );
        // END CUSTOM

        migrationBuilder.DropPrimaryKey(name: "PK_UserSymmetricKeys", table: "UserSymmetricKeys");

        migrationBuilder.DropPrimaryKey(name: "PK_UserAsymmetricKeys", table: "UserAsymmetricKeys");

        migrationBuilder.AddColumn<int>(
            name: "Id",
            table: "UserSymmetricKeys",
            type: "int",
            // CUSTOM: Modified generated settings
            nullable: true
        );
        // END CUSTOM

        // CUSTOM: Backfill ids in existing records
        migrationBuilder.Sql(
            @"
                SET @row_number = 0;
                UPDATE `UserSymmetricKeys` 
                SET `Id` = (@row_number := @row_number + 1);
            "
        );
        // END CUSTOM

        migrationBuilder.AddColumn<int>(
            name: "Id",
            table: "UserAsymmetricKeys",
            type: "int",
            // CUSTOM: Modified generated settings
            nullable: true
        );
        // END CUSTOM

        // CUSTOM: Backfill ids in existing records
        migrationBuilder.Sql(
            @"
                SET @row_number = 0;
                UPDATE `UserAsymmetricKeys` 
                SET `Id` = (@row_number := @row_number + 1);
            "
        );
        // END CUSTOM

        migrationBuilder.AddPrimaryKey(
            name: "PK_UserSymmetricKeys",
            table: "UserSymmetricKeys",
            column: "Id"
        );

        migrationBuilder.AddPrimaryKey(
            name: "PK_UserAsymmetricKeys",
            table: "UserAsymmetricKeys",
            column: "Id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_UserSymmetricKeys_UserId_KeyUsedFor",
            table: "UserSymmetricKeys",
            columns: ["UserId", "KeyUsedFor"],
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_UserAsymmetricKeys_UserId_KeyUsedFor",
            table: "UserAsymmetricKeys",
            columns: ["UserId", "KeyUsedFor"],
            unique: true
        );

        // CUSTOM: Modify Id column to be auto-increment and restore FK references
        migrationBuilder.Sql(
            "ALTER TABLE `UserAsymmetricKeys` MODIFY COLUMN `Id` int NOT NULL AUTO_INCREMENT FIRST;"
        );
        migrationBuilder.Sql(
            "ALTER TABLE `UserSymmetricKeys` MODIFY COLUMN `Id` int NOT NULL AUTO_INCREMENT FIRST;"
        );

        migrationBuilder.AddForeignKey(
            name: "FK_UserSymmetricKeys_Users_UserId",
            table: "UserSymmetricKeys",
            column: "UserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade
        );

        migrationBuilder.AddForeignKey(
            name: "FK_UserAsymmetricKeys_Users_UserId",
            table: "UserAsymmetricKeys",
            column: "UserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade
        );
        // END CUSTOM
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // CUSTOM : Drop FK references
        migrationBuilder.DropForeignKey(
            name: "FK_UserSymmetricKeys_Users_UserId",
            table: "UserSymmetricKeys"
        );

        migrationBuilder.DropForeignKey(
            name: "FK_UserAsymmetricKeys_Users_UserId",
            table: "UserAsymmetricKeys"
        );
        // END CUSTOM

        migrationBuilder.DropPrimaryKey(name: "PK_UserSymmetricKeys", table: "UserSymmetricKeys");

        migrationBuilder.DropIndex(
            name: "IX_UserSymmetricKeys_UserId_KeyUsedFor",
            table: "UserSymmetricKeys"
        );

        migrationBuilder.DropPrimaryKey(name: "PK_UserAsymmetricKeys", table: "UserAsymmetricKeys");

        migrationBuilder.DropIndex(
            name: "IX_UserAsymmetricKeys_UserId_KeyUsedFor",
            table: "UserAsymmetricKeys"
        );

        migrationBuilder.DropColumn(name: "Id", table: "UserSymmetricKeys");

        migrationBuilder.DropColumn(name: "Id", table: "UserAsymmetricKeys");

        migrationBuilder.AddPrimaryKey(
            name: "PK_UserSymmetricKeys",
            table: "UserSymmetricKeys",
            column: "UserId"
        );

        migrationBuilder.AddPrimaryKey(
            name: "PK_UserAsymmetricKeys",
            table: "UserAsymmetricKeys",
            column: "UserId"
        );

        // CUSTOM: Restore FK references
        migrationBuilder.AddForeignKey(
            name: "FK_UserSymmetricKeys_Users_UserId",
            table: "UserSymmetricKeys",
            column: "UserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade
        );

        migrationBuilder.AddForeignKey(
            name: "FK_UserAsymmetricKeys_Users_UserId",
            table: "UserAsymmetricKeys",
            column: "UserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade
        );
        // END CUSTOM
    }
}
