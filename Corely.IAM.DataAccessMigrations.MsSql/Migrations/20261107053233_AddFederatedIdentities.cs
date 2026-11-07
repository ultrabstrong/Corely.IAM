using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Corely.IAM.DataAccessMigrations.MsSql.Migrations
{
    /// <inheritdoc />
    public partial class AddFederatedIdentities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FederatedIdentities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderType = table.Column<int>(type: "int", nullable: false),
                    Issuer = table.Column<string>(
                        type: "nvarchar(255)",
                        maxLength: 255,
                        nullable: false
                    ),
                    Subject = table.Column<string>(
                        type: "nvarchar(255)",
                        maxLength: 255,
                        nullable: false
                    ),
                    Email = table.Column<string>(
                        type: "nvarchar(254)",
                        maxLength: 254,
                        nullable: true
                    ),
                    EmailVerified = table.Column<bool>(type: "bit", nullable: false),
                    ReauthenticatedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReauthenticatedSessionId = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: true
                    ),
                    CreatedUtc = table.Column<DateTime>(
                        type: "DATETIME2",
                        nullable: false,
                        defaultValueSql: "(SYSUTCDATETIME())"
                    ),
                    ModifiedUtc = table.Column<DateTime>(type: "DATETIME2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FederatedIdentities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FederatedIdentities_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "IdentityProviders",
                columns: table => new
                {
                    Type = table.Column<int>(type: "int", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    AllowSignUp = table.Column<bool>(type: "bit", nullable: false),
                    ClientId = table.Column<string>(
                        type: "nvarchar(500)",
                        maxLength: 500,
                        nullable: false
                    ),
                    CredentialKind = table.Column<int>(type: "int", nullable: false),
                    EncryptedCredential = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: true
                    ),
                    CertificateThumbprint = table.Column<string>(
                        type: "nvarchar(128)",
                        maxLength: 128,
                        nullable: true
                    ),
                    Tenant = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: true
                    ),
                    CreatedUtc = table.Column<DateTime>(
                        type: "DATETIME2",
                        nullable: false,
                        defaultValueSql: "(SYSUTCDATETIME())"
                    ),
                    ModifiedUtc = table.Column<DateTime>(type: "DATETIME2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityProviders", x => x.Type);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_FederatedIdentities_Issuer_Subject",
                table: "FederatedIdentities",
                columns: new[] { "Issuer", "Subject" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_FederatedIdentities_UserId_ProviderType",
                table: "FederatedIdentities",
                columns: new[] { "UserId", "ProviderType" },
                unique: true
            );

            migrationBuilder.Sql(
                "INSERT INTO [FederatedIdentities] ([Id], [UserId], [ProviderType], [Issuer], [Subject], [Email], [EmailVerified], [CreatedUtc], [ModifiedUtc]) "
                    + "SELECT [Id], [UserId], 1, 'https://accounts.google.com', [GoogleSubjectId], [Email], 1, [CreatedUtc], [ModifiedUtc] FROM [GoogleAuths];"
            );

            migrationBuilder.DropTable(name: "GoogleAuths");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoogleAuths",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedUtc = table.Column<DateTime>(
                        type: "DATETIME2",
                        nullable: false,
                        defaultValueSql: "(SYSUTCDATETIME())"
                    ),
                    Email = table.Column<string>(
                        type: "nvarchar(254)",
                        maxLength: 254,
                        nullable: false
                    ),
                    GoogleSubjectId = table.Column<string>(
                        type: "nvarchar(255)",
                        maxLength: 255,
                        nullable: false
                    ),
                    ModifiedUtc = table.Column<DateTime>(type: "DATETIME2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleAuths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoogleAuths_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAuths_GoogleSubjectId",
                table: "GoogleAuths",
                column: "GoogleSubjectId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAuths_UserId",
                table: "GoogleAuths",
                column: "UserId",
                unique: true
            );

            migrationBuilder.Sql(
                "INSERT INTO [GoogleAuths] ([Id], [UserId], [GoogleSubjectId], [Email], [CreatedUtc], [ModifiedUtc]) "
                    + "SELECT [Id], [UserId], [Subject], COALESCE([Email], ''), [CreatedUtc], [ModifiedUtc] FROM [FederatedIdentities] WHERE [ProviderType] = 1;"
            );

            migrationBuilder.DropTable(name: "FederatedIdentities");

            migrationBuilder.DropTable(name: "IdentityProviders");
        }
    }
}
