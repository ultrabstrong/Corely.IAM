# DataAccessMigrations CLI Separation Plan

## Overview

Separate the CLI commands from `Corely.IAM.DataAccessMigrations` into a new `Corely.IAM.DataAccessMigrations.Cli` project, while keeping the migrations and core infrastructure in the original project.

## Goals

1. **Additive change** - Original project continues to work unchanged
2. **Clean separation** - CLI concerns separated from EF Core migration infrastructure
3. **Service registration pattern** - Expose `AddMySqlIamDbContext()` extension for DI setup
4. **Multi-provider ready** - Design supports adding MsSQL (and other providers) later with a single CLI
5. **Future-ready** - Structure allows for later consolidation/refactoring

---

## Architecture

### Before

```
Corely.IAM.DataAccessMigrations/          (Exe)
+-- Program.cs
+-- Commands/
+-- Attributes/
+-- Migrations/
+-- ConfigurationProvider.cs
+-- EFMySqlConfiguration.cs
+-- DatabaseConnectionValidator.cs
+-- IAMDesignTimeDbContextFactory.cs
```

### After

```
Corely.IAM.DataAccessMigrations/          (Library - keeps migrations + DI extension)
+-- Migrations/
+-- EFMySqlConfiguration.cs               (internal)
+-- ServiceCollectionExtensions.cs        (public - AddMySqlIamDbContext)
+-- IAMDesignTimeDbContextFactory.cs
+-- (retains existing Program.cs and commands for backward compatibility)

Corely.IAM.DataAccessMigrations.Cli/      (Exe - new CLI project)
+-- Program.cs                            (duplicated, adapted)
+-- ConfigurationProvider.cs              (duplicated)
+-- DatabaseConnectionValidator.cs        (duplicated)
+-- Commands/                             (duplicated)
|   +-- CommandBase.cs
|   +-- Config.cs
|   +-- Database.cs
|   +-- ConfigCommands/
|   |   +-- Init.cs
|   |   +-- Show.cs
|   |   +-- ShowPath.cs
|   +-- DatabaseCommands/
|       +-- Create.cs
|       +-- Drop.cs
|       +-- ListMigrations.cs
|       +-- Migrate.cs
|       +-- Script.cs
|       +-- Status.cs
|       +-- TestConnection.cs
+-- Attributes/                           (duplicated)
    +-- AttributeBase.cs
    +-- ArgumentAttribute.cs
    +-- OptionAttribute.cs
```

---

## Implementation Steps

### Phase 1: Prepare DataAccessMigrations Project

- [x] **1.1** Fix `EFMySqlConfiguration.cs` - Change `Assembly.GetExecutingAssembly()` to `typeof(EFMySqlConfiguration).Assembly`
- [x] **1.2** Create `ServiceCollectionExtensions.cs` with public `AddMySqlIamDbContext()` extension method

### Phase 2: Create New CLI Project

- [x] **2.1** Create `Corely.IAM.DataAccessMigrations.Cli.csproj` with:
  - OutputType: Exe
  - Same publish settings as original
  - Reference to `Corely.IAM.DataAccessMigrations`
  - Required NuGet packages (System.CommandLine, etc.)

- [x] **2.2** Duplicate files to new project:
  - `Program.cs` (adapt to use service extension)
  - `ConfigurationProvider.cs`
  - `DatabaseConnectionValidator.cs`
  - All `Commands/` files
  - All `Attributes/` files

- [x] **2.3** Update namespaces in duplicated files from `Corely.IAM.DataAccessMigrations` to `Corely.IAM.DataAccessMigrations.Cli`

- [x] **2.4** Update `Program.cs` to use `AddMySqlIamDbContext()` extension

### Phase 3: Update Build Scripts

- [x] **3.1** Update `RebuildAndTest.ps1`:
  ```powershell
  dotnet publish Corely.IAM.DataAccessMigrations.Cli\Corely.IAM.DataAccessMigrations.Cli.csproj -c Release -r win-x64 -p:DebugType=none
  ```

- [x] **3.2** Update `CopyCorelyTools.ps1`:
  - Add new source path for CLI project
  - Copy to `corely-db-cli.exe` (new executable, does NOT replace original)
  - Both `corely-db` (original) and `corely-db-cli` (new) available for comparison

### Phase 4: Validation

- [x] **4.1** Build solution - verify no errors
- [ ] **4.2** Test original `Corely.IAM.DataAccessMigrations` still works
- [ ] **4.3** Test new `Corely.IAM.DataAccessMigrations.Cli` works
- [ ] **4.4** Run `CopyCorelyTools.ps1` and verify `corely-db` command works

---

## Key Code Changes

### 1. EFMySqlConfiguration.cs (Fix Assembly Reference)

```csharp
// Before
b => b.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name)

// After
b => b.MigrationsAssembly(typeof(EFMySqlConfiguration).Assembly.GetName().Name)
```

### 2. New ServiceCollectionExtensions.cs

```csharp
using Corely.IAM.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.DataAccessMigrations;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMySqlIamDbContext(
        this IServiceCollection services,
        string connectionString)
    {
        var configuration = new EFMySqlConfiguration(connectionString);
        
        services.AddDbContext<IamDbContext>(options =>
        {
            configuration.Configure(options);
        });
        
        return services;
    }
}
```

### 3. CLI Program.cs (Using Extension)

```csharp
// Instead of manually configuring DbContext:
services.AddMySqlIamDbContext(ConfigurationProvider.GetConnectionString());
```

---

## Files to Duplicate

| Source (DataAccessMigrations) | Target (DataAccessMigrations.Cli) |
|-------------------------------|-----------------------------------|
| Program.cs | Program.cs |
| ConfigurationProvider.cs | ConfigurationProvider.cs |
| DatabaseConnectionValidator.cs | DatabaseConnectionValidator.cs |
| Commands/CommandBase.cs | Commands/CommandBase.cs |
| Commands/Config.cs | Commands/Config.cs |
| Commands/Database.cs | Commands/Database.cs |
| Commands/ConfigCommands/Init.cs | Commands/ConfigCommands/Init.cs |
| Commands/ConfigCommands/Show.cs | Commands/ConfigCommands/Show.cs |
| Commands/ConfigCommands/ShowPath.cs | Commands/ConfigCommands/ShowPath.cs |
| Commands/DatabaseCommands/Create.cs | Commands/DatabaseCommands/Create.cs |
| Commands/DatabaseCommands/Drop.cs | Commands/DatabaseCommands/Drop.cs |
| Commands/DatabaseCommands/ListMigrations.cs | Commands/DatabaseCommands/ListMigrations.cs |
| Commands/DatabaseCommands/Migrate.cs | Commands/DatabaseCommands/Migrate.cs |
| Commands/DatabaseCommands/Script.cs | Commands/DatabaseCommands/Script.cs |
| Commands/DatabaseCommands/Status.cs | Commands/DatabaseCommands/Status.cs |
| Commands/DatabaseCommands/TestConnection.cs | Commands/DatabaseCommands/TestConnection.cs |
| Attributes/AttributeBase.cs | Attributes/AttributeBase.cs |
| Attributes/ArgumentAttribute.cs | Attributes/ArgumentAttribute.cs |
| Attributes/OptionAttribute.cs | Attributes/OptionAttribute.cs |

---

## Future Considerations

> **See also:** `Future-MultiProvider-Support.md` for multi-provider database design notes.

1. **Delete original commands** - Once CLI project is validated, original commands in DataAccessMigrations can be removed
2. **Shared command base** - Consider moving `CommandBase` and attributes to a shared library if multiple CLI tools need them
3. **Configuration abstraction** - `ConfigurationProvider` could be abstracted if needed by multiple projects

---

## Rollback Plan

If issues arise:
1. The original `Corely.IAM.DataAccessMigrations` project remains fully functional
2. Simply revert `CopyCorelyTools.ps1` to use original project output
3. Remove CLI project from solution
