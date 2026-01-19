# Future: Multi-Provider Database Support

> **Note:** This document captures future design considerations. It is NOT in scope for the current CLI separation refactor.

## Context

The CLI separation refactor (see `DataAccessMigrations-CLI-Refactor.md`) is designed to later support multiple database providers (MySQL, MsSQL, etc.) with a single CLI.

## Target Architecture

```
Corely.IAM.DataAccessMigrations/           (MySQL migrations library)
+-- Migrations/
+-- EFMySqlConfiguration.cs
+-- ServiceCollectionExtensions.cs         (AddMySqlIamDbContext)

Corely.IAM.DataAccessMigrations.MsSql/     (MsSQL migrations library - FUTURE)
+-- Migrations/
+-- EFMsSqlConfiguration.cs
+-- ServiceCollectionExtensions.cs         (AddMsSqlIamDbContext)

Corely.IAM.DataAccessMigrations.Cli/       (Single CLI for all providers)
+-- Program.cs                             (provider selection logic)
+-- ConfigurationProvider.cs               (reads provider from settings)
+-- Commands/
```

## Provider Selection Strategy

The CLI will need to:
1. Read a `Provider` setting from config (e.g., "MySql" or "MsSql")
2. Register the appropriate DbContext based on provider
3. Both provider libraries expose a consistent service extension pattern

Example config:
```json
{
  "Provider": "MySql",
  "ConnectionStrings": {
    "DataRepoConnection": "..."
  }
}
```

Example Program.cs pattern:
```csharp
var provider = ConfigurationProvider.GetProvider();
var connectionString = ConfigurationProvider.GetConnectionString();

if (provider == "MySql")
    services.AddMySqlIamDbContext(connectionString);
else if (provider == "MsSql")
    services.AddMsSqlIamDbContext(connectionString);
```

## Design Decisions Made in CLI Separation Refactor

To support multi-provider later, the CLI separation refactor uses:

1. **Provider-specific service extension naming** - `AddMySqlIamDbContext()` allows MsSQL to later add `AddMsSqlIamDbContext()`
2. **Commands are provider-agnostic** - Commands work with `IamDbContext` directly, not provider-specific types
