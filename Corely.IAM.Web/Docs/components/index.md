# Components

The management components a host places in its own page shells ([Hosting Pages](../hosting-pages.md)),
and the shared components they are built from.

## Management Components

| Component | Namespace | Doc |
|-----------|-----------|-----|
| `AccountDetail` | `Corely.IAM.Web.Components.Accounts` | [Accounts](accounts.md) |
| `UserList`, `UserDetail` | `Corely.IAM.Web.Components.Users` | [Users](users.md) |
| `GroupList`, `GroupDetail` | `Corely.IAM.Web.Components.Groups` | [Groups](groups.md) |
| `RoleList`, `RoleDetail` | `Corely.IAM.Web.Components.Roles` | [Roles](roles.md) |
| `PermissionList`, `PermissionDetail` | `Corely.IAM.Web.Components.Permissions` | [Permissions](permissions.md) |
| `ApiKeyList`, `ApiKeyDetail` | `Corely.IAM.Web.Components.ApiKeys` | [API keys](api-keys.md) |
| `AuditLog`, `AuditEntryDetail` | `Corely.IAM.Web.Components.Audit` | [Audit](audit.md) |
| `AcceptInvitation` | `Corely.IAM.Web.Components.Invitations` | [Invitations](invitations.md) |
| `Profile` | `Corely.IAM.Web.Components` | [Profile](profile.md) |
| `PlatformSettingsRedirect`, `PlatformAuditSettings`, `PlatformSecuritySettings`, `PlatformSettingsFrame` | `Corely.IAM.Web.Components.Platform` | [Audit](audit.md#platform-settings) |

Every association between these pages can be added and removed from both sides. Each side takes the
permission the service checks, which belongs to the item being changed, so a role page's Add Users
needs Update on users and the bound on what the caller can grant.

| Association | Pages |
|-------------|-------|
| User and group | `UserDetail`, `GroupDetail` |
| User and role | `UserDetail`, `RoleDetail` |
| Group and role | `GroupDetail`, `RoleDetail` |
| API key and role | `ApiKeyDetail`, `RoleDetail` |
| API key and group | `ApiKeyDetail`, `GroupDetail` |
| Role and permission | `RoleDetail`, `PermissionDetail` |
| User and account | `AccountDetail` invites and removes; `AcceptInvitation` joins, `Profile` leaves |

## Shared Components

Reusable Blazor components in the `Corely.IAM.Web.Components.Shared` namespace.

## Component Inventory

| Component | Purpose | Key Parameters |
|-----------|---------|----------------|
| `PermissionView` | Authorization gate: show/hide UI by CRUDX | `Action`, `Resource`, `ResourceIds`, `Undetermined` |
| `EntityPickerModal` | Multi-select modal with search and pagination | `FetchItemsAsync`, `ExcludeIds`, `OnConfirm` |
| `ResourceTypePicker` | Grouped, searchable resource types as a list or cards | `Types`, `Multiple`, `Cards`, `ReadOnly` |
| `FormModal` | Generic form modal with confirm/cancel | `Title`, `ChildContent`, `OnConfirm` |
| `ConfirmModal` | Destructive action confirmation | `Title`, `Message`, `Type`, `OnConfirm` |
| `EffectivePermissionsPanel` | Permission tree with role/group derivation | `AccountId`, `Permissions` |
| `AccountScope` | Renders its content only in the account its route names, switching first | `AccountId`, `ChildContent` |
| `EncryptionSigningPanel` | Tabbed crypto provider UI for testing | `SymProvider`, `AsymProvider`, `SigProvider` |
| `Alert` | Dismissible Bootstrap alert | `Message`, `Type`, `Dismissible` |
| `Pagination` | Page navigation control | `Skip`, `Take`, `TotalCount`, `OnPageChanged` |
| `LoadingSpinner` | Full-screen loading overlay | `Visible` |
| `AuthenticatedContent` | Delays rendering until auth loaded | `ChildContent` |
| `LoggingErrorBoundary` | Error boundary with logging and recovery | `ChildContent` |

## Services and scopes

A component that calls services inherits `OwningComponentBase`, takes those services from
`ScopedServices`, and signs its own scope in through `IBlazorUserContextAccessor` resolved from the same
scope. Blazor Server shares one DI scope across a browser tab, so without this every component in the tab
would use one `DbContext`, and two loading together fail with "A second operation was started on this
context instance". `PermissionView`, `TotpSection`, `PasswordSection`, `LinkedAccountsSection`,
`AuthenticatedContent`, `AccountHeader` and `AccountScope` work this way; components that only render what they
are given, such as `EncryptionSigningPanel`, do not need to.

```csharp
private IMfaService MfaService => ScopedServices.GetRequiredService<IMfaService>();
```

Never `@inject` a scoped service that reaches the database into such a component: property injection
resolves from the tab's scope even inside `OwningComponentBase`.

## Topics

- [Accounts](accounts.md)
- [Users](users.md)
- [Groups](groups.md)
- [Roles](roles.md)
- [Permissions](permissions.md)
- [API keys](api-keys.md)
- [Invitations](invitations.md)
- [Profile](profile.md)
- [Audit and platform settings](audit.md)
- [PermissionView](permission-view.md)
- [EntityPickerModal](entity-picker-modal.md)
- [ResourceTypePicker](resource-type-picker.md)
- [FormModal](form-modal.md)
- [ConfirmModal](confirm-modal.md)
- [EffectivePermissionsPanel](effective-permissions.md)
- [EncryptionSigningPanel](encryption-signing.md)
