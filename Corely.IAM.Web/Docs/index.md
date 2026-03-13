# Corely.IAM.Web Documentation

Pre-built Blazor Server UI for Corely.IAM. Provides authentication pages, multi-tenant account management, RBAC visualization, and permission CRUD — all with authorization gating.

- **Complete auth flow** — sign in, register, sign out, account switching (Razor Pages)
- **Multi-tenant management** — account selection, user/group/role/permission CRUD (Blazor Server)
- **Authorization gates** — `PermissionView` component hides UI based on CRUDX permissions
- **Effective permissions** — visualize permission grants through roles and groups
- **Security headers** — CSP, HSTS, X-Frame-Options out of the box
- **Customizable styling** — Bootstrap-first CSS with scoped utility classes

## Topics

- [Setup](setup.md)
- [Authentication Flow](authentication-flow.md)
- [Authorization UI](authorization-ui.md)
- [Pages](pages/index.md)
    - [Dashboard](pages/dashboard.md)
    - [Accounts](pages/accounts.md)
    - [Users](pages/users.md)
    - [Groups](pages/groups.md)
    - [Roles](pages/roles.md)
    - [Permissions](pages/permissions.md)
    - [Invitations](pages/invitations.md)
    - [Profile](pages/profile.md)
- [Components](components/index.md)
    - [PermissionView](components/permission-view.md)
    - [EntityPickerModal](components/entity-picker-modal.md)
    - [FormModal](components/form-modal.md)
    - [ConfirmModal](components/confirm-modal.md)
    - [EffectivePermissionsPanel](components/effective-permissions.md)
    - [EncryptionSigningPanel](components/encryption-signing.md)
- [Base Classes](base-classes.md)
- [Middleware](middleware.md)
- [Services](services.md)
- [Styling](styling.md)
- [Security](security.md)

## Quick Start

```csharp
// Program.cs
builder.Services.AddIAMWeb();        // Cookie auth, Razor Pages, auth services
builder.Services.AddIAMWebBlazor();  // Blazor auth state, user context accessor

var iamOptions = IAMOptions.Create(builder.Configuration, securityConfigProvider, efConfig);
builder.Services.AddIAMServices(iamOptions);

var app = builder.Build();
app.UseIAMWebAuthentication();       // CorrelationId → SecurityHeaders → AuthToken → Auth → Authz

app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddAdditionalAssemblies(typeof(Corely.IAM.Web.AppRoutes).Assembly)
    .AddInteractiveServerRenderMode();
```
