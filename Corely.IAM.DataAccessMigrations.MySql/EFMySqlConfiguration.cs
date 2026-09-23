using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Corely.IAM.DataAccessMigrations.MySql;

internal class EFMySqlConfiguration(string connectionString, string? historyTable = null)
    : EFMySqlConfigurationBase(connectionString)
{
    public override void Configure(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseMySQL(
            connectionString,
            b =>
            {
                b.MigrationsAssembly(typeof(EFMySqlConfiguration).Assembly.GetName().Name);
                b.MigrationsHistoryTable(historyTable ?? MigrationConstants.DEFAULT_HISTORY_TABLE);
            }
        );
    }
}
