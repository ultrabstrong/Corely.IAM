# Corely.IAM.Web Documentation

Blazor Server UI for Corely.IAM: the authentication pages, and components for multi-tenant account
management, RBAC visualization and permission CRUD, all with authorization gating. The library ships
components only; the host writes a short page per component and owns every path.

- **Complete auth flow**: sign in, register, sign out, account switching, MFA verification, Google Sign-In (Razor Pages at fixed routes), returning to the page that sent you
- **Management components**: account, user, group, role, permission and audit components (Blazor Server), placed by the host
- **Account scoped paths**: `AccountScope` lets a host put the account in its URLs safely
- **Authorization gates**: `PermissionView` component hides UI based on CRUDX permissions
- **Effective permissions**: visualize permission grants through roles and groups
- **Customizable styling**: Bootstrap-first CSS with scoped utility classes

## Topics

- [Setup](setup.md)
- [Hosting Pages](hosting-pages.md)
- [Authentication Flow](authentication-flow.md)
- [Authorization UI](authorization-ui.md)
- [Components](components/index.md)
    - [Accounts](components/accounts.md)
    - [Users](components/users.md)
    - [Groups](components/groups.md)
    - [Roles](components/roles.md)
    - [Permissions](components/permissions.md)
    - [API keys](components/api-keys.md)
    - [Invitations](components/invitations.md)
    - [Profile](components/profile.md)
    - [Audit and platform settings](components/audit.md)
    - [PermissionView](components/permission-view.md)
    - [EntityPickerModal](components/entity-picker-modal.md)
    - [FormModal](components/form-modal.md)
    - [ConfirmModal](components/confirm-modal.md)
    - [EffectivePermissionsPanel](components/effective-permissions.md)
    - [EncryptionSigningPanel](components/encryption-signing.md)
- [Base Classes](base-classes.md)
- [Middleware](middleware.md)
- [Bearer API](bearer-api.md)
- [Services](services.md)
- [Styling](styling.md)
- [Security](security.md)

## Quick Start

```csharp
// Program.cs
builder.Services.AddIAMWeb();                       // Cookie auth, Razor Pages, auth services
builder.Services.AddIAMWebBlazor<MyIamLinks>();     // Blazor auth state, the host's IIamWebLinks

var iamOptions = IAMOptions.Create(builder.Configuration, securityConfigProvider, efConfig);
builder.Services.AddIAMServices(iamOptions);

var app = builder.Build();
app.UseIAMWebAuthentication();       // CorrelationId → AuthToken → Auth → Authz

app.MapRazorPages();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
```

Then a shell per page, as [Hosting Pages](hosting-pages.md) shows.
