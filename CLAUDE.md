# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Host-agnostic, multi-tenant identity and access management library for .NET applications. Provides authentication, authorization, RBAC, and permission management without external service dependencies. Multi-target framework: .NET 9.0 and .NET 10.0.

## Build Commands

```powershell
# Full rebuild, format, and test
.\RebuildAndTest.ps1

# Build
dotnet build Corely.IAM.sln
```

## Testing

```powershell
# Run all tests
dotnet test Corely.IAM.UnitTests

# Run a single test class
dotnet test --filter "UserProcessorTests" Corely.IAM.UnitTests

# Run a single test method
dotnet test --filter "UserProcessorTests.CreateUserAsync_WithValidRequest_ReturnsSuccess" Corely.IAM.UnitTests
```

## Code Formatting

CSharpier enforced via MSBuild integration. Files are auto-formatted on build.

**IMPORTANT for Claude Code:** After making changes, ALWAYS run `.\RebuildAndTest.ps1` to format, rebuild, and test everything before committing.

## Migrations

```powershell
# Run from repo root — all scripts target all 3 DB providers (MySQL, MariaDB, SQL Server)
.\AddMigration.ps1 "MigrationName"    # Creates migration in all providers
.\RemoveMigration.ps1                  # Removes last migration from all providers
.\ListMigrations.ps1                   # Lists migrations (no DB connection needed)
```

## Architecture

### Solution Structure

| Project | Purpose |
|---------|---------|
| `Corely.IAM` | Core library — business logic, data access, security (multi-target net9.0/net10.0) |
| `Corely.IAM.UnitTests` | Test suite (XUnit, Moq, AutoFixture, FluentAssertions) |
| `Corely.IAM.ConsoleTest` | Console app for manual testing and demonstration |
| `Corely.IAM.DevTools` | Developer utilities for crypto operations (encryption, hashing, signing, encoding) |
| `Corely.IAM.DataAccessMigrations.Cli` | Database migration CLI tool (System.CommandLine) |
| `Corely.IAM.DataAccessMigrations.MySql` | MySQL EF Core migrations |
| `Corely.IAM.DataAccessMigrations.MariaDb` | MariaDB EF Core migrations |
| `Corely.IAM.DataAccessMigrations.MsSql` | SQL Server EF Core migrations |

### Layered Architecture

```
Services (public) → Processors (internal) → Repositories/UoW → EF Core DbContext → Database
```

Every processor and service is wrapped with **decorator layers** via Scrutor:
- `AuthorizationDecorator` — checks permissions before calling the inner implementation
- `TelemetryDecorator` — logs operations

Registration order in `ServiceRegistrationExtensions.cs` matters: decorators are applied bottom-up (last registered = outermost).

Authorization is split into two layers:
- **Service decorators** — validate context only (`HasUserContext()` / `HasAccountContext()`). They do NOT check CRUDX permissions.
- **Processor decorators** — enforce specific CRUDX permission checks on resources via `AuthorizationProvider.IsAuthorizedAsync()`.

Service methods that appear "unguarded" (e.g., `RegisterUsersWithGroupAsync`) are protected at the processor level where the actual work happens.

### Domain Structure

Each domain (Accounts, Users, BasicAuths, Groups, Roles, Permissions) follows a consistent folder layout:

```
Domain/
├── Constants/        # Domain constants (SCREAMING_SNAKE_CASE)
├── Entities/         # EF Core entities
├── Models/           # Request/response/domain models
├── Processors/       # Business logic + authorization/telemetry decorators
├── Mappers/          # Entity ↔ Model mapping
└── Validators/       # FluentValidation rules
```

### Data Layer

- **Entity Framework Core** — primary ORM, single `IamDbContext` for all providers via `IEFConfiguration`
- Entity configurations auto-discovered via reflection in `IamDbContext.OnModelCreating`
- Three DB providers (MySQL, MariaDB, SQL Server) each in separate migration projects

**SQL Server constraint**: No cascade deletes on M:M relationships. All many-to-many relationships use explicit join entities (`JoinEntities.cs`) with `DeleteBehavior.NoAction`. Processors must manually `.Include()` and `.Clear()` collections before deleting entities.

### DI Registration

- **Production**: `AddIAMServicesWithEF()` — registers EF Core repositories and UoW
- **Testing**: `AddIAMServicesWithMockDb()` — registers in-memory mock repositories

New services go in `Services/` with an interface, registered in `ServiceRegistrationExtensions.cs`. New processors go in their domain's `Processors/` folder and should follow the existing Authorization + Telemetry decorator pattern.

### Security Model

- Encryption keys stored encrypted in the database using system keys provisioned via `ISymmetricKeyStoreProvider` / `IAsymmetricKeyStoreProvider` (never in code)
- Always use `ISymmetricEncryptedValue` or `IAsymmetricEncryptedValue` — never store decrypted values as strings
- CRUDX permission model (Create, Read, Update, Delete, Execute) with wildcard support (`ResourceId == Guid.Empty` = all resources)
- JWT-based authentication via `AuthenticationProvider`
- Host-agnostic auth context: `UserContextProvider` implements both `IUserContextProvider` (read) and `IUserContextSetter` (write) — no HttpContext dependency
- Multi-tenant user model: users exist independently of accounts (M:M relationship). There is no concept of "user A administrates user B" — account owners can register/deregister users with account entities but cannot read or modify other users directly.

## Development Patterns

### Philosophy

Favor brevity over verbosity when planning and writing code. Code that isn't written cannot break, and doesn't need to be maintained.

### Comments

Comments should explain *why*, not *what*. Do not add comments that describe exactly what the code below them does — the code itself should be self-documenting. Good comments explain:
- Non-obvious business logic or domain rules
- Why a particular approach was chosen over alternatives
- Edge cases or gotchas that aren't apparent from the code

```csharp
// BAD - describes what the code does
// Create the user
await CreateUserAsync(request);
// Get the account
var account = await GetAccountAsync(accountId);

// GOOD - explains why (when needed)
// Wildcard permission — Guid.Empty grants access to all resources of this type
if (permission.ResourceId == Guid.Empty) return true;
```

### Primary Constructors

Use primary constructors — all projects support C# 12+:

```csharp
// CORRECT - primary constructor
public class UserProcessor(IRepo<UserEntity> userRepo, IValidationProvider validationProvider)
{
    // ...
}

// WRONG - traditional constructor
public class UserProcessor
{
    private readonly IRepo<UserEntity> _userRepo;
    public UserProcessor(IRepo<UserEntity> userRepo) { _userRepo = userRepo; }
}
```

### Service Registration

New services go in `Services/` folder with interface, registered as `Scoped` in `ServiceRegistrationExtensions.cs`. Follow the existing pattern of adding Authorization and Telemetry decorators via Scrutor's `.Decorate<>()`.

### String Validation

Use `string.IsNullOrWhiteSpace()` instead of `string.IsNullOrEmpty()` for string validation:

```csharp
// CORRECT - catches null, empty, and whitespace-only strings
if (string.IsNullOrWhiteSpace(input))
    return false;

// WRONG - allows whitespace-only strings like "   "
if (string.IsNullOrEmpty(input))
    return false;
```

Only use `IsNullOrEmpty` when whitespace-only strings are intentionally valid input.

### Time Abstraction

Use `TimeProvider` instead of `DateTime.UtcNow` or `DateTimeOffset.UtcNow` for testability:

```csharp
// CORRECT - inject TimeProvider
public class MyService(TimeProvider timeProvider)
{
    public void DoWork()
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
    }
}

// WRONG - direct DateTime usage
public class MyService
{
    public void DoWork()
    {
        var utcNow = DateTime.UtcNow;
    }
}
```

`TimeProvider.System` is registered as a singleton in DI.

### Magic Strings

Use constants instead of magic strings. When a string value is used in multiple places or has semantic meaning, define it as a constant:

```csharp
// CORRECT - use defined constants
if (role.Name == RoleConstants.OWNER_ROLE_NAME) return true;

// WRONG - magic string
if (role.Name == "Owner") return true;
```

### Result Pattern

All operations return typed result objects with result codes. No exceptions for business logic failures:

```csharp
// Return result codes, not exceptions
return new CreateUserResult(CreateUserResultCode.UserExistsError, "Username already taken", Guid.Empty);
```

### Naming Conventions

- `Service` = public top-level coordination; `Processor` = internal business logic; `Provider` = non-domain-specific functionality; `Repo` = repository (internal)
- `Model` = domain/provider data objects; `Entity` = database data objects; `DTO` = data transfer between layers
- `_camelCase` for private fields, `PascalCase` for properties/methods, `USE_SCREAMING_SNAKE_CASE` for constants
- `Async` suffix on all async methods
- Prefix interfaces with `I`, postfix abstract classes with `Base`
- One class/enum/interface per file
- Use `using` statements (not fully qualified names); no `#region` tags; simplified collection initializers
- Use `Corely.Security` for all encryption/hashing — domain-agnostic code goes in `Corely.Common`

### Plans

Store implementation plans in `Plans/` at the repository root.
