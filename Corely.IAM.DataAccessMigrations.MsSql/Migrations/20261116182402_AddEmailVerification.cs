using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.MsSql.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerifiedUtc",
                table: "Users",
                type: "datetime2",
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "EmailVerificationResendSeconds",
                table: "PlatformSecuritySettings",
                type: "int",
                nullable: false,
                defaultValue: 60
            );

            migrationBuilder.AddColumn<int>(
                name: "EmailVerificationTtlSeconds",
                table: "PlatformSecuritySettings",
                type: "int",
                nullable: false,
                defaultValue: 1800
            );

            migrationBuilder.AddColumn<bool>(
                name: "RequireVerifiedEmail",
                table: "PlatformSecuritySettings",
                type: "bit",
                nullable: false,
                defaultValue: true
            );

            migrationBuilder.Sql("EXEC(N'UPDATE Users SET EmailVerifiedUtc = SYSUTCDATETIME()')");

            migrationBuilder.CreateTable(
                name: "EmailVerificationChallenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecretHash = table.Column<string>(
                        type: "nvarchar(250)",
                        maxLength: 250,
                        nullable: false
                    ),
                    DeviceId = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpiresUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUtc = table.Column<DateTime>(
                        type: "DATETIME2",
                        nullable: false,
                        defaultValueSql: "(SYSUTCDATETIME())"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailVerificationChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailVerificationChallenges_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "EmailVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(
                        type: "nvarchar(254)",
                        maxLength: 254,
                        nullable: false
                    ),
                    SecretHash = table.Column<string>(
                        type: "nvarchar(250)",
                        maxLength: 250,
                        nullable: false
                    ),
                    SentUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InvalidatedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUtc = table.Column<DateTime>(
                        type: "DATETIME2",
                        nullable: false,
                        defaultValueSql: "(SYSUTCDATETIME())"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailVerifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationChallenges_AccountId",
                table: "EmailVerificationChallenges",
                column: "AccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationChallenges_UserId",
                table: "EmailVerificationChallenges",
                column: "UserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerifications_UserId",
                table: "EmailVerifications",
                column: "UserId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "EmailVerificationChallenges");

            migrationBuilder.DropTable(name: "EmailVerifications");

            migrationBuilder.DropColumn(name: "EmailVerifiedUtc", table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationResendSeconds",
                table: "PlatformSecuritySettings"
            );

            migrationBuilder.DropColumn(
                name: "EmailVerificationTtlSeconds",
                table: "PlatformSecuritySettings"
            );

            migrationBuilder.DropColumn(
                name: "RequireVerifiedEmail",
                table: "PlatformSecuritySettings"
            );
        }
    }
}
