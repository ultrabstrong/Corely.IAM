# Host App Integration

Integrate the Corely.IAM.Web UI library into an ASP.NET Core host application.

## 1) Install Package

Reference the `Corely.IAM.Web` project or package. It depends on `Corely.IAM` transitively.

## 2) Register Services

```csharp
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddIAMWeb();
builder.Services.AddIAMWebBlazor();
```

Call `AddIAMWeb()` and `AddIAMWebBlazor()` before `AddIAMServices()`.

## 3) Register IAM Core Services

```csharp
var securityConfigProvider = new SecurityConfigurationProvider(builder.Configuration);
Func<IServiceProvider, IEFConfiguration> efConfig = sp =>
    new MsSqlEFConfiguration(connectionString, sp.GetRequiredService<ILoggerFactory>());

var iamOptions = IAMOptions.Create(builder.Configuration, securityConfigProvider, efConfig);
builder.Services.AddIAMServices(iamOptions);
```

See the [Corely.IAM setup guide](../../Corely.IAM/Docs/step-by-step-setup.md) for details on `IAMOptions` and database configuration.

## 4) Configure Middleware

Order matters — `UseIAMWebAuthentication()` must come before `UseHttpsRedirection()`:

```csharp
var app = builder.Build();
app.UseIAMWebAuthentication();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
```

## 5) Map Endpoints

```csharp
app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddAdditionalAssemblies(typeof(Corely.IAM.Web.AppRoutes).Assembly)
    .AddInteractiveServerRenderMode();
```

`AddAdditionalAssemblies` registers the Corely.IAM.Web Blazor components for routing. `AddInteractiveServerRenderMode` enables Blazor Server interactivity for the management pages.

## 6) Include Static Assets

CSS is auto-served from `_content/Corely.IAM.Web/`. Reference it in your layout:

```html
<link rel="stylesheet" href="_content/Corely.IAM.Web/css/iam-web.css" />
```

## 7) Configure appsettings

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=CorelIAM;Trusted_Connection=True;"
  },
  "Security": {
    "SystemKey": "your-hex-key-from-devtools"
  },
  "SecurityOptions": {
    "AuthTokenTtlSeconds": 3600
  },
  "Database": {
    "Provider": "mssql"
  }
}
```

## What AddIAMWeb() Registers

| Service | Lifetime | Purpose |
|---------|----------|---------|
| Razor Pages | — | Pre-authentication page routing |
| `IHttpContextAccessor` | Singleton | HTTP context for cookie management |
| `IAuthCookieManager` | Singleton | Set/delete auth cookies (HttpOnly, Secure, SameSite=Strict) |
| `IUserContextClaimsBuilder` | Singleton | Maps `UserContext` to `ClaimsPrincipal` |
| Cookie Authentication | — | ASP.NET Core cookie auth with `/signin` and `/signout` paths |
| Authorization | — | ASP.NET Core authorization middleware |

## What AddIAMWebBlazor() Registers

| Service | Lifetime | Purpose |
|---------|----------|---------|
| `IBlazorUserContextAccessor` | Scoped | Cached user context for Blazor components |
| `AuthenticationStateProvider` | Scoped | Blazor auth state derived from `UserContext` |
| `IAccountDisplayState` | Scoped | Account name display + change notifications for NavBar |
| Cascading Auth State | — | Propagates auth state to all Blazor components |

## What UseIAMWebAuthentication() Adds

Middleware pipeline in order:

1. **CorrelationIdMiddleware** — assigns `X-Correlation-ID` header, enriches Serilog context
2. **SecurityHeadersMiddleware** — adds CSP, HSTS, X-Frame-Options, Permissions-Policy
3. **AuthenticationTokenMiddleware** — reads `auth_token` cookie, validates JWT, sets `UserContext` + `ClaimsPrincipal`
4. **UseAuthentication()** — ASP.NET Core authentication
5. **UseAuthorization()** — ASP.NET Core authorization

## Complete Example

See `Corely.IAM.WebApp/Program.cs` for the reference host implementation.
