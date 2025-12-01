using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                        Name = table
                            .Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        Description = table
                            .Column<string>(type: "longtext", nullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4"),
                        AccountId = table.Column<int>(type: "int", nullable: false),
                        ResourceType = table
                            .Column<string>(type: "longtext", nullable: false)
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
                        ModifiedUtc = table.Column<DateTime>(
                            type: "TIMESTAMP",
                            nullable: false,
                            defaultValueSql: "(UTC_TIMESTAMP)"
                        ),
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
                name: "IX_Permissions_AccountId_Name",
                table: "Permissions",
                columns: new[] { "AccountId", "Name" },
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
        }
    }
}
