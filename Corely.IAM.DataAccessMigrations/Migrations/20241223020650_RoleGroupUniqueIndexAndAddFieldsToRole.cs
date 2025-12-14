using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.Migrations
{
    /// <inheritdoc />
    public partial class RoleGroupUniqueIndexAndAddFieldsToRole : Migration
    {
        private static readonly string[] columns = new[] { "AccountId", "Name" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CUSTOM: Drop foreign key constraints
            migrationBuilder.DropForeignKey(name: "FK_Roles_Accounts_AccountId", table: "Roles");

            migrationBuilder.DropForeignKey(name: "FK_Groups_Accounts_AccountId", table: "Groups");
            // END CUSTOM

            migrationBuilder.DropIndex(name: "IX_Roles_AccountId", table: "Roles");

            migrationBuilder.DropIndex(name: "IX_Groups_AccountId", table: "Groups");

            migrationBuilder.RenameColumn(name: "RoleName", table: "Roles", newName: "Name");

            migrationBuilder.RenameColumn(name: "GroupName", table: "Groups", newName: "Name");

            migrationBuilder
                .AddColumn<string>(
                    name: "Description",
                    table: "Roles",
                    type: "longtext",
                    nullable: true
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemDefined",
                table: "Roles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder
                .AddColumn<string>(
                    name: "Description",
                    table: "Groups",
                    type: "longtext",
                    nullable: true
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            // CUSTOM: Reorder columns
            migrationBuilder.Sql(
                "ALTER TABLE `Groups` MODIFY COLUMN `Description` longtext NULL AFTER `Name`;"
            );
            migrationBuilder.Sql(
                "ALTER TABLE `Roles` MODIFY COLUMN `Description` longtext NULL AFTER `Name`;"
            );
            migrationBuilder.Sql(
                "ALTER TABLE `Roles` MODIFY COLUMN `IsSystemDefined` tinyint(1) NOT NULL AFTER `Description`;"
            );
            // END CUSTOM

            migrationBuilder.CreateIndex(
                name: "IX_Roles_AccountId_Name",
                table: "Roles",
                columns: columns,
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Groups_AccountId_Name",
                table: "Groups",
                columns: columns,
                unique: true
            );

            // CUSTOM: Add foreign key constraints
            migrationBuilder.AddForeignKey(
                name: "FK_Roles_Accounts_AccountId",
                table: "Roles",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Accounts_AccountId",
                table: "Groups",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );
            // END CUSTOM
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // CUSTOM: Drop foreign key constraints
            migrationBuilder.DropForeignKey(name: "FK_Roles_Accounts_AccountId", table: "Roles");

            migrationBuilder.DropForeignKey(name: "FK_Groups_Accounts_AccountId", table: "Groups");
            // END CUSTOM

            migrationBuilder.DropIndex(name: "IX_Roles_AccountId_Name", table: "Roles");

            migrationBuilder.DropIndex(name: "IX_Groups_AccountId_Name", table: "Groups");

            migrationBuilder.DropColumn(name: "Description", table: "Roles");

            migrationBuilder.DropColumn(name: "IsSystemDefined", table: "Roles");

            migrationBuilder.DropColumn(name: "Description", table: "Groups");

            migrationBuilder.RenameColumn(name: "Name", table: "Roles", newName: "RoleName");

            migrationBuilder.RenameColumn(name: "Name", table: "Groups", newName: "GroupName");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_AccountId",
                table: "Roles",
                column: "AccountId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Groups_AccountId",
                table: "Groups",
                column: "AccountId"
            );

            // CUSTOM: Add foreign key constraints
            migrationBuilder.AddForeignKey(
                name: "FK_Roles_Accounts_AccountId",
                table: "Roles",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Accounts_AccountId",
                table: "Groups",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );
            // END CUSTOM
        }
    }
}
