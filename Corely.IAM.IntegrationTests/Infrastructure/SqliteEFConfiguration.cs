using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Corely.IAM.IntegrationTests.Infrastructure;

internal sealed class SqliteEFConfiguration(SqliteConnection connection)
    : EFSqliteConfigurationBase(connection.ConnectionString)
{
    public override void Configure(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseSqlite(connection);
}
