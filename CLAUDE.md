# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Host-agnostic, multi-tenant identity and access management library for .NET applications. Provides authentication, authorization, RBAC, and permission management without external service dependencies. Targets .NET 10.0.

## Build Commands

```powershell
# Full rebuild, format, and test
.\RebuildAndTest.ps1

# Build
dotnet build Corely.IAM.slnx
```

## Testing

### Tiers

Each tier owns exactly one seam. Full reasoning, the per-item test case inventory, and the
findings from building it live in `Plans/Completed/testing-strategy.md`.

| Tier | Owns | Substrate | Project |
|------|------|-----------|---------|
| Unit | One class's logic, dependencies substituted | No database | `Corely.IAM.UnitTests`, `Corely.IAM.Web.UnitTests`, `Corely.IAM.DevTools.UnitTests`, `Corely.IAM.DataAccessMigrations.Cli.UnitTests` |
| Integration | Persistence — EF translation, schema, provider behavior | SQLite / Testcontainers | `Corely.IAM.IntegrationTests` |
| Functional | HTTP — middleware, cookies, redirects, ASP.NET pipeline | `WebApplicationFactory` in-process | `Corely.IAM.Web.FunctionalTests` |
| E2E | Browser — JS, real cookie enforcement, external redirects | Playwright | *deliberately none* |

### Where does a new test go?

Walk down and stop at the **first** tier that can prove the case:

1. Provable with no database and no host? → **Unit**
2. Needs real SQL translation, real schema, or provider behavior? → **Integration**
3. Needs the HTTP pipeline — middleware, cookies, redirects, antiforgery? → **Functional**
4. Needs a real browser — JS, actual cookie enforcement, external OAuth? → **E2E**

**A case proven at tier N is never re-proven at tier N+1.** Higher tiers exercise the seam they
own, not logic that already passed below. A functional test asserting a permission calculation is
in the wrong tier — it should assert only that the pipeline surfaced the decision.

If a case seems to fit two tiers, it is usually two cases. Split it.

**`MockRepo` stays for the unit tier.** It is a hand-written re-implementation of EF semantics and
therefore can drift, but the integration tier now covers everything it cannot model — translation,
constraints, join-entity delete behavior, provider differences. Replacing it wholesale would slow
the unit suite by roughly 40x per test (measured: 1365 unit tests in ~4s against the mock; 66
integration tests in ~8s against SQLite). Put behavior that depends on the database in the
integration tier rather than trying to make the mock more faithful.

**Do not propose replacing the repository layer or `MockRepo` with the EF in-memory provider, with
raw `DbContext` injection, or with `DbSet` mocking.** All three have been evaluated against
Microsoft's own testing guidance, which recommends the repository pattern for exactly this case and
explicitly discourages the alternatives — the in-memory provider is slower than SQLite, cannot run
`ExecuteUpdateAsync` (used here for token revocation and password-recovery expiry), and is
supported only for legacy applications.

The reasoning, the sources, the honest cost, and the specific conditions that *would* justify
changing course are recorded in `Corely.DataAccess/DESIGN-RATIONALE.md`. Read it before raising the
question; if none of the listed conditions hold, the answer stands.

E2E is deliberately empty. Do not add Playwright unless the gap needs JS/Blazor, verification that
a browser *enforces* cookie attributes (rather than that the app *sets* them), or an external OAuth
redirect. Otherwise the answer is Functional.

### Running tests

The test projects run on **xunit.v3 / Microsoft.Testing.Platform**, not VSTest. `global.json` at
the repo root opts `dotnet test` into MTP mode; without it the build fails outright on .NET 10 SDK.
This changes the command line: projects and solutions are named with `--project` / `--solution`
rather than positionally, and `--filter` becomes `--filter-query` with a path expression.

```powershell
# Everything (solution-wide; this is what RebuildAndTest.ps1 runs, plus --coverage)
dotnet test --solution Corely.IAM.slnx

# Unit tier
dotnet test --project Corely.IAM.UnitTests
dotnet test --project Corely.IAM.Web.UnitTests
dotnet test --project Corely.IAM.DevTools.UnitTests
dotnet test --project Corely.IAM.DataAccessMigrations.Cli.UnitTests

# Integration tier — real EF on SQLite. No external dependencies.
dotnet test --project Corely.IAM.IntegrationTests

# Functional tier — boots the real WebApp in-process on SQLite. No external dependencies.
dotnet test --project Corely.IAM.Web.FunctionalTests

# Single test class / method. The filter is /assembly/namespace/class/method — `*` any segment.
dotnet test --project Corely.IAM.UnitTests --filter-query "/*/*/UserProcessorTests/*"
dotnet test --project Corely.IAM.UnitTests --filter-query "/*/*/UserProcessorTests/CreateUser_Fails_WhenUserExists"
```

A filter matching nothing exits **8**, not 0 — a typo'd class name looks like a clean run in the
summary but fails the exit code. Check the reported test count.

**Nothing above needs a running database, Docker, or any manual setup.** The integration and
functional tiers create their own SQLite databases, seed through the real registration service, and
drive a controllable clock — a seven-day session expiry is asserted in milliseconds, never by
sleeping.

### Provider matrix (opt-in, needs Docker)

Both shipped providers are exercised by Testcontainers. These are **skipped by default**
because spinning database containers takes minutes:

```powershell
$env:CORELY_RUN_CONTAINER_TESTS = "1"
dotnet test --project Corely.IAM.IntegrationTests --filter-query "/*/*/*ProviderMatrixTests/*"
```

If Docker is not running, start it — `Start-Process "C:\Program Files\Docker\Docker\Docker Desktop.exe"`,
then wait for `docker info` to succeed. Docker being down is never a reason to report a behavior
as unverifiable; it is a reason to start Docker.

**When a tier is added, its run command goes here in the same change** — a tier that exists but is
undocumented is one a future session will not find.

### Local verification capabilities

Recorded so no session claims a behavior "cannot be verified locally" without checking. All of
this is directly reachable and requires no manual setup by the user:

- **Databases** — SQLite in-memory, LocalDB, a local SQL Server instance, and any provider via
  Docker.
- **Docker** — not always running, but **startable on request**. Not a blocker.
- **The WebApp** — launchable locally (`dotnet run` in `Corely.IAM.WebApp`), with a demo dataset
  defined in `Corely.IAM.WebApp/DemoSetup/SeedWebAppDemo.ps1`.
- **Browser automation** — can drive the running app: sign in, read cookies, inspect pages.
- **Direct database access** — rows can be read and written to arrange or verify state.

Before saying something cannot be tested or verified, check it against this list.

## Code Formatting

CSharpier enforced via MSBuild integration. Files are auto-formatted on build.

**IMPORTANT for Claude Code:** After making changes, ALWAYS run `.\RebuildAndTest.ps1` to format, rebuild, and test everything before committing.

## Migrations

```powershell
# Run from repo root — all scripts target both DB providers (MySQL, SQL Server)
.\AddMigration.ps1 "MigrationName"    # Creates migration in all providers
.\RemoveMigration.ps1                  # Removes last migration from all providers
.\ListMigrations.ps1                   # Lists migrations (no DB connection needed)
```

## Running the WebApp Locally

### Prerequisites

1. **.NET 10.0 SDK**
2. **SQL Server** (LocalDB or full instance) — or MySQL if you change the provider

### Setup Steps

**1. Generate a system encryption key**

```powershell
cd Corely.IAM.DevTools
dotnet run -- sym-encrypt --create
# Outputs a hex key string — copy it
```

**2. Configure `Corely.IAM.WebApp/appsettings.json`**

Fill in the two required values (see `appsettings.template.json` for reference):

| Key | Value |
|-----|-------|
| `ConnectionStrings:DefaultConnection` | Your SQL Server connection string (e.g. `Server=(localdb)\MSSQLLocalDB;Database=CorelIAM;Trusted_Connection=True;`) |
| `Security:SystemKey` | The hex key from step 1 |

`Database:Provider` defaults to `"mssql"`. Change to `"mysql"` if needed.

**3. Create the database and apply migrations**

```powershell
cd Corely.IAM.DataAccessMigrations.Cli
dotnet run -- db create -p MsSql -c "your-connection-string"
```

- `db create` creates the database and applies all pending migrations
- Provider and connection string come from `--provider` / `--connection-string`, or from
  `CORELY_IAM_DB_PROVIDER` / `CORELY_IAM_DB_CONNECTION`. There is no settings file - as an
  installed tool it would live in the tool's own install directory, shared machine-wide.
- IAM records migrations in `__CorelyIamMigrationsHistory`, not the default. `--history-table`
  overrides it for databases migrated before that default existed.

**4. Run the app**

Set `Corely.IAM.WebApp` as the startup project in Visual Studio and press F5, or:

```powershell
cd Corely.IAM.WebApp
dotnet run
```

The app launches at **https://localhost:7100**.

### Optional: Seq Logging

The default config sends structured logs to [Seq](https://datalust.co/seq) at `http://localhost:5341`. If Seq isn't running, the app still works — console logging is unaffected. Remove the Seq entry from `Serilog:WriteTo` in `appsettings.json` to suppress connection warnings.

## Architecture

### Solution Structure

| Project | Purpose |
|---------|---------|
| `Corely.IAM` | Core library — business logic, data access, security (net10.0) |
| `Corely.IAM.UnitTests` | Test suite (XUnit, Moq, AutoFixture, FluentAssertions) |
| `Corely.IAM.ConsoleTest` | Console app for manual testing and demonstration |
| `Corely.IAM.DevTools` | Developer utilities for crypto operations (encryption, hashing, signing, encoding) |
| `Corely.IAM.DataAccessMigrations.Cli` | Database migration CLI, published as the `corely-iam-db` .NET tool (System.CommandLine) |
| `Corely.IAM.DataAccessMigrations.MySql` | MySQL EF Core migrations |
| `Corely.IAM.DataAccessMigrations.MsSql` | SQL Server EF Core migrations |

### Layered Architecture

```
Services (public) → Processors (internal) → Repositories/UoW → EF Core DbContext → Database
```

Processors are wrapped with **authorization + telemetry decorators** via Scrutor. Services always have telemetry decorators, and only the services that still need a service-layer context gate keep authorization decorators.
- `AuthorizationDecorator` — services validate context when needed; processors enforce permissions before calling the inner implementation
- `TelemetryDecorator` — logs operations

Registration order in `ServiceRegistrationExtensions.cs` matters: decorators are applied bottom-up (last registered = outermost).

Authorization is split into two layers:
- **Service decorators** — validate context only (`HasUserContext()` / `HasAccountContext()` / `IsNonSystemUserContext()`). They do NOT check CRUDX permissions.
- **Processor decorators** — enforce specific CRUDX permission checks on resources via `AuthorizationProvider.IsAuthorizedAsync()`.

Service decorators use `IsNonSystemUserContext()` for "self" operations (MFA, password, Google auth, deregister self) that require a real user. Other operations use `HasUserContext()` / `HasAccountContext()` which allow system context to pass through.

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
- Two DB providers (MySQL, SQL Server) each in separate migration projects. MySQL uses
  Oracle's `MySql.EntityFrameworkCore`; MariaDB support was dropped when Pomelo stopped
  shipping (no EF 10 release, no commits since August 2025).

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
- Host-agnostic auth context: `IUserContextProvider` (read-only) for reading context, `IAuthenticationService` for setting context (`AuthenticateWithTokenAsync`, `AuthenticateAsSystem`) — no HttpContext dependency
- System context for headless processes: `IAuthenticationService.AuthenticateAsSystem()` creates a fully-permissioned context that bypasses permission checks but blocks "self" operations (MFA, password, Google auth). `IsNonSystemUserContext()` on `IAuthorizationProvider` gates these self operations.
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
