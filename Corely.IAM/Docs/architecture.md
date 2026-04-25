# Architecture

Layered architecture with decorator-based cross-cutting concerns and a typed result pattern.

## Layers

```
Services (public) → Processors (internal) → Repositories/UoW → EF Core DbContext → Database
```

- **Services** — public API surface, orchestration and coordination
- **Processors** — internal business logic, domain rules, authorization checks
- **Repositories** — data access via `IRepo<T>` and `IReadonlyRepo<T>`
- **DbContext** — `IamDbContext`, single context for all providers

## Decorator Pattern

Every service and processor is wrapped with decorators registered via Scrutor:

```
TelemetryDecorator → [AuthorizationDecorator] → Implementation
```

- **Authorization decorators** — service level decorators are only used where a context gate is still required; processor level decorators enforce CRUDX permissions
- **Telemetry decorators** — structured logging of method entry/exit

Registration order in `ServiceRegistrationExtensions.cs` matters: last registered = outermost (first to execute).

### System Context Handling

Authorization decorators distinguish between two categories of operations:

- **"Self" operations** (MFA, password, Google auth, deregister self) — require a real user, blocked for system context via `IsNonSystemUserContext()`
- **"Targeting" operations** (register group, list users, etc.) — system context passes through via `HasUserContext()` or `HasAccountContext()`

System context also bypasses CRUDX permission checks at the processor layer (`IsAuthorizedAsync()` returns `true`).

## Result Pattern

All operations return typed result objects with result codes. No exceptions for business logic failures:

```csharp
public record CreateUserResult(CreateUserResultCode ResultCode, string Message, Guid CreatedId);

public enum CreateUserResultCode
{
    Success,
    UserExistsError,
    ValidationError,
}
```

Common result types:
- `RetrieveSingleResult<T>` — single entity with optional effective permissions
- `RetrieveListResult<T>` — paginated list via `PagedResult<T>`
- `ModifyResult` — generic success/failure for updates

## Validation

FluentValidation is used for all input validation. Validators are auto-discovered from the assembly and injected via `IValidationProvider`.

## Database

- **Entity Framework Core** with three providers: MySQL, MariaDB, SQL Server
- Entity configurations auto-discovered via reflection in `IamDbContext.OnModelCreating`
- **SQL Server constraint**: no cascade deletes on M:M relationships — processors manually clear collections before deleting
- Migrations are in separate projects per provider

## Multi-Target

Corely.IAM targets both `net9.0` and `net10.0`.

## Time Abstraction

`TimeProvider.System` is registered as a singleton. All time-dependent code uses `TimeProvider` instead of `DateTime.UtcNow`.
