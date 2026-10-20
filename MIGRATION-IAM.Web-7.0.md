# Migrating to Corely.IAM.Web 7.0

Corely.IAM.Web now ships components only. Its Blazor pages lost their routes, its layout and
navigation moved to `Corely.IAM.WebApp`, and every link a component renders comes from the host.
The authentication Razor Pages keep their fixed routes. Corely.IAM itself does not change.

## 1. Stop routing the library

Remove the IAM.Web assembly from the router and from endpoint mapping. `MapRazorPages()` still maps
sign in and the other authentication pages.

```razor
<!-- Routes.razor -->
<Router AppAssembly="typeof(Routes).Assembly">
```

```csharp
app.MapRazorComponents<App>().AddInteractiveServerRenderMode(); // no AddAdditionalAssemblies
```

## 2. Implement `IIamWebLinks`

```csharp
builder.Services.AddIAMWebBlazor<MyIamLinks>();
```

`MyIamLinks : IIamWebLinks` returns the host's path for each page: `Dashboard`, `Profile`,
`AcceptInvitation`, `Account`, `Users`/`User`, `Groups`/`Group`, `Roles`/`Role`,
`Permissions`/`Permission`, `Audit`/`AuditEntry`. Account scoped ones take the account id. There is
no default. `AddIAMWebBlazor()` without links remains for a host that renders only building blocks
such as `TotpSection`.

## 3. Write a shell per page

Each former page is a component with the same name, minus `Page` for the platform settings pair:

| Was routed at | Component | Namespace |
|---------------|-----------|-----------|
| `/accounts/{Id}` | `AccountDetail` (now always the current account; no `Id`) | `Corely.IAM.Web.Components.Accounts` |
| `/users`, `/users/{Id}` | `UserList`, `UserDetail` | `Corely.IAM.Web.Components.Users` |
| `/groups`, `/groups/{Id}` | `GroupList`, `GroupDetail` | `Corely.IAM.Web.Components.Groups` |
| `/roles`, `/roles/{Id}` | `RoleList`, `RoleDetail` | `Corely.IAM.Web.Components.Roles` |
| `/permissions`, `/permissions/{Id}` | `PermissionList`, `PermissionDetail` | `Corely.IAM.Web.Components.Permissions` |
| `/audit`, `/audit/{Id}` | `AuditLog`, `AuditEntryDetail` | `Corely.IAM.Web.Components.Audit` |
| `/accept-invitation` | `AcceptInvitation` | `Corely.IAM.Web.Components.Invitations` |
| `/profile` | `Profile` | `Corely.IAM.Web.Components` |
| `/platform-settings` | `PlatformSettingsRedirect` | `Corely.IAM.Web.Components.Platform` |
| `/platform-settings/audit` | `PlatformAuditSettings` (was `PlatformAuditSettingsPage`) | `Corely.IAM.Web.Components.Platform` |
| `/platform-settings/security` | `PlatformSecuritySettings` (was `PlatformSecuritySettingsPage`) | `Corely.IAM.Web.Components.Platform` |

The `Components.Pages.*` namespaces are gone. A shell owns the path, the render mode and the title;
wrap account scoped components in `AccountScope`:

```razor
@page "/accounts/{AccountId:guid}/users"
@rendermode @(new InteractiveServerRenderMode(prerender: false))
@attribute [Authorize]

<PageTitle>Users</PageTitle>

<AccountScope AccountId="AccountId">
    <UserList />
</AccountScope>

@code {
    [Parameter] public Guid AccountId { get; set; }
}
```

`Corely.IAM.Web/Docs/hosting-pages.md` has the details, and `Corely.IAM.WebApp` has every shell.

## 4. Bring your own layout

`MainLayout`, `NavBar` and `Footer` left the library (the namespace
`Corely.IAM.Web.Components.Layout` is gone). Copy them from `Corely.IAM.WebApp/Components/Layout`
if you used them. `ThemeToggle`, `AuthenticatedContent`, `AccountHeader`, `LoggingErrorBoundary` and
`RedirectToLogin` stay.

## 5. Platform settings paths

`IAMWebOptions.PlatformSettingsPages` starts empty. Add IAM's two areas with your paths:

```csharp
builder.Services.AddIAMWeb(o =>
{
    o.PlatformSettingsPages.Add(
        new("Audit", "/platform-settings/audit", "bi-journal-text", PlatformSettings.RESOURCE_TYPE));
    o.PlatformSettingsPages.Add(
        new("Security", "/platform-settings/security", "bi-shield-lock", SecurityPolicy.RESOURCE_TYPE));
});
```

`ReadablePlatformSettingsPagesAsync` is now public, for a host's own navigation.

## 6. Smaller changes

| Change | Instead |
|--------|---------|
| `AppRoutes.Dashboard`, `Accounts`, `Users`, `Groups`, `Roles`, `Permissions`, `Audit`, `Profile`, `AcceptInvitation`, `PlatformSettings*` removed | Your own routes. `AppRoutes` keeps the authentication endpoints and gains `SwitchAccount` |
| The sign in flow landed on `/` | It lands on `IAMWebOptions.HomePath`, default `/` |
| Sign in ignored `ReturnUrl` | Sign in, MFA, Google sign in, account selection and switching return to a local `returnUrl` |
| `IPostAuthenticationFlowService.CompleteSignInAsync(httpContext, result)` | Takes an optional `returnUrl` |
| `IAuthCookieManager` | Gains `SetReturnUrlCookie` and `TakeReturnUrlCookie`, for Google sign in |
| `EffectivePermissionsPanel` | Requires `AccountId`, for its links |
| `LoggingErrorBoundary`'s Go Home | Goes to `IAMWebOptions.HomePath` |
| `_AuthLayout`'s Profile link and Select Account's invitation link | Shown when the host registers `IIamWebLinks` |
| `AuthenticatedPageBase` | Gains `CurrentAccountId` |
