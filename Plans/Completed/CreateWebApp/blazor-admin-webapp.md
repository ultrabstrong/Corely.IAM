# Blazor Admin Web App — Reference Analysis & Patterns to Duplicate

Analysis of `SampleBlazorApp` (DocsToData.AdminPortalWebApp) for patterns to replicate in a new Corely.IAM admin portal.

---

## 1. Authentication Flow

### Overview
Auth uses a **cookie-based JWT** pattern: IAM issues a JWT, the webapp stores it in an HttpOnly cookie, and middleware restores user context on each request.

### SignIn Flow (`Pages/Authentication/SignIn.cshtml` + `.cshtml.cs`)
1. **Razor Page** (not Blazor) — full server-side POST, no SignalR involvement
2. User submits `username` + `password` form
3. Calls `IAuthenticationService.SignInAsync(new SignInRequest(username, password, deviceId))`
4. On success, stores JWT in `authentication_token` cookie via `AuthenticationCookieHelper`
5. **Device ID**: Creates/reads a `device_id` cookie (1-year expiry) for device tracking
6. **Post-signin redirect**: If user has accounts but no `CurrentAccount` → `/select-account`; otherwise → `/`
7. Error display maps `SignInResultCode` to user-friendly messages

### SelectAccount Flow (`Pages/Authentication/SelectAccount.cshtml` + `.cshtml.cs`)
1. Shows list of `AvailableAccounts` from `UserContext`
2. User picks one → POST calls `IAuthenticationService.SwitchAccountAsync(new SwitchAccountRequest(accountId))`
3. New JWT issued, cookie updated → redirect to `/`
4. Guards: If no user context → redirect to `/signin`; if already has CurrentAccount → redirect to `/`

### SwitchAccount Flow (`Pages/Authentication/SwitchAccount.cshtml.cs`)
1. Inline POST handler (no visible page) for the NavBar's account dropdown
2. Same `SwitchAccountAsync` → cookie update pattern
3. Accepts `returnUrl` form field, validates it's a local URL before redirecting

### SignOut Flow (`Pages/Authentication/SignOut.cshtml.cs`)
1. POST-only (GET redirects to `/`)
2. Simply deletes the `authentication_token` cookie → redirect to `/signin`

### Key Decision: Auth Pages are Razor Pages, NOT Blazor Components
This is intentional — cookie manipulation requires `HttpResponse`, which is only available during HTTP requests (not SignalR connections). All 4 auth pages use Razor Pages with `_AuthLayout.cshtml` (minimal layout with no nav bar user context).

---

## 2. Middleware Pipeline

### Order in `Program.cs` (matters!)
```
CorrelationIdMiddleware
SecurityHeadersMiddleware
UseHttpsRedirection
UseStaticFiles            ← before auth (no auth needed for CSS/JS)
AuthenticationTokenMiddleware  ← IAM context restoration
UseAuthentication         ← ASP.NET Core auth
UseAuthorization
UseAntiforgery
MapRazorPages             ← auth pages
MapRazorComponents        ← Blazor components
```

### AuthenticationTokenMiddleware
- Reads `authentication_token` cookie on every request
- Calls `IUserContextProvider.SetUserContextAsync(token)` to validate JWT and restore user context
- On success: builds `ClaimsPrincipal` via `UserContextClaimsBuilder` → sets `HttpContext.User`
- On failure: deletes stale cookie, continues as unauthenticated

### CorrelationIdMiddleware
- Reads or generates `X-Correlation-ID` header
- Adds to response headers
- Pushes into Serilog `LogContext` so all logs in that request include it

### SecurityHeadersMiddleware
- Sets: `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `X-XSS-Protection: 1; mode=block`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy` disabling camera, mic, geolocation, etc.
- `Cache-Control: no-store, no-cache` (prevent caching sensitive data)
- `Content-Security-Policy` — allows `'unsafe-inline'` and `'unsafe-eval'` (required by Blazor Server/SignalR)

---

## 3. Blazor ↔ IAM Bridge (Interactive Server Mode)

### Problem
Blazor Interactive Server components communicate via SignalR (WebSocket), NOT HTTP. The `AuthenticationTokenMiddleware` only runs on HTTP requests. After the initial page load, subsequent component interactions don't trigger middleware.

### Solution: BlazorUserContextAccessor
- Wraps `IUserContextProvider` with thread-safe lazy initialization
- **Fast path**: If `IUserContextProvider` already has context (set by middleware on initial HTTP request), return it
- **Fallback**: For SignalR reconnections, reads cookie from `IHttpContextAccessor.HttpContext` and calls `SetUserContextAsync(token)`
- Uses `SemaphoreSlim` for thread safety, `_initializationAttempted` flag to prevent repeated failed attempts
- **Registered as Scoped** (one per circuit/connection)

### IamAuthenticationStateProvider
- Extends `AuthenticationStateProvider` (Blazor's auth system)
- Uses `BlazorUserContextAccessor.GetUserContextAsync()` → `UserContextClaimsBuilder.BuildPrincipal()`
- Enables `<AuthorizeView>`, `[Authorize]`, and `<AuthorizeRouteView>` in Blazor

### UserContextClaimsBuilder
- Static helper, used by both middleware and AuthenticationStateProvider
- Maps `UserContext` → `ClaimsPrincipal` with claims: `NameIdentifier`, `Name`, `Email`, `AccountId`, `AccountName`
- Uses `"Cookie"` as `authenticationType` (must be non-null for `IsAuthenticated == true`)

### AuthenticatedPageBase (abstract ComponentBase)
- Base class for pages requiring auth
- `OnInitializedAsync` calls `BlazorUserContextAccessor.GetUserContextAsync()` before page logic runs
- Exposes `IsAuthenticated` property
- Provides `OnInitializedAuthenticatedAsync()` virtual method — pages override this instead of `OnInitializedAsync`
- Defines `RenderMode = InteractiveServerRenderMode(prerender: false)` — **prerendering disabled** to prevent auth state flicker

---

## 4. Permission-Based UI (Real-Time Element Control)

### PermissionView Component
The key component for showing/hiding UI elements based on IAM permissions:

```razor
<PermissionView Action="AuthAction.Create" Resource="@PermissionConstants.EXTRACTION">
    <button class="btn btn-primary">Create Template</button>
</PermissionView>
```

- Parameters: `Action` (AuthAction enum), `Resource` (string), optional `ResourceIds` (Guid[])
- Injects `IAuthorizationProvider` and calls `IsAuthorizedAsync(action, resource, resourceId?)`
- Supports `<Authorized>` and `<NotAuthorized>` render fragments, plus `ChildContent` shorthand
- **Caches**: Re-checks only when parameters change (tracks `_lastParams` tuple)
- Doesn't show anything until the async check completes (`_checkComplete` guard)

### Usage Examples from ExtractionTemplates page
- **Page-level**: Create button only visible with `AuthAction.Create` permission
- **Row-level**: Edit/Delete buttons per-row, checking specific `ResourceIds` for each entity
- This pattern will be directly applicable to IAM entity CRUD pages

### AuthenticatedContent Component
- Wraps content that should only render after user context is initialized
- Prevents child components from rendering (and making service calls) before auth is ready
- Usage: `<AuthenticatedContent>...page content...</AuthenticatedContent>`

---

## 5. Service Registration & Configuration

### Program.cs Registration Order
```csharp
builder.Services.AddRazorPages();                         // Auth pages
builder.Services.AddRazorComponents().AddInteractiveServerComponents();  // Blazor
builder.Services.AddHttpContextAccessor();                // For BlazorUserContextAccessor
builder.Services.AddSingleton(TimeProvider.System);       // Testability
builder.Services.AddScoped<IBlazorUserContextAccessor, BlazorUserContextAccessor>();

// Cookie auth (minimal — actual auth is IAM)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => { options.LoginPath = "/signin"; options.LogoutPath = "/signout"; });
builder.Services.AddAuthorization();

// Blazor auth state bridge
builder.Services.AddScoped<AuthenticationStateProvider, IamAuthenticationStateProvider>();
builder.Services.AddCascadingAuthenticationState();

// IAM services
builder.Services.AddIAMServicesWithEF(config, securityConfigProvider, efConfigFactory);
```

### SecurityConfigurationProvider
- Implements `ISecurityConfigurationProvider`
- Reads `Security:SystemKey` from `IConfiguration`
- Creates `InMemorySymmetricKeyStoreProvider` with the key

### EF Configuration
- `MsSqlConfiguration` extends `EFMsSqlConfigurationBase`
- Logs only `CommandExecuted` events via `EFEventDataLogger`
- Debug mode: `EnableSensitiveDataLogging()` + `EnableDetailedErrors()`

### Logging
- Serilog configured from `appsettings.json` via `ReadFrom.Configuration()`
- Sinks: Console, Seq (dev), Application Insights (prod)
- Enrichments: `Application` name, `CorrelationId` (via middleware)

---

## 6. Project Structure

```
WebApp/
├── Program.cs                              # Host builder + DI + pipeline
├── Routes.cs                               # AppRoutes constants
├── Security/
│   ├── AuthenticationConstants.cs          # Cookie names, paths
│   └── SecurityConfigurationProvider.cs    # ISecurityConfigurationProvider impl
├── Services/
│   ├── BlazorUserContextAccessor.cs        # SignalR auth context bridge
│   ├── IamAuthenticationStateProvider.cs   # Blazor AuthenticationStateProvider
│   └── UserContextClaimsBuilder.cs         # UserContext → ClaimsPrincipal
├── Middleware/
│   ├── AuthenticationTokenMiddleware.cs    # Cookie → IAM context
│   ├── CorrelationIdMiddleware.cs          # Request tracing
│   └── SecurityHeadersMiddleware.cs        # Security headers
├── DataAccess/
│   └── MsSqlConfiguration.cs              # EF provider config
├── Pages/                                  # Razor Pages (server-side, NOT Blazor)
│   ├── Authentication/
│   │   ├── SignIn.cshtml + .cshtml.cs
│   │   ├── SignOut.cshtml + .cshtml.cs
│   │   ├── SelectAccount.cshtml + .cshtml.cs
│   │   └── SwitchAccount.cshtml + .cshtml.cs
│   └── Shared/
│       └── _AuthLayout.cshtml              # Minimal layout for auth pages
├── Components/                             # Blazor components
│   ├── App.razor                           # Root: <head>, <body>, scripts, reconnect UI
│   ├── Routes.razor                        # AuthorizeRouteView + RedirectToLogin
│   ├── _Imports.razor                      # Global usings
│   ├── AuthenticatedPageBase.cs            # Base class for auth-required pages
│   ├── Layout/
│   │   ├── MainLayout.razor                # App shell: nav + body + footer
│   │   └── NavBar.razor                    # Top nav: account switcher, sign out
│   ├── Pages/
│   │   ├── Home.razor                      # Dashboard (unauthenticated + authenticated views)
│   │   ├── Settings.razor                  # Placeholder
│   │   └── Extraction/                     # Domain pages
│   │       ├── ExtractionTemplates.razor   # List page with permission-based CRUD buttons
│   │       └── ExtractionTemplateEditor.razor  # Edit page
│   └── Shared/
│       ├── Alert.razor + AlertType.cs
│       ├── AuthenticatedContent.razor
│       ├── ConfirmModal.razor + ConfirmModalType.cs
│       ├── LoadingSpinner.razor
│       ├── LoggingErrorBoundary.razor
│       ├── PermissionView.razor
│       └── RedirectToLogin.razor
└── wwwroot/
    ├── lib/ (bootstrap 5.3.3, bootstrap-icons 1.11.3 via libman)
    ├── app.css
    └── js/modal-keyboard.js
```

---

## 7. UI Framework & Patterns

- **Bootstrap 5.3.3** + **Bootstrap Icons 1.11.3** via LibMan (CDN download, no npm)
- **No JS framework** — pure server-rendered Blazor with minimal JS interop (only `modal-keyboard.js` for Escape key handling)
- **Reconnect UI**: Custom styled `components-reconnect-modal` in `App.razor` for SignalR disconnections
- **Error handling**: `LoggingErrorBoundary` wraps `@Body` in `MainLayout`, logs to Serilog, shows user-friendly error with "Try Again" / "Go Home"
- **Loading pattern**: `<LoadingSpinner Visible="_loading" />` + `_loading` bool, set true before async call, false after
- **Confirmation pattern**: `<ConfirmModal>` with Loading state, backdrop click dismiss, Escape key dismiss

---

## 8. Key Configuration (appsettings.template.json)

Settings we'll need in the IAM admin app:
```json
{
  "ConnectionStrings": { "DefaultConnection": "..." },
  "Security": { "SystemKey": "<secret>" },
  "SecurityOptions": { "MaxLoginAttempts": 5, "AuthTokenTtlSeconds": 3600 },
  "PasswordValidationOptions": { "MinimumLength": 8, "RequireUppercase": true, ... },
  "Serilog": { ... }
}
```

---

## 9. Items to Duplicate in New Webapp

### Must duplicate (infrastructure)
- [ ] `Program.cs` — host builder, service registration, middleware pipeline
- [ ] `SecurityConfigurationProvider` — `ISecurityConfigurationProvider` impl
- [ ] `MsSqlConfiguration` — EF config with logging
- [ ] `AuthenticationTokenMiddleware` — cookie → IAM context
- [ ] `CorrelationIdMiddleware` — request tracing
- [ ] `SecurityHeadersMiddleware` — security headers
- [ ] `BlazorUserContextAccessor` — SignalR auth bridge
- [ ] `IamAuthenticationStateProvider` — Blazor auth bridge
- [ ] `UserContextClaimsBuilder` — claims builder
- [ ] `AuthenticationConstants` + `AuthenticationCookieHelper` — cookie handling
- [ ] `AppRoutes` — centralized route constants
- [ ] `appsettings.template.json` — config template

### Must duplicate (auth pages — Razor Pages)
- [ ] `SignIn.cshtml` + `.cshtml.cs` — login form
- [ ] `SignOut.cshtml` + `.cshtml.cs` — logout handler
- [ ] `SelectAccount.cshtml` + `.cshtml.cs` — account picker
- [ ] `SwitchAccount.cshtml` + `.cshtml.cs` — inline account switch
- [ ] `_AuthLayout.cshtml` — minimal layout for auth pages

### Must duplicate (Blazor shell)
- [ ] `App.razor` — root document with reconnect UI
- [ ] `Routes.razor` — `AuthorizeRouteView` with redirect
- [ ] `_Imports.razor` — global usings
- [ ] `MainLayout.razor` — app shell (nav + body + footer)
- [ ] `NavBar.razor` — top nav with account switcher + sign out
- [ ] `AuthenticatedPageBase.cs` — base class for auth pages

### Must duplicate (shared components)
- [ ] `PermissionView.razor` — permission-based show/hide
- [ ] `AuthenticatedContent.razor` — auth gate wrapper
- [ ] `RedirectToLogin.razor` — unauthenticated redirect
- [ ] `LoggingErrorBoundary.razor` — error boundary with logging
- [ ] `Alert.razor` + `AlertType.cs` — alert component
- [ ] `ConfirmModal.razor` + `ConfirmModalType.cs` — confirmation dialog
- [ ] `LoadingSpinner.razor` — loading indicator

### Must duplicate (static assets)
- [ ] `libman.json` — Bootstrap 5.3.3 + Bootstrap Icons
- [ ] `app.css` — base styles
- [ ] `js/modal-keyboard.js` — modal Escape key handler

### NOT duplicating (domain-specific to DocsToData)
- Extraction pages/services/blob storage
- Azure Identity / Blob Storage
- Metering, Entitlements, Quota services
- MistralDocumentAI, Textract connectors
- Application Insights sink
