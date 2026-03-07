using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.MariaDb.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .CreateTable(
                    name: "Invitations",
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
                        Token = table
                            .Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        CreatedByUserId = table.Column<Guid>(
                            type: "char(36)",
                            nullable: false,
                            collation: "ascii_general_ci"
                        ),
                        Email = table
                            .Column<string>(type: "varchar(254)", maxLength: 254, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        Description = table
                            .Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        ExpiresUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                        AcceptedByUserId = table.Column<Guid>(
                            type: "char(36)",
                            nullable: true,
                            collation: "ascii_general_ci"
                        ),
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
                .Annotation("MySql:CharSet", "utf8mb4");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Invitations");
        }
    }
}
