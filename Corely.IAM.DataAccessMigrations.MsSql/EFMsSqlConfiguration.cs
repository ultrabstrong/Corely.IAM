using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Corely.IAM.DataAccessMigrations.MsSql;

internal class EFMsSqlConfiguration(string connectionString)
    : EFMsSqlConfigurationBase(connectionString)
{
    public override void Configure(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(
            connectionString,
            b => b.MigrationsAssembly(typeof(EFMsSqlConfiguration).Assembly.GetName().Name)
        );
    }
}
