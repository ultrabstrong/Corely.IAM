# Migrating to Corely.IAM 2.0

Two things need action: MariaDB is gone, and MySQL databases must be recreated. Everything else
is a recompile.

## MariaDB is no longer supported

`Corely.IAM.DataAccessMigrations.MariaDb` is removed, along with `DatabaseProvider.MariaDb` and the
`"mariadb"` configuration value.

MariaDB was supported because Pomelo happened to support it, not because it was a deliberate
target. Pomelo has no EF Core 10 release, no preview, and no commits since August 2025, so it
blocked the framework upgrade outright. Its replacement - Oracle's `MySql.EntityFrameworkCore` -
does not support MariaDB.

If you are running on MariaDB, you need to move to MySQL or SQL Server. There is no in-place path.

## MySQL databases must be recreated

MySQL now uses `MySql.EntityFrameworkCore` instead of `Pomelo.EntityFrameworkCore.MySql`. The two
providers generate different SQL, and the previous migrations were authored against Pomelo, so they
have been replaced by a single regenerated initial migration.

An existing MySQL database will not migrate forward. Drop and recreate it:

```powershell
cd Corely.IAM.DataAccessMigrations.Cli
dotnet run -- db drop
dotnet run -- db create
```

SQL Server is unaffected - its migration history is unchanged.

## Provider configuration changed shape

Oracle's provider resolves server capabilities from the connection, so there is no `ServerVersion`
to declare:

```csharp
// 1.x
optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

// 2.0
optionsBuilder.UseMySQL(connectionString);
```

Its ADO driver is `MySql.Data` rather than `MySqlConnector`, which matters only if you opened
connections directly.

## Target framework

`net10.0` only. 1.x multi-targeted `net9.0` and `net10.0`; .NET 9 reached end of support in May
2026.

Consumes Corely.Common 2.0.0, Corely.DataAccess 3.0.0, Corely.Security 3.0.0 and EF Core 10.

## ExecuteUpdateAsync signature changed

This comes from Corely.DataAccess 3.0.0. `IRepo.ExecuteUpdateAsync` no longer takes EF's setter
expression:

```csharp
// 1.x
Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> setProperties

// 2.0
Action<IUpdateSetters<T>> setProperties
```

Call sites are unchanged in practice - `s => s.SetProperty(x => x.Prop, value)` compiles against
both. Only code that built those expressions programmatically needs rewriting.

## Verification

The provider matrix runs the full schema, migrations and authorization queries against real MySQL
and SQL Server containers:

```powershell
$env:CORELY_RUN_CONTAINER_TESTS = "1"
dotnet test --project Corely.IAM.IntegrationTests --filter-query "/*/*/*ProviderMatrixTests/*"
```
