# Blazor components own their scopes

**Status: phase 1 done, phase 2 not started.** Phase 1 (a scope per component) shipped in
Corely.IAM.Web 3.4.0 and 3.5.0, Billing.Web 2.2.0, Billing.Web.IAM 2.1.0 and Billing.IAM 2.1.0;
DocsToData converted and committed, push pending the owner. Phase 2 (a scope per operation, at the end
of this file) fixes the stale reads phase 1 left in place. Found while verifying auditing
([auditing.md](../Completed/auditing.md)): `/audit` never finishes loading.

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

### Corely.Billing (step 2): done, released

- `BillingComponentBase` and `BillingPageBase` inherit `OwningComponentBase`; the base calls
  `IBillingAccountAccessor.GetAccountIdAsync()` from the component's own scope first, which is where
  `Corely.Billing.Web.IAM`'s accessor signs that scope in. No change to the accessor was needed: it
  already went through `IBlazorUserContextAccessor`, which signs in whatever scope resolves it.
- `BillingCallGate` and `SerializedAsync` are gone; the five components call their services directly.
- **Not in the plan: `UsageExporter` takes `IJSRuntime`.** Resolved from a component's scope it would
  get a JS runtime not attached to the circuit, so it is no longer registered: the base builds it with
  `ActivatorUtilities` from `ScopedServices` plus the injected `IJSRuntime`, and disposes it.
- `UsageChart` overrides `DisposeAsyncCore`. The dashboard test that asserted chart and table never
  overlap their calls was the gate's premise and is replaced by one that they load.
- `OwningComponentInjectionTests` in `Corely.Billing.Web.UnitTests`. Demo `Home` pages converted.
  Billing's CLAUDE.md boundary note and the Web and Web.IAM docs rewritten.
- Released: Billing.Web 2.2.0, Billing.Web.IAM 2.1.0 and Billing.IAM 2.1.0 (on Corely.IAM 3.4.0 and
  IAM.Web 3.5.0). Verified in the WithIAM demo, reseeded: olivia reads grants, carla edits and saves
  one, bobby is refused, with a clean log; the Portal demo's dashboard, simulate, export, grants and
  usage pages load clean.

### DocsToData (step 3): done, committed locally, not pushed

- Took the stable packages, converted the nav bar, home page, four modals and the code behind pages.
  `ProviderModelSelect` and `ProviderLabel` stay as they are: their resolver only reads provider
  profiles. Four pages that disposed resources override `DisposeAsyncCore`.
- The injection test lives in the portal's functional tier, on the host's real registrations
  captured through `PortalFactory`.
- **Found while verifying:** the template editor threw (and lost the circuit) when the add field
  textarea inside a `PermissionView` had not rendered yet; owned scopes made that latent race likely.
  The JS helper now skips a reference that is not an element, as `initSortable` already did, and the
  page keeps the request pending until it attaches.
- **Corely.IAM 3.4.0's two factor rule** refused the local seed's platform members; the seed now
  enrolls them in TOTP, with their secrets in the seed header and `docs/local-development.md`.
- Verified: the full suite passes except the Functions host tier, which fails the same way on `main`
  (the host exits with code 1); the portal tier passes including the browser tests; a click through
  as admin on desktop and phone, Run Once end to end, and provisioner signing in with a code,
  entering Demo Account and creating a grant, with a clean log.
- **Not pushed.** DocsToData's CLAUDE.md rations pushes (GitHub Free Actions budget); the two commits
  wait for the owner.

## Phase 2: a scope per operation

The owner, 2026-11-06, after stale reads in two apps were traced to component lifetime scopes. Worked
out from Corely.DataAccess, where a repository level fix was considered first and rejected; that
repository's `Plans/Completed/fresh-reads-by-default.md` is the record of the options and why each failed.
Facts below marked as checked were read from the code on that date; the component inventory and line
numbers are a snapshot, and the compiler is the authority once the work starts.

### Why phase 1 is not enough

Phase 1 gave each component a scope that lives as long as the component. That stopped components
sharing a context, but a component on screen for minutes keeps one `DbContext` for minutes. EF Core
returns the instance it already tracks for any row it has loaded before, so every read after the
first can be stale ([identity resolution](https://learn.microsoft.com/ef/core/change-tracking/identity-resolution#identity-resolution-and-queries)).
Two seen so far:

- DocsToData's `WorkflowJob` reads the job on a five second timer through its component scope and kept
  showing the status it first loaded while the Functions host changed it. `DocumentJobService` works
  around it with `AsNoTracking()` on ten queries.
- Billing.Web's `GrantList` cancels a request (written with `ExecuteUpdateAsync`), reloads through the
  same component scope, and gets the tracked request back still pending.

Microsoft's guidance for EF Core in Blazor Server
([Blazor with EF Core](https://learn.microsoft.com/aspnet/core/blazor/blazor-ef-core)) is a context per
operation, and a context for a component's lifetime only "for longer-lived operations that take
advantage of EF Core's change tracking." Phase 1 chose the second; the components need the first.

Corely.DataAccess will not change to absorb this. EF Core has no way to refresh tracked entities from
a query (dotnet/efcore#16491, open since 2019; two community implementations closed unmerged in 2026 as
too complex), and every repository level alternative pushes EF's change tracking into what callers
must know. Corely.DataAccess's `Plans/Completed/fresh-reads-by-default.md` records the full reasoning. The scope's
lifetime is the host's to choose, and this is the host.

### Ruled out

| Option | Why not |
|--------|---------|
| Keep `OwningComponentBase` | Its scope is private, created once and disposed with the component. It cannot give an operation a fresh one |
| A component scope renewed at the start of each load | A timer tick and a button action can overlap; renewing disposes the context under the one still running. Where an operation starts stays implicit, so a forgotten renewal brings the stale read back silently |
| `AsNoTracking()` at each read that matters | Sidesteps the repositories call site by call site, and leaves every other component exposed |
| Copying the component's `UserContext` into each new scope | `IUserContextSetter` is internal to Corely.IAM. Signing in again is the same call phase 1 already makes per component, and it also catches a revoked session |

Phase 1's table ruled out "a custom base or package wrapping `OwningComponentBase`" because the
framework type already did the job. For a scope per operation it does not, so phase 2 adds its own.

### The shape

1. **One operation, one scope.** An operation is one user visible action: the reads that render
   together on load, one timer tick, or a button's action together with the reload that follows it.
   Each runs in a scope of its own, signed in, disposed when it ends. Nothing is remembered between
   operations, so every read is fresh. Components running at once still never share a context, so
   phase 1's fix holds.
2. **Corely.IAM.Web: `IOperationScopes`**, public, registered scoped by `AddIAMWebBlazor`, so one
   instance per tab:

   ```csharp
   public interface IOperationScopes
   {
       Task<T?> RunAsync<T>(Func<IServiceProvider, Task<T>> work);
       Task<bool> RunAsync(Func<IServiceProvider, Task> work);
   }
   ```

   Each call:
   1. creates an `AsyncServiceScope` from `IServiceScopeFactory`;
   2. signs it in: `IAuthenticationService.AuthenticateWithTokenAsync(token)` resolved from the new
      scope, the call `BlazorUserContextAccessor` makes today;
   3. on success runs `work` with the new scope's provider and returns its result (`true` for the non
      generic form);
   4. on failure does not run `work`, calls `NavigationManager.NavigateTo(NavigationManager.Uri,
      forceLoad: true)`, and returns `default` (`false`);
   5. disposes the scope either way.

   **The token** is read from the auth cookie through `IHttpContextAccessor` once, the first time the
   tab's instance needs it, and kept for the tab. Microsoft's guidance is to copy what is needed from
   `HttpContext` rather than reach for it later from interactive code
   ([HttpContext in Blazor](https://learn.microsoft.com/aspnet/core/blazor/components/httpcontext)), and
   a circuit's `HttpContext` is fixed at connection anyway, so a renewed cookie never reaches it.

   **Why a forced reload on failure:** auth tokens live an hour by default
   (`SecurityPolicy.AUTH_TOKEN_TTL_SECONDS_DEFAULT`). Under phase 1 a component signed in at its start
   stays signed in past expiry; under phase 2 the first operation after expiry fails to sign in. A full
   reload goes through `AuthenticationTokenMiddleware`, which renews from the refresh token or sends
   the user to sign in. A page left open past its token's life reloads on its next action. A revoked
   session now takes effect on the next action rather than the next page.
3. **Corely.IAM.Web base classes.**
   - `AuthenticatedPageBase` inherits `ComponentBase`, not `OwningComponentBase`. It injects
     `IOperationScopes` and exposes `protected Task<T?> InOperationAsync<T>(Func<IServiceProvider,
     Task<T>> work)` and the non generic form. `OnInitializedAsync` loads `UserContext` through one
     operation (`IBlazorUserContextAccessor.GetUserContextAsync()` resolved from its provider) and
     redirects to sign in as now when there is no user. `BlazorUserContextAccessor` the property goes.
   - New `OperationComponentBase` (`ComponentBase`, injects `IOperationScopes`, exposes the same two
     methods, no redirect) for components that call services without being pages.
   - `ScopedServices` no longer exists on either, so every remaining use fails to compile. The compiler
     is the inventory; the lists below are what it will report today.
   - `EntityListPageBase`, `TotpSection` and `LinkedAccountsSection` override `DisposeAsyncCore()`, and
     `NavBar` overrides `Dispose(bool)`, only because `OwningComponentBase` owned disposal. Each
     implements `IAsyncDisposable` or `IDisposable` itself for whatever it still has to release
     (`IAccountDisplayState` unsubscription, cancellation tokens), or drops the override if nothing is
     left.
4. **Corely.Billing.Web: `IBillingOperationScopes`**, the same two methods. Billing.Web cannot
   reference IAM.Web; Billing.Web.IAM can and does (checked: `PackageReference Corely.IAM.Web` in
   its csproj). So:
   - Billing.Web's default implementation creates a scope, calls
     `IBillingAccountAccessor.GetAccountIdAsync()` in it to sign it in (what `BillingComponentBase`
     does today per component), runs the work and disposes the scope.
   - Billing.Web.IAM replaces it with an implementation that delegates to IAM.Web's `IOperationScopes`,
     so token capture and the forced reload live in one place.
   - `BillingComponentBase` and `BillingPageBase` inherit `ComponentBase`, inject
     `IBillingOperationScopes`, expose `InOperationAsync` like IAM.Web's, and lose the `_signIn` step in
     `OnInitializedAsync` and the sealed `OnParametersSetAsync` that awaited it.
     `OnParametersSetSignedInAsync` becomes `OnParametersSetAsync`, a breaking rename for derived
     components.
   - `UsageExporter` is built inside the export's operation with
     `ActivatorUtilities.CreateInstance<UsageExporter>(provider, JSRuntime)` and disposed at its end,
     instead of once per component from `ScopedServices`.
5. **Call sites.** Every `private X Name => ScopedServices.GetRequiredService<X>();` property goes.
   Each operation resolves what it uses from the provider it is handed. No component keeps a service
   in a field across operations. `WorkflowJob.LoadJobAsync`, for example, wraps its body, including the
   neighbour, charges, template and result reads, in one `InOperationAsync`, and the cancel and retry
   handlers each run the action and the reload that follows in one more.
6. **Edits across operations are safe as they stand.** Components hold models, never entities, and
   every update takes a request model the service applies inside its own call:
   `IModificationService.Modify*Async`, `IPlatformService.UpdateSecuritySettingsAsync`,
   `IAuditService.UpdateAccountSettingsAsync` and `UpdatePlatformSettingsAsync`,
   `IGrantService.UpdateGrantAsync`, `IDocumentWorkflowTemplateService.UpdateWorkflowTemplateAsync`
   and `UpdateStepTemplateAsync`, `IExtractionTemplateService.UpdateTemplateAsync`,
   `UpdateFieldAsync` and `UpdateTableColumnAsync`, `IDocumentRetentionService.UpdateSettingsAsync`,
   `INotificationSettingsService.UpdateNotificationChannelAsync`. A form loaded in one operation and
   saved in another needs no change.
7. **Stays on the tab's scope:** `IAccountDisplayState`; `IamAuthenticationStateProvider` (it signs
   the tab's scope in for `AuthorizeView` and touches nothing else); `IOperationScopes` and
   `IBillingOperationScopes` themselves (they hold the token and create scopes, and never reach the
   database on their own); the three `DemoLayout.razor` files (`Corely.IAM.Demos.UsersOnly`,
   `Corely.IAM.Demos.SharedAccount`, `Corely.Billing.Demos.WithIAM`), which inject
   `IBlazorUserContextAccessor` and only read the signed-in user once to show it (checked). A
   `LayoutComponentBase` cannot take a base class of ours, and reading the user is not a stale read
   worth an operation.
8. **`AuthenticatedContent`** holds its children back until the user is signed in. It becomes an
   `OperationComponentBase` that runs one empty operation in `OnInitializedAsync`
   (`InOperationAsync(_ => Task.CompletedTask)`) and shows its children when it returns `true`. A
   `false` has already started the forced reload.
9. **DocsToData** removes every `AsNoTracking()` from `DocumentJobService.cs` (ten, at lines 210, 223,
   302, 324, 336, 477, 670, 680, 763 and 785 when written).

### Component inventory

**Corely.IAM.Web** (`Corely.IAM.Web/Components`)
- Through `AuthenticatedPageBase`, gaining `InOperationAsync` from it: `Accounts/AccountDetail`,
  `ApiKeys/ApiKeyList`, `ApiKeys/ApiKeyDetail`, `Audit/AuditLog`, `Audit/AuditEntryDetail`,
  `Groups/GroupList`, `Groups/GroupDetail`, `Invitations/AcceptInvitation`,
  `Permissions/PermissionList`, `Permissions/PermissionDetail`, `Roles/RoleList`, `Roles/RoleDetail`,
  `Users/UserList`, `Users/UserDetail`, `Profile`, `Platform/PlatformSecuritySettings`,
  `Platform/PlatformAuditSettings`.
- From `OwningComponentBase` to `OperationComponentBase`: `Shared/AccountHeader`, `Shared/AccountScope`,
  `Shared/AuditPurgeModal`, `Shared/AuditSettingsSection`, `Shared/AuthenticatedContent`,
  `Shared/LinkedAccountsSection`, `Shared/PasswordSection`, `Shared/PermissionView`,
  `Shared/ResourceAccessPanel`, `Shared/TotpSection`, `Platform/PlatformSettingsFrame`,
  `Platform/PlatformSettingsRedirect`.
- `Corely.IAM.WebApp`: `Pages/Home`, `Layout/NavBar`.
- Demos: `Corely.IAM.Demos.UsersOnly` `Pages/Home`, `Pages/Profile`; `Corely.IAM.Demos.SharedAccount`
  `Pages/Home`, `Pages/Profile`, `Pages/Team`.

**Corely.Billing**
- Bases: `Corely.Billing.Web/Components/BillingComponentBase`, `BillingPageBase`.
- Through them: `ConsumptionTable`, `GrantEditor`, `GrantList`, `GrantRequestForm`,
  `RegistrationList`, `UsageChart`, `UsageDashboard`.
- Demos: `Corely.Billing.Demos.Portal` `Pages/Home`, `Pages/PlanPage`, `Pages/TeamPage`;
  `Corely.Billing.Demos.WithIAM` `Pages/Home`.

**DocsToData** (`DocsToData.WebApp/Components`)
- From `OwningComponentBase` to IAM.Web's `OperationComponentBase`: `Layout/NavBar`,
  `Layout/FeedbackLink`, `Pages/Root`, `Pages/Home`, `Pages/Extraction/CreateTemplateModal`,
  `Pages/DocumentWorkflows/CreateDocumentWorkflowModal`, `Shared/AccountDashboard`,
  `Shared/DashboardPlaceholders`, `Shared/GettingStartedCard`.
- Through an IAM.Web or Billing.Web base: `Pages/Feedback`, `Pages/Sftp`,
  `Pages/PlatformRetentionSettings`, `Pages/PlatformNotificationSettings`,
  `Pages/Extraction/RunOnce`, `Pages/Extraction/ExtractionTemplates`,
  `Pages/Extraction/ExtractionTemplateEditor`, `Pages/DocumentWorkflows/DocumentWorkflows`,
  `Pages/DocumentWorkflows/DocumentWorkflowEditor`,
  `Pages/DocumentWorkflows/DocumentWorkflowStepEditor`, `Pages/DocumentWorkflows/WorkflowJobs`,
  `Pages/DocumentWorkflows/WorkflowJob`, `Shared/RetentionGrantWarning`, `Shared/RetentionNote`,
  `Shared/RetentionSummary`.

### Tests

1. **`IOperationScopes`** (Corely.IAM.Web.UnitTests): two calls get two different scopes; each is
   signed in with the token; the cookie is read once per instance; a failed sign in skips the work,
   forces a reload of the current URI and returns `default`; the scope is disposed after success,
   failure and a throwing `work`.
2. **`IBillingOperationScopes`** (Corely.Billing.Web.UnitTests): the default implementation signs each
   scope in through `IBillingAccountAccessor`; Billing.Web.IAM's delegates to `IOperationScopes`.
3. **The injection tests** (`OwningComponentInjectionTests` in all three repositories) are rewritten:
   no component type inherits `OwningComponentBase`, and no `[Inject]` of a scoped service outside
   the allow list (`IAccountDisplayState`, `IOperationScopes`, `IBillingOperationScopes`,
   `NavigationManager`, `IJSRuntime`, and `IBlazorUserContextAccessor` for layout components only).
   The check now covers every component type, not only `OwningComponentBase` ones, since none will
   be. The fixture test that proves a violation is reported stays.
4. **Freshness**, one per repository, each written to fail on the phase 1 code first:
   - DocsToData portal functional tier: with the `AsNoTracking()` calls removed, the job page shows a
     status changed through another scope on its next refresh tick.
   - Billing.Web: after a cancel, `GrantList` shows the request as cancelled, not pending.
   - Corely.IAM.Web functional tier: a detail page reloaded after a rename made through another scope
     shows the new name.
5. **Expiry**, Corely.IAM.Web functional tier: with `AuthTokenTtlSeconds` set short, the first
   operation after expiry forces a reload and the reload renews the session.

### Verification

1. Each repository's `RebuildAndTest.ps1`, sequenced as its CLAUDE.md says.
2. Every page loaded in a browser with the server log captured, as in phase 1: a page passes when it
   loads and the log has nothing at warning or above.
3. By hand: a running job's status moves on the job page without a reload; a cancelled grant request
   leaves the pending list at once; a page left open past a short token lifetime reloads on its next
   action and stays signed in.

### Versions and order

1. **Corely.IAM.Web, major.** `AuthenticatedPageBase` loses `ScopedServices` and
   `BlazorUserContextAccessor`, so derived pages stop compiling until converted.
2. **Corely.Billing.Web and Corely.Billing.Web.IAM, major**, against the new IAM.Web: the bases lose
   `ScopedServices` and `OnParametersSetSignedInAsync`.
3. **DocsToData** takes both, converts its components, removes the `AsNoTracking()` calls.
4. **Corely.DataAccess**: nothing. Its `Docs/repositories.md` already states the rule ("Scopes and
   Fresh Reads"), with no release.

Each is its own branch and pull request in its own repository; tags are pushed only with the owner's
say so. This plan stays in `Plans/InProgress/` until step 3 ships, and per CLAUDE.md that holds
Corely.IAM releases until then.

### Docs

`Corely.IAM.Web/Docs/base-classes.md`, `components/index.md` and `setup.md`, and Billing.Web's
`Docs/setup.md`: a component that calls services runs each action through `InOperationAsync` (or
`IOperationScopes` where it has no base), and why, in a short paragraph replacing phase 1's two rules.

### Done when

No component in Corely.IAM.Web, Corely.Billing.Web, their demos or DocsToData inherits
`OwningComponentBase` or reads `ScopedServices`; the rewritten injection tests and the freshness and
expiry tests pass; `DocumentJobService` has no `AsNoTracking()`; every page loads clean in a browser;
and the new IAM.Web and Billing packages are released and taken by DocsToData.
