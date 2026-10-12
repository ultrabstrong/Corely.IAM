using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformSecuritySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .CreateTable(
                    name: "PlatformSecuritySettings",
                    columns: table => new
                    {
                        Id = table.Column<int>(type: "int", nullable: false),
                        PasswordMinimumLength = table.Column<int>(type: "int", nullable: false),
                        PasswordMaximumLength = table.Column<int>(type: "int", nullable: false),
                        RequireUppercase = table.Column<bool>(type: "tinyint(1)", nullable: false),
                        RequireLowercase = table.Column<bool>(type: "tinyint(1)", nullable: false),
                        RequireDigit = table.Column<bool>(type: "tinyint(1)", nullable: false),
                        RequireNonAlphanumeric = table.Column<bool>(
                            type: "tinyint(1)",
                            nullable: false
                        ),
                        MaxLoginAttempts = table.Column<int>(type: "int", nullable: false),
                        LockoutCooldownSeconds = table.Column<int>(type: "int", nullable: false),
                        AuthTokenTtlSeconds = table.Column<int>(type: "int", nullable: false),
                        AuthSessionTtlSeconds = table.Column<int>(type: "int", nullable: false),
                        MfaChallengeTimeoutSeconds = table.Column<int>(
                            type: "int",
                            nullable: false
                        ),
                        TotpRecoveryCodeCount = table.Column<int>(type: "int", nullable: false),
                        CreatedUtc = table.Column<DateTime>(
                            type: "TIMESTAMP",
                            nullable: false,
                            defaultValueSql: "(UTC_TIMESTAMP)"
                        ),
                        ModifiedUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_PlatformSecuritySettings", x => x.Id);
                    }
                )
                .Annotation("MySQL:Charset", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PlatformSecuritySettings");
        }
    }
}
