using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.MsSql.Migrations
{
    /// <inheritdoc />
    public partial class AddKeyVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE UserSymmetricKeys SET Version = 1");
            migrationBuilder.Sql("UPDATE UserAsymmetricKeys SET Version = 1");
            migrationBuilder.Sql("UPDATE AccountSymmetricKeys SET Version = 1");
            migrationBuilder.Sql("UPDATE AccountAsymmetricKeys SET Version = 1");

            migrationBuilder.CreateIndex(
                name: "IX_UserSymmetricKeys_UserId_KeyUsedFor_Version",
                table: "UserSymmetricKeys",
                columns: new[] { "UserId", "KeyUsedFor", "Version" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_UserAsymmetricKeys_UserId_KeyUsedFor_Version",
                table: "UserAsymmetricKeys",
                columns: new[] { "UserId", "KeyUsedFor", "Version" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_AccountSymmetricKeys_AccountId_KeyUsedFor_Version",
                table: "AccountSymmetricKeys",
                columns: new[] { "AccountId", "KeyUsedFor", "Version" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_AccountAsymmetricKeys_AccountId_KeyUsedFor_Version",
                table: "AccountAsymmetricKeys",
                columns: new[] { "AccountId", "KeyUsedFor", "Version" },
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
                name: "IX_UserSymmetricKeys_UserId_KeyUsedFor_Version",
                table: "UserSymmetricKeys"
            );

            migrationBuilder.DropIndex(
                name: "IX_UserAsymmetricKeys_UserId_KeyUsedFor_Version",
                table: "UserAsymmetricKeys"
            );

            migrationBuilder.DropIndex(
                name: "IX_AccountSymmetricKeys_AccountId_KeyUsedFor_Version",
                table: "AccountSymmetricKeys"
            );

            migrationBuilder.DropIndex(
                name: "IX_AccountAsymmetricKeys_AccountId_KeyUsedFor_Version",
                table: "AccountAsymmetricKeys"
            );
        }
    }
}
