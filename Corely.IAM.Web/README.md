# Corely.IAM.Web

Reusable Razor Class Library (RCL) that provides a complete Blazor Server UI for the Corely.IAM identity and access management library. Drop it into any ASP.NET Core host app to get a fully-functional IAM web interface with minimal wiring.

---

## What the library provides

- **Blazor pages** — Users, Groups, Roles, Permissions, Account detail, Profile, Dashboard, Home
- **Razor Pages** — Sign In, Register, Sign Out, Select Account, Switch Account, Create Account
- **Shared components** — Alert, Pagination, ConfirmModal, FormModal, EntityPickerModal, EffectivePermissionsPanel, PermissionView, LoadingSpinner, and more
- **Middleware** — correlation ID, security headers (CSP, X-Frame-Options, etc.), JWT cookie → UserContext
- **Authentication** — cookie-based auth wired to Corely.IAM's JWT token system
- **Static assets** — `iam-web.css` (custom styles), `modal-keyboard.js`
- **Route constants** — `AppRoutes` class with all page paths

---

## What the host is responsible for

- **`SecurityConfigurationProvider`** — reads the system encryption key from config and implements `ISecurityConfigurationProvider`. The library defines the interface; the host provides the implementation.
- **Database / EF configuration** — choosing and wiring the EF provider (SQL Server, MySQL, MariaDB)
- **Blazor host shell** — `App.razor`, `Routes.razor`, `Program.cs`
- **Static asset references** — Bootstrap, Bootstrap Icons (must be served by the host)
- **Logging** — Serilog or any other provider of your choice

---

## Integration steps

### 1. Add a project reference

```xml
<ProjectReference Include="..\Corely.IAM.Web\Corely.IAM.Web.csproj" />
```

### 2. Implement `SecurityConfigurationProvider`

The host must provide an implementation of `ISecurityConfigurationProvider` that returns the system symmetric key. Create this in your host project — the library intentionally does not provide it so the host controls how the key is sourced (config, key vault, environment variable, etc.).

```csharp
// YourHost/Security/SecurityConfigurationProvider.cs
using Corely.IAM.Security.Providers;
using Corely.Security.KeyStore;

internal class SecurityConfigurationProvider(IConfiguration configuration)
    : ISecurityConfigurationProvider
{
    private readonly InMemorySymmetricKeyStoreProvider _keyStoreProvider = new(
        configuration["Security:SystemKey"]
            ?? throw new InvalidOperationException("Security:SystemKey not found in configuration")
    );

    public ISymmetricKeyStoreProvider GetSystemSymmetricKey() => _keyStoreProvider;
}
```

### 3. Register services in `Program.cs`

```csharp
using Corely.IAM.Web.Extensions;

// Razor Pages + cookie auth + shared services
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddIAMWeb();       // Razor Pages, auth, cookie manager
builder.Services.AddIAMWebBlazor(); // Blazor auth state, user context accessor

// Instantiate your SecurityConfigurationProvider and wire up IAM core services
var securityConfigProvider = new SecurityConfigurationProvider(builder.Configuration);
builder.Services.AddIAMServicesWithEF(builder.Configuration, securityConfigProvider, efConfig);
```

`AddIAMWeb()` registers:
- `IAuthCookieManager` — reads/writes the auth JWT cookie
- `IUserContextClaimsBuilder` — converts `UserContext` to `ClaimsPrincipal`
- Cookie authentication scheme (login path `/signin`, logout path `/signout`)
- Authorization services

`AddIAMWebBlazor()` registers:
- `IBlazorUserContextAccessor` — access the current user context from Blazor components
- `AuthenticationStateProvider` — Blazor auth state backed by the cookie
- `IAccountDisplayState` — reactive state for displaying the current account name in the navbar
- Cascading authentication state

### 4. Set up the middleware pipeline

Order matters — call `UseIAMWebAuthentication()` before static files and routing:

```csharp
using Corely.IAM.Web.Extensions;

app.UseIAMWebAuthentication(); // correlation ID → security headers → JWT validation → auth/authz
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddAdditionalAssemblies(typeof(Corely.IAM.Web.AppRoutes).Assembly)
    .AddInteractiveServerRenderMode();
```

`UseIAMWebAuthentication()` applies middleware in this order:
1. `CorrelationIdMiddleware` — assigns a correlation ID to every request
2. `SecurityHeadersMiddleware` — sets CSP, X-Frame-Options, X-Content-Type-Options, etc.
3. `AuthenticationTokenMiddleware` — validates the JWT auth cookie and populates `UserContext`
4. `UseAuthentication()` — ASP.NET Core authentication
5. `UseAuthorization()` — ASP.NET Core authorization

### 5. Set up the Blazor router (`Routes.razor`)

The library's Blazor pages live in the `Corely.IAM.Web` assembly. Register it as an additional assembly so the router discovers them:

```razor
<Router AppAssembly="typeof(Routes).Assembly"
        AdditionalAssemblies="new[] { typeof(Corely.IAM.Web.AppRoutes).Assembly }">
    <Found Context="routeData">
        <AuthorizeRouteView RouteData="routeData"
                            DefaultLayout="typeof(Corely.IAM.Web.Components.Layout.MainLayout)">
            <NotAuthorized>
                <Corely.IAM.Web.Components.Shared.RedirectToLogin />
            </NotAuthorized>
        </AuthorizeRouteView>
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
    <NotFound>
        <LayoutView Layout="typeof(Corely.IAM.Web.Components.Layout.MainLayout)">
            <p>Page not found.</p>
        </LayoutView>
    </NotFound>
</Router>
```

### 6. Reference static assets in `App.razor`

The library's CSS is served from the RCL's static asset path. Bootstrap and Bootstrap Icons must also be present (served by the host via libman, npm, or CDN):

```html
<link rel="stylesheet" href="lib/bootstrap/dist/css/bootstrap.min.css" />
<link rel="stylesheet" href="lib/bootstrap-icons/font/bootstrap-icons.min.css" />
<link rel="stylesheet" href="app.css" />
<link rel="stylesheet" href="_content/Corely.IAM.Web/css/iam-web.css" />

<!-- at end of body -->
<script src="lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
<script src="_framework/blazor.web.js"></script>
```

---

## Required configuration (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=CorelyIAM;..."
  },
  "Database": {
    "Provider": "mssql"
  },
  "Security": {
    "SystemKey": "<hex key — generate with Corely.IAM.DevTools: dotnet run -- sym-encrypt --create>"
  },
  "SecurityOptions": {
    "MaxLoginAttempts": 5,
    "AuthTokenTtlSeconds": 3600
  },
  "PasswordValidationOptions": {
    "MinimumLength": 8,
    "RequireUppercase": true,
    "RequireLowercase": true,
    "RequireDigit": true,
    "RequireSpecialCharacter": true
  }
}
```

| Key | Required | Notes |
|-----|:--------:|-------|
| `ConnectionStrings:DefaultConnection` | ✅ | Database connection string |
| `Database:Provider` | ✅ | `mssql`, `mysql`, or `mariadb` |
| `Security:SystemKey` | ✅ | Base64 symmetric key — generate with DevTools |
| `SecurityOptions:*` | ✅ | Login lockout and token TTL |
| `PasswordValidationOptions:*` | ✅ | Password rules |

Generate a system key with the DevTools project:

```powershell
cd Corely.IAM.DevTools
dotnet run -- sym-encrypt --create
```

---

## Available routes (`AppRoutes`)

All routes are defined as constants on `Corely.IAM.Web.AppRoutes`:

| Constant | Path | Description |
|----------|------|-------------|
| `Dashboard` | `/` | Home dashboard |
| `Users` | `/users` | User list |
| `Groups` | `/groups` | Group list |
| `Roles` | `/roles` | Role list |
| `Permissions` | `/permissions` | Permission list |
| `Accounts` | `/accounts` | Account detail (by ID) |
| `Profile` | `/profile` | Current user profile |
| `SignIn` | `/signin` | Sign in (Razor Page) |
| `SignOut` | `/signout` | Sign out (Razor Page) |
| `Register` | `/register` | Registration (Razor Page) |
| `SelectAccount` | `/select-account` | Account picker (Razor Page) |
| `CreateAccount` | `/create-account` | Create account (Razor Page) |

---

## Shared components

Import the library's namespace in your `_Imports.razor` or reference components fully qualified:

```razor
@using Corely.IAM.Web.Components.Shared
```

| Component | Description |
|-----------|-------------|
| `Alert` | Dismissable alert banner |
| `ConfirmModal` | Generic confirmation dialog |
| `FormModal` | Generic form dialog |
| `EntityPickerModal` | Searchable paginated entity picker |
| `EffectivePermissionsPanel` | Displays CRUDX badges + role derivation tree. Accepts `UseCard` parameter (default `true`) |
| `PermissionView` | Conditionally renders children based on CRUDX permission check |
| `Pagination` | Server-side pagination controls |
| `LoadingSpinner` | Loading indicator |
| `RedirectToLogin` | Redirects unauthenticated users to `/signin` |

---

## Architecture notes

- The library targets **net10.0** and uses **Blazor Interactive Server** render mode throughout. Pre-rendering is disabled on all pages (`prerender: false`).
- All Blazor pages inherit from base classes in `Components/` (`EntityDetailPageBase`, `EntityListPageBase`, `AuthenticatedPageBase`) which handle loading state, error messages, confirmation dialogs, and navigation guards.
- The `IBlazorUserContextAccessor` service is the correct way to access the current user context from Blazor components. Do not use `IHttpContextAccessor` directly in Blazor components.
