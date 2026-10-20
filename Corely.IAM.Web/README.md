# Corely.IAM.Web

Reusable Razor Class Library (RCL) that provides the Blazor Server UI for the Corely.IAM identity and access management library: authentication pages, and management components the host places at its own paths.

> **Upgrading from 6.x?** See [MIGRATION-IAM.Web-7.0.md](https://github.com/ultrabstrong/Corely.IAM/blob/master/MIGRATION-IAM.Web-7.0.md).
> The package ships components only: the host writes a page per component and supplies every link.

---

## What the library provides

- **Management components**: account, users, groups, roles, permissions, audit, profile, invitations and platform settings, each placed by the host in a short page shell
- **Razor Pages**: Sign In, Register, Sign Out, Select Account, Switch Account, Create Account
- **Shared components**: Alert, Pagination, ConfirmModal, FormModal, EntityPickerModal, EffectivePermissionsPanel, PermissionView, LoadingSpinner, and more
- **Middleware**: correlation ID, JWT cookie → UserContext. Security headers are the host's; see [Security](Docs/security.md#security-headers)
- **Authentication**: cookie-based auth wired to Corely.IAM's JWT token system
- **Static assets**: `iam-web.css` (custom styles), `modal-keyboard.js`
- **Account route guard**: `AccountScope`, so a host can put the account in its paths
- **Route constants**: `AppRoutes` names the authentication endpoints, the only fixed paths

---

## What the host is responsible for

- **`SecurityConfigurationProvider`**: reads the system encryption key from config and implements `ISecurityConfigurationProvider`. The library defines the interface; the host provides the implementation.
- **Database / EF configuration**: choosing and wiring the EF provider (SQL Server, MySQL)
- **Blazor host shell**: `App.razor`, `Routes.razor`, `Program.cs`, its layout and navigation
- **Page shells and links**: a page per component at the host's paths, and an `IIamWebLinks` saying where each lives ([Hosting Pages](Docs/hosting-pages.md))
- **Static asset references**: Bootstrap, Bootstrap Icons (must be served by the host)
- **Logging**: Serilog or any other provider of your choice

---

## Integration steps

### 1. Add a project reference

```xml
<ProjectReference Include="..\Corely.IAM.Web\Corely.IAM.Web.csproj" />
```

### 2. Implement `SecurityConfigurationProvider`

The host must provide an implementation of `ISecurityConfigurationProvider` that returns the system symmetric key. Create this in your host project. The library intentionally does not provide it, so the host controls how the key is sourced (config, key vault, environment variable, etc.).

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
builder.Services.AddIAMWebBlazor<MyIamLinks>(); // Blazor auth state, the host's IIamWebLinks

// Instantiate your SecurityConfigurationProvider and wire up IAM core services
var securityConfigProvider = new SecurityConfigurationProvider(builder.Configuration);
builder.Services.AddIAMServicesWithEF(builder.Configuration, securityConfigProvider, efConfig);
```

`AddIAMWeb()` registers:
- `IAuthCookieManager`: reads/writes the auth JWT cookie
- `IUserContextClaimsBuilder`: converts `UserContext` to `ClaimsPrincipal`
- Cookie authentication scheme (login path `/signin`, logout path `/signout`)
- Authorization services

`AddIAMWebBlazor<TLinks>()` registers:
- `IBlazorUserContextAccessor`: access the current user context from Blazor components
- `AuthenticationStateProvider`: Blazor auth state backed by the cookie
- `IAccountDisplayState`: reactive state for displaying the current account name in the navbar
- Cascading authentication state
- `IIamWebLinks`: the host's `TLinks`

### 4. Set up the middleware pipeline

Order matters. Call `UseIAMWebAuthentication()` before the endpoints are mapped. `MapStaticAssets()`
with `WithStaticAssets()` fingerprints IAM.Web's CSS and scripts, so a browser never keeps an old
copy after an upgrade:

```csharp
using Corely.IAM.Web.Extensions;

app.UseIAMWebAuthentication(); // correlation ID → JWT validation → auth/authz
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapRazorComponents<App>()
    .WithStaticAssets()
    .AddInteractiveServerRenderMode();
```

`UseIAMWebAuthentication()` applies middleware in this order:
1. `CorrelationIdMiddleware`: assigns a correlation ID to every request
2. `AuthenticationTokenMiddleware`: validates the JWT auth cookie and populates `UserContext`
3. `UseAuthentication()`: ASP.NET Core authentication
4. `UseAuthorization()`: ASP.NET Core authorization

It sets no security headers. Set them in the host, before this call; [Security](Docs/security.md#security-headers)
has a starting set.

### 5. Write the page shells and the router (`Routes.razor`)

The library has no routed Blazor pages, so the router routes only the host's assembly. Each page is
a short shell around a component, at a path the host chooses:

```razor
@page "/accounts/{AccountId:guid}/users"
@rendermode @(new InteractiveServerRenderMode(prerender: false))

<PageTitle>Users</PageTitle>

<AccountScope AccountId="AccountId">
    <UserList />
</AccountScope>

@code {
    [Parameter] public Guid AccountId { get; set; }
}
```

```razor
<Router AppAssembly="typeof(Routes).Assembly">
    <Found Context="routeData">
        <AuthorizeRouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)">
            <NotAuthorized>
                <Corely.IAM.Web.Components.Shared.RedirectToLogin />
            </NotAuthorized>
        </AuthorizeRouteView>
    </Found>
</Router>
```

[Hosting Pages](Docs/hosting-pages.md) covers every shell, `IIamWebLinks` and `AccountScope`.
`Corely.IAM.WebApp` has a working shell for every component.

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
    "SystemKey": "<hex key, generated with Corely.IAM.DevTools: dotnet run -- sym-encrypt --create>"
  },
  "SecurityOptions": {
    "MaxLoginAttempts": 5,
    "AuthTokenTtlSeconds": 3600,
    "AuthSessionTtlSeconds": 604800
  },
  "PasswordValidationOptions": {
    "MinimumLength": 8,
    "RequireUppercase": true,
    "RequireLowercase": true,
    "RequireDigit": true,
    "RequireNonAlphanumeric": true
  }
}
```

| Key | Required | Notes |
|-----|:--------:|-------|
| `ConnectionStrings:DefaultConnection` | ✅ | Database connection string |
| `Database:Provider` | ✅ | `mssql` or `mysql` |
| `Security:SystemKey` | ✅ | Base64 symmetric key, generated with DevTools |
| `SecurityOptions:*` | ✅ | Login lockout, access-token TTL, and renewable session TTL |
| `PasswordValidationOptions:*` | ✅ | Password rules |

Generate a system key with the DevTools project:

```powershell
cd Corely.IAM.DevTools
dotnet run -- sym-encrypt --create
```

---

## Fixed routes (`AppRoutes`)

The authentication endpoints are Razor Pages at fixed routes, named on `Corely.IAM.Web.AppRoutes`:
`SignIn`, `Register`, `SignOut`, `SelectAccount`, `SwitchAccount`, `CreateAccount`, `VerifyMfa`,
`GoogleCallback`, `RegisterWithGoogle`, `Privacy` and `Terms`. Every other path is the host's.

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
| `RedirectToLogin` | Redirects unauthenticated users to `/signin`, returning to the page afterwards |
| `AccountScope` | Renders its content only in the account its route names, switching first |

---

## Architecture notes

- The library targets **net10.0** and uses **Blazor Interactive Server** render mode throughout. Pre-rendering is disabled in every shell (`prerender: false`).
- All management components inherit from base classes in `Components/` (`EntityDetailPageBase`, `EntityListPageBase`, `AuthenticatedPageBase`) which handle loading state, error messages, confirmation dialogs, and navigation guards.
- The `IBlazorUserContextAccessor` service is the correct way to access the current user context from Blazor components. Do not use `IHttpContextAccessor` directly in Blazor components.
