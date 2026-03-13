# Resource Types

Resource types are compile-time concepts that categorize what a permission grants access to. The `IResourceTypeRegistry` provides runtime discoverability for UI dropdowns, validation, and documentation.

## Features

- **Pre-registered IAM types** — all built-in types available at startup
- **Custom types** — host apps register additional types during DI setup
- **Case-insensitive** — `"account"`, `"Account"`, and `"ACCOUNT"` are treated as the same type
- **Validation integration** — `PermissionValidator` rejects unknown resource types
- **No persistence** — resource types are not stored in the database; they exist only in memory

## Built-in Resource Types

| Constant | Value | Description |
|----------|-------|-------------|
| `PermissionConstants.ACCOUNT_RESOURCE_TYPE` | `"account"` | Accounts |
| `PermissionConstants.USER_RESOURCE_TYPE` | `"user"` | Users |
| `PermissionConstants.GROUP_RESOURCE_TYPE` | `"group"` | Groups |
| `PermissionConstants.ROLE_RESOURCE_TYPE` | `"role"` | Roles |
| `PermissionConstants.PERMISSION_RESOURCE_TYPE` | `"permission"` | Permissions |
| `PermissionConstants.ALL_RESOURCE_TYPES` | `"*"` | All resource types (wildcard) |

## Registering Custom Types

Register custom resource types via `IAMOptions` during startup:

```csharp
var options = IAMOptions.Create(configuration, securityConfigProvider, efConfig)
    .RegisterResourceType("invoice", "Customer invoices")
    .RegisterResourceType("report", "Financial reports");
```

Duplicate names (including case variants) are silently overwritten with the latest value.

## IResourceTypeRegistry Interface

```csharp
public interface IResourceTypeRegistry
{
    IReadOnlyCollection<ResourceTypeInfo> GetAll();
    ResourceTypeInfo? Get(string name);
    bool Exists(string name);
}

public record ResourceTypeInfo(string Name, string Description);
```

Resolve `IResourceTypeRegistry` from DI to query registered types:

```csharp
var registry = serviceProvider.GetRequiredService<IResourceTypeRegistry>();
var all = registry.GetAll();
var invoice = registry.Get("invoice");
var exists = registry.Exists("account");
```

## Validation

`PermissionValidator` injects `IResourceTypeRegistry` and rejects permissions with unregistered resource types. The wildcard `"*"` passes validation since it is pre-registered.

## UI Integration

In `Corely.IAM.Web`, the permission creation form uses a `<select>` dropdown populated from `IResourceTypeRegistry.GetAll()`. The wildcard `"*"` is excluded from the dropdown (system-only). Selecting a resource type auto-populates the description field with the registry's description.

## Notes

- Resource types are registered during DI setup only — no runtime add/remove
- The registry uses `ConcurrentDictionary` with `StringComparer.OrdinalIgnoreCase`
- Existing permission data (the `ResourceType` string column) is unaffected by the registry
- The `AuthorizationProvider` still compares resource type strings directly — the registry is for discoverability, not enforcement
