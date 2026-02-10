# Plan: Rename Logging Decorators → Telemetry Decorators

## Context

The decorator layer currently named "Logging" should be called "Telemetry" to better reflect its purpose. This is a straightforward rename across class names, file names, test files, DI registrations, and documentation.

## Scope

**27 files** total: 9 decorator classes, 9 test classes, 1 DI registration file, 1 documentation file. Plus 7 git renames (the other files are edited in-place via class name replacement).

No namespace changes. No logic changes. No behavioral changes.

## Changes

### 1. Rename Decorator Files (9 files — git mv + class rename)

| Current File | New File |
|---|---|
| `Corely.IAM/Accounts/Processors/AccountProcessorLoggingDecorator.cs` | `…AccountProcessorTelemetryDecorator.cs` |
| `Corely.IAM/Users/Processors/UserProcessorLoggingDecorator.cs` | `…UserProcessorTelemetryDecorator.cs` |
| `Corely.IAM/BasicAuths/Processors/BasicAuthProcessorLoggingDecorator.cs` | `…BasicAuthProcessorTelemetryDecorator.cs` |
| `Corely.IAM/Groups/Processors/GroupProcessorLoggingDecorator.cs` | `…GroupProcessorTelemetryDecorator.cs` |
| `Corely.IAM/Roles/Processors/RoleProcessorLoggingDecorator.cs` | `…RoleProcessorTelemetryDecorator.cs` |
| `Corely.IAM/Permissions/Processors/PermissionProcessorLoggingDecorator.cs` | `…PermissionProcessorTelemetryDecorator.cs` |
| `Corely.IAM/Services/RegistrationServiceLoggingDecorator.cs` | `…RegistrationServiceTelemetryDecorator.cs` |
| `Corely.IAM/Services/DeregistrationServiceLoggingDecorator.cs` | `…DeregistrationServiceTelemetryDecorator.cs` |
| `Corely.IAM/Services/AuthenticationServiceLoggingDecorator.cs` | `…AuthenticationServiceTelemetryDecorator.cs` |

Inside each file: replace class name and `ILogger<*LoggingDecorator>` generic parameter.

### 2. Rename Test Files (9 files — git mv + class rename)

| Current File | New File |
|---|---|
| `Corely.IAM.UnitTests/Accounts/Processors/AccountProcessorLoggingDecoratorTests.cs` | `…AccountProcessorTelemetryDecoratorTests.cs` |
| `Corely.IAM.UnitTests/Users/Processors/UserProcessorLoggingDecoratorTests.cs` | `…UserProcessorTelemetryDecoratorTests.cs` |
| `Corely.IAM.UnitTests/BasicAuths/Processors/BasicAuthProcessorLoggingDecoratorTests.cs` | `…BasicAuthProcessorTelemetryDecoratorTests.cs` |
| `Corely.IAM.UnitTests/Groups/Processors/GroupProcessorLoggingDecoratorTests.cs` | `…GroupProcessorTelemetryDecoratorTests.cs` |
| `Corely.IAM.UnitTests/Roles/Processors/RoleProcessorLoggingDecoratorTests.cs` | `…RoleProcessorTelemetryDecoratorTests.cs` |
| `Corely.IAM.UnitTests/Permissions/Processors/PermissionProcessorLoggingDecoratorTests.cs` | `…PermissionProcessorTelemetryDecoratorTests.cs` |
| `Corely.IAM.UnitTests/Services/RegistrationServiceLoggingDecoratorTests.cs` | `…RegistrationServiceTelemetryDecoratorTests.cs` |
| `Corely.IAM.UnitTests/Services/DeregistrationServiceLoggingDecoratorTests.cs` | `…DeregistrationServiceTelemetryDecoratorTests.cs` |
| `Corely.IAM.UnitTests/Services/AuthenticationServiceLoggingDecoratorTests.cs` | `…AuthenticationServiceTelemetryDecoratorTests.cs` |

Inside each file: replace test class name and decorator class references.

### 3. Update DI Registration

**File:** `Corely.IAM/ServiceRegistrationExtensions.cs`

Replace all 9 `*LoggingDecorator` references with `*TelemetryDecorator`.

### 4. Update Documentation

**File:** `CLAUDE.md`

Replace references to "LoggingDecorator" / "Logging decorator" / "logging decorators" with "TelemetryDecorator" / "Telemetry decorator" / "telemetry decorators".

## What Stays the Same

- `ILogger` injection and `_logger` field names — still using ILogger under the hood
- `ExecuteWithLoggingAsync()` extension method — lives in `Corely.Common`, not in scope
- Namespaces — no changes needed
- All classes remain `internal`

## Verification

1. `dotnet build Corely.IAM.sln` — confirm no compile errors
2. `dotnet test Corely.IAM.UnitTests` — confirm all tests pass
3. `dotnet csharpier format .` — ensure formatting compliance
