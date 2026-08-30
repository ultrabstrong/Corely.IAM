using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Corely.IAM.IntegrationTests.Providers;

/// <summary>
/// Mirrors the configurations the migration assemblies define. Those are internal to their own
/// assemblies, so the migrations assembly is named here by string instead - the same name EF
/// resolves at design time.
/// </summary>
internal sealed class TestMsSqlConfiguration(string connectionString)
    : EFMsSqlConfigurationBase(connectionString)
{
    public const string MIGRATIONS_ASSEMBLY = "Corely.IAM.DataAccessMigrations.MsSql";

    public override void Configure(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseSqlServer(
            connectionString,
            b => b.MigrationsAssembly(MIGRATIONS_ASSEMBLY)
        );
}

internal sealed class TestMySqlConfiguration(string connectionString, string migrationsAssembly)
    : EFMySqlConfigurationBase(connectionString)
{
    public const string MYSQL_MIGRATIONS_ASSEMBLY = "Corely.IAM.DataAccessMigrations.MySql";
    public const string MARIADB_MIGRATIONS_ASSEMBLY = "Corely.IAM.DataAccessMigrations.MariaDb";

    public override void Configure(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(connectionString),
            b => b.MigrationsAssembly(migrationsAssembly)
        );
}
