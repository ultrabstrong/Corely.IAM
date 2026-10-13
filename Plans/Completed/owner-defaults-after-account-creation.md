# Owner defaults after account creation

**Status: done.** Released in Corely.IAM 4.0.0.

## Decisions

- A type registered after accounts exist reaches their owners. Done automatically at startup, the
  way the platform Owner role is completed, so a host does nothing beyond registering the type.
- A changed owner default applies to accounts created afterwards only. Startup never narrows or
  widens an existing row, and never removes anything already provisioned. Taking something away
  would be an explicit, one time call by the host, which is not built until a host needs it.

## As built

- `IPermissionProcessor.CreateMissingOwnerDefaultsAsync` gives every non platform account's Owner
  role the owner default of each registered type with owner actions that the account has no
  system-defined permission for (on `Guid.Empty`). Returns how many it added.
- A permission is unique on account, type, resource and actions, so when the account already has
  an ordinary row with exactly those actions it is made system-defined and attached to the Owner role
  instead of duplicated.
- The platform account is skipped: `CompletePlatformOwnerPermissionsAsync` already gives its Owner
  role full access on every type.
- `OwnerDefaultsStartup` runs it at every start, logging and carrying on if it fails, like
  `PlatformOwnerPermissionsStartup`.
- `ResourceTypeInfo.ToOwnerDefaultEntity` in `PermissionMapper` builds the row for both this and
  `CreateDefaultSystemPermissionsAsync`.
- Tests: unit (a new type is added, a second run adds nothing, a changed default is left alone, an
  identical row is adopted, the platform account is skipped, both decorators) and integration on
  SQLite (a missing row is recreated, an identical ordinary row is adopted).

## The gaps it closes

A host registers a new resource type, or starts giving owners a capability they did not have, after
accounts already exist. Those accounts' owners got nothing from the new owner default, and adding it
by hand under system context landed an ordinary, unprotected row.
