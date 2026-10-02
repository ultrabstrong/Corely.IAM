# Explicit permission resource types

**Status: phase 1 ready to build once the phase 1 questions below are answered. Phase 2 is a proposal
for discussion; do not build it.**

## Questions for you

Everything else in this plan was checked against the code of Corely.IAM, Corely.Billing and
DocsToData. These need a decision from you. Each has a recommendation in the section it links to.

**Phase 1 (an agent needs these):**

1. **The step for existing accounts** (decision 2). It can only be a service method, because the
   migration CLI never sees a host's registered types. Recommendation: make it reusable and add only,
   which also closes the first gap in `owner-defaults-after-account-creation.md`. Or keep it strictly
   one off?
2. **Rows that already exist** (decision 6). The permissions table is unique on account, type, id and
   all five actions, so a migrated or backfilled row can collide with an identical row. Recommendation:
   reuse the existing row, link it to the same roles, and mark it system-defined when it is an owner
   default. That makes such a row undeletable.
3. **`*` rows people created themselves** (decision 3). Recommendation: expand each into one row per
   IAM type. Confirm.
4. **Deleting the constant** (decision 5). Confirm.
5. **The shape of owner actions** (decision 1). No parser for a `"cRUdX"` string exists, and
   `AuthAction` is not a flags enum. Recommendation: `params AuthAction[] ownerActions`. Confirm.

**Consumers (needed before DocsToData upgrades, not to build IAM):**

6. **Corely.Billing's owner actions** for `grant`, `consumption` and `quota`. Owners reach all three
   through `*` today. `grant` with Read only is what stops owners writing grants, once phase 2 lands.
7. **DocsToData's owner actions** for its `extraction`, `document_workflows` and `sftp` types. Owners
   create SFTP users in the UI today (`SftpAdministrationAuthorizationDecorator` checks Create on
   `sftp`), so `sftp` without Create stops that unless DocsToData creates them itself.
8. **Where DocsToData runs the step for existing accounts.** Its deploy applies IAM migrations in
   `migrate-database.yml`; `DocsToData.Functions` already runs under system context. Until the step
   runs, existing owners lose access to every DocsToData and Billing type.

**Phase 2 (for when we discuss it):**

9. **The two rules proposed** in Phase 2.
10. **A gap that exists today.** Anyone with Update on users and Read on roles can assign themselves the
    Owner role; nothing in `AssignRolesToUserAsync` or `AssignRolesToGroupAsync` restricts it. Phase 2
    closes it. In DocsToData only owners hold those permissions today, so waiting for 3.0 looks safe.
    Agree?

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
- **Invitations carry no role**, so they are no path to escalation (relevant to phase 2).

Outside this repository, owners reach these host types only through `*` today:

| Consumer | Types | Registered in |
|----------|-------|---------------|
| Corely.Billing.IAM | `grant`, `consumption`, `quota` | `IAMOptionsExtensions.RegisterBillingResourceTypes` |
| DocsToData | `extraction`, `document_workflows`, `sftp` | `DocsToData.Core/Extensions/IAMOptionsExtensions.cs` |

Each needs owner actions declared (questions 6 and 7) and existing accounts need the step in decision 2,
or owners lose access to all six.

## Phase 1: remove the resource type wildcard

### Decide before building

1. **How a host's own types get owner defaults.** Decided: the host gives the owner's actions when it
   registers the type, defaulting to none. IAM applies them to the Owner role only, and only when the
   account is created, inside the same unit of work as the rest of account registration. Recommended
   shape (question 5):

   ```csharp
   options.RegisterResourceType("sftp", "SFTP access", AuthAction.Read, AuthAction.Update, AuthAction.Execute);
   ```

   IAM's own five types are pre-registered with all five actions. They are not configurable.
   `ResourceTypeInfo` gains the owner actions, and `CreateDefaultSystemPermissionsAsync` builds the
   Owner role's permissions from the registry and nothing else.

   Anything beyond the owner's defaults stays with the host: other roles, a starter grant, or
   permissions added later by an async process, done under system context.

   Considered and set aside: a host decorator on account registration that adds the owner's
   permissions afterwards. It needs IAM's internals to find the Owner role, every host with its own
   types writes the same code, and its rows cannot be system-defined.

2. **Existing accounts.** The migration expands every `*` row (decision 3), which gives existing owners
   the five IAM types. It cannot give them host types: the migration CLI is a separate tool that never
   sees a host's `RegisterResourceType` calls. So the host types need a step the host runs after
   upgrading, and that step can only be a service method.

   Recommendation (question 1): one idempotent operation, "make sure this account's Owner role holds
   every registered owner default", used both by account registration and by a system context only
   method that runs it for every account. It only adds rows; it never removes or narrows. Run again
   after a host adds a type, it also closes the first gap in `owner-defaults-after-account-creation.md`.
   The service decorator refuses it outside system context (`HasUserContext() &&
   !IsNonSystemUserContext()`).

3. **`*` rows.** Recommendation (question 3): the migration turns every `*` row, the owner's included,
   into one row per IAM type with the same actions, the same `IsSystemDefined` and the same role links,
   then deletes the `*` row and its `RolePermissions` rows. One rule covers the owner's row and rows
   people created (the demo seed creates some). Nobody gains access. Access to host types is lost, and
   the migration guide says so.

4. **Which owner permissions are system-defined.** Decided: all owner defaults, IAM and host types
   alike, and nothing else. No API creates or removes a system-defined permission, under system context
   or otherwise. `IsOwnerSystemPermission` becomes "a system-defined permission linked to the Owner
   role" rather than a match on `*`.

5. **The constant.** Recommendation (question 4): delete `ALL_RESOURCE_TYPES` in the preflight rather
   than mark it obsolete. The preflight already breaks behavior, we are the only consumers, and a
   constant that still compiles invites the next caller to use it.

6. **Rows that already exist.** The `Permissions` table has a unique index on `AccountId`,
   `ResourceType`, `ResourceId` and the five action columns. A row produced by decision 2 or 3 can
   therefore collide with one already there: an owner who once created `role, all, CRUDX` already has
   the row the owner default needs. Recommendation (question 2): reuse the existing row instead of
   inserting, add the missing role links, and set `IsSystemDefined` when the row is an owner default.
   The reused row then cannot be deleted, which matches what it now is.

### Changes

- **Constant and registry:** delete `ALL_RESOURCE_TYPES` and its registry entry. `RegisterResourceType`
  rejects `"*"` explicitly; once `*` is no longer pre-registered, the duplicate check alone would let it
  through. `PermissionValidator` then rejects `"*"` with no change of its own.
- **Owner actions:** `RegisterResourceType` takes them (decision 1). `IAMOptions.CustomResourceTypes`
  carries them alongside the description, `ServiceRegistrationExtensions` passes them to the registry,
  and `ResourceTypeInfo` exposes them.
- **Owner defaults:** `CreateDefaultSystemPermissionsAsync` becomes the idempotent operation in
  decision 2: one system-defined row per registered type with owner actions, `ResourceId = Guid.Empty`,
  linked to the Owner role, reusing identical rows per decision 6. Account registration calls it as
  today; the system context method calls it per account, one unit of work per account.
- **Authorization:** `AuthorizationProvider.IsAuthorizedAsync`, `GetAuthorizedResourceIdsAsync` and
  `PermissionProcessor.GetEffectivePermissionsForUserAsync` match the exact resource type only.
- **Owner role protection:** the Owner role's permissions stay protected three ways, now that there are
  several rows instead of one:

  | Path | Today | Phase 1 |
  |------|-------|---------|
  | Delete the permission | `DeletePermissionAsync` refuses any system-defined permission | Unchanged |
  | Detach it from the Owner role | `RemovePermissionsFromRoleAsync` refuses `IsOwnerSystemPermission`, a match on the `*` row's exact makeup | Refuses any system-defined permission linked to the Owner role; `IsOwnerSystemPermission` goes |
  | Weaken it in place | Not possible: no operation updates a permission's actions | Unchanged |

  These guards sit in the processors, not the authorization decorators, so they hold against system
  context too. Deleting or renaming the Owner role is already refused.
- **Migration:** `.\AddMigration.ps1 "RemoveWildcardResourceType"` for both providers, with provider
  specific SQL for decisions 3 and 6. Ids come from `NEWID()` on SQL Server and `UUID()` on MySQL (the
  column is `char(36)` there); `CreatedUtc` has a database default on both. The join table is
  `RolePermissions` (`PermissionsId`, `RolesId`). It changes data only, so the model snapshot does not
  change. `Down` is a no-op: the expanded rows work under the previous code, which merely loses
  nothing it needs, and the `*` rows cannot be rebuilt faithfully anyway. Every existing migration
  implements `Down`, so throwing would block rolling back past this point. The migration guide says to
  back up first.
- **Corely.IAM.Web:** drop the `*` filter in `PermissionList.razor`. The role page shows a remove
  button on every permission; today the Owner role has one row whose removal fails, after phase 1 it
  has five or more. Give `Permission` an `IsSystemDefined` with an internal setter, as `Role` already
  has, and hide remove and delete for system-defined permissions, showing the same System badge roles
  use.
- **Demo seed:** replace the `*` permissions in `SeedWebAppDemo.ps1` with explicit types.

### Tests

- **Unit:** registry (no `*`, `RegisterResourceType("*")` rejected, owner actions stored, IAM types
  carry all five), validator rejects `"*"`, the owner defaults operation (one row per type with owner
  actions, none for types without, idempotent on a second run, reuses an identical row and marks it
  system-defined), `AuthorizationProvider` treats `"*"` as an ordinary unmatched string, the Owner role
  guard refuses any system-defined row and allows other rows, the system context only method refuses a
  user context. Existing tests that use `ALL_RESOURCE_TYPES` are rewritten or removed:
  `AuthorizationProviderTests`, `RoleProcessorTests`, `ResourceTypeRegistryTests`,
  `PermissionProcessorTests`, `PermissionMapperTests`, `ServiceRegistrationExtensionsTests`.
- **Integration (SQLite):** an owner manages every IAM type in a new account and has no access to a
  registered host type without owner actions; a host type with owner actions is granted exactly those.
- **Integration (provider matrix):** migrate to the migration before this one, insert an owner `*` row,
  a user-created partial `*` row linked to a second role, and a row identical to one the expansion
  produces; migrate to latest; assert the expanded rows, their role links, `IsSystemDefined`, the reuse
  of the identical row, and that no `*` row remains. Needs Docker and `CORELY_RUN_CONTAINER_TESTS=1`;
  the SQLite tier never runs migrations.
- **Functional:** none; nothing in the HTTP pipeline changes.

### Docs

Remove the resource type wildcard from `Corely.IAM/Docs/authorization.md`, `resource-types.md`,
`iam-options.md`, `usage-shapes.md`, `domains/permissions.md`, `domains/roles.md`, `domains/accounts.md`,
`Corely.IAM.Web/Docs/pages/permissions.md` and `Docs/Permission Model.md`. Document owner actions on
`RegisterResourceType` and the system context method for existing accounts. `MIGRATION-3.0.md` at the
repository root covers decisions 2, 3 and 6, the backup before migrating, and the order: deploy the
code declaring owner actions, apply the migration, run the existing accounts step.

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

## Phase 2: no escalation

**Proposal for discussion. Not decided; do not build.**

### Every path that grants access today

| Operation | Checks today | How it escalates |
|-----------|--------------|------------------|
| Create a permission | Create on `permission` | Writes any type with any actions, `grant: C` included |
| Attach permissions to a role | Update on the role, Read on the permissions | Attaches a powerful existing permission to the caller's own role |
| Assign roles to a user | Update on the user, Read on the roles | Gives the caller any role, the Owner role included |
| Assign roles to a group | Update on the group, Read on the roles | The same, through a group the caller belongs to |
| Add users to a group | Update on the group, Read on the users | Joins a group whose roles hold more than the caller |

Invitations carry no role, so they are not on the list.

### Proposed rules

- **Rule A, grant only what you hold.** Each operation above succeeds only if the caller already holds
  every action it would hand out, on that resource type, covering that resource id. A caller's
  `Guid.Empty` covers any id; a specific id covers only itself. For role assignment and group
  membership, "what it hands out" is every permission of the roles involved. This is how Kubernetes
  RBAC and SQL's `WITH GRANT OPTION` work. System context bypasses it, as it bypasses every check.
- **Rule B, the Owner role's permissions belong to IAM.** No user can attach or detach permissions on
  the Owner role. Its permissions are the registered owner defaults plus whatever the host adds under
  system context. This is the second half from earlier, "an owner cannot remove what their role
  needs", without having to decide what a role needs.

Together: an owner cannot mint `grant: C`, because they do not hold it (rule A). They cannot strip the
Owner role (rule B and phase 1). Only someone holding all of the Owner role's permissions can make
another owner (rule A), which closes the gap in question 10.

Removals (detaching a permission, removing a role or a member) only reduce access and get no new rule.

### Decided in discussion

**System-defined stays locked down.** The only way a host gets a system-defined permission is the owner
default it gives in `RegisterResourceType`, which IAM creates for the Owner role. Broader rules wait for
a real use case. What follows, with the call on each:

1. Permissions a host adds to the Owner role later, under system context, are ordinary rows. Rule B
   would protect them from users. Adding a feature to existing accounts is a real hole, recorded in
   `owner-defaults-after-account-creation.md` (decision 2's operation may close most of it).
2. A system-defined row cannot be revoked by anyone. A host that needs to take an owner capability away
   later (a downgraded plan) gives it as an ordinary permission rather than an owner default. Accepted:
   system-defined permissions have a very thin surface, and only the owner depends on them. Every other
   member can be given roles and permissions that carry no system-defined rows at all.
3. Changing a type's owner default affects accounts created afterwards; existing accounts keep their
   rows. Recorded in the same plan.
