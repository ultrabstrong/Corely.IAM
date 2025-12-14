using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.Migrations;

/// <inheritdoc />
public partial class RenameJtiToPublicIdInUserTokens : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "Jti",
            table: "UserAuthTokens",
            newName: "PublicId"
        );

        migrationBuilder.RenameIndex(
            name: "IX_UserAuthTokens_Jti",
            table: "UserAuthTokens",
            newName: "IX_UserAuthTokens_PublicId"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "PublicId",
            table: "UserAuthTokens",
            newName: "Jti"
        );

        migrationBuilder.RenameIndex(
            name: "IX_UserAuthTokens_PublicId",
            table: "UserAuthTokens",
            newName: "IX_UserAuthTokens_Jti"
        );
    }
}
