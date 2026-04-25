# SQL Server Support Implementation Plan

## Overview

Add SQL Server (MsSql) support to the IAM database migration tooling, enabling users to choose SQL Server as their database provider alongside MySQL and MariaDB.

## Goals

1. **MsSql project** - Create `Corely.IAM.DataAccessMigrations.MsSql` with its own migrations
2. **Provider selection** - Add MsSql to CLI provider selection
3. **Update helper scripts** - Include MsSql project in migration scripts

---

## Phase 1: MsSql Migration Project

### 1.1 Update Project File

Update `Corely.IAM.DataAccessMigrations.MsSql.csproj` with required dependencies:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.10">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.10" />
  </ItemGroup>

  <ItemGroup>
    <Folder Include="Migrations\" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Corely.IAM\Corely.IAM.csproj" />
  </ItemGroup>
</Project>
```

- [x] **1.1.1** Update csproj with SQL Server dependencies

### 1.2 Create Project Files

Create the following files in `Corely.IAM.DataAccessMigrations.MsSql`:

- [x] **1.2.1** `MsSqlDesignTimeConstants.cs` - Design-time constants for SQL Server
- [x] **1.2.2** `EFMsSqlConfiguration.cs` - SQL Server-specific EF configuration
- [x] **1.2.3** `IAMDesignTimeDbContextFactory.cs` - Design-time factory for SQL Server
- [x] **1.2.4** `ServiceCollectionExtensions.cs` - `AddMsSqlIamDbContext()` extension
- [x] **1.2.5** Delete `Class1.cs` placeholder file

### 1.3 File Contents

#### MsSqlDesignTimeConstants.cs
```csharp
namespace Corely.IAM.DataAccessMigrations.MsSql;

internal static class MsSqlDesignTimeConstants
{
    public const string DesignTimeMarker = "designtimeonly";

    public const string DesignTimeConnectionString =
        $"Server={DesignTimeMarker};Database={DesignTimeMarker};Trusted_Connection=True;TrustServerCertificate=True;";
}
```

#### EFMsSqlConfiguration.cs
```csharp
using Corely.DataAccess.EntityFramework.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Corely.IAM.DataAccessMigrations.MsSql;

internal class EFMsSqlConfiguration(string connectionString)
    : EFConfigurationBase(connectionString)
{
    public override void Configure(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(
            connectionString,
            b => b.MigrationsAssembly(typeof(EFMsSqlConfiguration).Assembly.GetName().Name)
        );
    }
}
```

#### IAMDesignTimeDbContextFactory.cs
```csharp
using Corely.IAM.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Corely.IAM.DataAccessMigrations.MsSql;

internal class IAMDesignTimeDbContextFactory : IDesignTimeDbContextFactory<IamDbContext>
{
    public IamDbContext CreateDbContext(string[] args)
    {
        var configuration = new EFMsSqlConfiguration(MsSqlDesignTimeConstants.DesignTimeConnectionString);
        var optionsBuilder = new DbContextOptionsBuilder<IamDbContext>();
        configuration.Configure(optionsBuilder);
        return new IamDbContext(configuration);
    }
}
```

#### ServiceCollectionExtensions.cs
```csharp
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.DataAccess;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.DataAccessMigrations.MsSql;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMsSqlIamDbContext(
        this IServiceCollection services,
        string connectionString)
    {
        var configuration = new EFMsSqlConfiguration(connectionString);

        services.AddDbContext<IamDbContext>(options =>
        {
            configuration.Configure(options);
        });

        services.AddSingleton<IEFConfiguration>(configuration);

        return services;
    }
}
```

### 1.4 Generate Initial Migration

- [x] **1.4.1** Run `dotnet ef migrations add InitialMigration` from MsSql project

### 1.5 Add InternalsVisibleTo

- [x] **1.5.1** Update `Corely.IAM\AssemblyInfo.cs` to include MsSql project:
```csharp
[assembly: InternalsVisibleTo("Corely.IAM.DataAccessMigrations.MsSql")]
```

---

## Phase 2: CLI Updates

### 2.1 Add MsSql to DatabaseProvider Enum

Update `Corely.IAM.DataAccessMigrations.Cli\DatabaseProvider.cs`:

```csharp
public enum DatabaseProvider
{
    MySql,
    MariaDb,
    MsSql
}
```

- [x] **2.1.1** Add MsSql to DatabaseProvider enum

### 2.2 Update Program.cs Provider Selection

Add MsSql case to the provider switch in `Program.cs`:

```csharp
case DatabaseProvider.MsSql:
    tempServices.AddMsSqlIamDbContext(connectionString);
    break;
```

- [x] **2.2.1** Add using statement for MsSql namespace
- [x] **2.2.2** Add MsSql case to provider switch

### 2.3 Update CLI Project References

Update `Corely.IAM.DataAccessMigrations.Cli.csproj`:

```xml
<ProjectReference Include="..\Corely.IAM.DataAccessMigrations.MsSql\Corely.IAM.DataAccessMigrations.MsSql.csproj" />
```

- [x] **2.3.1** Add MsSql project reference to CLI

---

## Phase 3: Update Helper Scripts

### 3.1 Update AddMigration.ps1

Add MsSql project to the projects array:

```powershell
$projects = @(
    "Corely.IAM.DataAccessMigrations.MySql",
    "Corely.IAM.DataAccessMigrations.MariaDb",
    "Corely.IAM.DataAccessMigrations.MsSql"
)
```

- [x] **3.1.1** Add MsSql to AddMigration.ps1

### 3.2 Update RemoveMigration.ps1

- [x] **3.2.1** Add MsSql to RemoveMigration.ps1

### 3.3 Update ListMigrations.ps1

- [x] **3.3.1** Add MsSql to ListMigrations.ps1

---

## Phase 4: Documentation and Cleanup

### 4.1 Update README

- [x] **4.1.1** Add MsSql to supported databases list
- [x] **4.1.2** Add MsSql connection string example

### 4.2 Update Settings Template

- [ ] **4.2.1** Document MsSql connection string format in template comments (optional)

---

## SQL Server Connection String Format

```
Server=localhost;Database=IAM;User Id=sa;Password=yourpassword;TrustServerCertificate=True;
```

Or with Windows Authentication:
```
Server=localhost;Database=IAM;Trusted_Connection=True;TrustServerCertificate=True;
```

---

## Validation Checklist

- [x] MsSql project builds successfully
- [x] MsSql migrations can be created via `dotnet ef migrations add`
- [x] CLI correctly selects MsSql provider based on config
- [x] `provider list` shows MySql, MariaDb, and MsSql
- [x] `provider set MsSql` correctly updates config file
- [x] `config init MsSql` creates valid settings file
- [x] `AddMigration.ps1` creates migration in all three projects
- [x] `RemoveMigration.ps1` removes migration from all three projects
- [x] `ListMigrations.ps1` lists migrations from all three projects
- [x] README updated with MsSql documentation

---

## Notes

- SQL Server uses `Microsoft.EntityFrameworkCore.SqlServer` package (not Pomelo)
- No `ServerVersion` detection needed for SQL Server (unlike MySQL/MariaDB)
- Connection string format differs from MySQL/MariaDB
- Consider adding `TrustServerCertificate=True` for development scenarios
