# Hosting Pages

Corely.IAM.Web ships components, not routed pages. The host writes a short page per component (a
shell), chooses its path, and tells the components where every other page lives through
`IIamWebLinks`. `Corely.IAM.WebApp` has a shell for every component and is the working example.

## A shell

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

The title is the host's; the component renders its own heading but no `<PageTitle>`. A detail
component takes the entity id from the shell:

```razor
@page "/accounts/{AccountId:guid}/users/{Id:guid}"
@rendermode @(new InteractiveServerRenderMode(prerender: false))

<PageTitle>User</PageTitle>

<AccountScope AccountId="AccountId">
    <UserDetail Id="Id" />
</AccountScope>

@code {
    [Parameter] public Guid AccountId { get; set; }
    [Parameter] public Guid Id { get; set; }
}
```

Shells need `[Authorize]`, so a signed out request is sent to sign in with the page as its return
URL. An `_Imports.razor` beside them can carry it once:

```razor
@attribute [Authorize]
```

| Component | Account scoped | Parameters |
|-----------|----------------|------------|
| `AccountDetail` | yes | none |
| `UserList`, `GroupList`, `RoleList`, `PermissionList`, `AuditLog` | yes | none |
| `UserDetail`, `GroupDetail`, `RoleDetail`, `PermissionDetail`, `AuditEntryDetail` | yes | `Id` |
| `Profile` | no | none |
| `AcceptInvitation` | no | none; reads `?token=` itself |
| `PlatformSettingsRedirect`, `PlatformAuditSettings`, `PlatformSecuritySettings` | no | none |

## Links: `IIamWebLinks`

Every link a component renders to another IAM page comes from the host's `IIamWebLinks`. There is
no default, because a default would be a path chosen by the library.

```csharp
internal sealed class PortalIamLinks : IIamWebLinks
{
    public string Dashboard() => "/";
    public string Profile() => "/profile";
    public string AcceptInvitation() => "/accept-invitation";
    public string Account(Guid accountId) => $"/accounts/{accountId}";
    public string Users(Guid accountId) => $"/accounts/{accountId}/users";
    public string User(Guid accountId, Guid userId) => $"/accounts/{accountId}/users/{userId}";
    // Groups, Group, Roles, Role, Permissions, Permission, Audit, AuditEntry the same way
}

builder.Services.AddIAMWebBlazor<PortalIamLinks>();
```

Account scoped links take the account id, which the component reads from the signed in user's
context; a host that keeps the account out of its paths ignores it. `Dashboard()` is the host's
dashboard page. `IAMWebOptions.HomePath` (default `/`) is where the sign in flow lands when it has
nowhere better to go; a host usually makes them the same.

A host that renders none of the linking components (only `ThemeToggle`, `TotpSection` and the
other building blocks) can call `AddIAMWebBlazor()` with no links. A component that needs
`IIamWebLinks` then fails to render, naming the missing service.

Platform settings pages are host paths too, added to `IAMWebOptions.PlatformSettingsPages`; see
[Audit](components/audit.md#platform-settings).

## The account comes from the token: `AccountScope`

Putting the account in the path (`/accounts/<id>/users`) is the recommended shape: a link opens the
right account for anyone who can enter it. `AccountScope` makes that safe:

1. It compares its `AccountId` with the signed in user's current account.
2. Equal: it renders its child content.
3. Different, or no current account: it navigates (full page load) to
   `/switch-account?accountId=<id>&returnUrl=<this page>`. Switching checks access, issues a token
   for that account and comes back; a refused switch lands on account selection.
4. Signed out: it sends the user to sign in with this page as the return URL.

Nothing renders but a spinner until the accounts match.

**The route's account is never used for data.** Components load everything for the account in the
signed token (`UserContext.CurrentAccount`), never for the id in the path; `AccountScope` only
guarantees the two agree before anything renders. A component that took its account from a route
parameter would turn a URL into an authorization input.

## One current account per browser

The current account lives in the auth cookie, so it is one per browser, not per tab. Opening a
second account's page in another tab switches the browser into that account, and the first tab
follows it on its next load (where its own `AccountScope` switches back). This is how IAM works,
as the AWS and Azure consoles do; account scoped paths make switching more frequent, not less safe.

## Layout and navigation

The library has no layout or navigation bar. The host writes its own; these pieces are reusable in
it: `ThemeToggle`, `AuthenticatedContent`, `AccountHeader`, `LoggingErrorBoundary` and
`RedirectToLogin`. The router routes only the host's assembly:

```razor
<Router AppAssembly="typeof(Routes).Assembly">
    <Found Context="routeData">
        <AuthorizeRouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)">
            <NotAuthorized><RedirectToLogin /></NotAuthorized>
        </AuthorizeRouteView>
    </Found>
</Router>
```
