# Corely.IAM Web — IAM Management Portal

## Context

The Corely.IAM library now has full CRUD service coverage (Registration, Retrieval, Modification, Deregistration, Authentication). A web UI will provide management for all IAM operations — accounts, users, groups, roles, and permissions. Based on patterns proven in the `SampleBlazorApp` (DocsToData.AdminPortalWebApp), adapted for the Corely.IAM repo.

**Key decisions:**
- **Two-project architecture** for reusability:
    - **`Corely.IAM.Web`** — Razor Class Library (RCL) with all reusable pages, middleware, and services. Any ASP.NET Core app can reference this and call `AddIAMWeb()` + `UseIAMWebAuthentication()`.
    - **`Corely.IAM.WebApp`** — Standalone Blazor Server app. Thin shell that references `Corely.IAM.Web`. Serves as both a standalone management app and a reference for integration.
- Multi-provider DB support from the start (config-based: `mssql`, `mysql`, `mariadb`)
- Includes user **registration page** (self-service sign-up)
- Fixes sample app's SignOut gap: calls `IAuthenticationService.SignOutAsync` to invalidate JWT server-side
- Auto-selects account when user has exactly one

**Rendering model — two tiers of compatibility:**
- **Tier 1: Razor Pages (universal)** — All pages (auth + entity management) are Razor Pages. Works in any ASP.NET Core app (MVC, Razor Pages, Blazor). Full page reloads on actions. This is the base layer and provides complete IAM management functionality.
- **Tier 2: Blazor Interactive Server (enhancement)** — Optional Blazor component versions of entity management pages. SPA-like UX via SignalR — no page reloads, real-time permission gating, richer interactions. Only usable in Blazor Server apps.
- **Everything is server-side** — no WebAssembly. The browser never executes .NET code.
- **Route separation** — Razor Pages use `/iam/*` prefix (e.g., `/iam/accounts`). Blazor components use clean routes (e.g., `/accounts`). Non-Blazor apps use the `/iam/*` routes. Blazor apps can use either — the standalone WebApp defaults to the Blazor component routes.

---

## Production TODOs

Items intentionally deferred but required before production deployment:

- [ ] **Key vault integration** — `SecurityConfigurationProvider` reads `Security:SystemKey` from config. Production should load from Azure Key Vault / AWS Secrets Manager / similar
- [ ] **Token refresh / sliding expiration** — Currently JWT expires silently after TTL (default 1 hour). Add refresh token or sliding window to avoid surprise logouts
- [ ] **User invite mechanism** — User2 accepts invite from User1 to join AccountX. Out of scope for now.
- [ ] **HTTPS enforcement** — Cookie `SameSite: Strict` only applies when `Request.IsHttps`. Production must always be HTTPS.
- [ ] **CSP nonce** — Current CSP uses `unsafe-inline` / `unsafe-eval` (required by Blazor Server). Investigate nonce-based CSP if stricter policy needed.
- [ ] **Rate limiting** — No rate limiting on auth endpoints. Add middleware or reverse proxy rate limiting before production.
- [ ] **Audit logging** — IAM operations are logged via telemetry decorators, but no dedicated audit trail UI exists yet.
- [ ] **Password reset flow** — No forgot-password mechanism. Requires email integration.
- [ ] **Service-level tests for Retrieval/Modification** — Processor-level tests exist (40 List/Get + 17 Update = 57 total), but `RetrievalService` and `ModificationService` have no dedicated test files. Tracked separately from this web app plan.

---

## Architecture: Two-Project Split

### `Corely.IAM.Web` (Razor Class Library)

Everything reusable. A consuming app references this library and gets IAM auth + management UI for free.

```
Corely.IAM.Web/
├── Extensions/
│   ├── IamWebServiceExtensions.cs       ← AddIAMWeb() + AddIAMWebBlazor()
│   └── IamWebAppExtensions.cs           ← UseIAMWebAuthentication()
├── Security/
│   ├── AuthenticationConstants.cs        ← Cookie names, paths, helper methods
│   └── SecurityConfigurationProvider.cs  ← ISecurityConfigurationProvider impl
├── Middleware/
│   ├── AuthenticationTokenMiddleware.cs
│   ├── CorrelationIdMiddleware.cs
│   └── SecurityHeadersMiddleware.cs
├── Services/
│   ├── BlazorUserContextAccessor.cs      ← SignalR auth bridge (Blazor-only)
│   ├── IamAuthenticationStateProvider.cs  ← Blazor auth bridge (Blazor-only)
│   └── UserContextClaimsBuilder.cs       ← Claims builder (universal)
├── Pages/                                ← Razor Pages (universal — works everywhere)
│   ├── Authentication/
│   │   ├── SignIn.cshtml + .cshtml.cs
│   │   ├── Register.cshtml + .cshtml.cs
│   │   ├── SignOut.cshtml + .cshtml.cs
│   │   ├── SelectAccount.cshtml + .cshtml.cs
│   │   └── SwitchAccount.cshtml + .cshtml.cs
│   ├── Management/                       ← Entity CRUD (Razor Pages — universal)
│   │   ├── Dashboard.cshtml + .cshtml.cs
│   │   ├── Accounts/
│   │   │   ├── Index.cshtml + .cshtml.cs     ← List
│   │   │   └── Detail.cshtml + .cshtml.cs    ← Detail + edit + relationships
│   │   ├── Users/      (same pattern)
│   │   ├── Groups/     (same pattern)
│   │   ├── Roles/      (same pattern)
│   │   └── Permissions/ (same pattern)
│   └── Shared/
│       ├── _AuthLayout.cshtml            ← Minimal layout for auth pages
│       ├── _ManagementLayout.cshtml      ← Full layout for entity pages (nav, footer)
│       ├── _Pagination.cshtml            ← Pagination partial
│       └── _Alert.cshtml                 ← Alert partial
├── Components/                           ← Blazor components (Blazor-only enhancement)
│   ├── AuthenticatedPageBase.cs
│   ├── Layout/
│   │   ├── MainLayout.razor
│   │   └── NavBar.razor
│   ├── Pages/
│   │   ├── Home.razor                    ← Dashboard (Blazor version)
│   │   ├── Accounts/ (list + detail)
│   │   ├── Users/     (list + detail)
│   │   ├── Groups/    (list + detail)
│   │   ├── Roles/     (list + detail)
│   │   └── Permissions/ (list + detail)
│   └── Shared/
│       ├── PermissionView.razor
│       ├── AuthenticatedContent.razor
│       ├── RedirectToLogin.razor
│       ├── LoggingErrorBoundary.razor
│       ├── Alert.razor + AlertType.cs
│       ├── ConfirmModal.razor + ConfirmModalType.cs
│       ├── LoadingSpinner.razor
│       └── Pagination.razor
├── AppRoutes.cs
├── _Imports.razor
└── wwwroot/
    ├── css/iam-web.css
    └── js/modal-keyboard.js
```

**Extension method usage (consuming apps)**:
```csharp
// Plain ASP.NET Core app (MVC / Razor Pages, no Blazor):
builder.Services.AddIAMWeb();                     // Middleware services, claims builder, cookie auth
builder.Services.AddIAMServicesWithEF(config, securityConfig, efFactory);
app.UseIAMWebAuthentication();                    // Middleware pipeline
app.MapRazorPages();                              // Auth + management Razor Pages from RCL

// Blazor Server app — adds interactive component support:
builder.Services.AddIAMWeb();                     // Base (same as above)
builder.Services.AddIAMWebBlazor();               // + BlazorUserContextAccessor, AuthenticationStateProvider
builder.Services.AddIAMServicesWithEF(config, securityConfig, efFactory);
app.UseIAMWebAuthentication();
app.MapRazorPages();                              // Auth Razor Pages (still needed for cookie flows)
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();  // Blazor entity pages
```

### `Corely.IAM.WebApp` (Standalone App)

Thin shell — reference implementation and standalone management tool. Uses Blazor for the enhanced entity management UX.

```
Corely.IAM.WebApp/
├── Corely.IAM.WebApp.csproj    ← References Corely.IAM.Web + Corely.IAM
├── Program.cs                  ← AddIAMWeb(), AddIAMWebBlazor(), AddIAMServicesWithEF()
├── Components/
│   ├── App.razor               ← Root HTML document (head, body, scripts, reconnect UI)
│   └── Routes.razor            ← AuthorizeRouteView
├── DataAccess/
│   ├── MsSqlEFConfiguration.cs
│   ├── MySqlEFConfiguration.cs
│   └── DatabaseProvider.cs (enum)
├── Properties/launchSettings.json
├── appsettings.json
├── appsettings.template.json
├── libman.json                 ← Bootstrap 5.3.3 + Bootstrap Icons
└── wwwroot/                    ← App-specific static assets
```

---

## Phase 1: Project Skeleton & Infrastructure

### 1a. Create `Corely.IAM.Web` (RCL)

**`Corely.IAM.Web.csproj`**
- SDK: `Microsoft.NET.Sdk.Razor`
- Target: `net10.0`
- References: `Corely.IAM.csproj` (project ref)
- Packages:
    - `Microsoft.AspNetCore.Components.Web`
    - `CSharpier.MsBuild`
- Add to `Corely.IAM.sln`

**Extension methods:**

`IamWebServiceExtensions.cs` — `AddIAMWeb()` (universal — works in any ASP.NET Core app):
```csharp
services.AddRazorPages();
services.AddHttpContextAccessor();
services.AddAuthentication(Cookie).AddCookie(loginPath: /signin, logoutPath: /signout);
services.AddAuthorization();
```

`IamWebServiceExtensions.cs` — `AddIAMWebBlazor()` (Blazor-specific — only call in Blazor apps):
```csharp
services.AddScoped<IBlazorUserContextAccessor, BlazorUserContextAccessor>();
services.AddScoped<AuthenticationStateProvider, IamAuthenticationStateProvider>();
services.AddCascadingAuthenticationState();
```

`IamWebAppExtensions.cs` — `UseIAMWebAuthentication()` (universal):
```csharp
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<AuthenticationTokenMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
```

**Middleware, Services, Security** — same as described in reference analysis doc (`Plans/blazor-admin-webapp.md`), now in `Corely.IAM.Web/`.

### 1b. Create `Corely.IAM.WebApp` (Standalone)

**`Corely.IAM.WebApp.csproj`**
- SDK: `Microsoft.NET.Sdk.Web`
- Target: `net10.0`
- References: `Corely.IAM.Web.csproj`, `Corely.IAM.csproj`
- Packages:
    - `Microsoft.EntityFrameworkCore.SqlServer`
    - `Pomelo.EntityFrameworkCore.MySql`
    - `Serilog.AspNetCore`
    - `Serilog.Sinks.Console`
    - `Serilog.Sinks.Seq`
    - `CSharpier.MsBuild`
- Add to `Corely.IAM.sln`

**`Program.cs`:**
```csharp
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddIAMWeb();        // Universal: middleware, auth pages, management pages, cookie auth
builder.Services.AddIAMWebBlazor();  // Blazor: BlazorUserContextAccessor, AuthStateProvider
builder.Services.AddIAMServicesWithEF(config, securityConfig, efFactory);

app.UseIAMWebAuthentication();  // From Corely.IAM.Web
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorPages();            // Auth + management Razor Pages
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();  // Blazor entity pages
```

**Multi-provider EF config** — follow DevTools pattern:
- Read `"Database:Provider"` from config (`mssql` | `mysql` | `mariadb`)
- Switch on enum → return appropriate `IEFConfiguration`
- `MsSqlEFConfiguration` / `MySqlEFConfiguration` in `DataAccess/`
- Both inherit from `EFMsSqlConfigurationBase` / `EFMySqlConfigurationBase`
- Include EF SQL logging via `EFEventDataLogger` + `#if DEBUG` sensitive data

**Files to create (Phase 1):**

*Corely.IAM.Web:*
- `Corely.IAM.Web/Corely.IAM.Web.csproj`
- `Corely.IAM.Web/Extensions/IamWebServiceExtensions.cs`
- `Corely.IAM.Web/Extensions/IamWebAppExtensions.cs`
- `Corely.IAM.Web/Security/AuthenticationConstants.cs`
- `Corely.IAM.Web/Security/SecurityConfigurationProvider.cs`
- `Corely.IAM.Web/Middleware/AuthenticationTokenMiddleware.cs`
- `Corely.IAM.Web/Middleware/CorrelationIdMiddleware.cs`
- `Corely.IAM.Web/Middleware/SecurityHeadersMiddleware.cs`
- `Corely.IAM.Web/Services/BlazorUserContextAccessor.cs`
- `Corely.IAM.Web/Services/IamAuthenticationStateProvider.cs`
- `Corely.IAM.Web/Services/UserContextClaimsBuilder.cs`

*Corely.IAM.WebApp:*
- `Corely.IAM.WebApp/Corely.IAM.WebApp.csproj`
- `Corely.IAM.WebApp/Program.cs`
- `Corely.IAM.WebApp/Properties/launchSettings.json`
- `Corely.IAM.WebApp/appsettings.json`
- `Corely.IAM.WebApp/appsettings.template.json`
- `Corely.IAM.WebApp/DataAccess/MsSqlEFConfiguration.cs`
- `Corely.IAM.WebApp/DataAccess/MySqlEFConfiguration.cs`
- `Corely.IAM.WebApp/DataAccess/DatabaseProvider.cs`

---

## Phase 2: Auth Pages + Layouts + Static Assets

### Auth Pages (Razor Pages in `Corely.IAM.Web`)

All auth pages use Razor Pages because cookie manipulation requires `HttpResponse` (not available over SignalR).

**SignIn** (`Pages/Authentication/SignIn.cshtml` + `.cshtml.cs`)
- Form: username + password
- Calls `IAuthenticationService.SignInAsync`
- On success: set `authentication_token` cookie + `auth_token_id` cookie (for server-side sign-out)
- Device ID cookie management (1-year persistent)
- Post-signin redirect:
    - No accounts → `/iam` (dashboard)
    - Exactly 1 account → auto-switch via `SwitchAccountAsync`, then `/iam`
    - Multiple accounts → `/select-account`
- Error mapping: generic "Invalid username or password" for auth failures

**Register** (`Pages/Authentication/Register.cshtml` + `.cshtml.cs`)
- Form: username + email + password + confirm password
- Client-side validation (DataAnnotation + password match)
- Calls `IRegistrationService.RegisterUserAsync`
- On success: auto-sign-in → redirect to `/iam`
- Error mapping: username/email exists, validation failures

**SelectAccount** (`Pages/Authentication/SelectAccount.cshtml` + `.cshtml.cs`)
- Lists `AvailableAccounts` as buttons
- POST calls `SwitchAccountAsync` → cookie update → redirect to `/iam`

**SwitchAccount** (`Pages/Authentication/SwitchAccount.cshtml.cs`)
- POST-only handler for NavBar account dropdown
- `SwitchAccountAsync` → cookie update → redirect to `returnUrl`

**SignOut** (`Pages/Authentication/SignOut.cshtml.cs`)
- POST-only
- **FIX from sample**: Calls `IAuthenticationService.SignOutAsync(new SignOutRequest(tokenId))` using stored `auth_token_id` cookie
- Deletes all auth cookies → redirect to `/signin`

### Layouts

**`_AuthLayout.cshtml`** — Minimal layout for auth pages (brand name, sign-in/register links, no nav user context)

**`_ManagementLayout.cshtml`** — Full layout for entity management pages:
- Top nav bar: brand, entity navigation links (Accounts, Users, Groups, Roles, Permissions), account switcher dropdown, username, sign out form
- Content area
- Footer
- Permission checks in the nav for visibility of entity links

### Static Assets
- `Corely.IAM.Web/wwwroot/css/iam-web.css` — Component and page styles
- `Corely.IAM.Web/wwwroot/js/modal-keyboard.js` — Modal Escape key handler + confirmation dialog JS
- `Corely.IAM.WebApp/libman.json` — Bootstrap 5.3.3 + Bootstrap Icons 1.11.3
- `Corely.IAM.WebApp/wwwroot/app.css` — App-level styles

### AppRoutes

`AppRoutes.cs` — Centralized route constants for both Razor Pages and Blazor:
```csharp
public static class AppRoutes
{
    // Auth (Razor Pages)
    public const string SignIn = "/signin";
    public const string Register = "/register";
    public const string SignOut = "/signout";
    public const string SelectAccount = "/select-account";

    // Management (Razor Pages — universal)
    public const string Dashboard = "/iam";
    public const string Accounts = "/iam/accounts";
    public const string Users = "/iam/users";
    public const string Groups = "/iam/groups";
    public const string Roles = "/iam/roles";
    public const string Permissions = "/iam/permissions";

    // Blazor routes (enhancement — used by Blazor apps)
    public static class Blazor
    {
        public const string Dashboard = "/";
        public const string Accounts = "/accounts";
        public const string Users = "/users";
        public const string Groups = "/groups";
        public const string Roles = "/roles";
        public const string Permissions = "/permissions";
    }
}
```

**Files to create (Phase 2):**

*Corely.IAM.Web:*
- `Pages/Authentication/SignIn.cshtml` + `.cshtml.cs`
- `Pages/Authentication/Register.cshtml` + `.cshtml.cs`
- `Pages/Authentication/SignOut.cshtml` + `.cshtml.cs`
- `Pages/Authentication/SelectAccount.cshtml` + `.cshtml.cs`
- `Pages/Authentication/SwitchAccount.cshtml` + `.cshtml.cs`
- `Pages/Shared/_AuthLayout.cshtml`
- `Pages/Shared/_ManagementLayout.cshtml`
- `Pages/Shared/_Pagination.cshtml`
- `Pages/Shared/_Alert.cshtml`
- `AppRoutes.cs`
- `wwwroot/css/iam-web.css`
- `wwwroot/js/modal-keyboard.js`

*Corely.IAM.WebApp:*
- `Components/App.razor`
- `Components/Routes.razor`
- `Components/_Imports.razor`
- `libman.json`
- `wwwroot/app.css`

---

## Phase 3: Entity Management — Razor Pages (Universal)

All in `Corely.IAM.Web/Pages/Management/`. These pages work in **any ASP.NET Core app** — no Blazor required.

### Dashboard (`/iam`)
- `Dashboard.cshtml` + `.cshtml.cs`
- Authenticated: cards linking to entity management pages (Accounts, Users, Groups, Roles, Permissions)
- Unauthenticated: welcome + sign in CTA + register link
- Uses `_ManagementLayout`

### Entity List Pages

Each entity gets a list page following the same Razor Pages pattern:
1. Table with columns for key properties
2. Pagination via `_Pagination` partial (skip/take query params)
3. Create form (inline or modal) — permission-gated server-side
4. Row actions: View (link to detail page), Delete (POST form with confirmation) — permission-gated
5. Authorization check in `OnGetAsync` / `OnPostAsync` via `IAuthorizationProvider`

| Entity | Route | List Call | Create Call | Delete Call | Columns |
|--------|-------|-----------|-------------|-------------|---------|
| **Accounts** | `/iam/accounts` | `ListAccountsAsync` | `RegisterAccountAsync` | `DeregisterAccountAsync` | Name, Created |
| **Users** | `/iam/users` | `ListUsersAsync` | N/A (from detail) | `DeregisterUserFromAccountAsync` | Username, Email |
| **Groups** | `/iam/groups` | `ListGroupsAsync` | `RegisterGroupAsync` | `DeregisterGroupAsync` | Name, Description |
| **Roles** | `/iam/roles` | `ListRolesAsync` | `RegisterRoleAsync` | `DeregisterRoleAsync` | Name, Description, SystemDefined |
| **Permissions** | `/iam/permissions` | `ListPermissionsAsync` | `RegisterPermissionAsync` | `DeregisterPermissionAsync` | ResourceType, ResourceId, CRUDX flags |

Create is handled via POST form handlers on the same page:
- Accounts: name input
- Groups: name + description
- Roles: name + description
- Permissions: resourceType + resourceId + CRUDX checkboxes

### Entity Detail Pages

Each entity detail page handles property editing and relationship management:

**Account Detail** (`/iam/accounts/{id}`)
- Properties section: Account name (editable form) → `ModifyAccountAsync`
- Users section: Table of users in account
    - Add user (username search form) → `RegisterUserWithAccountAsync`
    - Remove user (POST form) → `DeregisterUserFromAccountAsync`

**User Detail** (`/iam/users/{id}`)
- Properties section: Username + Email (editable form) → `ModifyUserAsync`
- Roles section: Assigned roles table
    - Assign (role picker) → `RegisterRolesWithUserAsync`
    - Remove (POST form) → `DeregisterRolesFromUserAsync`

**Group Detail** (`/iam/groups/{id}`)
- Properties section: Name + Description (editable form) → `ModifyGroupAsync`
- Users section: Add/remove via `RegisterUsersWithGroupAsync` / `DeregisterUsersFromGroupAsync`
- Roles section: Add/remove via `RegisterRolesWithGroupAsync` / `DeregisterRolesFromGroupAsync`

**Role Detail** (`/iam/roles/{id}`)
- Properties section: Name + Description (editable form) → `ModifyRoleAsync` (blocked for system-defined)
- Permissions section: Add/remove via `RegisterPermissionsWithRoleAsync` / `DeregisterPermissionsFromRoleAsync`

**Permission Detail** (`/iam/permissions/{id}`)
- View only (no update — per design decision)
- Shows resource type, resource ID, CRUDX flags
- Delete button (POST form) → `DeregisterPermissionAsync`

**Files to create (Phase 3):**

- `Pages/Management/Dashboard.cshtml` + `.cshtml.cs`
- `Pages/Management/Accounts/Index.cshtml` + `.cshtml.cs`
- `Pages/Management/Accounts/Detail.cshtml` + `.cshtml.cs`
- `Pages/Management/Users/Index.cshtml` + `.cshtml.cs`
- `Pages/Management/Users/Detail.cshtml` + `.cshtml.cs`
- `Pages/Management/Groups/Index.cshtml` + `.cshtml.cs`
- `Pages/Management/Groups/Detail.cshtml` + `.cshtml.cs`
- `Pages/Management/Roles/Index.cshtml` + `.cshtml.cs`
- `Pages/Management/Roles/Detail.cshtml` + `.cshtml.cs`
- `Pages/Management/Permissions/Index.cshtml` + `.cshtml.cs`
- `Pages/Management/Permissions/Detail.cshtml` + `.cshtml.cs`

---

## Phase 4: Blazor Components (Enhancement)

Optional Blazor component versions of entity management pages for a richer SPA experience. Only usable in Blazor Server apps that call `AddIAMWebBlazor()`.

### Blazor Shell (in `Corely.IAM.Web`)
- `Components/Layout/MainLayout.razor` — NavBar + `LoggingErrorBoundary` wrapping `@Body` + Footer
- `Components/Layout/NavBar.razor` — Brand, entity nav links, account switcher dropdown, username, sign out button
- `Components/AuthenticatedPageBase.cs` — Base class with `prerender: false`, `OnInitializedAuthenticatedAsync()`
- `Components/_Imports.razor` — Global usings for the RCL

### Shared Blazor Components
All in `Corely.IAM.Web/Components/Shared/`:
- **`PermissionView.razor`** — Show/hide UI based on IAM permissions. Caches auth check, re-evaluates on parameter change.
- **`AuthenticatedContent.razor`** — Gate rendering until user context is initialized
- **`RedirectToLogin.razor`** — Force navigate to `/signin`
- **`LoggingErrorBoundary.razor`** — Error boundary with Serilog logging, "Try Again" + "Go Home" recovery
- **`Alert.razor`** + `AlertType.cs` — Bootstrap alert (Danger/Warning/Info/Success) with optional dismiss
- **`ConfirmModal.razor`** + `ConfirmModalType.cs` — Confirmation dialog with Escape key, backdrop click, loading spinner
- **`LoadingSpinner.razor`** — Centered bootstrap spinner
- **`Pagination.razor`** — Skip/Take pagination controls with page size selector

### Blazor Entity Pages
All in `Corely.IAM.Web/Components/Pages/`. Same functionality as the Razor Pages but with Blazor interactivity — no page reloads, real-time permission gating via `PermissionView`, inline editing, confirmation modals:

- **`Home.razor`** (`/`) — Dashboard with entity cards
- **`Accounts/AccountList.razor`** (`/accounts`) — Table + pagination + create modal + delete
- **`Accounts/AccountDetail.razor`** (`/accounts/{id}`) — Edit properties + manage users
- **`Users/UserList.razor`** (`/users`) — Same pattern
- **`Users/UserDetail.razor`** (`/users/{id}`) — Edit properties + manage roles
- **`Groups/GroupList.razor`** (`/groups`) + **`GroupDetail.razor`** (`/groups/{id}`)
- **`Roles/RoleList.razor`** (`/roles`) + **`RoleDetail.razor`** (`/roles/{id}`)
- **`Permissions/PermissionList.razor`** (`/permissions`) + **`PermissionDetail.razor`** (`/permissions/{id}`)

### Key differences from Razor Pages versions:
- No full page reloads — state managed in component
- `PermissionView` for element-level show/hide instead of server-side conditionals
- `ConfirmModal` for delete confirmations instead of JS `confirm()`
- `LoadingSpinner` during async operations
- `Pagination` component instead of query-string-based paging

**Files to create (Phase 4):**

- `Components/_Imports.razor`
- `Components/AuthenticatedPageBase.cs`
- `Components/Layout/MainLayout.razor`
- `Components/Layout/NavBar.razor`
- `Components/Shared/PermissionView.razor`
- `Components/Shared/AuthenticatedContent.razor`
- `Components/Shared/RedirectToLogin.razor`
- `Components/Shared/LoggingErrorBoundary.razor`
- `Components/Shared/Alert.razor` + `AlertType.cs`
- `Components/Shared/ConfirmModal.razor` + `ConfirmModalType.cs`
- `Components/Shared/LoadingSpinner.razor`
- `Components/Shared/Pagination.razor`
- `Components/Pages/Home.razor`
- `Components/Pages/Accounts/AccountList.razor`
- `Components/Pages/Accounts/AccountDetail.razor`
- `Components/Pages/Users/UserList.razor`
- `Components/Pages/Users/UserDetail.razor`
- `Components/Pages/Groups/GroupList.razor`
- `Components/Pages/Groups/GroupDetail.razor`
- `Components/Pages/Roles/RoleList.razor`
- `Components/Pages/Roles/RoleDetail.razor`
- `Components/Pages/Permissions/PermissionList.razor`
- `Components/Pages/Permissions/PermissionDetail.razor`

---

## Phase 5: Unit Tests (`Corely.IAM.Web.UnitTests`)

The `Corely.IAM.Web` RCL is intended for reuse across multiple apps — it needs its own test project. The `Corely.IAM.WebApp` standalone app does not need tests (thin shell, no business logic).

### 5a. Project Setup

**`Corely.IAM.Web.UnitTests.csproj`**
- SDK: `Microsoft.NET.Sdk`
- Target: `net10.0`
- References: `Corely.IAM.Web.csproj`
- Packages: `xunit`, `Moq`, `FluentAssertions`, `Microsoft.NET.Test.Sdk`, `Microsoft.AspNetCore.Mvc.Testing` (for Razor Page model testing), `bunit` (for Blazor component testing)
- Add to `Corely.IAM.sln`

### 5b. Service Tests

**`UserContextClaimsBuilderTests`** (pure logic, no mocks needed)
- Null UserContext → anonymous principal (`IsAuthenticated == false`)
- Null User → anonymous principal
- Valid user → principal with NameIdentifier, Name claims
- User with email → includes Email claim
- User with CurrentAccount → includes AccountId + AccountName claims
- User without CurrentAccount → no account claims

**`BlazorUserContextAccessorTests`** (mock `IUserContextProvider` + `IHttpContextAccessor`)
- Provider already has context → returns it immediately (fast path)
- No provider context, valid cookie → calls `SetUserContextAsync`, returns new context
- No provider context, no HttpContext → returns null
- No provider context, no cookie → returns null
- `SetUserContextAsync` fails → returns null
- Second call after failed init → returns null (doesn't retry)
- Thread safety — concurrent calls only initialize once

**`IamAuthenticationStateProviderTests`** (mock `IBlazorUserContextAccessor`)
- Authenticated user → returns authenticated AuthenticationState with correct claims
- No user context → returns unauthenticated state

### 5c. Middleware Tests

**`AuthenticationTokenMiddlewareTests`** (mock `IUserContextProvider`, use `DefaultHttpContext`)
- No cookie → calls next, no claims set
- Valid cookie → sets user context, builds claims principal on `HttpContext.User`
- Invalid/expired cookie → deletes cookie, continues unauthenticated
- Exception during token validation → deletes cookie, continues

**`CorrelationIdMiddlewareTests`**
- No incoming header → generates new correlation ID, adds to response
- Existing `X-Correlation-ID` header → uses it, adds to response

**`SecurityHeadersMiddlewareTests`**
- Verify all expected headers present: `X-Frame-Options`, `X-Content-Type-Options`, `X-XSS-Protection`, `Referrer-Policy`, `Permissions-Policy`, `Cache-Control`, `Content-Security-Policy`

### 5d. Auth Page Model Tests

**`SignInModelTests`** (mock `IAuthenticationService`)
- `OnPostAsync` with valid credentials → sets cookie, redirects
- Invalid credentials → returns Page with error message
- Post-signin: 0 accounts → redirect to `/iam`
- Post-signin: 1 account → auto-switch + redirect to `/iam`
- Post-signin: multiple accounts → redirect to `/select-account`

**`RegisterModelTests`** (mock `IRegistrationService` + `IAuthenticationService`)
- Valid registration → creates user, auto-signs-in, redirects to `/iam`
- Duplicate username → returns Page with error
- Validation failure → returns Page with error

**`SignOutModelTests`** (mock `IAuthenticationService`)
- POST → calls `SignOutAsync` with token ID, deletes cookies, redirects to `/signin`
- GET → redirects to `/iam`

**`SelectAccountModelTests`** (mock `IAuthenticationService` + `IUserContextProvider`)
- No user context → redirect to `/signin`
- Already has CurrentAccount → redirect to `/iam`
- POST with valid accountId → switches, updates cookie, redirects to `/iam`

**`SwitchAccountModelTests`** (mock `IAuthenticationService`)
- Valid switch → updates cookie, redirects to returnUrl
- Invalid returnUrl (non-local) → redirects to `/iam`

### 5e. Management Page Model Tests

**`AccountListModelTests`** (mock `IRetrievalService` + `IAuthorizationProvider`)
- `OnGetAsync` loads paginated account list
- `OnPostCreateAsync` with valid input → creates account, redirects
- `OnPostDeleteAsync` → deregisters account, redirects
- Unauthorized → returns Forbid

**`AccountDetailModelTests`** (mock `IRetrievalService` + `IModificationService` + `IRegistrationService`)
- `OnGetAsync` loads account with users
- `OnPostEditAsync` → modifies account name
- `OnPostAddUserAsync` → registers user with account
- `OnPostRemoveUserAsync` → deregisters user from account

Similar patterns for User, Group, Role, Permission page models (vary by relationships managed).

### Files to create (Phase 5)

- `Corely.IAM.Web.UnitTests/Corely.IAM.Web.UnitTests.csproj`
- `Corely.IAM.Web.UnitTests/Services/UserContextClaimsBuilderTests.cs`
- `Corely.IAM.Web.UnitTests/Services/BlazorUserContextAccessorTests.cs`
- `Corely.IAM.Web.UnitTests/Services/IamAuthenticationStateProviderTests.cs`
- `Corely.IAM.Web.UnitTests/Middleware/AuthenticationTokenMiddlewareTests.cs`
- `Corely.IAM.Web.UnitTests/Middleware/CorrelationIdMiddlewareTests.cs`
- `Corely.IAM.Web.UnitTests/Middleware/SecurityHeadersMiddlewareTests.cs`
- `Corely.IAM.Web.UnitTests/Pages/Auth/SignInModelTests.cs`
- `Corely.IAM.Web.UnitTests/Pages/Auth/RegisterModelTests.cs`
- `Corely.IAM.Web.UnitTests/Pages/Auth/SignOutModelTests.cs`
- `Corely.IAM.Web.UnitTests/Pages/Auth/SelectAccountModelTests.cs`
- `Corely.IAM.Web.UnitTests/Pages/Auth/SwitchAccountModelTests.cs`
- `Corely.IAM.Web.UnitTests/Pages/Management/AccountListModelTests.cs`
- `Corely.IAM.Web.UnitTests/Pages/Management/AccountDetailModelTests.cs`
- `Corely.IAM.Web.UnitTests/Pages/Management/UserListModelTests.cs`
- `Corely.IAM.Web.UnitTests/Pages/Management/UserDetailModelTests.cs`
- `Corely.IAM.Web.UnitTests/Pages/Management/GroupListModelTests.cs`
- `Corely.IAM.Web.UnitTests/Pages/Management/GroupDetailModelTests.cs`
- `Corely.IAM.Web.UnitTests/Pages/Management/RoleListModelTests.cs`
- `Corely.IAM.Web.UnitTests/Pages/Management/RoleDetailModelTests.cs`
- `Corely.IAM.Web.UnitTests/Pages/Management/PermissionListModelTests.cs`
- `Corely.IAM.Web.UnitTests/Pages/Management/PermissionDetailModelTests.cs`

---

## Verification

After each phase:
1. `.\RebuildAndTest.ps1` — ensures solution builds and all existing + new tests pass
2. `dotnet run --project Corely.IAM.WebApp` — verify the app starts
3. Manual browser testing:
    - Phase 2: Sign in/out/register flow, account selection, layouts render
    - Phase 3: Entity list/detail pages load via `/iam/*` routes, CRUD works, pagination works
    - Phase 4: Blazor entity pages load via clean routes, SPA interactivity works
    - Phase 5: All unit tests green

---

## Files Summary

| Phase | Corely.IAM.Web | Corely.IAM.WebApp | Corely.IAM.Web.UnitTests | Total |
|-------|---------------|-------------------|--------------------------|-------|
| 1 | 11 (extensions, middleware, services, security) | 8 (csproj, Program, config, DataAccess) | 0 | ~19 |
| 2 | 12 (auth pages, layouts, partials, routes, assets) | 5 (App.razor, Routes, css, libman) | 0 | ~17 |
| 3 | 11 (dashboard + 5 entity × 2 pages each) | 0 | 0 | ~11 |
| 4 | 25 (Blazor shell + shared components + 11 entity pages) | 0 | 0 | ~25 |
| 5 | 0 | 0 | 22 (csproj + 21 test classes) | ~22 |
| **Total** | **~59** | **~13** | **~22** | **~94** |
