# MariaDB Support Implementation Plan

## Overview

Add MariaDB support to the IAM database migration tooling, enabling users to choose between MySQL and MariaDB as their database provider.

## Goals

1. **MariaDB project** - Create `Corely.IAM.DataAccessMigrations.MariaDb` with its own migrations
2. **Provider selection** - CLI reads provider type from config and uses appropriate migrations
3. **Provider management commands** - Commands to list, set, and change providers
4. **Migration helper scripts** - Scripts to create/remove migrations across all provider projects

---

## Phase 1: MariaDB Project & CLI Updates

### 1.1 Create MariaDB Migration Project

Create `Corely.IAM.DataAccessMigrations.MariaDb` with the same structure as MySQL:

```
Corely.IAM.DataAccessMigrations.MariaDb/
+-- Migrations/
+-- EFMariaDbConfiguration.cs
+-- MariaDbDesignTimeConstants.cs
+-- IAMDesignTimeDbContextFactory.cs
+-- ServiceCollectionExtensions.cs          (AddMariaDbIamDbContext)
+-- Corely.IAM.DataAccessMigrations.MariaDb.csproj
```

**Files to create:**

- [x] **1.1.1** `Corely.IAM.DataAccessMigrations.MariaDb.csproj` - Same structure as MySql project
- [x] **1.1.2** `MariaDbDesignTimeConstants.cs` - Design-time constants for MariaDB
- [x] **1.1.3** `EFMariaDbConfiguration.cs` - MariaDB-specific EF configuration
- [x] **1.1.4** `IAMDesignTimeDbContextFactory.cs` - Design-time factory for MariaDB
- [x] **1.1.5** `ServiceCollectionExtensions.cs` - `AddMariaDbIamDbContext()` extension
- [x] **1.1.6** Generate initial migration: `dotnet ef migrations add InitialMigration`

### 1.2 Update CLI Settings Format

Update `corely-iam-db-migration-settings.json` to include provider type:

```json
{
  "Provider": "MySql",
  "ConnectionStrings": {
    "DataRepoConnection": "Server=localhost;Port=3306;Database=IAM;Uid=root;Pwd=yourpassword;"
  }
}
```

**Files to update:**

- [x] **1.2.1** `ConfigurationProvider.cs` - Add `GetProvider()` method
- [x] **1.2.2** `Program.cs` - Register DbContext based on provider type

### 1.3 Update CLI Program.cs for Provider Selection

```csharp
var provider = ConfigurationProvider.GetProvider();
var connectionString = ConfigurationProvider.GetConnectionString();

switch (provider?.ToLower())
{
    case "mysql":
        services.AddMySqlIamDbContext(connectionString);
        break;
    case "mariadb":
        services.AddMariaDbIamDbContext(connectionString);
        break;
    default:
        throw new InvalidOperationException($"Unknown provider: {provider}");
}
```

### 1.4 Add Provider Management Commands

#### 1.4.1 List Available Providers Command

```
corely-db provider list
```

Output:
```
Available database providers:
  - MySql
  - MariaDb
```

- [x] Create `Commands/Provider.cs` - Parent command
- [x] Create `Commands/ProviderCommands/List.cs` - List available providers

#### 1.4.2 Show Current Provider Command

```
corely-db provider show
```

Output:
```
Current provider: MySql
```

- [x] Create `Commands/ProviderCommands/Show.cs` - Show current provider

#### 1.4.3 Set Provider Command

```
corely-db provider set MariaDb
```

- [x] Create `Commands/ProviderCommands/Set.cs` - Change provider in config file

### 1.5 Update Config Init Command

Require provider type as an argument:

```bash
# New syntax (provider required)
corely-db config init MySql
corely-db config init MariaDb

# With connection string
corely-db config init MySql -c "Server=localhost;Database=mydb;..."

# With force overwrite
corely-db config init MySql -f
```

- [x] **1.5.1** Update `Commands/ConfigCommands/Init.cs` to require provider argument
- [x] **1.5.2** Validate provider is one of the supported types
- [x] **1.5.3** Include provider in generated settings file

### 1.6 Update Build Scripts

- [x] **1.6.1** Update `RebuildAndTest.ps1` to build MariaDb project
- [x] **1.6.2** Update CLI project to reference both MySql and MariaDb projects
- [x] **1.6.3** Add MariaDb project to solution

---

## Phase 2: Migration Helper Scripts

### 2.1 Add Migration Script

Create `AddMigration.ps1` that creates a migration in all provider projects:

```powershell
# Usage: .\AddMigration.ps1 <MigrationName>
# Example: .\AddMigration.ps1 AddUserPreferences

param(
    [Parameter(Mandatory=$true)]
    [string]$MigrationName
)

$projects = @(
    "Corely.IAM.DataAccessMigrations.MySql",
    "Corely.IAM.DataAccessMigrations.MariaDb"
)

foreach ($project in $projects) {
    Write-Host "Creating migration '$MigrationName' in $project..." -ForegroundColor Cyan
    Push-Location $project
    dotnet ef migrations add $MigrationName
    Pop-Location
}

Write-Host "Migration '$MigrationName' created in all projects." -ForegroundColor Green
```

- [x] **2.1.1** Create `AddMigration.ps1` in solution root

### 2.2 Remove Migration Script

Create `RemoveMigration.ps1` that removes the last migration from all provider projects:

```powershell
# Usage: .\RemoveMigration.ps1
# Removes the last unapplied migration from all provider projects

param(
    [switch]$Force
)

$projects = @(
    "Corely.IAM.DataAccessMigrations.MySql",
    "Corely.IAM.DataAccessMigrations.MariaDb"
)

foreach ($project in $projects) {
    Write-Host "Removing last migration from $project..." -ForegroundColor Cyan
    Push-Location $project
    if ($Force) {
        dotnet ef migrations remove --force
    } else {
        dotnet ef migrations remove
    }
    Pop-Location
}

Write-Host "Last migration removed from all projects." -ForegroundColor Green
```

- [x] **2.2.1** Create `RemoveMigration.ps1` in solution root

### 2.3 List Migrations Script

Create `ListMigrations.ps1` that shows migrations across all provider projects:

```powershell
# Usage: .\ListMigrations.ps1
# Lists migrations in all provider projects

$projects = @(
    "Corely.IAM.DataAccessMigrations.MySql",
    "Corely.IAM.DataAccessMigrations.MariaDb"
)

foreach ($project in $projects) {
    Write-Host "`n=== $project ===" -ForegroundColor Cyan
    Push-Location $project
    dotnet ef migrations list
    Pop-Location
}
```

- [x] **2.3.1** Create `ListMigrations.ps1` in solution root

### 2.4 Add Scripts to Solution Items

Add all migration helper scripts to the Solution Items folder in `Corely.IAM.sln`:

- [x] **2.4.1** Add `AddMigration.ps1` to Solution Items
- [x] **2.4.2** Add `RemoveMigration.ps1` to Solution Items
- [x] **2.4.3** Add `ListMigrations.ps1` to Solution Items

Update the solution file's Solution Items section:
```
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "Solution Items", "Solution Items", "{...}"
    ProjectSection(SolutionItems) = preProject
        ...
        AddMigration.ps1 = AddMigration.ps1
        RemoveMigration.ps1 = RemoveMigration.ps1
        ListMigrations.ps1 = ListMigrations.ps1
    EndProjectSection
EndProject
```

---

## Target Architecture (After Implementation)

```
Corely.IAM.DataAccessMigrations.MySql/      (MySQL migrations library)
+-- Migrations/
+-- EFMySqlConfiguration.cs
+-- MySqlDesignTimeConstants.cs
+-- IAMDesignTimeDbContextFactory.cs
+-- ServiceCollectionExtensions.cs          (AddMySqlIamDbContext)

Corely.IAM.DataAccessMigrations.MariaDb/    (MariaDB migrations library)
+-- Migrations/
+-- EFMariaDbConfiguration.cs
+-- MariaDbDesignTimeConstants.cs
+-- IAMDesignTimeDbContextFactory.cs
+-- ServiceCollectionExtensions.cs          (AddMariaDbIamDbContext)

Corely.IAM.DataAccessMigrations.Cli/        (Single CLI for all providers)
+-- Program.cs                              (provider selection logic)
+-- ConfigurationProvider.cs                (reads provider from settings)
+-- Commands/
    +-- Provider.cs                         (provider parent command)
    +-- ProviderCommands/
        +-- List.cs                         (list available providers)
        +-- Show.cs                         (show current provider)
        +-- Set.cs                          (change provider)

Solution Root/
+-- AddMigration.ps1                        (create migration in all projects)
+-- RemoveMigration.ps1                     (remove migration from all projects)
+-- ListMigrations.ps1                      (list migrations in all projects)
```

---

## Updated Settings File Format

```json
{
  "Provider": "MySql",
  "ConnectionStrings": {
    "DataRepoConnection": "Server=localhost;Port=3306;Database=IAM;Uid=root;Pwd=yourpassword;"
  }
}
```

---

## Updated CLI Commands

| Command | Description |
|---------|-------------|
| `config init <provider> [-f] [-c]` | Create settings file with specified provider |
| `provider list` | List available database providers |
| `provider show` | Show current provider from config |
| `provider set <provider>` | Change provider in config file |

---

## Validation Checklist

- [x] MariaDB project builds successfully
- [x] MariaDB migrations can be created via `dotnet ef migrations add`
- [x] CLI correctly selects provider based on config
- [x] `provider list` shows both MySql and MariaDb
- [x] `provider set` correctly updates config file
- [x] `config init` requires provider argument
- [x] `AddMigration.ps1` creates migration in both projects
- [x] `RemoveMigration.ps1` removes migration from both projects
- [x] README updated with new commands
