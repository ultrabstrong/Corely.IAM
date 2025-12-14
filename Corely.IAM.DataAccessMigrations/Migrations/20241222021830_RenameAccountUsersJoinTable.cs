using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.Migrations;

/// <inheritdoc />
public partial class RenameAccountUsersJoinTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_AccountEntityUserEntity_Accounts_AccountsId",
            table: "AccountEntityUserEntity"
        );

        migrationBuilder.DropForeignKey(
            name: "FK_AccountEntityUserEntity_Users_UsersId",
            table: "AccountEntityUserEntity"
        );

        migrationBuilder.DropPrimaryKey(
            name: "PK_AccountEntityUserEntity",
            table: "AccountEntityUserEntity"
        );

        migrationBuilder.RenameTable(name: "AccountEntityUserEntity", newName: "UserAccounts");

        migrationBuilder.RenameIndex(
            name: "IX_AccountEntityUserEntity_UsersId",
            table: "UserAccounts",
            newName: "IX_UserAccounts_UsersId"
        );

        migrationBuilder.AddPrimaryKey(
            name: "PK_UserAccounts",
            table: "UserAccounts",
            columns: new[] { "AccountsId", "UsersId" }
        );

        migrationBuilder.AddForeignKey(
            name: "FK_UserAccounts_Accounts_AccountsId",
            table: "UserAccounts",
            column: "AccountsId",
            principalTable: "Accounts",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade
        );

        migrationBuilder.AddForeignKey(
            name: "FK_UserAccounts_Users_UsersId",
            table: "UserAccounts",
            column: "UsersId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_UserAccounts_Accounts_AccountsId",
            table: "UserAccounts"
        );

        migrationBuilder.DropForeignKey(
            name: "FK_UserAccounts_Users_UsersId",
            table: "UserAccounts"
        );

        migrationBuilder.DropPrimaryKey(name: "PK_UserAccounts", table: "UserAccounts");

        migrationBuilder.RenameTable(name: "UserAccounts", newName: "AccountEntityUserEntity");

        migrationBuilder.RenameIndex(
            name: "IX_UserAccounts_UsersId",
            table: "AccountEntityUserEntity",
            newName: "IX_AccountEntityUserEntity_UsersId"
        );

        migrationBuilder.AddPrimaryKey(
            name: "PK_AccountEntityUserEntity",
            table: "AccountEntityUserEntity",
            columns: new[] { "AccountsId", "UsersId" }
        );

        migrationBuilder.AddForeignKey(
            name: "FK_AccountEntityUserEntity_Accounts_AccountsId",
            table: "AccountEntityUserEntity",
            column: "AccountsId",
            principalTable: "Accounts",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade
        );

        migrationBuilder.AddForeignKey(
            name: "FK_AccountEntityUserEntity_Users_UsersId",
            table: "AccountEntityUserEntity",
            column: "UsersId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade
        );
    }
}
