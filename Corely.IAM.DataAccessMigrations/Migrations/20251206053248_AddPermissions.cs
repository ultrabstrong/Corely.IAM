using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.Migrations;

/// <inheritdoc />
public partial class AddPermissions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<DateTime>(
            name: "ModifiedUtc",
            table: "UserSymmetricKeys",
            type: "TIMESTAMP",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "TIMESTAMP",
            oldDefaultValueSql: "(UTC_TIMESTAMP)"
        );

        migrationBuilder.AlterColumn<DateTime>(
            name: "ModifiedUtc",
            table: "Users",
            type: "TIMESTAMP",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "TIMESTAMP",
            oldDefaultValueSql: "(UTC_TIMESTAMP)"
        );

        migrationBuilder.AlterColumn<DateTime>(
            name: "ModifiedUtc",
            table: "UserAsymmetricKeys",
            type: "TIMESTAMP",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "TIMESTAMP",
            oldDefaultValueSql: "(UTC_TIMESTAMP)"
        );

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

        migrationBuilder.AlterColumn<DateTime>(
            name: "ModifiedUtc",
            table: "Roles",
            type: "TIMESTAMP",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "TIMESTAMP",
            oldDefaultValueSql: "(UTC_TIMESTAMP)"
        );

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

        migrationBuilder.AlterColumn<DateTime>(
            name: "ModifiedUtc",
            table: "Groups",
            type: "TIMESTAMP",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "TIMESTAMP",
            oldDefaultValueSql: "(UTC_TIMESTAMP)"
        );

        migrationBuilder.AlterColumn<DateTime>(
            name: "ModifiedUtc",
            table: "BasicAuths",
            type: "TIMESTAMP",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "TIMESTAMP",
            oldDefaultValueSql: "(UTC_TIMESTAMP)"
        );

        migrationBuilder.AlterColumn<DateTime>(
            name: "ModifiedUtc",
            table: "AccountSymmetricKeys",
            type: "TIMESTAMP",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "TIMESTAMP",
            oldDefaultValueSql: "(UTC_TIMESTAMP)"
        );

        migrationBuilder.AlterColumn<DateTime>(
            name: "ModifiedUtc",
            table: "Accounts",
            type: "TIMESTAMP",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "TIMESTAMP",
            oldDefaultValueSql: "(UTC_TIMESTAMP)"
        );

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

        migrationBuilder.AlterColumn<DateTime>(
            name: "ModifiedUtc",
            table: "AccountAsymmetricKeys",
            type: "TIMESTAMP",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "TIMESTAMP",
            oldDefaultValueSql: "(UTC_TIMESTAMP)"
        );

        migrationBuilder
            .CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation(
                            "MySql:ValueGenerationStrategy",
                            MySqlValueGenerationStrategy.IdentityColumn
                        ),
                    Description = table
                        .Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    ResourceType = table
                        .Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResourceId = table.Column<int>(type: "int", nullable: false),
                    Create = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Read = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Update = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Delete = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Execute = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder
            .CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    PermissionsId = table.Column<int>(type: "int", nullable: false),
                    RolesId = table.Column<int>(type: "int", nullable: false),
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
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RolesId",
                        column: x => x.RolesId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            )
            .Annotation("MySql:CharSet", "utf8mb4");

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
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "RolePermissions");

        migrationBuilder.DropTable(name: "Permissions");

        migrationBuilder.AlterColumn<DateTime>(
            name: "ModifiedUtc",
            table: "UserSymmetricKeys",
            type: "TIMESTAMP",
            nullable: false,
            defaultValueSql: "(UTC_TIMESTAMP)",
            oldClrType: typeof(DateTime),
            oldType: "TIMESTAMP",
            oldNullable: true
        );

        migrationBuilder.AlterColumn<DateTime>(
            name: "ModifiedUtc",
            table: "Users",
            type: "TIMESTAMP",
            nullable: false,
            defaultValueSql: "(UTC_TIMESTAMP)",
            oldClrType: typeof(DateTime),
            oldType: "TIMESTAMP",
            oldNullable: true
        );

        migrationBuilder.AlterColumn<DateTime>(
            name: "ModifiedUtc",
            table: "UserAsymmetricKeys",
            type: "TIMESTAMP",
            nullable: false,
            defaultValueSql: "(UTC_TIMESTAMP)",
            oldClrType: typeof(DateTime),
            oldType: "TIMESTAMP",
            oldNullable: true
        );

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

        migrationBuilder.AlterColumn<DateTime>(
            name: "ModifiedUtc",
            table: "Roles",
            type: "TIMESTAMP",
            nullable: false,
            defaultValueSql: "(UTC_TIMESTAMP)",
            oldClrType: typeof(DateTime),
            oldType: "TIMESTAMP",
            oldNullable: true
        );

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

        migrationBuilder.AlterColumn<DateTime>(
            name: "ModifiedUtc",
            table: "Groups",
            type: "TIMESTAMP",
            nullable: false,
            defaultValueSql: "(UTC_TIMESTAMP)",
            oldClrType: typeof(DateTime),
            oldType: "TIMESTAMP",
            oldNullable: true
        );

        migrationBuilder.AlterColumn<DateTime>(
            name: "ModifiedUtc",
            table: "BasicAuths",
            type: "TIMESTAMP",
            nullable: false,
            defaultValueSql: "(UTC_TIMESTAMP)",
            oldClrType: typeof(DateTime),
            oldType: "TIMESTAMP",
            oldNullable: true
        );

        migrationBuilder.AlterColumn<DateTime>(
            name: "ModifiedUtc",
            table: "AccountSymmetricKeys",
            type: "TIMESTAMP",
            nullable: false,
            defaultValueSql: "(UTC_TIMESTAMP)",
            oldClrType: typeof(DateTime),
            oldType: "TIMESTAMP",
            oldNullable: true
        );

        migrationBuilder.AlterColumn<DateTime>(
            name: "ModifiedUtc",
            table: "Accounts",
            type: "TIMESTAMP",
            nullable: false,
            defaultValueSql: "(UTC_TIMESTAMP)",
            oldClrType: typeof(DateTime),
            oldType: "TIMESTAMP",
            oldNullable: true
        );

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

        migrationBuilder.AlterColumn<DateTime>(
            name: "ModifiedUtc",
            table: "AccountAsymmetricKeys",
            type: "TIMESTAMP",
            nullable: false,
            defaultValueSql: "(UTC_TIMESTAMP)",
            oldClrType: typeof(DateTime),
            oldType: "TIMESTAMP",
            oldNullable: true
        );
    }
}
