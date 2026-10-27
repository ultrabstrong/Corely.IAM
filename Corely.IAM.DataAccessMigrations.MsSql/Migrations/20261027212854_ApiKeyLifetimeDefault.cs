using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.MsSql.Migrations
{
    /// <inheritdoc />
    public partial class ApiKeyLifetimeDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "EXEC(N'UPDATE PlatformSecuritySettings SET ApiKeyMaxLifetimeDays = 365 WHERE ApiKeyMaxLifetimeDays = 0')"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
