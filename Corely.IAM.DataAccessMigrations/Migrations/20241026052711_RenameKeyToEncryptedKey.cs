using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.Migrations;

/// <inheritdoc />
public partial class RenameKeyToEncryptedKey : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "Key",
            table: "UserSymmetricKeys",
            newName: "EncryptedKey"
        );

        migrationBuilder.RenameColumn(
            name: "PrivateKey",
            table: "UserAsymmetricKeys",
            newName: "EncryptedPrivateKey"
        );

        migrationBuilder.RenameColumn(
            name: "Key",
            table: "AccountSymmetricKeys",
            newName: "EncryptedKey"
        );

        migrationBuilder.RenameColumn(
            name: "PrivateKey",
            table: "AccountAsymmetricKeys",
            newName: "EncryptedPrivateKey"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "EncryptedKey",
            table: "UserSymmetricKeys",
            newName: "Key"
        );

        migrationBuilder.RenameColumn(
            name: "EncryptedPrivateKey",
            table: "UserAsymmetricKeys",
            newName: "PrivateKey"
        );

        migrationBuilder.RenameColumn(
            name: "EncryptedKey",
            table: "AccountSymmetricKeys",
            newName: "Key"
        );

        migrationBuilder.RenameColumn(
            name: "EncryptedPrivateKey",
            table: "AccountAsymmetricKeys",
            newName: "PrivateKey"
        );
    }
}
