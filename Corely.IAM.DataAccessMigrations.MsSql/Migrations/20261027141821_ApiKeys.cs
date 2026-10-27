using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.MsSql.Migrations
{
    /// <inheritdoc />
    public partial class ApiKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApiKeyMaxLifetimeDays",
                table: "PlatformSecuritySettings",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<string>(
                name: "ApiKeyResourceTypes",
                table: "PlatformSecuritySettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<bool>(
                name: "ApiKeysEnabled",
                table: "PlatformSecuritySettings",
                type: "bit",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<bool>(
                name: "PlatformAccountApiKeysEnabled",
                table: "PlatformSecuritySettings",
                type: "bit",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<Guid>(
                name: "ActorApiKeyId",
                table: "AuditEntries",
                type: "uniqueidentifier",
                nullable: true
            );

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    Description = table.Column<string>(
                        type: "nvarchar(500)",
                        maxLength: 500,
                        nullable: true
                    ),
                    LockedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalSuccessfulLogins = table.Column<int>(type: "int", nullable: false),
                    LastSuccessfulLoginUtc = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: true
                    ),
                    FailedLoginsSinceLastSuccess = table.Column<int>(type: "int", nullable: false),
                    TotalFailedLogins = table.Column<int>(type: "int", nullable: false),
                    LastFailedLoginUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUtc = table.Column<DateTime>(
                        type: "DATETIME2",
                        nullable: false,
                        defaultValueSql: "(SYSUTCDATETIME())"
                    ),
                    ModifiedUtc = table.Column<DateTime>(type: "DATETIME2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiKeys_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "ApiKeyGroups",
                columns: table => new
                {
                    ApiKeysId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeyGroups", x => new { x.ApiKeysId, x.GroupsId });
                    table.ForeignKey(
                        name: "FK_ApiKeyGroups_ApiKeys_ApiKeysId",
                        column: x => x.ApiKeysId,
                        principalTable: "ApiKeys",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_ApiKeyGroups_Groups_GroupsId",
                        column: x => x.GroupsId,
                        principalTable: "Groups",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "ApiKeyRoles",
                columns: table => new
                {
                    ApiKeysId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RolesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeyRoles", x => new { x.ApiKeysId, x.RolesId });
                    table.ForeignKey(
                        name: "FK_ApiKeyRoles_ApiKeys_ApiKeysId",
                        column: x => x.ApiKeysId,
                        principalTable: "ApiKeys",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_ApiKeyRoles_Roles_RolesId",
                        column: x => x.RolesId,
                        principalTable: "Roles",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "ApiKeySecrets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApiKeyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slot = table.Column<int>(type: "int", nullable: false),
                    Prefix = table.Column<string>(
                        type: "nvarchar(8)",
                        maxLength: 8,
                        nullable: false
                    ),
                    SecretHash = table.Column<string>(
                        type: "nvarchar(250)",
                        maxLength: 250,
                        nullable: false
                    ),
                    ExpiresUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUsedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUtc = table.Column<DateTime>(
                        type: "DATETIME2",
                        nullable: false,
                        defaultValueSql: "(SYSUTCDATETIME())"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeySecrets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiKeySecrets_ApiKeys_ApiKeyId",
                        column: x => x.ApiKeyId,
                        principalTable: "ApiKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.DropForeignKey(name: "FK_UserAuthTokens_Users_UserId", table: "UserAuthTokens");

            migrationBuilder.DropPrimaryKey(name: "PK_UserAuthTokens", table: "UserAuthTokens");

            migrationBuilder.RenameTable(name: "UserAuthTokens", newName: "AuthTokens");

            migrationBuilder.RenameIndex(name: "IX_UserAuthTokens_ExpiresUtc", table: "AuthTokens", newName: "IX_AuthTokens_ExpiresUtc");

            migrationBuilder.RenameIndex(name: "IX_UserAuthTokens_RefreshExpiresUtc", table: "AuthTokens", newName: "IX_AuthTokens_RefreshExpiresUtc");

            migrationBuilder.RenameIndex(name: "IX_UserAuthTokens_SessionId", table: "AuthTokens", newName: "IX_AuthTokens_SessionId");

            migrationBuilder.RenameIndex(name: "IX_UserAuthTokens_UserId", table: "AuthTokens", newName: "IX_AuthTokens_UserId");

            migrationBuilder.AlterColumn<Guid>(name: "UserId", table: "AuthTokens", type: "uniqueidentifier", nullable: true, oldClrType: typeof(Guid), oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(name: "ApiKeyId", table: "AuthTokens", type: "uniqueidentifier", nullable: true);

            migrationBuilder.AddColumn<int>(name: "Slot", table: "AuthTokens", type: "int", nullable: true);

            migrationBuilder.AddPrimaryKey(name: "PK_AuthTokens", table: "AuthTokens", column: "Id");

            migrationBuilder.AddForeignKey(name: "FK_AuthTokens_Users_UserId", table: "AuthTokens", column: "UserId", principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(name: "FK_AuthTokens_ApiKeys_ApiKeyId", table: "AuthTokens", column: "ApiKeyId", principalTable: "ApiKeys", principalColumn: "Id", onDelete: ReferentialAction.Cascade);

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeyGroups_GroupsId",
                table: "ApiKeyGroups",
                column: "GroupsId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeyRoles_RolesId",
                table: "ApiKeyRoles",
                column: "RolesId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_AccountId_Name",
                table: "ApiKeys",
                columns: new[] { "AccountId", "Name" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeySecrets_ApiKeyId_Slot",
                table: "ApiKeySecrets",
                columns: new[] { "ApiKeyId", "Slot" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeySecrets_Prefix",
                table: "ApiKeySecrets",
                column: "Prefix",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuthTokens_ApiKeyId",
                table: "AuthTokens",
                column: "ApiKeyId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ApiKeyGroups");

            migrationBuilder.DropTable(name: "ApiKeyRoles");

            migrationBuilder.DropTable(name: "ApiKeySecrets");

            migrationBuilder.DropForeignKey(name: "FK_AuthTokens_ApiKeys_ApiKeyId", table: "AuthTokens");

            migrationBuilder.DropForeignKey(name: "FK_AuthTokens_Users_UserId", table: "AuthTokens");

            migrationBuilder.DropIndex(name: "IX_AuthTokens_ApiKeyId", table: "AuthTokens");

            migrationBuilder.Sql("DELETE FROM AuthTokens WHERE UserId IS NULL");

            migrationBuilder.DropColumn(name: "ApiKeyId", table: "AuthTokens");

            migrationBuilder.DropColumn(name: "Slot", table: "AuthTokens");

            migrationBuilder.DropIndex(name: "IX_AuthTokens_UserId", table: "AuthTokens");

            migrationBuilder.AlterColumn<Guid>(name: "UserId", table: "AuthTokens", type: "uniqueidentifier", nullable: false, oldClrType: typeof(Guid), oldType: "uniqueidentifier", oldNullable: true);

            migrationBuilder.DropPrimaryKey(name: "PK_AuthTokens", table: "AuthTokens");

            migrationBuilder.RenameTable(name: "AuthTokens", newName: "UserAuthTokens");

            migrationBuilder.RenameIndex(name: "IX_AuthTokens_ExpiresUtc", table: "UserAuthTokens", newName: "IX_UserAuthTokens_ExpiresUtc");

            migrationBuilder.RenameIndex(name: "IX_AuthTokens_RefreshExpiresUtc", table: "UserAuthTokens", newName: "IX_UserAuthTokens_RefreshExpiresUtc");

            migrationBuilder.RenameIndex(name: "IX_AuthTokens_SessionId", table: "UserAuthTokens", newName: "IX_UserAuthTokens_SessionId");

            migrationBuilder.CreateIndex(name: "IX_UserAuthTokens_UserId", table: "UserAuthTokens", column: "UserId");

            migrationBuilder.AddPrimaryKey(name: "PK_UserAuthTokens", table: "UserAuthTokens", column: "Id");

            migrationBuilder.AddForeignKey(name: "FK_UserAuthTokens_Users_UserId", table: "UserAuthTokens", column: "UserId", principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropTable(name: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "ApiKeyMaxLifetimeDays",
                table: "PlatformSecuritySettings"
            );

            migrationBuilder.DropColumn(
                name: "ApiKeyResourceTypes",
                table: "PlatformSecuritySettings"
            );

            migrationBuilder.DropColumn(name: "ApiKeysEnabled", table: "PlatformSecuritySettings");

            migrationBuilder.DropColumn(
                name: "PlatformAccountApiKeysEnabled",
                table: "PlatformSecuritySettings"
            );

            migrationBuilder.DropColumn(name: "ActorApiKeyId", table: "AuditEntries");        }
    }
}
