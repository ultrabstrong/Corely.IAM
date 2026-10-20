# Corely.IAM.Web: components only, the host owns every path

Status: planned 2026-10-20, not started. First of three plans in order: this one, then
`Corely.Billing/Plans/web-components-only.md`, then `DocsToData/Plans/New/account-scoped-routes.md`.

## Why

Corely.IAM.Web ships routed pages: `/users`, `/groups/{id}` and the rest, each with its logic, its
address and hardcoded links to other IAM pages (`AppRoutes.Users`). A plug in library deciding where
the host's pages live is the wrong way round, and it is why DocsToData cannot put the account in its
URLs (`/accounts/<id>/users`) without IAM changing.

Corely.Billing.Web came later and does it right: components do the work, the host supplies every
link. This plan brings IAM.Web to the same shape and goes one step further (owner's decision,
2026-10-20): **the library ships components only, no routed Blazor pages at all.** Every host writes
a short page shell per page; the docs show the shell, and the WebApp holds a working one for every
page. That is where component libraries have settled (MudBlazor, Radzen, Clerk), and where Microsoft
moved ASP.NET Core Identity in .NET 8, from shipped pages to pages generated into the app.

Two things come with it, because IAM is the library that knows about accounts:

1. **An account route guard**, so a host can put the account in its paths safely.
2. **Sign in that returns where you were going.** Today it never does (below), which breaks any deep
   link, such as an email's.

## What changes for a host

An app moving to this version writes one shell per IAM page it uses and registers its links. In
exchange it chooses every path. Nothing else about authentication, authorization or services
changes: the signed token stays the only source of the current account.

## Design

### 1. Each page becomes a component

Every Blazor page under `Corely.IAM.Web/Components/Pages` loses its `@page` and becomes a component
with the same name minus any `Page` suffix, in `Corely.IAM.Web/Components/` grouped as now:

| Today (route) | Becomes |
|---|---|
| `Users/UserList.razor` (`/users`) | `UserList` |
| `Users/UserDetail.razor` (`/users/{Id}`) | `UserDetail` with `[Parameter, EditorRequired] Guid Id` |
| `Groups/GroupList`, `Groups/GroupDetail` | same pattern |
| `Roles/RoleList`, `Roles/RoleDetail` | same pattern |
| `Permissions/PermissionList`, `Permissions/PermissionDetail` | same pattern |
| `Audit/AuditLog`, `Audit/AuditEntryDetail` | same pattern |
| `Accounts/AccountDetail` (`/accounts/{Id}`) | `AccountDetail` |
| `Profile` (`/profile`) | `Profile` |
| `Invitations/AcceptInvitation` (`/accept-invitation`) | `AcceptInvitation` |
| `Platform/PlatformAuditSettingsPage`, `Platform/PlatformSecuritySettingsPage` | `PlatformAuditSettings`, `PlatformSecuritySettings` |
| `Platform/PlatformSettingsRedirect` (`/platform-settings`) | `PlatformSettingsRedirect`, a component the host's shell renders |
| `Platform/PlatformSettingsFrame` | unchanged, already a component |

Rules for the move:

1. **Logic stays where it is.** The base classes (`AuthenticatedPageBase`, `EntityPageBase`,
   `EntityListPageBase`, `EntityDetailPageBase`) keep working for components; rename only if a name
   now misleads, and say so in the migration guide.
2. **Titles move to the shell.** A component renders no `<PageTitle>`; the host's shell does, so the
   host's naming ("Users, DocsToData Admin") is its own. Headings inside the component stay.
3. **A query string a page reads** (`[SupplyParameterFromQuery]`, e.g. the invitation token) becomes
   a parameter the shell passes, or stays as query reading if it truly belongs to the component;
   decide per page and note it in the docs.

### 2. Links come from the host: `IIamWebLinks`

Every link a component renders to another IAM page goes through one host supplied interface, in
`Corely.IAM.Web` (public, a host implements it):

```csharp
public interface IIamWebLinks
{
    string Dashboard();
    string Profile();
    string Account(Guid accountId);
    string Users(Guid accountId);
    string User(Guid accountId, Guid userId);
    string Groups(Guid accountId);
    string Group(Guid accountId, Guid groupId);
    string Roles(Guid accountId);
    string Role(Guid accountId, Guid roleId);
    string Permissions(Guid accountId);
    string Permission(Guid accountId, Guid permissionId);
    string Audit(Guid accountId);
    string AuditEntry(Guid accountId, Guid entryId);
}
```

1. **Account scoped links take the account id** (the component reads it from `UserContext`), so a
   host can put it in the path or ignore it.
2. **No default implementation.** `AddIAMWebBlazor` requires one: `AddIAMWebBlazor<TLinks>()` or a
   registration the startup validation checks, failing with a message naming `IIamWebLinks`. A
   default would be a default path, which is the thing being removed.
3. **Replace every `AppRoutes.<page>` use** in components and shared components (56 uses across 21
   files when this was written: `grep "AppRoutes\." Corely.IAM.Web/Components`). The navigation in
   `Components/Layout/NavBar.razor` and `Footer.razor` is the host's to own as well: see 5.
4. `PlatformSettingsPage` paths in `IAMWebOptions.PlatformSettingsPages` stay as they are (already
   host supplied); the two built in entries' default paths go, and the host adds its own entries
   with its own paths, documented.

### 3. `AppRoutes` keeps only the endpoints

The Razor Pages under `Corely.IAM.Web/Pages` (sign in, register, sign out, select account, create
account, verify MFA, Google callback and registration, switch account, privacy, terms) are endpoints
of the authentication flow, not pages a host lays out, and keep fixed routes (owner's call). So
`AppRoutes` keeps `SignIn`, `Register`, `SignOut`, `SelectAccount`, `CreateAccount`, `VerifyMfa`,
`GoogleCallback`, `RegisterWithGoogle`, `SwitchAccount` (add it: `/switch-account`, used as a
literal today), `Privacy`, and `Terms`. Fixed addresses are right for this layer, not merely cheaper
(owner's question, 2026-10-20): signing in sets a cookie, which only a real HTTP request can do, so
these are endpoints the library serves; ASP.NET Identity, OIDC callbacks and hosted sign in pages
all work this way. If a host ever needs them under another prefix, the answer is a prefix option,
not components; not built now.

Where the flow lands with nowhere else to go becomes `IAMWebOptions.HomePath`, default `/`, so a host
can override it (owner's decision). Every redirect that uses `AppRoutes.Dashboard` today reads it
instead, and `Dashboard` leaves `AppRoutes`. The page constants (`Accounts`, `Users`, `Groups`, `Roles`, `Permissions`, `Audit`, `Profile`,
`AcceptInvitation`, `PlatformSettings*`) go.

Code in the library that redirects to a page after an action (for instance
`AcceptInvitation.razor` sending to `/switch-account?accountId=…&returnUrl=/`) uses
`IIamWebLinks.Dashboard()` or the account's link instead of `/`. `IIamWebLinks.Dashboard()` is the
host's dashboard page; `IAMWebOptions.HomePath` is where the authentication flow lands; a host
usually makes them the same.

### 4. `AccountScope`: the account route guard

A component a host wraps around an account page's content:

```razor
<AccountScope AccountId="AccountId">
    <UserList />
</AccountScope>
```

1. It compares `AccountId` with `UserContext.CurrentAccount.Id`.
2. **Equal:** renders its child content.
3. **Different, or no current account:** navigates with `forceLoad: true` to
   `/switch-account?accountId={AccountId}&returnUrl={the current relative URI}`. `SwitchAccount`
   already checks access, sets the cookies and refuses a non local `returnUrl`; a refused switch
   lands on `SelectAccount`. Nothing is rendered meanwhile but a spinner.
4. **The route's account is never used for data.** Components keep reading the account from
   `UserContext`. Put this in `Docs/` and in this repository's `CLAUDE.md` as a rule: a component
   that took its account from a route parameter would make a URL an authorization input.
5. The current account is one per browser, as today: switching issues a token for the new account
   and the one cookie holds it, so every tab follows. The guard makes switching more frequent, not
   less safe. Document it as how IAM works (the AWS and Azure consoles work the same way); it is not
   a planned change (owner's decision).

### 5. Sign in returns to the page that sent you

Today a signed out request loses its destination: the cookie middleware redirects to
`/signin?ReturnUrl=…`, but `SignIn` ignores it, and `RedirectToLogin` and `AuthenticatedPageBase`
navigate to `AppRoutes.SignIn` with nothing attached. After sign in, `PostAuthenticationFlowService`
always goes to the dashboard or account selection.

1. `RedirectToLogin` and `AuthenticatedPageBase` pass the current relative URI as `returnUrl`.
2. `SignIn` reads `returnUrl` (both `ReturnUrl` and `returnUrl`; the cookie middleware uses the
   first), keeps it through the MFA step (the challenge already rides in `TempData`; the return URL
   rides beside it) and through Google sign in (`state`, or a short lived cookie, whichever the Google
   flow allows), and hands it to `PostAuthenticationFlowService`.
3. `PostAuthenticationFlowService.CompleteSignInAsync` takes an optional return URL. When it is local:
   with one account, switch as now and redirect to it; with several, redirect to it anyway (the
   page's `AccountScope` switches into the right account); with none, the dashboard. When it is
   absent, today's behaviour.
4. `SelectAccount` carries a `returnUrl` through to `SwitchAccount`.
5. Every place that accepts a return URL checks `Url.IsLocalUrl`, as `SwitchAccount` already does.
   An open redirect here would be a phishing tool.

### 6. The navigation belongs to the host

`Components/Layout/MainLayout.razor`, `NavBar.razor` and `Footer.razor` link to IAM's pages
directly. A host already supplies its own layout (DocsToData does). Either:

1. move them to the WebApp as its layout, keeping only reusable pieces (`ThemeToggle`,
   `AuthenticatedContent`, `AccountHeader`) in the library, or
2. keep them, rewritten on `IIamWebLinks`, as optional building blocks.

Decided: option 1, for the same reason the pages go (owner, 2026-10-20).

### 7. The WebApp becomes the reference host

`Corely.IAM.WebApp` gets a shell for every component above, at **account scoped paths**
(`/accounts/{AccountId:guid}/users`, `/accounts/{AccountId:guid}/users/{Id:guid}`, …; `/` the
dashboard, `/profile`, `/accept-invitation`, `/platform-settings/...`), each wrapping its component in
`AccountScope`, plus its `IIamWebLinks` implementation. It is the working example the docs point to.

A shell looks like this; the docs carry it verbatim:

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

The `Demos.UsersOnly` and `Demos.SharedAccount` hosts use the Razor Pages and not the Blazor pages,
so they change only if they reference something removed. Check with a build.

## Documentation

`Corely.IAM.Web/Docs/` describes the current version only (this repository's rules, and
`DOCUMENTATION-STYLE.md`):

1. `pages/` becomes `components/` entries (merge into the existing `components/` folder), one per
   component: what it shows, its parameters, the permissions it needs, and its shell.
2. A new `hosting-pages.md`: writing shells, registering `IIamWebLinks`, `AccountScope`, the rule
   that the account comes from the token, account scoped paths as the recommended shape, and the
   two tab behaviour.
3. `authentication-flow.md`: return URLs through sign in, MFA, Google and account selection.
4. `setup.md` and `index.md` updated for `AddIAMWebBlazor<TLinks>()` and no routed pages.
5. A migration guide at the repository root (the only place versions may be named): "moving to
   Corely.IAM.Web 7": write shells, implement `IIamWebLinks`, add `AccountScope`, drop the IAM.Web
   assembly from the router's `AdditionalAssemblies` and `AddAdditionalAssemblies` (the Razor Pages
   still map through `MapRazorPages`).

## Tests

Walk the tiers as `CLAUDE.md` says; expected placement:

1. **Unit (bUnit in `Corely.IAM.Web.UnitTests`):**
   - `AccountScope`: renders the child when the accounts match; navigates to
     `/switch-account?accountId=…&returnUrl=…` (encoded, relative) when they differ or there is no
     current account; never renders the child in that case.
   - Each moved component renders its links through `IIamWebLinks` (a fake recording calls), with no
     hardcoded path left. A test that scans the assembly for `@page` (no routed Blazor page remains)
     and for `AppRoutes.` page constants keeps it that way.
   - `PostAuthenticationFlowService`: a local return URL is honoured for one, several and no
     accounts; a non local one is ignored.
   - `PublicSurfaceTests` updated: `IIamWebLinks` and `AccountScope` are public roots.
2. **Functional (`Corely.IAM.Web.FunctionalTests`, booting the WebApp):**
   - an anonymous request to an account scoped page redirects to sign in with the return URL; signing
     in (with and without MFA) lands on that page;
   - a request for another account's page the user can enter switches and lands there; one the user
     cannot enter lands on account selection;
   - `returnUrl=https://evil.example` never redirects off site, at every step that takes one.
   - Existing functional tests move to the WebApp's new paths.
3. Prove a guard test catches its regression: break the comparison, watch it go red, restore.

## Release

1. `Corely.IAM.Web` **7.0.0**: removing routed pages and page constants is breaking. `Corely.IAM`
   itself does not change unless something in it has to; if it does, it releases first (a stable
   package never depends on a preview one).
2. **Before tagging, verify against DocsToData with a local package feed** (owner's go ahead): `dotnet pack` to a local
   folder, point a DocsToData working copy at it (`nuget.config` local source, not committed), and
   check the portal builds and its shells work. Only then ask the owner to tag. This avoids
   publishing a version that turns out unusable; a published version cannot be deleted.
3. Tag `Corely.IAM.Web-v7.0.0` **only with the owner's say so**, after `.\RebuildAndTest.ps1` is green
   and this plan has moved to `Plans/Completed/` (no package is tagged while a plan is outstanding).
4. Then Corely.Billing's plan, whose `Corely.Billing.Web.IAM` references Corely.IAM.Web.

## Decided with the owner (2026-10-20)

1. Section 6: the library's `MainLayout`, `NavBar` and `Footer` move out to the WebApp.
2. `IIamWebLinks` is one interface.
3. `/` is the default landing path, overridable through `IAMWebOptions.HomePath`.
4. Return URLs through the whole sign in flow are in scope.
5. One current account per browser stays, documented as how it works.
6. The authentication Razor Pages keep fixed routes.

Left to the building session: names of the moved components where a `Page` suffix or a clash with a
host's own page appears. Record them under As built.
