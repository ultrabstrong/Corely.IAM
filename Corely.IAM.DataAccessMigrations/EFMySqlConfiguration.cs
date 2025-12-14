using System.Reflection;
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Corely.IAM.DataAccessMigrations;

internal class EFMySqlConfiguration(string connectionString)
    : EFMySqlConfigurationBase(connectionString)
{
    public override void Configure(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(connectionString),
            b => b.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name)
        );
    }
}
