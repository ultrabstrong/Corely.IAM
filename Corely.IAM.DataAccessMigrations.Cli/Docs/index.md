# Corely.IAM.DataAccessMigrations.Cli

Command-line tool for managing the Corely.IAM database — creation, migration, scripting, and status. Built on `System.CommandLine` and distributed as a .NET tool.

All commands support `--help` for full argument and option details.

## Command Groups

| Group | Purpose |
|-------|---------|
| `db` | Database operations — create, migrate, drop, script, status |
| `provider` | Lists the available database providers |

## Install

```bash
dotnet tool install --global Corely.IAM.DataAccessMigrations.Cli
```

The command is `corely-iam-db`. Running from a clone of this repository works too — substitute `dotnet run --project Corely.IAM.DataAccessMigrations.Cli --` for `corely-iam-db` in every example below.

## Configuration

There is no settings file. Each `db` command resolves its provider and connection string from options first, then environment variables:

| Option | Environment variable |
|--------|---------------------|
| `-p, --provider` | `CORELY_IAM_DB_PROVIDER` |
| `-c, --connection-string` | `CORELY_IAM_DB_CONNECTION` |

Supported providers: `MsSql`, `MySql`. Parsing is case-insensitive.

Options are per invocation, which suits CI and test fixtures that receive a connection string at runtime. Environment variables suit an interactive shell, where setting them once avoids repeating the connection string on every command.

```powershell
$env:CORELY_IAM_DB_PROVIDER = "MsSql"
$env:CORELY_IAM_DB_CONNECTION = "Server=(localdb)\MSSQLLocalDB;Database=CorelyIam;Trusted_Connection=True;"
```

## Database Operations

### Create Database

Creates the database (if it does not exist) and applies all pending migrations in one step:

```bash
corely-iam-db db create
```

This is the recommended command for initial setup, and it is safe to re-run — it applies whatever is pending.

### Check Migration Status

View which migrations have been applied and which are pending:

```bash
corely-iam-db db status
```

### List Available Migrations

List all migrations defined in the current provider's migration assembly:

```bash
corely-iam-db db list
```

`db status` and `db list` accept `-a, --show-all` to include migrations belonging to other contexts sharing the database.

### Apply Migrations

Apply all pending migrations:

```bash
corely-iam-db db migrate
```

Migrate to a specific migration (applies or reverts as needed):

```bash
corely-iam-db db migrate "MigrationName"
```

Revert all migrations:

```bash
corely-iam-db db migrate 0
```

### Generate SQL Scripts

Generate a SQL script for all migrations:

```bash
corely-iam-db db script
```

Generate a script between two specific migrations:

```bash
corely-iam-db db script "FromMigration" "ToMigration"
```

Options:
- `-o, --output` — write to a file instead of console
- `-i, --idempotent` — generate an idempotent script safe to run multiple times

Production deployments typically use idempotent scripts:

```bash
corely-iam-db db script -i -o "deploy.sql"
```

Script generation resolves entirely from the migrations assembly, so it needs a provider but no connection string and no reachable database.

### Test Connection

```bash
corely-iam-db db test-connection
```

### Drop Database

```bash
corely-iam-db db drop
```

Prompts for confirmation. Use `-f, --force` to skip the prompt.

## Migrations History Table

IAM records its migrations in `__CorelyIamMigrationsHistory` rather than the default `__EFMigrationsHistory`, so that it can share a database with a consumer's own contexts without every context writing to one table.

`--history-table` overrides this. Its purpose is databases migrated before that default existed, whose IAM history lives in `__EFMigrationsHistory`:

```bash
corely-iam-db db migrate --history-table __EFMigrationsHistory
```

The alternative is to copy the records into the new table once — see the [tool README](../README.md) for the statements and how to verify the result.

## Providers

```bash
corely-iam-db provider list
```

Each provider uses its own migration assembly, both of which ship inside the tool:

| Provider | Migration Project |
|----------|------------------|
| `MySql` | `Corely.IAM.DataAccessMigrations.MySql` |
| `MsSql` | `Corely.IAM.DataAccessMigrations.MsSql` |

## Common Workflows

### First-Time Setup

```bash
corely-iam-db db create -p MsSql -c "your-connection-string"
```

### Adding a Migration (Development)

Migrations are created using the PowerShell scripts at the repository root, not this CLI:

```powershell
.\AddMigration.ps1 "MigrationName"    # Creates migration in both providers
.\RemoveMigration.ps1                  # Removes last migration from both providers
```

This CLI applies migrations that have already been created.

### Production Deployment

```bash
corely-iam-db db script -i -o "deploy.sql" -p MsSql
# Review and execute deploy.sql against the production database
```

## Notes

- `db create` is idempotent — safe to run on an existing database
- `db migrate 0` reverts all migrations but does not drop the database
- `db drop` is destructive and cannot be undone
- Migration assemblies are provider-specific — changing the provider changes which migrations are available
