# Corely IAM Database Migration CLI

Creates and migrates the Corely IAM database schema. This is the supported way to stand up the IAM
tables — the library itself never applies migrations at runtime.

The tool ships both provider migration sets, so nothing beyond it needs to be referenced.

| Provider | Value |
|----------|-------|
| SQL Server | `MsSql` |
| MySQL | `MySql` |

## Install

```bash
dotnet tool install --global Corely.IAM.DataAccessMigrations.Cli
```

The command is `corely-iam-db`.

## Configuration

Every `db` command takes the provider and connection string as options, falling back to environment
variables:

| Option | Environment variable |
|--------|---------------------|
| `-p, --provider` | `CORELY_IAM_DB_PROVIDER` |
| `-c, --connection-string` | `CORELY_IAM_DB_CONNECTION` |

The option wins when both are present. There is no settings file — an installed tool would have to
keep one in its own install directory, shared by every repository and CI job on the machine.

```bash
# Per invocation
corely-iam-db db create -p MsSql -c "Server=(localdb)\MSSQLLocalDB;Database=CorelyIam;Trusted_Connection=True;"

# Or once per shell
export CORELY_IAM_DB_PROVIDER=MsSql
export CORELY_IAM_DB_CONNECTION="Server=(localdb)\MSSQLLocalDB;Database=CorelyIam;Trusted_Connection=True;"
corely-iam-db db create
```

```powershell
$env:CORELY_IAM_DB_PROVIDER = "MsSql"
$env:CORELY_IAM_DB_CONNECTION = "Server=(localdb)\MSSQLLocalDB;Database=CorelyIam;Trusted_Connection=True;"
corely-iam-db db create
```

## Commands

| Command | Description |
|---------|-------------|
| `db create` | Create the database and apply all migrations |
| `db migrate [target]` | Apply pending migrations, or migrate to a specific target (`0` reverts all) |
| `db status` | Show applied and pending migrations |
| `db list` | List all available migrations |
| `db script [from] [to]` | Generate a SQL script from migrations |
| `db drop [-f]` | Drop the database (`-f` skips confirmation) |
| `db test-connection` | Test the database connection |
| `provider list` | List available database providers |

`db status` and `db list` take `-a, --show-all` to include migrations belonging to other contexts
in the same database. `db script` takes `-o, --output` to write to a file and `-i, --idempotent`
for a script that is safe to run repeatedly.

## Migrations history table

IAM records its migrations in `__CorelyIamMigrationsHistory` rather than the default
`__EFMigrationsHistory`, so it can share a database with your own contexts without their migration
records interleaving.

A database that was migrated before this default existed has its IAM history in
`__EFMigrationsHistory`. Either keep using it:

```bash
corely-iam-db db migrate --history-table __EFMigrationsHistory
```

or copy the records across once and drop the option:

```sql
-- SQL Server
SELECT * INTO __CorelyIamMigrationsHistory FROM __EFMigrationsHistory;

-- MySQL
CREATE TABLE __CorelyIamMigrationsHistory AS SELECT * FROM __EFMigrationsHistory;
```

That copies everything, which is what you want when IAM is the only context in the database. If it
shares the database, restrict the copy to the migration ids `corely-iam-db db list` reports.

Run `db status` afterwards: every IAM migration should read as applied. If they read as pending,
the copy did not land and migrating would try to recreate tables that already exist.

## Examples

```bash
# First-time setup
corely-iam-db db create -p MsSql -c "<connection string>"

# Deployment script, reviewed before it runs anywhere
corely-iam-db db script -i -o deploy.sql -p MsSql

# CI or a test fixture with a container-assigned connection string
CORELY_IAM_DB_PROVIDER=MsSql CORELY_IAM_DB_CONNECTION="$CONN" corely-iam-db db create
```

`db script` needs a provider but never opens a connection, so it works offline.

## Authoring migrations

Creating migrations is a development task that needs the repository, not this tool. Use the scripts
at the repository root, which target both providers:

```powershell
.\AddMigration.ps1 "MigrationName"
.\RemoveMigration.ps1
.\ListMigrations.ps1
```

## Notes

- `db create` is safe to run against an existing database — it applies whatever is pending.
- `db migrate 0` reverts all migrations but does not drop the database.
- `db drop` is destructive and cannot be undone.
