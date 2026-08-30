using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Corely.IAM.IntegrationTests.Infrastructure;

/// <summary>
/// Binds <c>IamDbContext</c> to a caller-owned SQLite connection.
///
/// SQLite rather than the EF InMemory provider: InMemory is not relational, cannot execute
/// set-based updates, enforces no constraints, and does no SQL translation - which is precisely
/// what this tier exists to exercise.
///
/// The connection is injected because an in-memory SQLite database lives only as long as its
/// connection is open, so every scope must share one.
/// </summary>
internal sealed class SqliteEFConfiguration(SqliteConnection connection)
    : EFSqliteConfigurationBase(connection.ConnectionString)
{
    public override void Configure(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseSqlite(connection);
}
