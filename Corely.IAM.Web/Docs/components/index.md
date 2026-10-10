# Shared Components

Reusable Blazor components in the `Corely.IAM.Web.Components.Shared` namespace.

## Component Inventory

| Component | Purpose | Key Parameters |
|-----------|---------|----------------|
| `PermissionView` | Authorization gate: show/hide UI by CRUDX | `Action`, `Resource`, `ResourceIds`, `Undetermined` |
| `EntityPickerModal` | Multi-select modal with search and pagination | `FetchItemsAsync`, `ExcludeIds`, `OnConfirm` |
| `FormModal` | Generic form modal with confirm/cancel | `Title`, `ChildContent`, `OnConfirm` |
| `ConfirmModal` | Destructive action confirmation | `Title`, `Message`, `Type`, `OnConfirm` |
| `EffectivePermissionsPanel` | Permission tree with role/group derivation | `Permissions` |
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
`AuthenticatedContent`, `AccountHeader` and `NavBar` work this way; components that only render what they
are given, such as `EncryptionSigningPanel`, do not need to.

```csharp
private IMfaService MfaService => ScopedServices.GetRequiredService<IMfaService>();
```

Never `@inject` a scoped service that reaches the database into such a component: property injection
resolves from the tab's scope even inside `OwningComponentBase`.

## Topics

- [PermissionView](permission-view.md)
- [EntityPickerModal](entity-picker-modal.md)
- [FormModal](form-modal.md)
- [ConfirmModal](confirm-modal.md)
- [EffectivePermissionsPanel](effective-permissions.md)
- [EncryptionSigningPanel](encryption-signing.md)
