using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.MariaDb.Migrations
{
    /// <inheritdoc />
    public partial class AddMfaAndGoogleAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoogleAuths",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    GoogleSubjectId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(254)", maxLength: 254, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: false, defaultValueSql: "(UTC_TIMESTAMP)"),
                    ModifiedUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleAuths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoogleAuths_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MfaChallenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ChallengeToken = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AccountId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ExpiresUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FailedAttempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: false, defaultValueSql: "(UTC_TIMESTAMP)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MfaChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MfaChallenges_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TotpAuths",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EncryptedSecret = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    CreatedUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: false, defaultValueSql: "(UTC_TIMESTAMP)"),
                    ModifiedUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TotpAuths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TotpAuths_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TotpRecoveryCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TotpAuthId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CodeHash = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UsedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TIMESTAMP", nullable: false, defaultValueSql: "(UTC_TIMESTAMP)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TotpRecoveryCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TotpRecoveryCodes_TotpAuths_TotpAuthId",
                        column: x => x.TotpAuthId,
                        principalTable: "TotpAuths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAuths_GoogleSubjectId",
                table: "GoogleAuths",
                column: "GoogleSubjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAuths_UserId",
                table: "GoogleAuths",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MfaChallenges_ChallengeToken",
                table: "MfaChallenges",
                column: "ChallengeToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MfaChallenges_ExpiresUtc",
                table: "MfaChallenges",
                column: "ExpiresUtc");

            migrationBuilder.CreateIndex(
                name: "IX_MfaChallenges_UserId",
                table: "MfaChallenges",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TotpAuths_UserId",
                table: "TotpAuths",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TotpRecoveryCodes_TotpAuthId",
                table: "TotpRecoveryCodes",
                column: "TotpAuthId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoogleAuths");

            migrationBuilder.DropTable(
                name: "MfaChallenges");

            migrationBuilder.DropTable(
                name: "TotpRecoveryCodes");

            migrationBuilder.DropTable(
                name: "TotpAuths");
        }
    }
}
