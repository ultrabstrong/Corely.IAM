using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.MariaDb.Migrations
{
    /// <inheritdoc />
    public partial class RenameProviderTypeCodeToProviderName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProviderTypeCode",
                table: "UserSymmetricKeys",
                newName: "ProviderName");

            migrationBuilder.RenameColumn(
                name: "ProviderTypeCode",
                table: "UserAsymmetricKeys",
                newName: "ProviderName");

            migrationBuilder.RenameColumn(
                name: "ProviderTypeCode",
                table: "AccountSymmetricKeys",
                newName: "ProviderName");

            migrationBuilder.RenameColumn(
                name: "ProviderTypeCode",
                table: "AccountAsymmetricKeys",
                newName: "ProviderName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProviderName",
                table: "UserSymmetricKeys",
                newName: "ProviderTypeCode");

            migrationBuilder.RenameColumn(
                name: "ProviderName",
                table: "UserAsymmetricKeys",
                newName: "ProviderTypeCode");

            migrationBuilder.RenameColumn(
                name: "ProviderName",
                table: "AccountSymmetricKeys",
                newName: "ProviderTypeCode");

            migrationBuilder.RenameColumn(
                name: "ProviderName",
                table: "AccountAsymmetricKeys",
                newName: "ProviderTypeCode");
        }
    }
}
