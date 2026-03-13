# Services

Five public services form the API surface of Corely.IAM. All are registered as scoped and wrapped with authorization and telemetry decorators.

## Service Overview

| Service | Purpose | Methods |
|---------|---------|---------|
| `IRegistrationService` | Create entities, manage relationships, invitations | 14 |
| `IDeregistrationService` | Delete entities, remove relationships | 11 |
| `IRetrievalService` | Query entities with filtering, ordering, pagination | 16 |
| `IModificationService` | Update entity properties | 4 |
| `IAuthenticationService` | Sign in, sign out, account switching | 4 |

## Decorator Pattern

Every service is wrapped with two decorator layers via Scrutor:

```
TelemetryDecorator → AuthorizationDecorator → Service Implementation
```

- **Authorization decorators** validate user/account context (not CRUDX permissions — those are at the processor level)
- **Telemetry decorators** log method entry/exit and timing

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
