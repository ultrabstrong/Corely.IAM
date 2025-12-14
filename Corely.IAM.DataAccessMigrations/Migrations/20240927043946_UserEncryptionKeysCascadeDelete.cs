using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.Migrations;

/// <inheritdoc />
public partial class UserEncryptionKeysCascadeDelete : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Users_UserAsymmetricKeys_AsymmetricKeyUserId",
            table: "Users"
        );

        migrationBuilder.DropForeignKey(
            name: "FK_Users_UserSymmetricKeys_SymmetricKeyUserId",
            table: "Users"
        );

        migrationBuilder.DropIndex(name: "IX_Users_AsymmetricKeyUserId", table: "Users");

        migrationBuilder.DropIndex(name: "IX_Users_SymmetricKeyUserId", table: "Users");

        migrationBuilder.DropColumn(name: "AsymmetricKeyUserId", table: "Users");

        migrationBuilder.DropColumn(name: "SymmetricKeyUserId", table: "Users");

        migrationBuilder.AddForeignKey(
            name: "FK_UserAsymmetricKeys_Users_UserId",
            table: "UserAsymmetricKeys",
            column: "UserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade
        );

        migrationBuilder.AddForeignKey(
            name: "FK_UserSymmetricKeys_Users_UserId",
            table: "UserSymmetricKeys",
            column: "UserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_UserAsymmetricKeys_Users_UserId",
            table: "UserAsymmetricKeys"
        );

        migrationBuilder.DropForeignKey(
            name: "FK_UserSymmetricKeys_Users_UserId",
            table: "UserSymmetricKeys"
        );

        migrationBuilder.AddColumn<int>(
            name: "AsymmetricKeyUserId",
            table: "Users",
            type: "int",
            nullable: true
        );

        migrationBuilder.AddColumn<int>(
            name: "SymmetricKeyUserId",
            table: "Users",
            type: "int",
            nullable: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_Users_AsymmetricKeyUserId",
            table: "Users",
            column: "AsymmetricKeyUserId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_Users_SymmetricKeyUserId",
            table: "Users",
            column: "SymmetricKeyUserId"
        );

        migrationBuilder.AddForeignKey(
            name: "FK_Users_UserAsymmetricKeys_AsymmetricKeyUserId",
            table: "Users",
            column: "AsymmetricKeyUserId",
            principalTable: "UserAsymmetricKeys",
            principalColumn: "UserId"
        );

        migrationBuilder.AddForeignKey(
            name: "FK_Users_UserSymmetricKeys_SymmetricKeyUserId",
            table: "Users",
            column: "SymmetricKeyUserId",
            principalTable: "UserSymmetricKeys",
            principalColumn: "UserId"
        );
    }
}
