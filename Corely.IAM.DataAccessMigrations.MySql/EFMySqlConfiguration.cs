using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Corely.IAM.DataAccessMigrations.MySql;

internal class EFMySqlConfiguration(string connectionString)
    : EFMySqlConfigurationBase(connectionString)
{
    public override void Configure(DbContextOptionsBuilder optionsBuilder)
    {
        var serverVersion = connectionString.Contains(MySqlDesignTimeConstants.DesignTimeMarker)
            ? MySqlDesignTimeConstants.DesignTimeServerVersion
            : ServerVersion.AutoDetect(connectionString);

        optionsBuilder.UseMySql(
            connectionString,
            serverVersion,
            b => b.MigrationsAssembly(typeof(EFMySqlConfiguration).Assembly.GetName().Name)
        );
    }
}
