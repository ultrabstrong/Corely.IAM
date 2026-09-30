using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AddKeyGenerations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Generation",
                table: "UserSymmetricKeys",
                type: "int",
                nullable: false,
                defaultValue: 1
            );

            migrationBuilder.AddColumn<int>(
                name: "Generation",
                table: "UserAsymmetricKeys",
                type: "int",
                nullable: false,
                defaultValue: 1
            );

            migrationBuilder.AddColumn<int>(
                name: "Generation",
                table: "AccountSymmetricKeys",
                type: "int",
                nullable: false,
                defaultValue: 1
            );

            migrationBuilder.AddColumn<int>(
                name: "Generation",
                table: "AccountAsymmetricKeys",
                type: "int",
                nullable: false,
                defaultValue: 1
            );

            migrationBuilder.CreateIndex(
                name: "IX_UserSymmetricKeys_UserId_KeyUsedFor_Generation",
                table: "UserSymmetricKeys",
                columns: new[] { "UserId", "KeyUsedFor", "Generation" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_UserAsymmetricKeys_UserId_KeyUsedFor_Generation",
                table: "UserAsymmetricKeys",
                columns: new[] { "UserId", "KeyUsedFor", "Generation" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_AccountSymmetricKeys_AccountId_KeyUsedFor_Generation",
                table: "AccountSymmetricKeys",
                columns: new[] { "AccountId", "KeyUsedFor", "Generation" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_AccountAsymmetricKeys_AccountId_KeyUsedFor_Generation",
                table: "AccountAsymmetricKeys",
                columns: new[] { "AccountId", "KeyUsedFor", "Generation" },
                unique: true
            );

            migrationBuilder.DropIndex(
                name: "IX_UserSymmetricKeys_UserId_KeyUsedFor",
                table: "UserSymmetricKeys"
            );

            migrationBuilder.DropIndex(
                name: "IX_UserAsymmetricKeys_UserId_KeyUsedFor",
                table: "UserAsymmetricKeys"
            );

            migrationBuilder.DropIndex(
                name: "IX_AccountSymmetricKeys_AccountId_KeyUsedFor",
                table: "AccountSymmetricKeys"
            );

            migrationBuilder.DropIndex(
                name: "IX_AccountAsymmetricKeys_AccountId_KeyUsedFor",
                table: "AccountAsymmetricKeys"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserSymmetricKeys_UserId_KeyUsedFor",
                table: "UserSymmetricKeys",
                columns: new[] { "UserId", "KeyUsedFor" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_UserAsymmetricKeys_UserId_KeyUsedFor",
                table: "UserAsymmetricKeys",
                columns: new[] { "UserId", "KeyUsedFor" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_AccountSymmetricKeys_AccountId_KeyUsedFor",
                table: "AccountSymmetricKeys",
                columns: new[] { "AccountId", "KeyUsedFor" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_AccountAsymmetricKeys_AccountId_KeyUsedFor",
                table: "AccountAsymmetricKeys",
                columns: new[] { "AccountId", "KeyUsedFor" },
                unique: true
            );

            migrationBuilder.DropIndex(
                name: "IX_UserSymmetricKeys_UserId_KeyUsedFor_Generation",
                table: "UserSymmetricKeys"
            );

            migrationBuilder.DropIndex(
                name: "IX_UserAsymmetricKeys_UserId_KeyUsedFor_Generation",
                table: "UserAsymmetricKeys"
            );

            migrationBuilder.DropIndex(
                name: "IX_AccountSymmetricKeys_AccountId_KeyUsedFor_Generation",
                table: "AccountSymmetricKeys"
            );

            migrationBuilder.DropIndex(
                name: "IX_AccountAsymmetricKeys_AccountId_KeyUsedFor_Generation",
                table: "AccountAsymmetricKeys"
            );

            migrationBuilder.DropColumn(name: "Generation", table: "UserSymmetricKeys");

            migrationBuilder.DropColumn(name: "Generation", table: "UserAsymmetricKeys");

            migrationBuilder.DropColumn(name: "Generation", table: "AccountSymmetricKeys");

            migrationBuilder.DropColumn(name: "Generation", table: "AccountAsymmetricKeys");
        }
    }
}
