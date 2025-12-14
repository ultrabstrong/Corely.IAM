using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.Migrations;

/// <inheritdoc />
public partial class AccountAsymmetricKey : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder
            .CreateTable(
                name: "AccountAsymmetricKeys",
                columns: table => new
                {
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    PublicKey = table
                        .Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PrivateKey = table
                        .Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedUtc = table.Column<DateTime>(
                        type: "TIMESTAMP",
                        nullable: false,
                        defaultValueSql: "(UTC_TIMESTAMP)"
                    ),
                    ModifiedUtc = table.Column<DateTime>(
                        type: "TIMESTAMP",
                        nullable: false,
                        defaultValueSql: "(UTC_TIMESTAMP)"
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountAsymmetricKeys", x => x.AccountId);
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
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AccountAsymmetricKeys");
    }
}
