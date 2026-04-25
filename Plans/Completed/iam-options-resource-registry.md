# IAM Options & Resource Type Registry

## Status: Complete

## Overview

Introduced an `IAMOptions` builder class that consolidates all IAM configuration into a single, fluent API with a unified `AddIAMServices` extension method. The primary new feature is a **resource type registry** — a singleton that holds all known resource types (IAM-defined + host-app custom) so they're discoverable at runtime for UI dropdowns, validation, and documentation.

Additional benefits:
- Pulls hardcoded crypto algorithm codes into configurable options with sensible defaults
- Absorbs `ISecurityConfigurationProvider`, `IConfiguration`, and `Func<IServiceProvider, IEFConfiguration>` into the options builder
- Collapses `AddIAMServicesWithEF` and `AddIAMServicesWithMockDb` into a single `AddIAMServices` method

---

## Design

### IAMOptions (builder)

```csharp
// EF/production path
IAMOptions
    .Create(configuration, securityConfigProvider, efConfigFactory)
    .RegisterResourceType("invoice", "Invoices")  // optional, repeatable
    .UseSymmetricEncryption(code)                  // optional, default AES
    .UseAsymmetricEncryption(code)                 // optional, default RSA
    .UseAsymmetricSignature(code)                  // optional, default ECDSA_SHA256
    .UseHash(code)                                 // optional, default SALTED_SHA256

// Mock/testing path (no EF config)
IAMOptions
    .Create(configuration, securityConfigProvider)
```

- **`Create()` is a static factory** with two overloads (EF vs mock) — private constructor ensures required params can't be skipped
- **Fluent chaining** — each method returns `this`
- **`CustomResourceTypes`** is `internal` with `StringComparer.OrdinalIgnoreCase` — host apps interact only via `RegisterResourceType()`; case-variant duplicates silently overwrite (latest wins)
- **Crypto setters** are `internal set` properties with defaults matching previous hardcoded values — no breaking change if untouched

### Single Registration Entry Point

```csharp
public static IServiceCollection AddIAMServices(
    this IServiceCollection serviceCollection,
    IAMOptions options)
```

- `AddIAMServicesWithEF` and `AddIAMServicesWithMockDb` were removed
- The single `AddIAMServices` method checks `options.EFConfigurationFactory != null` to decide EF vs mock data access registration
- All other registration (security, services, processors, decorators, registry) is shared

### ResourceTypeRegistry (singleton)

```csharp
public interface IResourceTypeRegistry
{
    IReadOnlyCollection<ResourceTypeInfo> GetAll();
    ResourceTypeInfo? Get(string name);
    bool Exists(string name);
}

public record ResourceTypeInfo(string Name, string Description);
```

- Backed by `ConcurrentDictionary<string, ResourceTypeInfo>` with `StringComparer.OrdinalIgnoreCase`
- **`Register()` is `internal`** — only callable during DI setup, not after the container is built
- **Duplicate guard** — `TryAdd` returns false → throws `InvalidOperationException` (catches case-variant duplicates like `"Account"` vs `"account"`)
- **Pre-filled** with all IAM-defined types on construction
- **No deletes** — registration only

### IAM-Defined Resource Types (constants)

Defined in `PermissionConstants` and pre-registered in the registry:

| Constant | Value | Registry Description |
|----------|-------|---------------------|
| `ACCOUNT_RESOURCE_TYPE` | `"account"` | Accounts |
| `USER_RESOURCE_TYPE` | `"user"` | Users |
| `GROUP_RESOURCE_TYPE` | `"group"` | Groups |
| `ROLE_RESOURCE_TYPE` | `"role"` | Roles |
| `PERMISSION_RESOURCE_TYPE` | `"permission"` | Permissions |
| `ALL_RESOURCE_TYPES` | `"*"` | All resource types (wildcard) |

### Host App Usage

```csharp
// Minimal (no custom types, default crypto)
var options = IAMOptions.Create(configuration, securityConfigProvider, efConfig);
services.AddIAMServices(options);

// With custom resource types
var options = IAMOptions.Create(configuration, securityConfigProvider, efConfig)
    .RegisterResourceType("invoice", "Customer invoices")
    .RegisterResourceType("report", "Financial reports");
services.AddIAMServices(options);

// Mock/testing (no EF config)
var options = IAMOptions.Create(configuration, securityConfigProvider);
services.AddIAMServices(options);
```

---

## What Was Implemented

### Phase 1: Core Infrastructure

**1.1 — `Corely.IAM/Permissions/Models/ResourceTypeInfo.cs`**
- `public record ResourceTypeInfo(string Name, string Description);`

**1.2 — `Corely.IAM/Permissions/Providers/IResourceTypeRegistry.cs`**
- Public interface with `GetAll()`, `Get(string name)`, `Exists(string name)`

**1.3 — `Corely.IAM/Permissions/Providers/ResourceTypeRegistry.cs`**
- `internal class` backed by `ConcurrentDictionary` with `OrdinalIgnoreCase`
- Constructor pre-registers all 6 IAM-defined types
- `internal void Register()` with `TryAdd` duplicate guard (case-insensitive)

**1.4 — `Corely.IAM/IAMOptions.cs`**
- Private constructor, two `Create()` overloads:
    - `Create(IConfiguration, ISecurityConfigurationProvider)` — mock path
    - `Create(IConfiguration, ISecurityConfigurationProvider, Func<IServiceProvider, IEFConfiguration>)` — EF path
- All three params (`IConfiguration`, `ISecurityConfigurationProvider`, `EFConfigurationFactory`) stored on options
- `CustomResourceTypes` dictionary uses `StringComparer.OrdinalIgnoreCase`
- Crypto code properties with defaults, fluent setters

### Phase 2: Wire Into DI

**2.1 — `Corely.IAM/ServiceRegistrationExtensions.cs`**
- Removed `AddIAMServicesWithEF` and `AddIAMServicesWithMockDb`
- Single public `AddIAMServices(IServiceCollection, IAMOptions)` method
- Checks `options.EFConfigurationFactory != null` for EF vs mock path
- Builds `ResourceTypeRegistry` with custom types from options, registers as `IResourceTypeRegistry` singleton
- Uses `options.Configuration` for config binding, `options.SecurityConfigurationProvider` for security, crypto codes from options instead of hardcoded constants

**2.2 — `Corely.IAM/Permissions/Validators/PermissionValidator.cs`**
- Injected `IResourceTypeRegistry` as constructor dependency
- Added `.Must()` rule: `ResourceType` must exist in registry
- Guarded with `.When(!IsNullOrWhiteSpace)` so it doesn't fire on empty values (caught by existing `NotEmpty()` rule)
- Wildcard `"*"` passes naturally since it's pre-registered in the registry

### Phase 3: Update Call Sites

All call sites updated to use `IAMOptions.Create(...)` + `services.AddIAMServices(options)`:

- **`Corely.IAM.WebApp/Program.cs`** — EF overload with `builder.Configuration`, security provider, EF config factory
- **`Corely.IAM.ConsoleTest/ServiceFactory.cs`** — EF overload
- **`Corely.IAM.DevTools/ServiceFactory.cs`** — EF overload inside conditional block (only registers when config is present)
- **`Corely.IAM.UnitTests/ServiceFactory.cs`** — Mock overload (no EF config)

### Phase 4: UI — Resource Type Dropdown + Description Display

**`Corely.IAM.Web/Components/Pages/Permissions/PermissionList.razor`**
- Replaced freeform `<input>` with `<select>` dropdown populated from `IResourceTypeRegistry`
- Excludes wildcard `"*"` from dropdown (system-only)
- Dropdown options display as `"{Name} — {Description}"`
- On resource type selection, auto-populates the Description field with the registry's description if the field is empty (leaves user-typed descriptions alone)
- Permission list table shows description inline next to resource type name (muted text, same line)

### Phase 5: Tests

**5.1 — `Corely.IAM.UnitTests/Permissions/Providers/ResourceTypeRegistryTests.cs`** (14 tests)
- Constructor pre-registers all IAM types (parameterized over all 6)
- `GetAll()`, `Get()` (exact, case-insensitive, unknown), `Exists()` (known, unknown)
- `Register()` adds new type, rejects duplicates, rejects case-variant duplicates (`"Account"`, `"ACCOUNT"`, `"aCCOUNT"`)
- Null/whitespace validation on name and description

**5.2 — `Corely.IAM.UnitTests/IAMOptionsTests.cs`** (19 tests)
- Both `Create()` overloads: valid params, null checks for all params (configuration, provider, EF factory)
- `Configuration` and `EFConfigurationFactory` property verification
- `RegisterResourceType` storage, case-variant overwrite behavior, null/whitespace validation
- Crypto property defaults and individual setters
- Fluent chaining returns same instance

**5.3 — `Corely.IAM.UnitTests/Permissions/Validators/PermissionValidatorTests.cs`**
- Updated constructor to inject mock `IResourceTypeRegistry`
- Added test: registered resource type passes validation
- Added test: unregistered resource type fails validation
- Existing tests preserved (empty/null resource type, CRUDX requirement)

**5.4 — `Corely.IAM.UnitTests/ServiceRegistrationExtensionsTests.cs`**
- All test methods renamed from `AddIAMServicesWithMockDb_*` / `AddIAMServicesWithEF_*` to `AddIAMServices_WithMockDb_*` / `AddIAMServices_WithEF_*`
- Updated all calls to use new single `AddIAMServices` signature
- Removed `#region` tags
- Added `IResourceTypeRegistry` to singleton and service registration checks
- Added 3 new tests: registry resolves, contains IAM-defined types, contains custom types from options

### Phase 6: DevTools Command

**`Corely.IAM.DevTools/Commands/Retrieval/ListResourceTypes.cs`**
- Nested class inside `Retrieval` partial class (auto-discovered by reflection)
- Lists all resource types in formatted table (Name, Description)
- Sorted alphabetically, no authentication required
- Uses colored output helpers (Info/Warn/Success)

---

## Files Changed (Summary)

### New Files (7)
| File | Purpose |
|------|---------|
| `Corely.IAM/IAMOptions.cs` | Builder with `Create()` factory, fluent config |
| `Corely.IAM/Permissions/Models/ResourceTypeInfo.cs` | Resource type record |
| `Corely.IAM/Permissions/Providers/IResourceTypeRegistry.cs` | Registry interface |
| `Corely.IAM/Permissions/Providers/ResourceTypeRegistry.cs` | Registry implementation |
| `Corely.IAM.UnitTests/IAMOptionsTests.cs` | 19 tests |
| `Corely.IAM.UnitTests/Permissions/Providers/ResourceTypeRegistryTests.cs` | 14 tests |
| `Corely.IAM.DevTools/Commands/Retrieval/ListResourceTypes.cs` | DevTools CLI command |

### Modified Files (8)
| File | Change |
|------|--------|
| `Corely.IAM/ServiceRegistrationExtensions.cs` | Collapsed to single `AddIAMServices`, wires registry + crypto from options |
| `Corely.IAM/Permissions/Validators/PermissionValidator.cs` | Validates resource type against registry |
| `Corely.IAM.WebApp/Program.cs` | Uses `IAMOptions.Create()` + `AddIAMServices()` |
| `Corely.IAM.ConsoleTest/ServiceFactory.cs` | Uses `IAMOptions.Create()` + `AddIAMServices()` |
| `Corely.IAM.DevTools/ServiceFactory.cs` | Uses `IAMOptions.Create()` + `AddIAMServices()` |
| `Corely.IAM.UnitTests/ServiceFactory.cs` | Uses `IAMOptions.Create()` + `AddIAMServices()` |
| `Corely.IAM.UnitTests/ServiceRegistrationExtensionsTests.cs` | Updated sigs, removed regions, +3 registry tests |
| `Corely.IAM.Web/Components/Pages/Permissions/PermissionList.razor` | Dropdown, auto-populate description, inline description display |

---

## Non-Goals

- **No database table** for resource types — they're compile-time concepts
- **No runtime add/remove** — registration happens during DI setup only
- **No migration** — resource types are not persisted; existing permission data (string `ResourceType` column) is unaffected
- **No breaking change to `AuthorizationProvider`** — it still compares strings; the registry is for discoverability, not enforcement at the auth layer

## Test Results

**1,171 passed, 0 failures** (1,171 IAM + 70 Web)
