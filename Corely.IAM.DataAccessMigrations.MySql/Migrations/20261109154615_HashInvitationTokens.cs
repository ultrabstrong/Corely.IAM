using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.MySql.Migrations
{
    /// <inheritdoc />
    public partial class HashInvitationTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Invitations_Token", table: "Invitations");

            migrationBuilder.DropColumn(name: "Token", table: "Invitations");

            migrationBuilder.AddColumn<string>(
                name: "SecretHash",
                table: "Invitations",
                type: "varchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: ""
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "SecretHash", table: "Invitations");

            migrationBuilder.AddColumn<string>(
                name: "Token",
                table: "Invitations",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_Token",
                table: "Invitations",
                column: "Token",
                unique: true
            );
        }
    }
}
