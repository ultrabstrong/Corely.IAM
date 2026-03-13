# Documentation Plan: Corely.IAM.Web

## Status: Planned

## Overview

Documentation for the Blazor Server UI library following the [Corely Documentation Style Guide](documentation-style-guide.md). Lives in `Corely.IAM.Web/Docs/`.

---

## Document Map

```
Docs/
├── index.md                         # Landing page + quick start
├── setup.md                         # Integration guide for host apps
├── authentication-flow.md           # Sign in, register, cookies, JWT middleware
├── authorization-ui.md              # PermissionView component, auth gating
├── pages/
│   ├── index.md                     # Page inventory + routing overview
│   ├── dashboard.md                 # Dashboard page
│   ├── accounts.md                  # Account detail, switching, creation
│   ├── users.md                     # User list, detail, group/role assignment
│   ├── groups.md                    # Group list, detail
│   ├── roles.md                     # Role list, detail, permission assignment
│   ├── permissions.md               # Permission list, detail, resource type dropdown
│   ├── invitations.md               # Invitation list, create, accept
│   └── profile.md                   # User profile, key providers, leave/delete account
├── components/
│   ├── index.md                     # Shared component inventory
│   ├── permission-view.md           # PermissionView (authorization gate)
│   ├── entity-picker-modal.md       # EntityPickerModal (multi-select)
│   ├── form-modal.md                # FormModal
│   ├── confirm-modal.md             # ConfirmModal
│   ├── effective-permissions.md     # EffectivePermissionsPanel
│   └── encryption-signing.md        # EncryptionSigningPanel
├── base-classes.md                   # AuthenticatedPageBase, EntityPageBase, EntityListPageBase, EntityDetailPageBase
├── middleware.md                     # CorrelationId, SecurityHeaders, AuthenticationToken
├── services.md                      # BlazorUserContextAccessor, AuthCookieManager, AuthenticationStateProvider, AccountDisplayState
├── styling.md                       # CSS classes, theming, customization
└── security.md                      # Cookie security, CSP headers, security headers
```

**Total: 23 documents**

---

## Document Specifications

### `index.md` — Landing Page

**Sections:**
- `# Corely.IAM.Web Documentation`
- Overview paragraph: pre-built Blazor Server UI for Corely.IAM, provides authentication pages, multi-tenant account management, RBAC visualization, permission CRUD — all with authorization gating
- Capabilities bullet list:
    - **Complete auth flow** — sign in, register, sign out, account switching (Razor Pages)
    - **Multi-tenant management** — account selection, user/group/role/permission CRUD (Blazor Server)
    - **Authorization gates** — `PermissionView` component hides UI based on CRUDX permissions
    - **Effective permissions** — visualize permission grants through roles and groups
    - **Security headers** — CSP, HSTS, X-Frame-Options out of the box
    - **Customizable styling** — Bootstrap-first CSS with scoped utility classes
- `## Topics` — links to all docs
- `## Quick Start` — 5-line setup: `AddIAMWeb()`, `AddIAMWebBlazor()`, `UseIAMWebAuthentication()`, `MapRazorPages()`, `MapBlazorHub()`

---

### `setup.md` — Host App Integration

**Numbered steps:**

1. **Install packages** — reference `Corely.IAM.Web` project/package
2. **Register services** — code example:
    ```csharp
    services.AddIAMWeb();        // Cookie auth, Razor Pages, auth services
    services.AddIAMWebBlazor();  // Blazor auth state, user context accessor
    ```
3. **Configure middleware** — order matters:
    ```csharp
    app.UseIAMWebAuthentication(); // CorrelationId → SecurityHeaders → AuthToken → Auth → Authz
    ```
4. **Map endpoints** —
    ```csharp
    app.MapRazorPages();
    app.MapRazorComponents<App>()
        .AddAdditionalAssemblies(typeof(Corely.IAM.Web.AppRoutes).Assembly)
        .AddInteractiveServerRenderMode();
    ```
5. **Include static assets** — CSS is auto-served from `_content/Corely.IAM.Web/`
6. **Configure appsettings** — `Security:AuthTokenTtlSeconds` for token lifetime
7. **What `AddIAMWeb()` registers** — table: Service | Lifetime | Purpose
8. **What `AddIAMWebBlazor()` registers** — table: Service | Lifetime | Purpose
9. **What `UseIAMWebAuthentication()` adds** — ordered middleware list

---

### `authentication-flow.md` — Authentication

**Sections:**
- Pre-authentication pages (Razor Pages, not Blazor): SignIn, Register, SelectAccount, CreateAccount, SignOut
- Sign-in flow diagram (numbered steps):
    1. User submits username/password at `/signin`
    2. `IAuthenticationService.SignInAsync()` validates credentials
    3. On success: `AuthCookieManager.SetAuthCookies()` stores HttpOnly cookies
    4. If single account: auto-switch and redirect to dashboard
    5. If multiple accounts: redirect to `/select-account`
- Registration flow: create user → auto sign in → redirect
- Account switching: select account → `SwitchAccountAsync()` → new token
- Cookie details table: Cookie Name | Purpose | Flags
    - `auth_token` — JWT token — HttpOnly, Secure, SameSite=Strict
    - `auth_token_id` — Token ID for revocation — HttpOnly, Secure, SameSite=Strict
    - `device_id` — Device fingerprint — 90-day expiry
- Token validation middleware: `AuthenticationTokenMiddleware` extracts cookie, validates, sets UserContext + ClaimsPrincipal

---

### `authorization-ui.md` — Authorization in the UI

**Sections:**
- `PermissionView` component: wraps UI elements, shows/hides based on permission
- Usage example:
    ```razor
    <PermissionView Action="AuthAction.Create" Resource="@PermissionConstants.PERMISSION_RESOURCE_TYPE">
        <button class="btn btn-primary">Create</button>
    </PermissionView>
    ```
- Parameters table: Parameter | Type | Description
- Authorized/NotAuthorized fragments
- How it works: calls `IAuthorizationProvider.IsAuthorizedAsync()` on parameter change, caches result
- Common patterns: gating buttons, gating entire sections, resource-specific gates

---

### `pages/index.md` — Page Inventory

**Sections:**
- Route table: Route | Page | Type | Description
    - `/signin` — SignIn — Razor Page — Login form
    - `/register` — Register — Razor Page — Registration form
    - `/select-account` — SelectAccount — Razor Page — Account selection
    - `/create-account` — CreateAccount — Razor Page — New account form
    - `/signout` — SignOut — Razor Page — Sign out
    - `/` — Dashboard — Blazor — Account overview
    - `/accept-invitation` — AcceptInvitation — Blazor — Token-based invitation acceptance
    - `/profile` — Profile — Blazor — User profile + keys
    - `/accounts/{id}` — AccountDetail — Blazor — Account detail
    - `/users` — UserList — Blazor — User table with search/sort
    - `/users/{id}` — UserDetail — Blazor — User detail + relations
    - `/groups` — GroupList — Blazor — Group table
    - `/groups/{id}` — GroupDetail — Blazor — Group detail
    - `/roles` — RoleList — Blazor — Role table
    - `/roles/{id}` — RoleDetail — Blazor — Role detail + permissions
    - `/permissions` — PermissionList — Blazor — Permission table + create
    - `/permissions/{id}` — PermissionDetail — Blazor — Permission detail
- `AppRoutes` class: all routes defined as string constants
- Render modes: Razor Pages for pre-auth, Blazor Server (no prerender) for management

---

### `pages/users.md` through `pages/profile.md`

**Per-page pattern:**
- Route and render mode
- Screenshot description (what the user sees)
- Features: search, sort, pagination, create, delete, relation management
- Injected services
- Authorization requirements (which PermissionView gates exist)
- Code example: how to navigate to the page or trigger key actions

---

### `components/index.md` — Shared Components

**Sections:**
- Component inventory table: Component | Purpose | Key Parameters
- All shared components listed with one-line descriptions
- Usage note: all components are in `Corely.IAM.Web.Components.Shared` namespace

---

### `components/permission-view.md` through `components/encryption-signing.md`

**Per-component pattern:**
- Purpose (one line)
- Parameters table (Parameter | Type | Default | Description)
- Usage example (Razor markup)
- Behavior notes (caching, callbacks, edge cases)

---

### `base-classes.md` — Page Base Classes

**Sections:**
- Class hierarchy diagram: `ComponentBase` → `AuthenticatedPageBase` → `EntityPageBase` → `EntityListPageBase<T>` / `EntityDetailPageBase`
- Per-class table: Class | Purpose | Key Members
- **AuthenticatedPageBase**: ensures user context loaded, provides `UserContext`, `IsAuthenticated`, `OnInitializedAuthenticatedAsync()`
- **EntityPageBase**: `LoadCoreAsync()` (abstract), `ExecuteSafeAsync()`, `SetResultMessage()`, loading/alert state
- **EntityListPageBase<T>**: pagination (`_skip`, `_take=25`, `_totalCount`), search (debounced 300ms), sort (cycle asc/desc/none), `CycleSortAsync()`, `OnPageChangedAsync()`
- **EntityDetailPageBase**: `Id` route parameter
- Code example: creating a custom page extending EntityListPageBase

---

### `middleware.md` — Middleware

**Sections:**
- Pipeline order diagram (numbered):
    1. `CorrelationIdMiddleware` — X-Correlation-ID propagation, Serilog context
    2. `SecurityHeadersMiddleware` — CSP, HSTS, X-Frame-Options, Permissions-Policy
    3. `AuthenticationTokenMiddleware` — cookie → JWT validation → UserContext + ClaimsPrincipal
    4. `UseAuthentication()` — ASP.NET Core
    5. `UseAuthorization()` — ASP.NET Core
- Per-middleware: purpose, what it adds/checks, configuration options

---

### `services.md` — Web Services

**Sections:**
- Service table: Interface | Implementation | Lifetime | Purpose
- **IAuthCookieManager**: SetAuthCookies, DeleteAuthCookies, GetOrCreateDeviceId — cookie management
- **IBlazorUserContextAccessor**: GetUserContextAsync — cached user context for Blazor components, 5-second semaphore timeout
- **IamAuthenticationStateProvider**: Blazor auth state derived from UserContext
- **IUserContextClaimsBuilder**: BuildPrincipal — maps UserContext to ClaimsPrincipal with standard + custom claims
- **IAccountDisplayState**: AccountName property + OnChanged event — syncs account display in NavBar
- Code examples for each service

---

### `styling.md` — CSS & Theming

**Sections:**
- Bootstrap-first approach: Corely.IAM.Web builds on Bootstrap 5
- CSS file: `wwwroot/css/iam-web.css`
- Key utility classes table: Class | Purpose
    - `.auth-container` / `.auth-card` — centered auth page cards
    - `.management-header` — page header with title + action buttons
    - `.props-grid` — CSS Grid for property label/value pairs
    - `.permission-badge` (`.active` / `.inactive`) — CRUDX status indicators
    - `.sys-badge` / `.user-badge` — system vs user-defined entity badges
    - `.entity-card` — hover-animated cards
    - `.icon-btn` — small icon buttons for table actions
    - `.ep-*` — effective permissions panel styles
- Customization: override CSS variables or add custom stylesheet after the IAM.Web reference
- Dark mode: not currently supported (note for future)

---

### `security.md` — Security Features

**Sections:**
- Cookie security: HttpOnly, Secure, SameSite=Strict on all auth cookies
- Content Security Policy: `'self'` + `'unsafe-inline'` for Blazor, `wss:` for SignalR
- Security headers table: Header | Value | Purpose
- HSTS: enabled in non-development environments
- Permissions-Policy: camera, microphone, geolocation, payment all denied
- Notes on CSP customization for host apps adding external scripts/styles

---

## Implementation Order

1. **Phase 1**: `index.md` + `setup.md` (entry points — most critical for adoption)
2. **Phase 2**: `authentication-flow.md` + `authorization-ui.md` (core concepts)
3. **Phase 3**: `pages/` (all 9 files — page reference)
4. **Phase 4**: `components/` (all 7 files — component reference)
5. **Phase 5**: `base-classes.md` + `services.md` + `middleware.md` (internals for advanced users)
6. **Phase 6**: `styling.md` + `security.md` (reference material)

---

## Notes

- **Cross-reference Corely.IAM docs** for service interfaces, models, and authorization details
- **Cross-reference Corely.Common** for FilterBuilder/OrderBuilder used in list pages
- **Don't document Corely.IAM core library concepts here** — link to IAM docs instead
- **Razor Page vs Blazor distinction** is important — pre-auth pages are traditional server-rendered, management pages are Blazor Server interactive
- **Demo references** should point to `Corely.IAM.WebApp` as the reference host implementation
