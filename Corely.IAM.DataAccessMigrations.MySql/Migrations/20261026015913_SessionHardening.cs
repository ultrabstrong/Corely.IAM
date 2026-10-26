using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.MySql.Migrations
{
    /// <inheritdoc />
    public partial class SessionHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RefreshExpiresUtc",
                table: "UserAuthTokens",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)
            );

            migrationBuilder.AddColumn<string>(
                name: "RefreshSecretHash",
                table: "UserAuthTokens",
                type: "varchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<Guid>(
                name: "ReplacedByTokenId",
                table: "UserAuthTokens",
                type: "char(36)",
                nullable: true
            );

            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                table: "UserAuthTokens",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000")
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "SessionStartedUtc",
                table: "UserAuthTokens",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)
            );

            migrationBuilder.AddColumn<int>(
                name: "AuthSessionIdleTimeoutSeconds",
                table: "PlatformSecuritySettings",
                type: "int",
                nullable: false,
                defaultValue: 86400
            );

            migrationBuilder.AddColumn<int>(
                name: "RefreshReuseGraceSeconds",
                table: "PlatformSecuritySettings",
                type: "int",
                nullable: false,
                defaultValue: 30
            );

            migrationBuilder.Sql(
                "UPDATE UserAuthTokens SET RefreshExpiresUtc = ExpiresUtc, SessionStartedUtc = IssuedUtc, SessionId = Id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthTokens_RefreshExpiresUtc",
                table: "UserAuthTokens",
                column: "RefreshExpiresUtc"
            );

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthTokens_SessionId",
                table: "UserAuthTokens",
                column: "SessionId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserAuthTokens_RefreshExpiresUtc",
                table: "UserAuthTokens"
            );

            migrationBuilder.DropIndex(
                name: "IX_UserAuthTokens_SessionId",
                table: "UserAuthTokens"
            );

            migrationBuilder.DropColumn(name: "RefreshExpiresUtc", table: "UserAuthTokens");

            migrationBuilder.DropColumn(name: "RefreshSecretHash", table: "UserAuthTokens");

            migrationBuilder.DropColumn(name: "ReplacedByTokenId", table: "UserAuthTokens");

            migrationBuilder.DropColumn(name: "SessionId", table: "UserAuthTokens");

            migrationBuilder.DropColumn(name: "SessionStartedUtc", table: "UserAuthTokens");

            migrationBuilder.DropColumn(
                name: "AuthSessionIdleTimeoutSeconds",
                table: "PlatformSecuritySettings"
            );

            migrationBuilder.DropColumn(
                name: "RefreshReuseGraceSeconds",
                table: "PlatformSecuritySettings"
            );
        }
    }
}
