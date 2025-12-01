using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.Migrations;

/// <inheritdoc />
public partial class CascadeDeleteSymmetricKey : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Accounts_SymmetricKeys_SymmetricKeyId",
            table: "Accounts"
        );

        migrationBuilder.DropTable(name: "SymmetricKeys");

        migrationBuilder.DropIndex(name: "IX_Accounts_SymmetricKeyId", table: "Accounts");

        migrationBuilder.DropColumn(name: "SymmetricKeyId", table: "Accounts");

        migrationBuilder
            .CreateTable(
                name: "AccountSymmetricKeys",
                columns: table => new
                {
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Key = table
                        .Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
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
                    table.PrimaryKey("PK_AccountSymmetricKeys", x => x.AccountId);
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
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AccountSymmetricKeys");

        migrationBuilder.AddColumn<int>(
            name: "SymmetricKeyId",
            table: "Accounts",
            type: "int",
            nullable: false,
            defaultValue: 0
        );

        migrationBuilder
            .CreateTable(
                name: "SymmetricKeys",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation(
                            "MySql:ValueGenerationStrategy",
                            MySqlValueGenerationStrategy.IdentityColumn
                        ),
                    CreatedUtc = table.Column<DateTime>(
                        type: "TIMESTAMP",
                        nullable: false,
                        defaultValueSql: "(UTC_TIMESTAMP)"
                    ),
                    Key = table
                        .Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModifiedUtc = table.Column<DateTime>(
                        type: "TIMESTAMP",
                        nullable: false,
                        defaultValueSql: "(UTC_TIMESTAMP)"
                    ),
                    Version = table.Column<int>(type: "int", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SymmetricKeys", x => x.Id);
                }
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_Accounts_SymmetricKeyId",
            table: "Accounts",
            column: "SymmetricKeyId"
        );

        migrationBuilder.AddForeignKey(
            name: "FK_Accounts_SymmetricKeys_SymmetricKeyId",
            table: "Accounts",
            column: "SymmetricKeyId",
            principalTable: "SymmetricKeys",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade
        );
    }
}
