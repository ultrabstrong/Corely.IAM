using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.MsSql.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountAuditSettings",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountMemberActions = table.Column<int>(type: "int", nullable: false),
                    PlatformMemberActions = table.Column<int>(type: "int", nullable: false),
                    RetentionDays = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(
                        type: "DATETIME2",
                        nullable: false,
                        defaultValueSql: "(SYSUTCDATETIME())"
                    ),
                    ModifiedUtc = table.Column<DateTime>(type: "DATETIME2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountAuditSettings", x => x.AccountId);
                    table.ForeignKey(
                        name: "FK_AccountAuditSettings_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Cohort = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Source = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    Service = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    Operation = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    Action = table.Column<int>(type: "int", nullable: false),
                    ResourceType = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    ResourceIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultCode = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    Details = table.Column<string>(
                        type: "nvarchar(500)",
                        maxLength: 500,
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "PlatformSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    AuditEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AuditMaxRetentionDays = table.Column<int>(type: "int", nullable: false),
                    AuditAllowedActions = table.Column<int>(type: "int", nullable: false),
                    SystemContextActions = table.Column<int>(type: "int", nullable: false),
                    AccountlessActions = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(
                        type: "DATETIME2",
                        nullable: false,
                        defaultValueSql: "(SYSUTCDATETIME())"
                    ),
                    ModifiedUtc = table.Column<DateTime>(type: "DATETIME2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformSettings", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_AccountId_OccurredUtc",
                table: "AuditEntries",
                columns: new[] { "AccountId", "OccurredUtc" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_ActorUserId_OccurredUtc",
                table: "AuditEntries",
                columns: new[] { "ActorUserId", "OccurredUtc" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AccountAuditSettings");

            migrationBuilder.DropTable(name: "AuditEntries");

            migrationBuilder.DropTable(name: "PlatformSettings");
        }
    }
}
