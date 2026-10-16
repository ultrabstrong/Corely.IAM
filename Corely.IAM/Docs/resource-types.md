# Resource Types

Resource types are compile-time concepts that categorize what a permission grants access to. The `IResourceTypeRegistry` provides runtime discoverability for UI dropdowns, validation, and documentation, and says what an account owner gets on each type.

## Features

- **Pre-registered IAM types**: all built-in types available at startup
- **Custom types**: host apps register additional types during DI setup
- **Owner actions**: each type declares which actions an account's Owner role gets on it
- **Case-insensitive**: `"account"`, `"Account"`, and `"ACCOUNT"` are treated as the same type
- **Validation integration**: `PermissionValidator` rejects unknown resource types
- **No persistence**: resource types are not stored in the database; they exist only in memory

## Built-in Resource Types

| Constant | Value | Description | Owner actions |
|----------|-------|-------------|---------------|
| `Account.RESOURCE_TYPE` | `"account"` | Accounts | All five |
| `User.RESOURCE_TYPE` | `"user"` | Users | All five |
| `Group.RESOURCE_TYPE` | `"group"` | Groups | All five |
| `Role.RESOURCE_TYPE` | `"role"` | Roles | All five |
| `Permission.RESOURCE_TYPE` | `"permission"` | Permissions | All five |
| `AuditEntry.RESOURCE_TYPE` | `"audit"` | Audit log | Read (view and export), Delete (purge) |
| `AccountAuditSettings.RESOURCE_TYPE` | `"audit_settings"` | Audit settings | Read, Update |
| `PlatformSettings.RESOURCE_TYPE` | `"platform_audit_settings"` | Platform audit settings | None |
| `SecurityPolicy.RESOURCE_TYPE` | `"platform_security_settings"` | Platform security settings | None |

The two platform settings types are meaningful only in the [platform account](platform.md), whose
owner holds full access on every type; an account owner elsewhere gets nothing on them. See [Auditing](auditing.md) for
what the audit types allow.

The owner actions of the built-in types are not configurable. There is no wildcard type: `"*"` is
rejected as a type name, and a permission grants access only to the type it names.

## Registering Custom Types

Register custom resource types via `IAMOptions` during startup, naming the actions an account
owner gets on each:

```csharp
var options = IAMOptions.Create(configuration, securityConfigProvider, efConfig)
    .RegisterResourceType("invoice", "Customer invoices", AuthAction.Read, AuthAction.Update)
    .RegisterResourceType("report", "Financial reports");
```

A type registered without owner actions gives the owner nothing on it; access to it comes only from
permissions created explicitly. Duplicate names (including case variants) are overwritten with the
latest value. A name already taken by a built-in type, or `"*"`, throws when services are added.

## Owner Defaults

When an account is created, its Owner role receives one system-defined permission per registered
type that has owner actions, on `Guid.Empty` (every resource of the type), with exactly those
actions, named `Resource : Actions` from the type's name: `Group : Full Access`,
`Quota : Read & Execute`, `Document Workflows : Create, Read, & Update`. These rows cannot be deleted or detached from the
Owner role, under system context or otherwise. They are ordinary permissions in every other way:
any role can be given the same row, and since a permission is unique on type, resource ID and
actions, a role that needs exactly those actions shares it rather than getting a copy.

Anything beyond the owner defaults is the host's to provision, under system context: other roles, a
starter grant, or permissions added later.

At startup, every existing account's Owner role is given the owner default of any type it has none
for, so a type registered after accounts exist reaches their owners on the next start. If the account
already has a permission with exactly those actions, that row is made system-defined and attached
rather than duplicated. The platform account is skipped, since its Owner role gets full access on
every type anyway.

This only ever adds. Changing the owner actions of a type an account already has a default for
affects accounts created afterwards; existing accounts keep their rows, and nothing is taken away
from an owner automatically.

## IResourceTypeRegistry Interface

```csharp
public interface IResourceTypeRegistry
{
    IReadOnlyCollection<ResourceTypeInfo> GetAll();
    ResourceTypeInfo? Get(string name);
    bool Exists(string name);
}

public record ResourceTypeInfo(
    string Name,
    string Description,
    IReadOnlyList<AuthAction> OwnerActions
);
```

Resolve `IResourceTypeRegistry` from DI to query registered types:

```csharp
var registry = serviceProvider.GetRequiredService<IResourceTypeRegistry>();
var all = registry.GetAll();
var invoice = registry.Get("invoice");
var exists = registry.Exists("account");
```

## Validation

`PermissionValidator` injects `IResourceTypeRegistry` and rejects permissions with unregistered resource types, `"*"` included.

## UI Integration

In `Corely.IAM.Web`, the permission creation form uses a `<select>` dropdown populated from `IResourceTypeRegistry.GetAll()`. Selecting a resource type auto-populates the description field with the registry's description.

## Notes

- Resource types are registered during DI setup only; there is no runtime add/remove
- The registry uses `ConcurrentDictionary` with `StringComparer.OrdinalIgnoreCase`
- Existing permission data (the `ResourceType` string column) is unaffected by the registry
- The `AuthorizationProvider` compares resource type strings directly. The registry is for discoverability and owner defaults, not enforcement
