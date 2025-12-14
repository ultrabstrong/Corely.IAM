using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.Migrations;

/// <inheritdoc />
public partial class AddRoles : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder
            .CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation(
                            "MySql:ValueGenerationStrategy",
                            MySqlValueGenerationStrategy.IdentityColumn
                        ),
                    RoleName = table
                        .Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roles_Accounts_AccountId",
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
                name: "GroupEntityRoleEntity",
                columns: table => new
                {
                    GroupsId = table.Column<int>(type: "int", nullable: false),
                    RolesId = table.Column<int>(type: "int", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_GroupEntityRoleEntity",
                        x => new { x.GroupsId, x.RolesId }
                    );
                    table.ForeignKey(
                        name: "FK_GroupEntityRoleEntity_Groups_GroupsId",
                        column: x => x.GroupsId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_GroupEntityRoleEntity_Roles_RolesId",
                        column: x => x.RolesId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder
            .CreateTable(
                name: "UserEntityRoleEntity",
                columns: table => new
                {
                    RolesId = table.Column<int>(type: "int", nullable: false),
                    UsersId = table.Column<int>(type: "int", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_UserEntityRoleEntity",
                        x => new { x.RolesId, x.UsersId }
                    );
                    table.ForeignKey(
                        name: "FK_UserEntityRoleEntity_Roles_RolesId",
                        column: x => x.RolesId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_UserEntityRoleEntity_Users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            )
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_GroupEntityRoleEntity_RolesId",
            table: "GroupEntityRoleEntity",
            column: "RolesId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_UserEntityRoleEntity_UsersId",
            table: "UserEntityRoleEntity",
            column: "UsersId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_Roles_AccountId",
            table: "Roles",
            column: "AccountId"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "GroupEntityRoleEntity");

        migrationBuilder.DropTable(name: "UserEntityRoleEntity");

        migrationBuilder.DropTable(name: "Roles");
    }
}
