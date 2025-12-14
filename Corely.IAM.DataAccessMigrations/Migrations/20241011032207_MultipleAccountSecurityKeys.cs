using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.Migrations;

/// <inheritdoc />
public partial class MultipleAccountSecurityKeys : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // CUSTOM : Drop FK references
        migrationBuilder.DropForeignKey(
            name: "FK_AccountSymmetricKeys_Accounts_AccountId",
            table: "AccountSymmetricKeys"
        );

        migrationBuilder.DropForeignKey(
            name: "FK_AccountAsymmetricKeys_Accounts_AccountId",
            table: "AccountAsymmetricKeys"
        );
        // END CUSTOM

        migrationBuilder.DropPrimaryKey(
            name: "PK_AccountSymmetricKeys",
            table: "AccountSymmetricKeys"
        );

        migrationBuilder.DropPrimaryKey(
            name: "PK_AccountAsymmetricKeys",
            table: "AccountAsymmetricKeys"
        );

        migrationBuilder.AddColumn<int>(
            name: "KeyUsedFor",
            table: "UserSymmetricKeys",
            type: "int",
            nullable: false,
            defaultValue: 0
        );

        migrationBuilder.AddColumn<int>(
            name: "KeyUsedFor",
            table: "UserAsymmetricKeys",
            type: "int",
            nullable: false,
            defaultValue: 0
        );

        migrationBuilder.AddColumn<int>(
            name: "Id",
            table: "AccountSymmetricKeys",
            type: "int",
            // CUSTOM: Modified generated settings
            nullable: true
        );
        // END CUSTOM

        // CUSTOM: Backfill ids in existing records
        migrationBuilder.Sql(
            @"
                SET @row_number = 0;
                UPDATE `AccountSymmetricKeys` 
                SET `Id` = (@row_number := @row_number + 1);
            "
        );
        // END CUSTOM

        migrationBuilder.AddColumn<int>(
            name: "KeyUsedFor",
            table: "AccountSymmetricKeys",
            type: "int",
            nullable: false,
            defaultValue: 0
        );

        migrationBuilder.AddColumn<int>(
            name: "Id",
            table: "AccountAsymmetricKeys",
            type: "int",
            // CUSTOM: Modified generated settings
            nullable: true
        );
        // END CUSTOM

        // CUSTOM: Backfill ids in existing records
        migrationBuilder.Sql(
            @"
                SET @row_number = 0;
                UPDATE `AccountAsymmetricKeys` 
                SET `Id` = (@row_number := @row_number + 1);
            "
        );
        // END CUSTOM

        migrationBuilder.AddColumn<int>(
            name: "KeyUsedFor",
            table: "AccountAsymmetricKeys",
            type: "int",
            nullable: false,
            defaultValue: 0
        );

        migrationBuilder.AddPrimaryKey(
            name: "PK_AccountSymmetricKeys",
            table: "AccountSymmetricKeys",
            column: "Id"
        );

        migrationBuilder.AddPrimaryKey(
            name: "PK_AccountAsymmetricKeys",
            table: "AccountAsymmetricKeys",
            column: "Id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_AccountSymmetricKeys_AccountId_KeyUsedFor",
            table: "AccountSymmetricKeys",
            columns: ["AccountId", "KeyUsedFor"],
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_AccountAsymmetricKeys_AccountId_KeyUsedFor",
            table: "AccountAsymmetricKeys",
            columns: ["AccountId", "KeyUsedFor"],
            unique: true
        );

        // CUSTOM: Modify Id column to be auto-increment and restore FK references
        migrationBuilder.Sql(
            "ALTER TABLE `AccountAsymmetricKeys` MODIFY COLUMN `Id` int NOT NULL AUTO_INCREMENT FIRST;"
        );
        migrationBuilder.Sql(
            "ALTER TABLE `AccountSymmetricKeys` MODIFY COLUMN `Id` int NOT NULL AUTO_INCREMENT FIRST;"
        );

        migrationBuilder.AddForeignKey(
            name: "FK_AccountSymmetricKeys_Accounts_AccountId",
            table: "AccountSymmetricKeys",
            column: "AccountId",
            principalTable: "Accounts",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade
        );

        migrationBuilder.AddForeignKey(
            name: "FK_AccountAsymmetricKeys_Accounts_AccountId",
            table: "AccountAsymmetricKeys",
            column: "AccountId",
            principalTable: "Accounts",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade
        );
        // END CUSTOM
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // CUSTOM: Drop FK references
        migrationBuilder.DropForeignKey(
            name: "FK_AccountSymmetricKeys_Accounts_AccountId",
            table: "AccountSymmetricKeys"
        );

        migrationBuilder.DropForeignKey(
            name: "FK_AccountAsymmetricKeys_Accounts_AccountId",
            table: "AccountAsymmetricKeys"
        );
        // END CUSTOM

        migrationBuilder.DropPrimaryKey(
            name: "PK_AccountSymmetricKeys",
            table: "AccountSymmetricKeys"
        );

        migrationBuilder.DropIndex(
            name: "IX_AccountSymmetricKeys_AccountId_KeyUsedFor",
            table: "AccountSymmetricKeys"
        );

        migrationBuilder.DropPrimaryKey(
            name: "PK_AccountAsymmetricKeys",
            table: "AccountAsymmetricKeys"
        );

        migrationBuilder.DropIndex(
            name: "IX_AccountAsymmetricKeys_AccountId_KeyUsedFor",
            table: "AccountAsymmetricKeys"
        );

        migrationBuilder.DropColumn(name: "KeyUsedFor", table: "UserSymmetricKeys");

        migrationBuilder.DropColumn(name: "KeyUsedFor", table: "UserAsymmetricKeys");

        migrationBuilder.DropColumn(name: "Id", table: "AccountSymmetricKeys");

        migrationBuilder.DropColumn(name: "KeyUsedFor", table: "AccountSymmetricKeys");

        migrationBuilder.DropColumn(name: "Id", table: "AccountAsymmetricKeys");

        migrationBuilder.DropColumn(name: "KeyUsedFor", table: "AccountAsymmetricKeys");

        migrationBuilder.AddPrimaryKey(
            name: "PK_AccountSymmetricKeys",
            table: "AccountSymmetricKeys",
            column: "AccountId"
        );

        migrationBuilder.AddPrimaryKey(
            name: "PK_AccountAsymmetricKeys",
            table: "AccountAsymmetricKeys",
            column: "AccountId"
        );

        // CUSTOM: Restore FK references
        migrationBuilder.AddForeignKey(
            name: "FK_AccountSymmetricKeys_Accounts_AccountId",
            table: "AccountSymmetricKeys",
            column: "AccountId",
            principalTable: "Accounts",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade
        );

        migrationBuilder.AddForeignKey(
            name: "FK_AccountAsymmetricKeys_Accounts_AccountId",
            table: "AccountAsymmetricKeys",
            column: "AccountId",
            principalTable: "Accounts",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade
        );
        // END CUSTOM
    }
}
