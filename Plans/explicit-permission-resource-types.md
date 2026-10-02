# Explicit permission resource types

**Status: both phases designed. Build phase 1 first; phase 2 follows and ships as 3.0.0.**

## The ask

Every account's Owner role holds one system-defined permission on resource type `*`: CRUDX on every
resource type, including types a host registers later. That makes the owner the owner of everything
in the app, not just of IAM in their account. Corely.Billing's grants showed the cost: an owner can
write their own grants because `*` covers a type IAM knows nothing about.

- **Phase 1:** remove the resource type wildcard. Every permission names the exact resource type it
  grants, so no layer can be granted access by accident.
- **Phase 2:** stop anyone who manages permissions, roles or memberships from granting more than they
  hold. Removing `*` does not do this on its own.

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

Checked and safe:

- **Every authorization check in Corely.IAM and Corely.IAM.Web names one of the five IAM types**
  (`account`, `user`, `group`, `role`, `permission`), in decorators and in `PermissionView` uses alike.
  An owner holding CRUDX on those five keeps everything they can do in IAM today.
- **Owner enforcement is keyed on the role, not on its permissions.** "An account keeps at least one
  owner" (`UserOwnershipProcessor`, `UserProcessor`, `GroupProcessor`) matches the Owner role by name
  and `IsSystemDefined`. The only check keyed on the permission's makeup is `IsOwnerSystemPermission`.
- **A host cannot override an IAM type.** `ResourceTypeRegistry.Register` throws when a name is already
  registered, so a host cannot re-register `role` with weaker owner actions.

Outside this repository, owners reach these host types only through `*` today:

| Consumer | Types | Registered in |
|----------|-------|---------------|
| Corely.Billing.IAM | `grant`, `consumption`, `quota` | `IAMOptionsExtensions.RegisterBillingResourceTypes` |
| DocsToData | `extraction`, `document_workflows`, `sftp` | `DocsToData.Core/Extensions/IAMOptionsExtensions.cs` |

## Phase 1: remove the resource type wildcard

### Decisions

1. **Owner defaults come from type registration.** The host gives the owner's actions when it registers
   the type, defaulting to none:

   ```csharp
   options.RegisterResourceType("sftp", "SFTP access", AuthAction.Read, AuthAction.Update, AuthAction.Execute);
   ```

   IAM applies them to the Owner role only, and only when the account is created, inside the same unit
   of work as the rest of account registration. IAM's own five types are pre-registered with all five
   actions and are not configurable. Anything beyond the owner's defaults (other roles, a starter grant,
   permissions added later by an async process) stays with the host, under system context.

   Set aside: a host decorator on account registration adding the owner's permissions afterwards. It
   needs IAM's internals to find the Owner role, every host repeats it, and its rows cannot be
   system-defined.

2. **System-defined means owner defaults, and nothing else.** All owner defaults, IAM and host types
   alike, are system-defined. No API creates or removes a system-defined permission, under system
   context or otherwise.

3. **`ALL_RESOURCE_TYPES` is deleted**, not marked obsolete.

4. **No data migration in the library.** The only database using IAM is the DocsToData dev proof of
   concept, with no real users. The session that builds this fixes that database by hand (see
   DocsToData below). The change is in code only, so there is no EF migration and the migration CLI does
   not change.

### Changes

- **Constant and registry:** delete `ALL_RESOURCE_TYPES` and its registry entry. `RegisterResourceType`
  rejects `"*"` explicitly; once `*` is no longer pre-registered, the duplicate check alone would let it
  through. `PermissionValidator` then rejects `"*"` with no change of its own.
- **Owner actions:** `RegisterResourceType(string name, string description, params AuthAction[]
  ownerActions)`. `IAMOptions.CustomResourceTypes` carries the actions alongside the description,
  `ServiceRegistrationExtensions` passes them to the registry, and `ResourceTypeInfo` exposes them.
- **Owner defaults:** `CreateDefaultSystemPermissionsAsync` creates one system-defined permission per
  registered type that has owner actions, with `ResourceId = Guid.Empty`, linked to the Owner role.
- **Authorization:** `AuthorizationProvider.IsAuthorizedAsync`, `GetAuthorizedResourceIdsAsync` and
  `PermissionProcessor.GetEffectivePermissionsForUserAsync` match the exact resource type only.
- **Owner role protection:** the Owner role's system-defined permissions stay protected three ways, now
  that there are several rows instead of one:

  | Path | Today | Phase 1 |
  |------|-------|---------|
  | Delete the permission | `DeletePermissionAsync` refuses any system-defined permission | Unchanged |
  | Detach it from the Owner role | `RemovePermissionsFromRoleAsync` refuses `IsOwnerSystemPermission`, a match on the `*` row's exact makeup | Refuses any system-defined permission linked to the Owner role; `IsOwnerSystemPermission` goes |
  | Weaken it in place | Not possible: no operation updates a permission's actions | Unchanged |

  These guards sit in the processors, not the authorization decorators, so they hold against system
  context too. Non-system permissions can still be added to and removed from the Owner role. Deleting
  or renaming the Owner role is already refused.
- **Corely.IAM.Web:** drop the `*` filter in `PermissionList.razor`. The role page shows a remove
  button on every permission; today the Owner role has one row whose removal fails, after phase 1 it
  has five or more. Give `Permission` an `IsSystemDefined` with an internal setter, as `Role` already
  has, and hide remove and delete for system-defined permissions, showing the same System badge roles
  use.
- **Demo seed:** replace the `*` permissions in `SeedWebAppDemo.ps1` with explicit types.

### Tests

- **Unit:** registry (no `*`, `RegisterResourceType("*")` rejected, owner actions stored, IAM types
  carry all five), validator rejects `"*"`, `CreateDefaultSystemPermissionsAsync` creates one row per
  type with owner actions and none for types without, `AuthorizationProvider` treats `"*"` as an
  ordinary unmatched string, the Owner role guard refuses any system-defined row and allows other rows.
  Existing tests that use `ALL_RESOURCE_TYPES` are rewritten or removed: `AuthorizationProviderTests`,
  `RoleProcessorTests`, `ResourceTypeRegistryTests`, `PermissionProcessorTests`,
  `PermissionMapperTests`, `ServiceRegistrationExtensionsTests`.
- **Integration (SQLite):** an owner manages every IAM type in a new account and has no access to a
  registered host type without owner actions; a host type with owner actions is granted exactly those.
- **Functional:** none; nothing in the HTTP pipeline changes.

### Docs

Remove the resource type wildcard from `Corely.IAM/Docs/authorization.md`, `resource-types.md`,
`iam-options.md`, `usage-shapes.md`, `domains/permissions.md`, `domains/roles.md`, `domains/accounts.md`,
`Corely.IAM.Web/Docs/pages/permissions.md` and `Docs/Permission Model.md`. Document owner actions on
`RegisterResourceType`. Start `MIGRATION-3.0.md` at the repository root: `ALL_RESOURCE_TYPES` is gone,
and hosts declare owner actions for their own types or owners lose access to them.

### Consumers

In order, after Corely.IAM 2.5.0:

- **Corely.Billing:** a release that takes IAM 2.5.0 and declares owner actions for `grant`,
  `consumption` and `quota` in `RegisterBillingResourceTypes`. Which actions is decided there.
- **DocsToData:** takes both, and declares CRUDX for `extraction`, `document_workflows` and `sftp`
  (adjustable later). Then the dev database is fixed by hand: each `*` row is replaced by explicit rows
  with the same actions and role links, and each Owner role gets its owner defaults for the six host
  types, as system-defined rows. The permissions table is unique on account, type, id and all five
  actions, so a row that already exists is reused rather than inserted.

### Release

One major for the whole change, not one per phase. We are the only consumers, so the breaking parts of
phase 1 ship in a minor:

| Release | Corely.IAM | Migration CLI | Corely.IAM.Web |
|---------|-----------|---------------|----------------|
| Phase 1 (preflight) | 2.5.0 | unchanged | 2.7.0 |
| Phase 2 (final) | 3.0.0 | 3.0.0 | 3.0.0 |

The preflight is breaking despite its minor version: owners lose implicit access to host types.
`MIGRATION-3.0.md` is started with the preflight and completed with phase 2. The migration CLI takes
3.0.0 with phase 2 because its major tracks Corely.IAM's.

## Phase 2: grant only what you hold

### Every path that grants access today

| Operation | Checks today | How it escalates |
|-----------|--------------|------------------|
| Create a permission | Create on `permission` | Writes any type with any actions, `grant: C` included |
| Attach permissions to a role | Update on the role, Read on the permissions | Attaches a powerful existing permission to the caller's own role |
| Assign roles to a user | Update on the user, Read on the roles | Gives the caller any role |
| Assign roles to a group | Update on the group, Read on the roles | The same, through a group the caller belongs to |
| Add users to a group | Update on the group, Read on the users | Joins a group whose roles hold more than the caller |

Checked and not on the list: creating a role or a group carries no permissions or members
(`CreateRoleRequest`, `CreateGroupRequest`); adding a user to an account and accepting an invitation
give no roles; `AssignOwnerRolesToUserAsync` runs only inside account registration, for the user
creating the account.

### The rule

Each operation above succeeds only if the caller already holds every action it would hand out, on that
resource type, covering that resource id. The caller's own permissions are the ones
`AuthorizationProvider` already loads: every permission in the current account reached through the
caller's roles and groups.

A grant is covered when, for each action it allows, the caller holds a permission that:

- has the same resource type;
- allows that action;
- has `ResourceId == Guid.Empty`, or the same resource id as the grant. A grant on `Guid.Empty` is
  covered only by the caller's own `Guid.Empty`.

The actions can come from different rows: Read from one permission and Update from another covers a
grant of Read and Update.

What each operation hands out:

| Operation | Hands out |
|-----------|-----------|
| Create a permission | The permission in the request |
| Attach permissions to a role | Those permissions |
| Assign roles to a user or a group | Every permission of those roles |
| Add users to a group | Every permission of every role of that group |

Behavior:

- **All or nothing.** If any part of a request is not covered, the whole request is refused with the
  operation's existing `UnauthorizedError`, as every authorization decorator already does. The message
  says the caller cannot grant permissions they do not hold.
- **System context bypasses the rule**, as it bypasses every check.
- **Removals get no rule.** Detaching a permission, removing a role or removing a member only reduces
  access. Existing guards stay: system-defined permissions cannot leave the Owner role, and an account
  keeps at least one owner.
- **Owners are bounded by their owner defaults.** An owner holds CRUDX on the five IAM types and the
  registered owner actions for host types, so they can hand out anything within those. With `grant`
  registered for Read only, an owner cannot create `grant: C`, attach it, or reach it through a role.
  Owners keep full control of the Owner role's non-system permissions, within the rule.
- **Delegation becomes safe.** Giving a member `permission: C`, `role: U` and `user: U` lets them manage
  access without being able to exceed their own. That closes the gap in
  `owner-role-self-assignment.md` as a side effect: assigning the Owner role needs every permission it
  holds.
- **Existing rows are untouched.** The rule applies to new grants only.
- **The 30 second permission cache applies,** as it does to every check: a caller who just lost a
  permission can still grant it until their cache expires.

### Where it lives

The check needs the permissions being handed out, which the decorators do not have: no authorization
decorator touches a repository, and no processor uses `IAuthorizationProvider`. The provider already
holds `IReadonlyRepo<PermissionEntity>` and the caller's permissions, so it does the loading:

- **`PermissionMapper`:** `entity.IsCoveredBy(IEnumerable<PermissionEntity> held)`, the coverage rule
  above as a pure extension, tested directly.
- **`IAuthorizationProvider`**, one method per shape of grant:
  - `CanGrantAsync(string resourceType, Guid resourceId, params AuthAction[] actions)`, for creating a
    permission;
  - `CanGrantPermissionsAsync(IEnumerable<Guid> permissionIds)`;
  - `CanGrantRolesAsync(IEnumerable<Guid> roleIds)`, loading every permission linked to those roles;
  - `CanGrantGroupAsync(Guid groupId)`, loading every permission linked to that group's roles.

  Each returns true under system context and false with no user context. Each loads only rows in the
  current account; an id from another account or one that does not exist hands out nothing, and the
  processor rejects it as invalid, as it does today.
- **The five authorization decorators** add one call each, after their existing checks:
  `PermissionProcessorAuthorizationDecorator.CreatePermissionAsync`,
  `RoleProcessorAuthorizationDecorator.AssignPermissionsToRoleAsync`,
  `UserProcessorAuthorizationDecorator.AssignRolesToUserAsync`,
  `GroupProcessorAuthorizationDecorator.AssignRolesToGroupAsync` and
  `GroupProcessorAuthorizationDecorator.AddUsersToGroupAsync`.

The services (`RegisterPermissionAsync`, `RegisterPermissionsWithRoleAsync`,
`RegisterRolesWithUserAsync`, `RegisterRolesWithGroupAsync`, `RegisterUsersWithGroupAsync`) reach these
through the decorated processors and need no change.

**Corely.IAM.Web** needs no change. The pickers keep listing every permission and role in the account;
choosing one the caller cannot grant shows the refusal message. Filtering the pickers to what the caller
can grant is a later improvement, not part of this.

### Tests

- **Unit:** `IsCoveredBy` (actions split across rows, `Guid.Empty` covers a specific id, a specific id
  does not cover `Guid.Empty`, another type never covers); each
  `CanGrant` method (system context, no context, fully covered, partly covered, ids from another account
  or missing); each of the five decorators refuses without calling the inner processor when the grant
  is not covered, and calls it when it is.
- **Integration (SQLite):** the role and group queries translate. A member holding `permission: C`,
  `role: U`, `user: U` and `group: U` can hand out what they hold and nothing more, cannot assign the
  Owner role, and cannot add themselves to a group holding more; an owner can do all of it within the
  owner defaults; an owner with `grant` registered for Read only cannot create `grant: C`.
- **Functional:** none; nothing in the HTTP pipeline changes.

### Docs

`Corely.IAM/Docs/authorization.md` gains a section on granting: the rule, what each operation hands
out, and delegation. `domains/permissions.md`, `domains/roles.md` and `domains/groups.md` note the
refusal on their grant operations. `MIGRATION-3.0.md` is completed: callers that relied on granting
beyond their own permissions are refused, and system context is the path for host provisioning.

### Release

Corely.IAM 3.0.0, Corely.IAM.Web 3.0.0 and the migration CLI 3.0.0, per the phase 1 release table.
No schema change and no data change.

### System-defined permissions

The only way a host gets a system-defined permission is an owner default in `RegisterResourceType`.
Broader rules wait for a real use case. What follows:

1. Permissions a host adds to the Owner role later, under system context, are ordinary rows that an
   owner can remove. Adding a feature to existing accounts is a real hole, recorded in
   `owner-defaults-after-account-creation.md`.
2. A system-defined row cannot be revoked by anyone. A host that needs to take an owner capability away
   later (a downgraded plan) gives it as an ordinary permission rather than an owner default. Accepted:
   system-defined permissions have a very thin surface, and only the owner depends on them. Every other
   member can be given roles and permissions that carry no system-defined rows at all.
3. Changing a type's owner default affects accounts created afterwards; existing accounts keep their
   rows. Recorded in the same plan.
