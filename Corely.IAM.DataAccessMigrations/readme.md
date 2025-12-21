# IAM Database Migration Tool

A command-line tool for managing IAM database migrations and schema.

## Building the Tool

### Development Build

```bash
dotnet build
dotnet run -- <command>
```

### Publishing as a Single Executable

To create a self-contained single-file executable:

```powershell
# For Windows x64
dotnet publish -c Release -r win-x64

# For Linux x64
dotnet publish -c Release -r linux-x64

# For macOS x64
dotnet publish -c Release -r osx-x64

# For macOS ARM (M1/M2/M3)
dotnet publish -c Release -r osx-arm64
```

The output will be in `bin/Release/net8.0/<runtime>/publish/` and will contain a single executable file.

## Configuration

The tool requires a `corely-iam-db-migration-settings.json` file in the current working directory. This file contains the database connection string.

### Settings File Location

The settings file should be placed in the directory where you run the tool from (current working directory).

### Creating the Settings File

You can create the settings file manually or use the built-in command:

```bash
# Create a template settings file
iammigrate config init

# Create with a specific connection string
iammigrate config init -c "Server=myserver;Database=mydb;User=myuser;Password=mypassword;"

# Overwrite an existing settings file
iammigrate config init -f
```

### Settings File Format

```json
{
  "ConnectionStrings": {
    "DataRepoConnection": "Server=localhost;Database=YourDatabase;User=root;Password=yourpassword;"
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
| `db create` | Create the database if it doesn't exist |
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
iammigrate --help
iammigrate db --help

# Test database connection
iammigrate db test-connection

# View migration status
iammigrate db status

# Apply all pending migrations
iammigrate db migrate

# Migrate to a specific migration
iammigrate db migrate 20241221063840_AddRoles

# Revert all migrations
iammigrate db migrate 0

# Generate an idempotent SQL script
iammigrate db script -i -o migration.sql

# Generate a script between two migrations
iammigrate db script 20241114143036_AddGroups 20241221063840_AddRoles -o upgrade.sql
```

## Managing Migrations (Development)

For creating new migrations during development, use the EF Core CLI tools:

Full help: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/?tabs=dotnet-core-cli

```bash
# Create a new migration
dotnet ef migrations add <MigrationName>

# Remove the last migration
dotnet ef migrations remove

