using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Corely.IAM.Web.FunctionalTests.Infrastructure;

/// <summary>
/// Binds <c>IamDbContext</c> to a caller-owned SQLite connection.
///
/// SQLite is used rather than the EF InMemory provider because InMemory is not relational: it
/// cannot execute set-based updates (<c>ExecuteUpdateAsync</c>), enforces no constraints, and does
/// no SQL translation - so it would not exercise the code paths these tests exist to cover.
///
/// The connection is injected rather than created here because an in-memory SQLite database lives
/// only as long as its connection is open. Every scope must share one connection or each request
/// would see an empty database.
/// </summary>
internal sealed class SqliteEFConfiguration(SqliteConnection connection)
    : EFSqliteConfigurationBase(connection.ConnectionString)
{
    public override void Configure(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseSqlite(connection);
}
