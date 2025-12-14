using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.Migrations
{
    /// <inheritdoc />
    public partial class RenameRoleAndGroupJoinTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupEntityRoleEntity_Groups_GroupsId",
                table: "GroupEntityRoleEntity"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_GroupEntityRoleEntity_Roles_RolesId",
                table: "GroupEntityRoleEntity"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_GroupEntityUserEntity_Groups_GroupsId",
                table: "GroupEntityUserEntity"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_GroupEntityUserEntity_Users_UsersId",
                table: "GroupEntityUserEntity"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_UserEntityRoleEntity_Roles_RolesId",
                table: "UserEntityRoleEntity"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_UserEntityRoleEntity_Users_UsersId",
                table: "UserEntityRoleEntity"
            );

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserEntityRoleEntity",
                table: "UserEntityRoleEntity"
            );

            migrationBuilder.DropPrimaryKey(
                name: "PK_GroupEntityUserEntity",
                table: "GroupEntityUserEntity"
            );

            migrationBuilder.DropPrimaryKey(
                name: "PK_GroupEntityRoleEntity",
                table: "GroupEntityRoleEntity"
            );

            migrationBuilder.RenameTable(name: "UserEntityRoleEntity", newName: "UserRoles");

            migrationBuilder.RenameTable(name: "GroupEntityUserEntity", newName: "UserGroups");

            migrationBuilder.RenameTable(name: "GroupEntityRoleEntity", newName: "GroupRoles");

            migrationBuilder.RenameIndex(
                name: "IX_UserEntityRoleEntity_UsersId",
                table: "UserRoles",
                newName: "IX_UserRoles_UsersId"
            );

            migrationBuilder.RenameIndex(
                name: "IX_GroupEntityUserEntity_UsersId",
                table: "UserGroups",
                newName: "IX_UserGroups_UsersId"
            );

            migrationBuilder.RenameIndex(
                name: "IX_GroupEntityRoleEntity_RolesId",
                table: "GroupRoles",
                newName: "IX_GroupRoles_RolesId"
            );

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserRoles",
                table: "UserRoles",
                columns: new[] { "RolesId", "UsersId" }
            );

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserGroups",
                table: "UserGroups",
                columns: new[] { "GroupsId", "UsersId" }
            );

            migrationBuilder.AddPrimaryKey(
                name: "PK_GroupRoles",
                table: "GroupRoles",
                columns: new[] { "GroupsId", "RolesId" }
            );

            migrationBuilder.AddForeignKey(
                name: "FK_GroupRoles_Groups_GroupsId",
                table: "GroupRoles",
                column: "GroupsId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_GroupRoles_Roles_RolesId",
                table: "GroupRoles",
                column: "RolesId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_UserGroups_Groups_GroupsId",
                table: "UserGroups",
                column: "GroupsId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_UserGroups_Users_UsersId",
                table: "UserGroups",
                column: "UsersId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Roles_RolesId",
                table: "UserRoles",
                column: "RolesId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Users_UsersId",
                table: "UserRoles",
                column: "UsersId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupRoles_Groups_GroupsId",
                table: "GroupRoles"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_GroupRoles_Roles_RolesId",
                table: "GroupRoles"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_UserGroups_Groups_GroupsId",
                table: "UserGroups"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_UserGroups_Users_UsersId",
                table: "UserGroups"
            );

            migrationBuilder.DropForeignKey(name: "FK_UserRoles_Roles_RolesId", table: "UserRoles");

            migrationBuilder.DropForeignKey(name: "FK_UserRoles_Users_UsersId", table: "UserRoles");

            migrationBuilder.DropPrimaryKey(name: "PK_UserRoles", table: "UserRoles");

            migrationBuilder.DropPrimaryKey(name: "PK_UserGroups", table: "UserGroups");

            migrationBuilder.DropPrimaryKey(name: "PK_GroupRoles", table: "GroupRoles");

            migrationBuilder.RenameTable(name: "UserRoles", newName: "UserEntityRoleEntity");

            migrationBuilder.RenameTable(name: "UserGroups", newName: "GroupEntityUserEntity");

            migrationBuilder.RenameTable(name: "GroupRoles", newName: "GroupEntityRoleEntity");

            migrationBuilder.RenameIndex(
                name: "IX_UserRoles_UsersId",
                table: "UserEntityRoleEntity",
                newName: "IX_UserEntityRoleEntity_UsersId"
            );

            migrationBuilder.RenameIndex(
                name: "IX_UserGroups_UsersId",
                table: "GroupEntityUserEntity",
                newName: "IX_GroupEntityUserEntity_UsersId"
            );

            migrationBuilder.RenameIndex(
                name: "IX_GroupRoles_RolesId",
                table: "GroupEntityRoleEntity",
                newName: "IX_GroupEntityRoleEntity_RolesId"
            );

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserEntityRoleEntity",
                table: "UserEntityRoleEntity",
                columns: new[] { "RolesId", "UsersId" }
            );

            migrationBuilder.AddPrimaryKey(
                name: "PK_GroupEntityUserEntity",
                table: "GroupEntityUserEntity",
                columns: new[] { "GroupsId", "UsersId" }
            );

            migrationBuilder.AddPrimaryKey(
                name: "PK_GroupEntityRoleEntity",
                table: "GroupEntityRoleEntity",
                columns: new[] { "GroupsId", "RolesId" }
            );

            migrationBuilder.AddForeignKey(
                name: "FK_GroupEntityRoleEntity_Groups_GroupsId",
                table: "GroupEntityRoleEntity",
                column: "GroupsId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_GroupEntityRoleEntity_Roles_RolesId",
                table: "GroupEntityRoleEntity",
                column: "RolesId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_GroupEntityUserEntity_Groups_GroupsId",
                table: "GroupEntityUserEntity",
                column: "GroupsId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_GroupEntityUserEntity_Users_UsersId",
                table: "GroupEntityUserEntity",
                column: "UsersId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_UserEntityRoleEntity_Roles_RolesId",
                table: "UserEntityRoleEntity",
                column: "RolesId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_UserEntityRoleEntity_Users_UsersId",
                table: "UserEntityRoleEntity",
                column: "UsersId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );
        }
    }
}
