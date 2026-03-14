# Pages

Corely.IAM.Web provides two types of pages: **Razor Pages** for pre-authentication flows and **Blazor Server** pages for management.

## Route Table

| Route | Page | Type | Description |
|-------|------|------|-------------|
| `/signin` | SignIn | Razor Page | Login form |
| `/register` | Register | Razor Page | Registration form |
| `/select-account` | SelectAccount | Razor Page | Account selection with search/pagination |
| `/create-account` | CreateAccount | Razor Page | New account form |
| `/signout` | SignOut | Razor Page | Sign out and clear cookies |
| `/verify-mfa` | VerifyMfa | Razor Page | MFA code entry after sign-in |
| `/google-callback` | GoogleCallback | Razor Page | Google Identity Services callback |
| `/privacy` | Privacy | Razor Page | Privacy policy |
| `/terms` | Terms | Razor Page | Terms of service |
| `/` | Dashboard | Blazor | Account overview |
| `/accept-invitation` | AcceptInvitation | Blazor | Token-based invitation acceptance |
| `/profile` | Profile | Blazor | User profile + encryption keys |
| `/accounts/{Id}` | AccountDetail | Blazor | Account detail, invitations, users, keys |
| `/users` | UserList | Blazor | User table with search/sort |
| `/users/{Id}` | UserDetail | Blazor | User detail + group/role assignment |
| `/groups` | GroupList | Blazor | Group table with search/sort |
| `/groups/{Id}` | GroupDetail | Blazor | Group detail + user/role management |
| `/roles` | RoleList | Blazor | Role table with search/sort |
| `/roles/{Id}` | RoleDetail | Blazor | Role detail + permission assignment |
| `/permissions` | PermissionList | Blazor | Permission table + create |
| `/permissions/{Id}` | PermissionDetail | Blazor | Permission detail (read-only) |

## AppRoutes Class

All routes are defined as string constants in `Corely.IAM.Web.AppRoutes`:

```csharp
public static class AppRoutes
{
    public const string SignIn = "/signin";
    public const string Register = "/register";
    public const string SignOut = "/signout";
    public const string SelectAccount = "/select-account";
    public const string CreateAccount = "/create-account";
    public const string VerifyMfa = "/verify-mfa";
    public const string GoogleCallback = "/google-callback";
    public const string Dashboard = "/";
    public const string AcceptInvitation = "/accept-invitation";
    public const string Profile = "/profile";
    public const string Accounts = "/accounts";
    public const string Users = "/users";
    public const string Groups = "/groups";
    public const string Roles = "/roles";
    public const string Permissions = "/permissions";
}
```

## Render Modes

- **Razor Pages** handle pre-authentication flows (sign in, register, account selection). These are traditional server-rendered pages with no Blazor dependency.
- **Blazor Server pages** use `InteractiveServerRenderMode(prerender: false)` for real-time interactivity. Prerender is disabled to avoid double-initialization issues with authentication state.

## Topics

- [Accounts](accounts.md)
- [Users](users.md)
- [Groups](groups.md)
- [Roles](roles.md)
- [Permissions](permissions.md)
- [Invitations](invitations.md)
- [Profile](profile.md)
- [Dashboard](dashboard.md)
