# Corely.IAM.DataAccessMigrations.Cli

Command-line tool for managing the Corely.IAM database — creation, migration, scripting, and provider selection. Built on `System.CommandLine`.

All commands support `--help` for full argument and option details.

## Command Groups

| Group | Purpose |
|-------|---------|
| `config` | Local settings file management (provider, connection string) |
| `provider` | Database provider selection |
| `db` | Database operations — create, migrate, drop, script, status |

## Setup

### 1) Initialize Settings

Create a local settings file with a database provider and connection string:

```bash
dotnet run -- config init mssql -c "Server=(localdb)\MSSQLLocalDB;Database=CorelIAM;Trusted_Connection=True;"
```

The `ProviderName` argument is required. Supported values: `MySql`, `MariaDb`, `MsSql`.

Options:
- `-c, --connection` — connection string
- `-f, --force` — overwrite existing settings file

### 2) Verify Connection

```bash
dotnet run -- config test-connection
```

### 3) View Current Settings

```bash
dotnet run -- config show
```

### 4) Update Connection String

```bash
dotnet run -- config set-connection "Server=prod-server;Database=CorelIAM;..."
```

## Database Operations

### Create Database

Creates the database (if it does not exist) and applies all pending migrations in one step:

```bash
dotnet run -- db create
```

This is the recommended command for initial setup. It combines database creation with full migration application.

### Check Migration Status

View which migrations have been applied and which are pending:

```bash
dotnet run -- db status
```

### List Available Migrations

List all migrations defined in the current provider's migration assembly:

```bash
dotnet run -- db list
```

### Apply Migrations

Apply all pending migrations:

```bash
dotnet run -- db migrate
```

Migrate to a specific migration (applies or reverts as needed):

```bash
dotnet run -- db migrate "MigrationName"
```

Revert all migrations:

```bash
dotnet run -- db migrate 0
```

### Generate SQL Scripts

Generate a SQL script for all migrations:

```bash
dotnet run -- db script
```

Generate a script between two specific migrations:

```bash
dotnet run -- db script "FromMigration" "ToMigration"
```

Options:
- `-o, --output` — write to a file instead of console
- `-i, --idempotent` — generate an idempotent script safe to run multiple times

Production deployments typically use idempotent scripts:

```bash
dotnet run -- db script -i -o "deploy.sql"
```

### Drop Database

```bash
dotnet run -- db drop
```

Prompts for confirmation. Use `-f, --force` to skip the prompt.

## Provider Management

Switch the configured database provider without recreating the settings file:

```bash
# List available providers
dotnet run -- provider list

# Show current provider
dotnet run -- provider show

# Change provider
dotnet run -- provider set mariadb
```

Each provider uses its own migration assembly:

| Provider | Migration Project |
|----------|------------------|
| `MySql` | `Corely.IAM.DataAccessMigrations.MySql` |
| `MariaDb` | `Corely.IAM.DataAccessMigrations.MariaDb` |
| `MsSql` | `Corely.IAM.DataAccessMigrations.MsSql` |

## Common Workflows

### First-Time Setup

```bash
dotnet run -- config init mssql -c "your-connection-string"
dotnet run -- db create
```

### Adding a Migration (Development)

Migrations are created using the PowerShell scripts at the repository root, not this CLI:

```powershell
.\AddMigration.ps1 "MigrationName"    # Creates migration in all 3 providers
.\RemoveMigration.ps1                  # Removes last migration from all providers
```

This CLI applies migrations that have already been created.

### Production Deployment

```bash
dotnet run -- config init mssql -c "prod-connection-string"
dotnet run -- db script -i -o "deploy.sql"
# Review and execute deploy.sql against the production database
```

## Notes

- Settings files are stored locally per project — they are not checked into source control
- `db create` is idempotent — safe to run on an existing database
- `db migrate 0` reverts all migrations but does not drop the database
- `db drop` is destructive and cannot be undone
- Migration assemblies are provider-specific — changing the provider changes which migrations are available
