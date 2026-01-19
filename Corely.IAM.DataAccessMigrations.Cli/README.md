# Corely IAM Database Migration CLI

A command-line tool for managing Corely IAM database migrations and schema.

## Supported Databases

This CLI tool supports migrations for:
- **MySQL** (via `Corely.IAM.DataAccessMigrations.MySql`)
- **MariaDB** (via `Corely.IAM.DataAccessMigrations.MariaDb`) - *coming soon*

## Creating Migrations (Development)

When the data model changes, new migrations need to be created. This requires the EF Core CLI tools and access to the source code.

### Prerequisites

```bash
# Install EF Core tools globally (if not already installed)
dotnet tool install --global dotnet-ef
```

### Creating a New Migration

Run from the migration project directory (e.g., `Corely.IAM.DataAccessMigrations.MySql`):

```bash
# Create a new migration
dotnet ef migrations add <MigrationName>

# Example
dotnet ef migrations add AddNewUserFields
```

### Other Migration Commands

```bash
# Remove the last migration (if not yet applied)
dotnet ef migrations remove

# List all migrations
dotnet ef migrations list

# Generate SQL script for all migrations
dotnet ef migrations script
```

For full documentation, see: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/

## Building

```powershell
# Build
dotnet build

# Publish as single executable
dotnet publish -c Release -r win-x64
```

The output will be in `bin/Release/net9.0/win-x64/publish/`.

## Installation

After building, copy the executable to a location in your PATH, or use the provided `CopyCorelyTools.ps1` script.

## Configuration

The tool requires a `corely-iam-db-migration-settings.json` file in the same directory as the executable.

### Creating the Settings File

```bash
# Create a template settings file
corely-db config init

# Create with a specific connection string
corely-db config init -c "Server=myserver;Database=mydb;User=myuser;Password=mypassword;"

# Overwrite an existing settings file
corely-db config init -f
```

### Settings File Format

```json
{
  "ConnectionStrings": {
    "DataRepoConnection": "Server=localhost;Port=3306;Database=IAM;Uid=root;Pwd=yourpassword;"
  }
}
```

## Commands

### Database Commands (`db`)

| Command | Description |
|---------|-------------|
| `db status` | Show the migration status (applied vs pending) |
| `db list` | List all available migrations |
| `db migrate [target]` | Apply pending migrations (optionally to a specific target) |
| `db script [from] [to]` | Generate a SQL script from migrations |
| `db create` | Create the database and apply all migrations |
| `db drop [-f]` | Drop the database (use `-f` to skip confirmation) |
| `db test-connection` | Test the database connection |

### Config Commands (`config`)

| Command | Description |
|---------|-------------|
| `config init [-f] [-c <connection>]` | Create a new settings file |
| `config show` | Display the current settings file contents |
| `config path` | Display the expected settings file path |

## Usage Examples

```bash
# View help
corely-db --help
corely-db db --help

# Test database connection
corely-db db test-connection

# View migration status
corely-db db status

# Apply all pending migrations
corely-db db migrate

# Migrate to a specific migration
corely-db db migrate 20241221063840_AddRoles

# Revert all migrations
corely-db db migrate 0

# Generate an idempotent SQL script
corely-db db script -i -o migration.sql

# Create database and apply migrations
corely-db db create
```
