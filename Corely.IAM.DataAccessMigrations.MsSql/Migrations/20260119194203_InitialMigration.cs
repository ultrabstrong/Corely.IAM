using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.MsSql.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountName = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    CreatedUtc = table.Column<DateTime>(
                        type: "DATETIME2",
                        nullable: false,
                        defaultValueSql: "(SYSUTCDATETIME())"
                    ),
                    ModifiedUtc = table.Column<DateTime>(type: "DATETIME2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(
                        type: "nvarchar(30)",
                        maxLength: 30,
                        nullable: false
                    ),
                    Email = table.Column<string>(
                        type: "nvarchar(254)",
                        maxLength: 254,
                        nullable: false
                    ),
                    Disabled = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Users", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "AccountAsymmetricKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KeyUsedFor = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderTypeCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    PublicKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EncryptedPrivateKey = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false
                    ),
                    CreatedUtc = table.Column<DateTime>(
                        type: "DATETIME2",
                        nullable: false,
                        defaultValueSql: "(SYSUTCDATETIME())"
                    ),
                    ModifiedUtc = table.Column<DateTime>(type: "DATETIME2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountAsymmetricKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountAsymmetricKeys_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "AccountSymmetricKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KeyUsedFor = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderTypeCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    EncryptedKey = table.Column<string>(
                        type: "nvarchar(256)",
                        maxLength: 256,
                        nullable: false
                    ),
                    CreatedUtc = table.Column<DateTime>(
                        type: "DATETIME2",
                        nullable: false,
                        defaultValueSql: "(SYSUTCDATETIME())"
                    ),
                    ModifiedUtc = table.Column<DateTime>(type: "DATETIME2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountSymmetricKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountSymmetricKeys_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedUtc = table.Column<DateTime>(
                        type: "DATETIME2",
                        nullable: false,
                        defaultValueSql: "(SYSUTCDATETIME())"
                    ),
                    ModifiedUtc = table.Column<DateTime>(type: "DATETIME2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Groups_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Create = table.Column<bool>(type: "bit", nullable: false),
                    Read = table.Column<bool>(type: "bit", nullable: false),
                    Update = table.Column<bool>(type: "bit", nullable: false),
                    Delete = table.Column<bool>(type: "bit", nullable: false),
                    Execute = table.Column<bool>(type: "bit", nullable: false),
                    IsSystemDefined = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(
                        type: "DATETIME2",
                        nullable: false,
                        defaultValueSql: "(SYSUTCDATETIME())"
                    ),
                    ModifiedUtc = table.Column<DateTime>(type: "DATETIME2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permissions_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSystemDefined = table.Column<bool>(type: "bit", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedUtc = table.Column<DateTime>(
                        type: "DATETIME2",
                        nullable: false,
                        defaultValueSql: "(SYSUTCDATETIME())"
                    ),
                    ModifiedUtc = table.Column<DateTime>(type: "DATETIME2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roles_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "BasicAuths",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Password = table.Column<string>(
                        type: "nvarchar(250)",
                        maxLength: 250,
                        nullable: false
                    ),
                    CreatedUtc = table.Column<DateTime>(
                        type: "DATETIME2",
                        nullable: false,
                        defaultValueSql: "(SYSUTCDATETIME())"
                    ),
                    ModifiedUtc = table.Column<DateTime>(type: "DATETIME2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BasicAuths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BasicAuths_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "UserAccounts",
                columns: table => new
                {
                    AccountsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsersId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccounts", x => new { x.AccountsId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_UserAccounts_Accounts_AccountsId",
                        column: x => x.AccountsId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_UserAccounts_Users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "UserAsymmetricKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KeyUsedFor = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderTypeCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    PublicKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EncryptedPrivateKey = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false
                    ),
                    CreatedUtc = table.Column<DateTime>(
                        type: "DATETIME2",
                        nullable: false,
                        defaultValueSql: "(SYSUTCDATETIME())"
                    ),
                    ModifiedUtc = table.Column<DateTime>(type: "DATETIME2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAsymmetricKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAsymmetricKeys_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "UserAuthTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IssuedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUtc = table.Column<DateTime>(
                        type: "DATETIME2",
                        nullable: false,
                        defaultValueSql: "(SYSUTCDATETIME())"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAuthTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAuthTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "UserSymmetricKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KeyUsedFor = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderTypeCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    EncryptedKey = table.Column<string>(
                        type: "nvarchar(256)",
                        maxLength: 256,
                        nullable: false
                    ),
                    CreatedUtc = table.Column<DateTime>(
                        type: "DATETIME2",
                        nullable: false,
                        defaultValueSql: "(SYSUTCDATETIME())"
                    ),
                    ModifiedUtc = table.Column<DateTime>(type: "DATETIME2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSymmetricKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSymmetricKeys_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "UserGroups",
                columns: table => new
                {
                    GroupsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsersId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroups", x => new { x.GroupsId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_UserGroups_Groups_GroupsId",
                        column: x => x.GroupsId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_UserGroups_Users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "GroupRoles",
                columns: table => new
                {
                    GroupsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RolesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupRoles", x => new { x.GroupsId, x.RolesId });
                    table.ForeignKey(
                        name: "FK_GroupRoles_Groups_GroupsId",
                        column: x => x.GroupsId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_GroupRoles_Roles_RolesId",
                        column: x => x.RolesId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    PermissionsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RolesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.PermissionsId, x.RolesId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionsId",
                        column: x => x.PermissionsId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RolesId",
                        column: x => x.RolesId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    RolesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsersId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.RolesId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RolesId",
                        column: x => x.RolesId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_AccountAsymmetricKeys_AccountId_KeyUsedFor",
                table: "AccountAsymmetricKeys",
                columns: new[] { "AccountId", "KeyUsedFor" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AccountName",
                table: "Accounts",
                column: "AccountName",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_AccountSymmetricKeys_AccountId_KeyUsedFor",
                table: "AccountSymmetricKeys",
                columns: new[] { "AccountId", "KeyUsedFor" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_BasicAuths_UserId",
                table: "BasicAuths",
                column: "UserId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_GroupRoles_RolesId",
                table: "GroupRoles",
                column: "RolesId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Groups_AccountId_Name",
                table: "Groups",
                columns: new[] { "AccountId", "Name" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_AccountId_ResourceType_ResourceId_Create_Read_Update_Delete_Execute",
                table: "Permissions",
                columns: new[]
                {
                    "AccountId",
                    "ResourceType",
                    "ResourceId",
                    "Create",
                    "Read",
                    "Update",
                    "Delete",
                    "Execute",
                },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RolesId",
                table: "RolePermissions",
                column: "RolesId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Roles_AccountId_Name",
                table: "Roles",
                columns: new[] { "AccountId", "Name" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_UsersId",
                table: "UserAccounts",
                column: "UsersId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_UserAsymmetricKeys_UserId_KeyUsedFor",
                table: "UserAsymmetricKeys",
                columns: new[] { "UserId", "KeyUsedFor" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthTokens_ExpiresUtc",
                table: "UserAuthTokens",
                column: "ExpiresUtc"
            );

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthTokens_UserId",
                table: "UserAuthTokens",
                column: "UserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_UsersId",
                table: "UserGroups",
                column: "UsersId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UsersId",
                table: "UserRoles",
                column: "UsersId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_UserSymmetricKeys_UserId_KeyUsedFor",
                table: "UserSymmetricKeys",
                columns: new[] { "UserId", "KeyUsedFor" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AccountAsymmetricKeys");

            migrationBuilder.DropTable(name: "AccountSymmetricKeys");

            migrationBuilder.DropTable(name: "BasicAuths");

            migrationBuilder.DropTable(name: "GroupRoles");

            migrationBuilder.DropTable(name: "RolePermissions");

            migrationBuilder.DropTable(name: "UserAccounts");

            migrationBuilder.DropTable(name: "UserAsymmetricKeys");

            migrationBuilder.DropTable(name: "UserAuthTokens");

            migrationBuilder.DropTable(name: "UserGroups");

            migrationBuilder.DropTable(name: "UserRoles");

            migrationBuilder.DropTable(name: "UserSymmetricKeys");

            migrationBuilder.DropTable(name: "Permissions");

            migrationBuilder.DropTable(name: "Groups");

            migrationBuilder.DropTable(name: "Roles");

            migrationBuilder.DropTable(name: "Users");

            migrationBuilder.DropTable(name: "Accounts");
        }
    }
}
