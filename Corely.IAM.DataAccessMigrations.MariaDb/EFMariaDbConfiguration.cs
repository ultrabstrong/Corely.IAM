using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Corely.IAM.DataAccessMigrations.MariaDb;

internal class EFMariaDbConfiguration(string connectionString)
    : EFMySqlConfigurationBase(connectionString)
{
    public override void Configure(DbContextOptionsBuilder optionsBuilder)
    {
        var serverVersion = connectionString.Contains(MariaDbDesignTimeConstants.DesignTimeMarker)
            ? MariaDbDesignTimeConstants.DesignTimeServerVersion
            : ServerVersion.AutoDetect(connectionString);

        optionsBuilder.UseMySql(
            connectionString,
            serverVersion,
            b => b.MigrationsAssembly(typeof(EFMariaDbConfiguration).Assembly.GetName().Name)
        );
    }
}
