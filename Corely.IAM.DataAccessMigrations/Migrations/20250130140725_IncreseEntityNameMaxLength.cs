using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.Migrations
{
    /// <inheritdoc />
    public partial class IncreseEntityNameMaxLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .AlterColumn<string>(
                    name: "Name",
                    table: "Roles",
                    type: "varchar(100)",
                    maxLength: 100,
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "varchar(50)",
                    oldMaxLength: 50
                )
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .AlterColumn<string>(
                    name: "Name",
                    table: "Permissions",
                    type: "varchar(250)",
                    maxLength: 250,
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "varchar(50)",
                    oldMaxLength: 50
                )
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .AlterColumn<string>(
                    name: "Name",
                    table: "Groups",
                    type: "varchar(100)",
                    maxLength: 100,
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "varchar(50)",
                    oldMaxLength: 50
                )
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .AlterColumn<string>(
                    name: "AccountName",
                    table: "Accounts",
                    type: "varchar(100)",
                    maxLength: 100,
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "varchar(50)",
                    oldMaxLength: 50
                )
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder
                .AlterColumn<string>(
                    name: "Name",
                    table: "Roles",
                    type: "varchar(50)",
                    maxLength: 50,
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "varchar(100)",
                    oldMaxLength: 100
                )
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .AlterColumn<string>(
                    name: "Name",
                    table: "Permissions",
                    type: "varchar(50)",
                    maxLength: 50,
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "varchar(250)",
                    oldMaxLength: 250
                )
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .AlterColumn<string>(
                    name: "Name",
                    table: "Groups",
                    type: "varchar(50)",
                    maxLength: 50,
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "varchar(100)",
                    oldMaxLength: 100
                )
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .AlterColumn<string>(
                    name: "AccountName",
                    table: "Accounts",
                    type: "varchar(50)",
                    maxLength: 50,
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "varchar(100)",
                    oldMaxLength: 100
                )
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
