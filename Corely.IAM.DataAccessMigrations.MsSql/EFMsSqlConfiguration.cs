using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Corely.IAM.DataAccessMigrations.MsSql;

internal class EFMsSqlConfiguration(string connectionString, string? historyTable = null)
    : EFMsSqlConfigurationBase(connectionString)
{
    public override void Configure(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(
            connectionString,
            b =>
            {
                b.MigrationsAssembly(typeof(EFMsSqlConfiguration).Assembly.GetName().Name);
                b.MigrationsHistoryTable(historyTable ?? MigrationConstants.DEFAULT_HISTORY_TABLE);
            }
        );
    }
}
