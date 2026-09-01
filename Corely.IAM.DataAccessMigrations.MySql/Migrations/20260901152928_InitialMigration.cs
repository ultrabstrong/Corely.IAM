using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.MySql.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase().Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "Accounts",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "char(36)", nullable: false),
                        AccountName = table.Column<string>(
                            type: "varchar(100)",
                            maxLength: 100,
                            nullable: false
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
                        table.PrimaryKey("PK_Accounts", x => x.Id);
                    }
                )
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "Users",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "char(36)", nullable: false),
                        Username = table.Column<string>(
                            type: "varchar(30)",
                            maxLength: 30,
                            nullable: false
                        ),
                        Email = table.Column<string>(
                            type: "varchar(254)",
                            maxLength: 254,
                            nullable: false
                        ),
                        LockedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
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
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "AccountAsymmetricKeys",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "char(36)", nullable: false),
                        AccountId = table.Column<Guid>(type: "char(36)", nullable: false),
                        KeyUsedFor = table.Column<string>(type: "varchar(255)", nullable: false),
                        ProviderName = table.Column<string>(type: "longtext", nullable: false),
                        Version = table.Column<int>(type: "int", nullable: false),
                        PublicKey = table.Column<string>(type: "longtext", nullable: false),
                        EncryptedPrivateKey = table.Column<string>(
                            type: "longtext",
                            nullable: false
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
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "AccountSymmetricKeys",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "char(36)", nullable: false),
                        AccountId = table.Column<Guid>(type: "char(36)", nullable: false),
                        KeyUsedFor = table.Column<string>(type: "varchar(255)", nullable: false),
                        ProviderName = table.Column<string>(type: "longtext", nullable: false),
                        Version = table.Column<int>(type: "int", nullable: false),
                        EncryptedKey = table.Column<string>(
                            type: "varchar(256)",
                            maxLength: 256,
                            nullable: false
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
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "Groups",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "char(36)", nullable: false),
                        Name = table.Column<string>(
                            type: "varchar(100)",
                            maxLength: 100,
                            nullable: false
                        ),
                        Description = table.Column<string>(type: "longtext", nullable: true),
                        AccountId = table.Column<Guid>(type: "char(36)", nullable: false),
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
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "Invitations",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "char(36)", nullable: false),
                        AccountId = table.Column<Guid>(type: "char(36)", nullable: false),
                        Token = table.Column<string>(
                            type: "varchar(64)",
                            maxLength: 64,
                            nullable: false
                        ),
                        CreatedByUserId = table.Column<Guid>(type: "char(36)", nullable: false),
                        Email = table.Column<string>(
                            type: "varchar(254)",
                            maxLength: 254,
                            nullable: false
                        ),
                        Description = table.Column<string>(
                            type: "varchar(200)",
                            maxLength: 200,
                            nullable: true
                        ),
                        ExpiresUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                        AcceptedByUserId = table.Column<Guid>(type: "char(36)", nullable: true),
                        AcceptedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                        RevokedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                        CreatedUtc = table.Column<DateTime>(
                            type: "TIMESTAMP",
                            nullable: false,
                            defaultValueSql: "(UTC_TIMESTAMP)"
                        ),
                        ModifiedUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Invitations", x => x.Id);
                        table.ForeignKey(
                            name: "FK_Invitations_Accounts_AccountId",
                            column: x => x.AccountId,
                            principalTable: "Accounts",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "Permissions",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "char(36)", nullable: false),
                        Description = table.Column<string>(type: "longtext", nullable: true),
                        AccountId = table.Column<Guid>(type: "char(36)", nullable: false),
                        ResourceType = table.Column<string>(type: "varchar(255)", nullable: false),
                        ResourceId = table.Column<Guid>(type: "char(36)", nullable: false),
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
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "Roles",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "char(36)", nullable: false),
                        Name = table.Column<string>(
                            type: "varchar(100)",
                            maxLength: 100,
                            nullable: false
                        ),
                        Description = table.Column<string>(type: "longtext", nullable: true),
                        IsSystemDefined = table.Column<bool>(type: "tinyint(1)", nullable: false),
                        AccountId = table.Column<Guid>(type: "char(36)", nullable: false),
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
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "BasicAuths",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "char(36)", nullable: false),
                        UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                        Password = table.Column<string>(
                            type: "varchar(250)",
                            maxLength: 250,
                            nullable: false
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
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "GoogleAuths",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "char(36)", nullable: false),
                        UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                        GoogleSubjectId = table.Column<string>(
                            type: "varchar(255)",
                            maxLength: 255,
                            nullable: false
                        ),
                        Email = table.Column<string>(
                            type: "varchar(254)",
                            maxLength: 254,
                            nullable: false
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
                        table.PrimaryKey("PK_GoogleAuths", x => x.Id);
                        table.ForeignKey(
                            name: "FK_GoogleAuths_Users_UserId",
                            column: x => x.UserId,
                            principalTable: "Users",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "MfaChallenges",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "char(36)", nullable: false),
                        UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                        ChallengeToken = table.Column<string>(
                            type: "varchar(128)",
                            maxLength: 128,
                            nullable: false
                        ),
                        DeviceId = table.Column<string>(
                            type: "varchar(100)",
                            maxLength: 100,
                            nullable: false
                        ),
                        AccountId = table.Column<Guid>(type: "char(36)", nullable: true),
                        ExpiresUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                        CompletedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                        FailedAttempts = table.Column<int>(
                            type: "int",
                            nullable: false,
                            defaultValue: 0
                        ),
                        CreatedUtc = table.Column<DateTime>(
                            type: "TIMESTAMP",
                            nullable: false,
                            defaultValueSql: "(UTC_TIMESTAMP)"
                        ),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_MfaChallenges", x => x.Id);
                        table.ForeignKey(
                            name: "FK_MfaChallenges_Users_UserId",
                            column: x => x.UserId,
                            principalTable: "Users",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "PasswordRecoverys",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "char(36)", nullable: false),
                        UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                        SecretHash = table.Column<string>(
                            type: "varchar(250)",
                            maxLength: 250,
                            nullable: false
                        ),
                        ExpiresUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                        CompletedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                        InvalidatedUtc = table.Column<DateTime>(
                            type: "datetime(6)",
                            nullable: true
                        ),
                        CreatedUtc = table.Column<DateTime>(
                            type: "TIMESTAMP",
                            nullable: false,
                            defaultValueSql: "(UTC_TIMESTAMP)"
                        ),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_PasswordRecoverys", x => x.Id);
                        table.ForeignKey(
                            name: "FK_PasswordRecoverys_Users_UserId",
                            column: x => x.UserId,
                            principalTable: "Users",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "TotpAuths",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "char(36)", nullable: false),
                        UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                        EncryptedSecret = table.Column<string>(
                            type: "varchar(500)",
                            maxLength: 500,
                            nullable: false
                        ),
                        IsEnabled = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false,
                            defaultValue: false
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
                        table.PrimaryKey("PK_TotpAuths", x => x.Id);
                        table.ForeignKey(
                            name: "FK_TotpAuths_Users_UserId",
                            column: x => x.UserId,
                            principalTable: "Users",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "UserAccounts",
                    columns: table => new
                    {
                        UsersId = table.Column<Guid>(type: "char(36)", nullable: false),
                        AccountsId = table.Column<Guid>(type: "char(36)", nullable: false),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_UserAccounts", x => new { x.AccountsId, x.UsersId });
                        table.ForeignKey(
                            name: "FK_UserAccounts_Accounts_AccountsId",
                            column: x => x.AccountsId,
                            principalTable: "Accounts",
                            principalColumn: "Id"
                        );
                        table.ForeignKey(
                            name: "FK_UserAccounts_Users_UsersId",
                            column: x => x.UsersId,
                            principalTable: "Users",
                            principalColumn: "Id"
                        );
                    }
                )
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "UserAsymmetricKeys",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "char(36)", nullable: false),
                        UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                        KeyUsedFor = table.Column<string>(type: "varchar(255)", nullable: false),
                        ProviderName = table.Column<string>(type: "longtext", nullable: false),
                        Version = table.Column<int>(type: "int", nullable: false),
                        PublicKey = table.Column<string>(type: "longtext", nullable: false),
                        EncryptedPrivateKey = table.Column<string>(
                            type: "longtext",
                            nullable: false
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
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "UserAuthTokens",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "char(36)", nullable: false),
                        UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                        AccountId = table.Column<Guid>(type: "char(36)", nullable: true),
                        DeviceId = table.Column<string>(type: "longtext", nullable: false),
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
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "UserSymmetricKeys",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "char(36)", nullable: false),
                        UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                        KeyUsedFor = table.Column<string>(type: "varchar(255)", nullable: false),
                        ProviderName = table.Column<string>(type: "longtext", nullable: false),
                        Version = table.Column<int>(type: "int", nullable: false),
                        EncryptedKey = table.Column<string>(
                            type: "varchar(256)",
                            maxLength: 256,
                            nullable: false
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
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "UserGroups",
                    columns: table => new
                    {
                        UsersId = table.Column<Guid>(type: "char(36)", nullable: false),
                        GroupsId = table.Column<Guid>(type: "char(36)", nullable: false),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_UserGroups", x => new { x.GroupsId, x.UsersId });
                        table.ForeignKey(
                            name: "FK_UserGroups_Groups_GroupsId",
                            column: x => x.GroupsId,
                            principalTable: "Groups",
                            principalColumn: "Id"
                        );
                        table.ForeignKey(
                            name: "FK_UserGroups_Users_UsersId",
                            column: x => x.UsersId,
                            principalTable: "Users",
                            principalColumn: "Id"
                        );
                    }
                )
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "GroupRoles",
                    columns: table => new
                    {
                        GroupsId = table.Column<Guid>(type: "char(36)", nullable: false),
                        RolesId = table.Column<Guid>(type: "char(36)", nullable: false),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_GroupRoles", x => new { x.GroupsId, x.RolesId });
                        table.ForeignKey(
                            name: "FK_GroupRoles_Groups_GroupsId",
                            column: x => x.GroupsId,
                            principalTable: "Groups",
                            principalColumn: "Id"
                        );
                        table.ForeignKey(
                            name: "FK_GroupRoles_Roles_RolesId",
                            column: x => x.RolesId,
                            principalTable: "Roles",
                            principalColumn: "Id"
                        );
                    }
                )
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "RolePermissions",
                    columns: table => new
                    {
                        RolesId = table.Column<Guid>(type: "char(36)", nullable: false),
                        PermissionsId = table.Column<Guid>(type: "char(36)", nullable: false),
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
                            principalColumn: "Id"
                        );
                        table.ForeignKey(
                            name: "FK_RolePermissions_Roles_RolesId",
                            column: x => x.RolesId,
                            principalTable: "Roles",
                            principalColumn: "Id"
                        );
                    }
                )
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "UserRoles",
                    columns: table => new
                    {
                        UsersId = table.Column<Guid>(type: "char(36)", nullable: false),
                        RolesId = table.Column<Guid>(type: "char(36)", nullable: false),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_UserRoles", x => new { x.RolesId, x.UsersId });
                        table.ForeignKey(
                            name: "FK_UserRoles_Roles_RolesId",
                            column: x => x.RolesId,
                            principalTable: "Roles",
                            principalColumn: "Id"
                        );
                        table.ForeignKey(
                            name: "FK_UserRoles_Users_UsersId",
                            column: x => x.UsersId,
                            principalTable: "Users",
                            principalColumn: "Id"
                        );
                    }
                )
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "TotpRecoveryCodes",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "char(36)", nullable: false),
                        TotpAuthId = table.Column<Guid>(type: "char(36)", nullable: false),
                        CodeHash = table.Column<string>(
                            type: "varchar(250)",
                            maxLength: 250,
                            nullable: false
                        ),
                        UsedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                        CreatedUtc = table.Column<DateTime>(
                            type: "TIMESTAMP",
                            nullable: false,
                            defaultValueSql: "(UTC_TIMESTAMP)"
                        ),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_TotpRecoveryCodes", x => x.Id);
                        table.ForeignKey(
                            name: "FK_TotpRecoveryCodes_TotpAuths_TotpAuthId",
                            column: x => x.TotpAuthId,
                            principalTable: "TotpAuths",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySQL:Charset", "utf8mb4");

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
                name: "IX_GoogleAuths_GoogleSubjectId",
                table: "GoogleAuths",
                column: "GoogleSubjectId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAuths_UserId",
                table: "GoogleAuths",
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
                name: "IX_Invitations_AccountId",
                table: "Invitations",
                column: "AccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_Token",
                table: "Invitations",
                column: "Token",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_MfaChallenges_ChallengeToken",
                table: "MfaChallenges",
                column: "ChallengeToken",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_MfaChallenges_ExpiresUtc",
                table: "MfaChallenges",
                column: "ExpiresUtc"
            );

            migrationBuilder.CreateIndex(
                name: "IX_MfaChallenges_UserId",
                table: "MfaChallenges",
                column: "UserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_PasswordRecoverys_ExpiresUtc",
                table: "PasswordRecoverys",
                column: "ExpiresUtc"
            );

            migrationBuilder.CreateIndex(
                name: "IX_PasswordRecoverys_UserId",
                table: "PasswordRecoverys",
                column: "UserId"
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
                name: "IX_TotpAuths_UserId",
                table: "TotpAuths",
                column: "UserId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_TotpRecoveryCodes_TotpAuthId",
                table: "TotpRecoveryCodes",
                column: "TotpAuthId"
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

            migrationBuilder.DropTable(name: "GoogleAuths");

            migrationBuilder.DropTable(name: "GroupRoles");

            migrationBuilder.DropTable(name: "Invitations");

            migrationBuilder.DropTable(name: "MfaChallenges");

            migrationBuilder.DropTable(name: "PasswordRecoverys");

            migrationBuilder.DropTable(name: "RolePermissions");

            migrationBuilder.DropTable(name: "TotpRecoveryCodes");

            migrationBuilder.DropTable(name: "UserAccounts");

            migrationBuilder.DropTable(name: "UserAsymmetricKeys");

            migrationBuilder.DropTable(name: "UserAuthTokens");

            migrationBuilder.DropTable(name: "UserGroups");

            migrationBuilder.DropTable(name: "UserRoles");

            migrationBuilder.DropTable(name: "UserSymmetricKeys");

            migrationBuilder.DropTable(name: "Permissions");

            migrationBuilder.DropTable(name: "TotpAuths");

            migrationBuilder.DropTable(name: "Groups");

            migrationBuilder.DropTable(name: "Roles");

            migrationBuilder.DropTable(name: "Users");

            migrationBuilder.DropTable(name: "Accounts");
        }
    }
}
