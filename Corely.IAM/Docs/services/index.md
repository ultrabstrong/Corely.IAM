# Services

Eight public services form the API surface of Corely.IAM. All are registered as scoped and wrapped with telemetry decorators. Service-layer authorization decorators remain only on the services that still need context gating before the implementation runs.

## Service Overview

| Service | Purpose | Methods |
|---------|---------|---------|
| `IRegistrationService` | Create entities, manage relationships | 14 |
| `IDeregistrationService` | Delete entities, remove relationships | 11 |
| `IRetrievalService` | Query entities with filtering, ordering, pagination | 16 |
| `IModificationService` | Update entity properties | 4 |
| `IAuthenticationService` | Sign in, sign out, account switching | 4 |
| `IMfaService` | TOTP setup, confirmation, status, recovery codes | 5 |
| `IGoogleAuthService` | Link/unlink Google auth, check auth methods | 3 |
| `IInvitationService` | Create, accept, revoke, and list invitations | 4 |

## Decorator Pattern

Services are wrapped with telemetry decorators via Scrutor, and some also keep an authorization decorator:

```
TelemetryDecorator → [AuthorizationDecorator] → Service Implementation
```

- **Authorization decorators** validate user/account context where a service still dereferences ambient context before handing off
- **Telemetry decorators** log method entry/exit and timing
- **`IAuthenticationService`**, `IRetrievalService`, `IModificationService`, and `IInvitationService` currently run without a service-layer authorization decorator because their permission/context enforcement now lives lower in the stack where the real domain work happens

Authorization decorators use `IsNonSystemUserContext()` for "self" operations (MFA, password, Google auth) that require a real user, and `HasUserContext()` / `HasAccountContext()` for targeting operations that system context can perform.

Registration order in `ServiceRegistrationExtensions.cs` determines nesting (last registered = outermost).

## Service vs Processor

| | Service | Processor |
|--|---------|-----------|
| **Visibility** | `public` | `internal` |
| **Purpose** | Orchestration, coordination | Business logic, data access |
| **Authorization** | Context validation only | CRUDX permission checks |
| **Consumers** | Host applications | Services only |

Service methods that appear "unguarded" (e.g., `RegisterUsersWithGroupAsync`) are protected at the processor level where the actual domain work happens.

## Result Pattern

All service methods return typed result objects with result codes. Business logic failures return error codes, not exceptions:

```csharp
var result = await registrationService.RegisterUserAsync(request);
if (result.ResultCode == RegisterUserResultCode.Success)
{
    var userId = result.Id;
}
```

## Topics

- [Registration](registration.md)
- [Deregistration](deregistration.md)
- [Retrieval](retrieval.md)
- [Modification](modification.md)
- [Authentication Service](authentication-service.md)
