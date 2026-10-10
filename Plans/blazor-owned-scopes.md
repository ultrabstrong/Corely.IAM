# Blazor components own their scopes

**Status: Corely.IAM.Web done, released as 3.4.0 and, with the audit pages, 3.5.0. Billing and
DocsToData next.** Found while verifying auditing ([auditing.md](auditing.md)): `/audit` never
finishes loading.

## Why

Blazor Server keeps one DI scope per browser tab (the circuit), not one per request. Every component
in a tab gets the same scoped services, so the same repos and the same `DbContext`. Components load in
parallel, and EF Core allows one query per context at a time, so two components loading together throw:

```
System.InvalidOperationException: A second operation was started on this context instance before a
previous operation completed.
```

The component that throws in `OnInitializedAsync` without catching it (the nav bar) ends the circuit,
and the page stays on its spinner.

- **Not caused by auditing.** Master fails the same way on `/profile`: the key panel and the two factor
  section collide. Auditing only changed the timing so `/audit` hits it on every load.
- **Not a library problem.** Corely.IAM, Corely.Billing and Corely.DataAccess assume one operation per
  scope, which every other host gives them (MVC, Razor Pages, APIs, SignalR hubs, Functions, hosted
  services, the CLI). Nothing in them runs work in parallel. Only Blazor Server keeps a scope alive
  across concurrent work, so the fix belongs in the Blazor components and nowhere below.
- **Blazor already has the answer.** `OwningComponentBase` gives a component its own DI scope, created
  with the component and disposed with it. Its `ScopedServices` resolves services from that scope, so
  each component gets its own repos and context. This is the pattern Microsoft documents for EF Core in
  Blazor Server when services rely on one context per unit of work, as IAM's do. No wrapper, no shared
  package: each component uses it directly.

Ruled out, and why:

| Option | Why not |
|--------|---------|
| A lock per context in Corely.DataAccess | Makes the data layer aware of one host and quietly absorbs real mistakes, such as a context registered as a singleton |
| A context per repo call (`IDbContextFactory`) | IAM's services rely on one tracked context and units of work across several repos |
| Queueing a tab's calls (Billing's `BillingCallGate`) | Serializes every load, has to wrap every call, and hides the shared context instead of removing it |
| A custom base or package wrapping `OwningComponentBase` | Adds tooling around something that already works as it is |

### The two rules

1. **A component that calls services inherits `OwningComponentBase`** (directly, or through a base
   class that does) and takes those services from `ScopedServices`. A component that only renders what
   it is given stays as it is.
2. **No `@inject` or `[Inject]` of a scoped service that reaches the database** in such a component.
   Property injection always resolves from the tab's scope, even inside `OwningComponentBase`, so it
   silently brings the bug back. Fine to inject:
   - singletons and framework services: `NavigationManager`, `IJSRuntime`, `TimeProvider`,
     `ILogger<T>`, `IOptions<T>`, `IResourceTypeRegistry`;
   - scoped UI state that components share across the tab on purpose and that never touches the
     database, such as IAM.Web's `IAccountDisplayState` (the account page tells the nav bar about a
     rename through it). This must stay on the tab's scope, so it stays injected.

### Signing in the new scope

IAM keeps the signed-in user in a scoped service (`IUserContextProvider`), and a component's own scope
starts empty. `BlazorUserContextAccessor.GetUserContextAsync()` signs in whatever scope it is resolved
from, using the auth cookie.

**Verified:** resolved from `ScopedServices`, it signs the component's own scope in. A spike that moved
only `AuthenticatedPageBase` onto `OwningComponentBase` loaded `/profile` signed in, and went on to load
its data (the page then failed only because its other sections still shared the tab's context).

So every component that calls IAM backed services, directly or through Billing or DocsToData services
that read the user context, calls `ScopedServices.GetRequiredService<IBlazorUserContextAccessor>()
.GetUserContextAsync()` before its first service call. IAM.Web's `AuthenticatedPageBase` already does
this for every page that inherits it.

## Projects

| Repository | Project | Shipped as | What changes |
|------------|---------|------------|--------------|
| Corely.IAM | `Corely.IAM.Web` | NuGet `Corely.IAM.Web` | Base classes, nav bar, shared components |
| Corely.IAM | `Corely.IAM.WebApp` | App in this repo | Nothing of its own beyond IAM.Web; verify only |
| Corely.Billing | `Corely.Billing.Web` | NuGet `Corely.Billing.Web` | Base classes, replace `BillingCallGate` |
| Corely.Billing | `Corely.Billing.Web.IAM` | NuGet `Corely.Billing.Web.IAM` | Check the account accessor works in a component's own scope |
| Corely.Billing | Demo hosts | Apps | Their own pages that call services |
| DocsToData | `DocsToData.AdminPortalWebApp` | App | Its pages, modals and nav bar |

Corely.Billing, Corely.Billing.IAM, Corely.IAM and the migration CLIs do not change.

## Order

1. **Corely.IAM.Web**, released first: DocsToData's pages inherit its `EntityPageBase`, and
   Billing.Web.IAM bridges to IAM.
2. **Corely.Billing.Web** and **Corely.Billing.Web.IAM**, released next, against the new IAM.Web.
3. **DocsToData**, last: takes both new packages, then fixes its own components.

Each is its own branch and pull request in its own repository. Versions are bumped in each package's
csproj; tags are pushed only with the owner's say so (see CLAUDE.md, Releasing). The change is not
breaking for consumers: a derived component that still uses `@inject` compiles and behaves as before,
it just keeps sharing the tab's context. A minor version bump for each package.

## Instructions

### Every repository

1. List every `.razor` and `.razor.cs` with `@inject` or `[Inject]`. For each, decide: does it call a
   scoped service (an IAM, Billing or DocsToData service, a repo, a provider backed by one)?
2. **No:** leave it.
3. **Yes:**
   - Inherit `OwningComponentBase`, or a base class that does.
   - Replace each scoped `@inject X Name` with a property that resolves from the component's scope:
     `private X Name => ScopedServices.GetRequiredService<X>();`. In a `.razor` file, put it in the
     `@code` block. Keep singleton and framework injections as they are.
   - If it calls anything that needs the signed-in user and does not inherit `AuthenticatedPageBase`,
     sign its scope in first with the accessor resolved from `ScopedServices` (above).
4. Add a unit test that fails when a component inheriting `OwningComponentBase` (directly or not)
   injects a service registered as scoped. Reflect over the component types, read their `[Inject]`
   properties (`@inject` compiles to these), and check each type's lifetime against the app's real
   service collection, with an explicit allow list for shared UI state such as `IAccountDisplayState`.
   This keeps rule 2 from regressing.
5. Run the repository's full test suite, then load every page in a browser with the browser's network
   panel open and the server log visible. A page passes when it loads, and the log has no
   `A second operation was started on this context instance`.

### Corely.IAM.Web

- `AuthenticatedPageBase` inherits `OwningComponentBase`. `BlazorUserContextAccessor` becomes
  `ScopedServices.GetRequiredService<IBlazorUserContextAccessor>()`. `EntityPageBase`,
  `EntityListPageBase<T>` and `EntityDetailPageBase` follow automatically.
- Pages that inherit those bases (`AccountDetail`, `GroupList`, `GroupDetail`, `RoleList`, `RoleDetail`,
  `PermissionList`, `PermissionDetail`, `UserList`, `UserDetail`, `Profile`, `AcceptInvitation`, and on
  the auditing branch `AuditLog` and `PlatformSettingsPage`): move their service `@inject`s to
  `ScopedServices`. Keep `IJSRuntime` and `IResourceTypeRegistry` (a singleton) injected.
- Components with no base class that call services: `NavBar`, `AccountHeader`,
  `AuthenticatedContent`, `PermissionView`, `EncryptionSigningPanel`, `LinkedAccountsSection`,
  `PasswordSection`, `TotpSection`, and on the auditing branch `AuditPurgeModal` and
  `AuditSettingsSection`. Each inherits `OwningComponentBase`, takes services from `ScopedServices`, and
  signs its scope in.
- `PermissionView` renders many times per page. Its own scope per instance means one sign in per
  instance (a user lookup and token check). Measure a list page; if it is slow, have `PermissionView`
  take the user context from a cascading value its page provides, instead of signing in its own scope.
- `ThemeToggle`, `RedirectToLogin` and `LoggingErrorBoundary` inject only framework services: no change.
- `IAccountDisplayState` is scoped and carries the account name from `AccountDetail` to `NavBar`. Keep
  it injected in both, so both use the tab's instance. Resolving it from `ScopedServices` would give each
  its own copy and break the rename.
- Docs: `Corely.IAM.Web/Docs/base-classes.md` and `components/*` say that components take services from
  `ScopedServices` and why, in a short paragraph with the rule. `setup.md` tells hosts building their own
  Blazor pages the two rules.
- Merge into `auditing` once released, then finish verifying `/audit`, `/platform-settings` and the
  account audit section.

### Corely.Billing.Web and Corely.Billing.Web.IAM

- `BillingComponentBase` and `BillingPageBase` inherit `OwningComponentBase`. Their injected services
  move to `ScopedServices`.
- Remove `BillingCallGate` and `SerializedAsync`. Components that wrapped calls in `SerializedAsync`
  call their services directly; each now has its own context.
- `ConsumptionTable`, `GrantEditor`, `GrantList`, `UsageChart`, `UsageDashboard`: move their `[Inject]`
  service properties to `ScopedServices`. Keep `TimeProvider` injected.
- `IBillingAccountAccessor` (from `Corely.Billing.Web.IAM` when IAM is present): confirm it works when
  resolved from a component's own scope. If it reads IAM's user context, the scope must be signed in
  first. Billing.Web cannot reference IAM.Web, so the IAM implementation of the accessor signs the scope
  in itself through `IBlazorUserContextAccessor` before reading the user.
- Demo hosts (`Corely.Billing.Demos.Portal`, `Corely.Billing.Demos.WithIAM`): their `Home` pages call
  services; same treatment.

### DocsToData

- Pages inheriting IAM.Web's `EntityPageBase` (`DocumentWorkflows`, `DocumentWorkflowEditor`,
  `DocumentWorkflowStepEditor`, `ExtractionTemplates`, `ExtractionTemplateEditor`, `RunOnce`, `Sftp`,
  `SftpUserDetail`) get their scope from the new IAM.Web. Move their `[Inject]` service properties in
  the `.razor.cs` files to `ScopedServices`.
- Pages inheriting Billing's `BillingPageBase` (`Grants`, `GrantEditor`, `Usage`) get their scope from
  the new Billing.Web.
- Components with no base class that call services: `NavBar`, `Home`, `CreateDocumentWorkflowModal`,
  `CreateTemplateModal`, `SftpChangePasswordModal`, `SftpUserFormModal`, `ProviderModelSelect` (check),
  `ProviderLabel` (check). Same treatment as IAM.Web's.
- `BusySpinner` and `JsonEditor`: check what they inject; likely framework only.

## Done when

Every Blazor component in Corely.IAM.Web, Corely.Billing.Web and DocsToData that calls a scoped
service runs in its own scope, each repository has the test that fails on a scoped injection, every
page loads in a browser with no "second operation" error in the log, and the new IAM.Web and Billing
packages are released and taken by DocsToData.

## Progress

### Corely.IAM.Web (step 1 of the Order): done, on master

1. **Reproduced on master.** `/profile` signed in as `admin`: NavBar's `ListAccountsAsync` threw "A
   second operation was started on this context instance".
2. **Components moved.** `AuthenticatedPageBase` inherits `OwningComponentBase`, so every page under it
   owns a scope. The pages' service `@inject`s became `ScopedServices` properties. `NavBar`,
   `AccountHeader`, `AuthenticatedContent`, `PermissionView`, `TotpSection`, `PasswordSection` and
   `LinkedAccountsSection` inherit `OwningComponentBase` and sign their own scope in.
   `IAccountDisplayState`, `NavigationManager`, `IJSRuntime`, `IOptions<T>`, `ILogger<T>` and
   `IResourceTypeRegistry` stay injected.
3. **Test.** `Corely.IAM.Web.UnitTests/Components/OwningComponentInjectionTests.cs` builds the real
   collection (`AddRazorComponents`, `AddIAMWeb`, `AddIAMWebBlazor`, `AddIAMServices` on EF) and fails on
   any `[Inject]` of a scoped service in an owning component. Allow list: `IAccountDisplayState`,
   `NavigationManager`, `IJSRuntime`. A second test proves it reports a fixture that injects
   `IRetrievalService`.
4. **Measured.** `/permissions` with 25 rows (26 `PermissionView`s, each signing in its own scope) on
   LocalDB: rows at about 125 ms after navigation, every `PermissionView` resolved about 30 ms later, on
   two loads. Not slow, so the cascading user context fallback was not applied.
5. **Docs.** `base-classes.md`, `components/index.md`, `components/permission-view.md` and `setup.md`.
6. **Verified.** `RebuildAndTest.ps1`: 1965 tests, 0 failed, 14 skipped (the opt-in container tests).
   In a browser with the server log captured, every IAM.Web page loaded with nothing at warning level
   or above in the log: `/`, `/profile`, `/accounts/{id}`, `/users`, `/users/{id}`, `/groups`,
   `/groups/{id}`, `/roles`, `/roles/{id}`, `/permissions`, `/permissions/{id}`,
   `/accept-invitation`. Renaming the account on `/accounts/{id}` updated the nav bar, so
   `IAccountDisplayState` is still shared.
7. **Version.** `Corely.IAM.Web` 3.3.0-preview.1 to 3.4.0, tagged and published. It depends on `Corely.IAM` 3.4.0-preview.1, the version in that csproj, so the stable package carries a prerelease dependency until `Corely.IAM` 3.4.0 ships.

### Decisions the plan did not cover

- **Disposal.** `OwningComponentBase` disposes its scope in `Dispose(bool)` and `DisposeAsyncCore()`.
  A component declaring its own `Dispose()` or `DisposeAsync()` would hide those and leak the scope, so
  `EntityListPageBase`, `TotpSection` and `LinkedAccountsSection` override `DisposeAsyncCore()` and call
  the base, and `NavBar` overrides `Dispose(bool)`. NavBar unsubscribes from `IAccountDisplayState`
  whatever `disposing` is, since the renderer disposes asynchronously and that path calls
  `Dispose(false)`.
- **`EncryptionSigningPanel` unchanged.** The plan lists it, but it injects only `IJSRuntime` and works
  on providers and a rotate callback its page passes in, so it renders what it is given (rule 1).
- **`AuthenticatedContent` now signs in its own scope, not the tab's.** It still holds its children
  back until the cookie is checked, but children that inject from the tab's scope no longer find that
  scope signed in. Nothing in this repository uses it; under the two rules every child signs in its own
  scope anyway.
- **`PermissionView` signs in through the accessor** instead of reading `IUserContextProvider`; its tests
  mock `IBlazorUserContextAccessor` now.
- **`AuthenticatedPageBase.BlazorUserContextAccessor`** is now a get only property resolving from
  `ScopedServices`. A derived class that assigned it would no longer compile; none here does.
- **Verification data.** The existing LocalDB database had `admin` with no account, and `/permissions`
  then throws on `UserContext!.CurrentAccount!` (also true on master). Rather than `-Reset` the database,
  I created the account "Scope Verification" (renamed "Scope Verification Renamed" by the rename check),
  the group "Verification Group" and 20 permissions on the Roles type described "Measure 0" to
  "Measure 19". They are still in that database.
- **Demo hosts and the WebApp dashboard, added at the owner's request.** `Corely.IAM.Demos.UsersOnly`
  (`Home`, `Profile`), `Corely.IAM.Demos.SharedAccount` (`Home`, `Profile`, `Team`) and the WebApp's
  `Home` inherit `OwningComponentBase` and take IAM services from `ScopedServices`; each already signs
  in first thing through the accessor, which now signs in the page's own scope. `IDbContextFactory<T>`,
  `TimeProvider` and `NavigationManager` stay injected. The demo layouts keep the accessor injected: a
  `LayoutComponentBase` cannot also own a scope, and the tab's scope is signed in by
  `IamAuthenticationStateProvider` anyway, the accessor's semaphore serializing the two. The injection
  test covers only IAM.Web's assembly, since the test project does not reference the hosts. Verified:
  `RebuildAndTest.ps1` green (1965, 0 failed), and both demos' `/`, `/profile` and `/team` loaded in a
  browser, with no warnings or errors in either log.
- **Found while verifying, not changed:** on the SharedAccount demo, `olivia` (the seeded owner) is
  refused Read on users, so `/team` says only the owner can see members. Master behaves the same. The
  demo database was seeded before this session, likely by a version before the Owner role carried its
  per type permissions; reseeding (drop both demo databases) would tell.

- **Auditing merged into master.** `AuditLog`, `PlatformSettingsPage`, `AuditPurgeModal` and
  `AuditSettingsSection` take their services from their own scope; `/audit`, `/platform-settings` and
  the account audit section load in a browser with a clean log. They ship in IAM.Web 3.5.0.

### Next

- Billing (step 2) and DocsToData (step 3), in their own repositories, against IAM.Web 3.5.0 and
  Corely.IAM 3.4.0.
