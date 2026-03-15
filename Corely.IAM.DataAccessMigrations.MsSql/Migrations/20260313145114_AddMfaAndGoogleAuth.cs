using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.MsSql.Migrations
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoogleSubjectId = table.Column<string>(
                        type: "nvarchar(255)",
                        maxLength: 255,
                        nullable: false
                    ),
                    Email = table.Column<string>(
                        type: "nvarchar(254)",
                        maxLength: 254,
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
                    table.PrimaryKey("PK_GoogleAuths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoogleAuths_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "MfaChallenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChallengeToken = table.Column<string>(
                        type: "nvarchar(128)",
                        maxLength: 128,
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
                    FailedAttempts = table.Column<int>(
                        type: "int",
                        nullable: false,
                        defaultValue: 0
                    ),
                    CreatedUtc = table.Column<DateTime>(
                        type: "DATETIME2",
                        nullable: false,
                        defaultValueSql: "(SYSUTCDATETIME())"
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
            );

            migrationBuilder.CreateTable(
                name: "TotpAuths",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EncryptedSecret = table.Column<string>(
                        type: "nvarchar(500)",
                        maxLength: 500,
                        nullable: false
                    ),
                    IsEnabled = table.Column<bool>(
                        type: "bit",
                        nullable: false,
                        defaultValue: false
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
                    table.PrimaryKey("PK_TotpAuths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TotpAuths_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "TotpRecoveryCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotpAuthId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodeHash = table.Column<string>(
                        type: "nvarchar(250)",
                        maxLength: 250,
                        nullable: false
                    ),
                    UsedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUtc = table.Column<DateTime>(
                        type: "DATETIME2",
                        nullable: false,
                        defaultValueSql: "(SYSUTCDATETIME())"
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "GoogleAuths");

            migrationBuilder.DropTable(name: "MfaChallenges");

            migrationBuilder.DropTable(name: "TotpRecoveryCodes");

            migrationBuilder.DropTable(name: "TotpAuths");
        }
    }
}
