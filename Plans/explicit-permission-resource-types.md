# Explicit permission resource types

**Status: phase 1 planned, phase 2 not yet designed.**

## The ask

Every account's Owner role holds one system-defined permission on resource type `*`: CRUDX on every
resource type, including types a host registers later. That makes the owner the owner of everything
in the app, not just of IAM in their account. Corely.Billing's grants showed the cost: an owner can
write their own grants because `*` covers a type IAM knows nothing about. A host's SFTP accounts would
be the same: the owner should read and update them, while the host creates and deletes them.

- **Phase 1:** remove the resource type wildcard. Every permission names the exact resource type it
  grants, so no layer can be granted access by accident.
- **Phase 2:** stop anyone who manages permissions from granting what they should not have. Removing
  `*` does not do this on its own (see Phase 2).

The resource id wildcard (`ResourceId == Guid.Empty`, every resource of one type) stays. It is scoped
to a single type and is how the owner covers all groups, roles and so on.

## What exists

| Where | What it does with `*` |
|-------|----------------------|
| `PermissionConstants.ALL_RESOURCE_TYPES` | Defines `"*"` |
| `ResourceTypeRegistry` | Pre-registers `"*"`, so `PermissionValidator` accepts it |
| `PermissionProcessor.CreateDefaultSystemPermissionsAsync` | Gives the Owner role one `*` CRUDX permission at account registration |
| `PermissionMapper.IsOwnerSystemPermission` | Identifies that one row; `RoleProcessor` uses it to refuse removing it from the Owner role |
| `AuthorizationProvider.IsAuthorizedAsync`, `GetAuthorizedResourceIdsAsync` | Match a permission whose type is the requested type or `*` |
| `PermissionProcessor.GetEffectivePermissionsForUserAsync` | Same match, for effective permissions shown to users |
| `Corely.IAM.Web` `PermissionList.razor` | Hides `*` from the resource type dropdown |
| `Corely.IAM.WebApp/DemoSetup/SeedWebAppDemo.ps1` | Seeds `all.read` and `all.execute` permissions on `*` |

Outside this repository, anything that relies on the owner reaching its own types through `*` loses
that access. Corely.Billing.IAM's grants are the known case; for grants that is the intended outcome.

## Phase 1: remove the resource type wildcard

### Decide before building

1. **How a host's own resource types get owner defaults.** Decided: the host gives the owner's default
   actions when it registers the type, defaulting to none:

   ```csharp
   options.RegisterResourceType("sftp", "SFTP access", ownerActions: "cRUdX");
   ```

   IAM applies them to the Owner role only, and only when the account is created, inside the same
   unit of work as the rest of account registration. IAM's own five types register with CRUDX the same
   way, so `CreateDefaultSystemPermissionsAsync` builds the Owner role's permissions from the registry
   and nothing else.

   Anything beyond the owner's defaults stays with the host: other roles, a starter grant, or
   permissions added later by an async process. The host does that under system context, since it is
   a system concern rather than something the creating user does.

   Considered and set aside: a host decorator on account registration that adds the owner's
   permissions afterwards. It needs IAM's internals to find the Owner role, every host with its own
   types writes the same code, and its rows cannot be system-defined.

2. **Existing accounts.** A migration can replace each Owner role's `*` row with the five IAM type
   rows, because IAM knows those types. It cannot know a host's types or their owner defaults.
   Recommendation: a migration guide step, run once by the host after upgrading, that applies its
   registered owner defaults to existing accounts. Whether that step is a service method or a CLI
   command is open.

3. **`*` permissions people created themselves.** Accounts can hold their own `*` rows (the demo seed
   does). Recommendation: the migration expands each into one row per IAM type with the same actions
   and role links. Nobody gains access; access to host types is lost, which the migration guide says.

4. **Which owner permissions are system-defined.** Recommendation: all of them, IAM and host defaults
   alike, so an owner cannot strip the Owner role and lock the account out. `IsOwnerSystemPermission`
   becomes "a system-defined permission linked to the Owner role" rather than a match on `*`.

5. **The constant.** Recommendation: delete `ALL_RESOURCE_TYPES` in the preflight rather than mark it
   obsolete. The preflight already breaks behavior, we are the only consumers, and a constant that still
   compiles invites the next caller to use it.

### Changes

- **Constant and registry:** delete `ALL_RESOURCE_TYPES` and its registry entry. `RegisterResourceType`
  rejects `"*"`, so a host cannot bring it back. `PermissionValidator` then rejects `"*"` with no change
  of its own, since the type is no longer registered.
- **Owner defaults:** `CreateDefaultSystemPermissionsAsync` creates one system-defined permission per
  registered type that has owner actions, each with `ResourceId = Guid.Empty`, linked to the Owner role.
- **Authorization:** `AuthorizationProvider` and `GetEffectivePermissionsForUserAsync` match the exact
  resource type only.
- **Owner role protection:** today the Owner role's one permission is protected three ways, and phase 1
  keeps all three while there are several rows instead of one:

  | Path | Today | Phase 1 |
  |------|-------|---------|
  | Delete the permission | `PermissionProcessor.DeletePermissionAsync` refuses any system-defined permission | Unchanged |
  | Detach it from the Owner role | `RoleProcessor.RemovePermissionsFromRoleAsync` refuses `IsOwnerSystemPermission`, a match on the `*` row's exact makeup | Refuses any system-defined permission linked to the Owner role; `IsOwnerSystemPermission` goes |
  | Weaken it in place | Not possible: there is no operation that updates a permission's actions | Unchanged |

  These guards sit in the processors, not the authorization decorators, so they hold against system
  context too. Deleting, renaming or emptying the Owner role itself is already refused
  (`RoleProcessor` refuses changes to system-defined roles).
- **Migration:** `AddMigration.ps1` for both providers, with provider specific SQL for decisions 2 and 3,
  including the `RolePermissions` join rows.
- **Corely.IAM.Web:** drop the `*` filter in `PermissionList.razor`.
- **Demo seed:** replace the `*` permissions in `SeedWebAppDemo.ps1` with explicit types.

### Tests

- **Unit:** registry (no `*`, `RegisterResourceType("*")` rejected, owner actions stored), validator
  rejects `"*"`, `CreateDefaultSystemPermissionsAsync` creates one row per type with owner actions,
  `AuthorizationProvider` no longer treats `"*"` as matching every type, the Owner role guard.
  Existing tests that use `ALL_RESOURCE_TYPES` are rewritten or removed:
  `AuthorizationProviderTests`, `RoleProcessorTests`, `ResourceTypeRegistryTests`,
  `PermissionProcessorTests`, `PermissionMapperTests`, `ServiceRegistrationExtensionsTests`.
- **Integration:** an owner can manage every IAM type in their account and has no access to a
  registered host type without owner actions; the migration's SQL expands `*` rows correctly on both
  providers (provider matrix).
- **Functional:** none expected; nothing in the HTTP pipeline changes.

### Docs

Remove the resource type wildcard from `Corely.IAM/Docs/authorization.md`, `resource-types.md`,
`iam-options.md`, `usage-shapes.md`, `domains/permissions.md`, `domains/roles.md`, `domains/accounts.md`,
`Corely.IAM.Web/Docs/pages/permissions.md` and `Docs/Permission Model.md`, and document owner actions on
`RegisterResourceType`. `MIGRATION-3.0.md` at the repository root covers decisions 2 and 3 (see Release).

### Release

One major for the whole change, not one per phase. We are the only consumers, so the breaking parts of
phase 1 ship in a minor:

| Release | Corely.IAM | Migration CLI | Corely.IAM.Web |
|---------|-----------|---------------|----------------|
| Phase 1 (preflight) | 2.5.0 | 2.2.0 | 2.7.0 |
| Phase 2 (final) | 3.0.0 | 3.0.0 | 3.0.0 |

The preflight is breaking despite its minor version: owners lose implicit access to host types, and
`*` rows are migrated away. `MIGRATION-3.0.md` is started with the preflight and completed with
phase 2, so one guide covers the whole path from 2.4.

## Phase 2: permission to manage permissions

**Not yet designed.** The problem, as the code stands:

- Creating a permission checks only Create on resource type `permission`
  (`PermissionProcessorAuthorizationDecorator`). It never checks what the new permission grants.
- Attaching permissions to a role checks Update on the role and Read on the permissions
  (`RoleProcessorAuthorizationDecorator`).

So anyone who can manage permissions and roles can write `grant: C`, or any other type, and attach it
to their own role. Phase 1 makes the owner's starting point explicit; phase 2 has to make it a limit.

Phase 2 has two halves, whatever mechanism carries them:

- an owner cannot create or attach a permission more permissive than what they hold;
- an owner cannot delete or detach the permissions their role needs. Today only system-defined rows
  are protected (decision 4 in phase 1); permissions a host adds later through system context are
  not.

**Decided: system-defined stays locked down.** The only way a host gets a system-defined permission is
the owner default it gives in `RegisterResourceType` (phase 1, decision 1), which IAM creates at account
registration. There is no API to create or remove a system-defined permission, under system context or
otherwise. Broader rules wait for a real use case.

What follows from it, with the call on each:

1. Permissions a host adds to the Owner role later, under system context, are ordinary rows. Whether an
   owner can detach them is the second half above, not a system-defined question. This is a real hole
   when a host adds a new feature to existing accounts; it is left for later and recorded in
   `owner-defaults-after-account-creation.md`.
2. A system-defined row cannot be revoked by anyone. A host that needs to take an owner capability away
   later (a downgraded plan) gives it as an ordinary permission rather than an owner default. Accepted:
   system-defined permissions have a very thin surface, and only the owner depends on them. Every other
   member can be given roles and permissions that carry no system-defined rows at all.
3. Changing a type's owner default affects accounts created afterwards. Existing accounts keep the rows
   they were created with. Worth solving, not now; recorded in the same plan.

Owner enforcement itself is keyed on the role, not on what its permissions contain: "an account keeps
at least one owner" (`UserOwnershipProcessor`, `UserProcessor`, `GroupProcessor`) matches the Owner
role by name and `IsSystemDefined`. The one check keyed on composition is
`PermissionMapper.IsOwnerSystemPermission`, which phase 1 already replaces.
