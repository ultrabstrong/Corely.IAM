using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.MariaDb.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase().Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "Accounts",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        AccountName = table
                            .Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        CreatedUtc = table.Column<DateTime>(
                            type: "TIMESTAMP",
                            nullable: false,
                            defaultValueSql: "(UTC_TIMESTAMP)"
                        ),
                        ModifiedUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Accounts", x => x.Id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "Users",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        Username = table
                            .Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        Email = table
                            .Column<string>(type: "varchar(254)", maxLength: 254, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        Disabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                        TotalSuccessfulLogins = table.Column<int>(type: "int", nullable: false),
                        LastSuccessfulLoginUtc = table.Column<DateTime>(
                            type: "datetime(6)",
                            nullable: true
                        ),
                        FailedLoginsSinceLastSuccess = table.Column<int>(
                            type: "int",
                            nullable: false
                        ),
                        TotalFailedLogins = table.Column<int>(type: "int", nullable: false),
                        LastFailedLoginUtc = table.Column<DateTime>(
                            type: "datetime(6)",
                            nullable: true
                        ),
                        CreatedUtc = table.Column<DateTime>(
                            type: "TIMESTAMP",
                            nullable: false,
                            defaultValueSql: "(UTC_TIMESTAMP)"
                        ),
                        ModifiedUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Users", x => x.Id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "AccountAsymmetricKeys",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        AccountId = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        KeyUsedFor = table
                            .Column<string>(type: "varchar(255)", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        ProviderTypeCode = table
                            .Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        Version = table.Column<int>(type: "int", nullable: false),
                        PublicKey = table
                            .Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        EncryptedPrivateKey = table
                            .Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        CreatedUtc = table.Column<DateTime>(
                            type: "TIMESTAMP",
                            nullable: false,
                            defaultValueSql: "(UTC_TIMESTAMP)"
                        ),
                        ModifiedUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
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
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "AccountSymmetricKeys",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        AccountId = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        KeyUsedFor = table
                            .Column<string>(type: "varchar(255)", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        ProviderTypeCode = table
                            .Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        Version = table.Column<int>(type: "int", nullable: false),
                        EncryptedKey = table
                            .Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        CreatedUtc = table.Column<DateTime>(
                            type: "TIMESTAMP",
                            nullable: false,
                            defaultValueSql: "(UTC_TIMESTAMP)"
                        ),
                        ModifiedUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
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
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "Groups",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        Name = table
                            .Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        Description = table
                            .Column<string>(type: "longtext", nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        AccountId = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        CreatedUtc = table.Column<DateTime>(
                            type: "TIMESTAMP",
                            nullable: false,
                            defaultValueSql: "(UTC_TIMESTAMP)"
                        ),
                        ModifiedUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
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
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "Permissions",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        Description = table
                            .Column<string>(type: "longtext", nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        AccountId = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        ResourceType = table
                            .Column<string>(type: "varchar(255)", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        ResourceId = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        Create = table.Column<bool>(type: "tinyint(1)", nullable: false),
                        Read = table.Column<bool>(type: "tinyint(1)", nullable: false),
                        Update = table.Column<bool>(type: "tinyint(1)", nullable: false),
                        Delete = table.Column<bool>(type: "tinyint(1)", nullable: false),
                        Execute = table.Column<bool>(type: "tinyint(1)", nullable: false),
                        IsSystemDefined = table.Column<bool>(type: "tinyint(1)", nullable: false),
                        CreatedUtc = table.Column<DateTime>(
                            type: "TIMESTAMP",
                            nullable: false,
                            defaultValueSql: "(UTC_TIMESTAMP)"
                        ),
                        ModifiedUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
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
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "Roles",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        Name = table
                            .Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        Description = table
                            .Column<string>(type: "longtext", nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        IsSystemDefined = table.Column<bool>(type: "tinyint(1)", nullable: false),
                        AccountId = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        CreatedUtc = table.Column<DateTime>(
                            type: "TIMESTAMP",
                            nullable: false,
                            defaultValueSql: "(UTC_TIMESTAMP)"
                        ),
                        ModifiedUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
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
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "BasicAuths",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        UserId = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        Password = table
                            .Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        CreatedUtc = table.Column<DateTime>(
                            type: "TIMESTAMP",
                            nullable: false,
                            defaultValueSql: "(UTC_TIMESTAMP)"
                        ),
                        ModifiedUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
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
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "UserAccounts",
                    columns: table => new
                    {
                        AccountsId = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        UsersId = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
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
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "UserAsymmetricKeys",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        UserId = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        KeyUsedFor = table
                            .Column<string>(type: "varchar(255)", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        ProviderTypeCode = table
                            .Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        Version = table.Column<int>(type: "int", nullable: false),
                        PublicKey = table
                            .Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        EncryptedPrivateKey = table
                            .Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        CreatedUtc = table.Column<DateTime>(
                            type: "TIMESTAMP",
                            nullable: false,
                            defaultValueSql: "(UTC_TIMESTAMP)"
                        ),
                        ModifiedUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
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
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "UserAuthTokens",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        UserId = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        AccountId = table.Column<Guid>(
                            type: "char(36)",
                            nullable: true,
                            collation: "ascii_general_ci"
                        ),
                        DeviceId = table
                            .Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        IssuedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                        ExpiresUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                        RevokedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                        CreatedUtc = table.Column<DateTime>(
                            type: "TIMESTAMP",
                            nullable: false,
                            defaultValueSql: "(UTC_TIMESTAMP)"
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
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "UserSymmetricKeys",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        UserId = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        KeyUsedFor = table
                            .Column<string>(type: "varchar(255)", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        ProviderTypeCode = table
                            .Column<string>(type: "longtext", nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        Version = table.Column<int>(type: "int", nullable: false),
                        EncryptedKey = table
                            .Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        CreatedUtc = table.Column<DateTime>(
                            type: "TIMESTAMP",
                            nullable: false,
                            defaultValueSql: "(UTC_TIMESTAMP)"
                        ),
                        ModifiedUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
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
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "UserGroups",
                    columns: table => new
                    {
                        GroupsId = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        UsersId = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
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
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "GroupRoles",
                    columns: table => new
                    {
                        GroupsId = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        RolesId = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
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
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "RolePermissions",
                    columns: table => new
                    {
                        PermissionsId = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        RolesId = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey(
                            "PK_RolePermissions",
                            x => new { x.PermissionsId, x.RolesId }
                        );
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
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "UserRoles",
                    columns: table => new
                    {
                        RolesId = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        UsersId = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
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
                )
                .Annotation("MySql:CharSet", "utf8mb4");

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
                name: "IX_Permissions_AccountId_ResourceType_ResourceId_Create_Read_Up~",
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
