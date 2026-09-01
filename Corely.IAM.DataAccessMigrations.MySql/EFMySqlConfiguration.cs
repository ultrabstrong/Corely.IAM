using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Corely.IAM.DataAccessMigrations.MySql;

internal class EFMySqlConfiguration(string connectionString)
    : EFMySqlConfigurationBase(connectionString)
{
    public override void Configure(DbContextOptionsBuilder optionsBuilder)
    {
        // Oracle's provider resolves server capabilities from the connection, so unlike Pomelo
        // there is no ServerVersion to declare - and no design-time stand-in needed for one.
        optionsBuilder.UseMySQL(
            connectionString,
            b => b.MigrationsAssembly(typeof(EFMySqlConfiguration).Assembly.GetName().Name)
        );
    }
}
