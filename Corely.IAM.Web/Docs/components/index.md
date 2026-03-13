# Shared Components

Reusable Blazor components in the `Corely.IAM.Web.Components.Shared` namespace.

## Component Inventory

| Component | Purpose | Key Parameters |
|-----------|---------|----------------|
| `PermissionView` | Authorization gate — show/hide UI by CRUDX | `Action`, `Resource`, `ResourceIds` |
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

## Topics

- [PermissionView](permission-view.md)
- [EntityPickerModal](entity-picker-modal.md)
- [FormModal](form-modal.md)
- [ConfirmModal](confirm-modal.md)
- [EffectivePermissionsPanel](effective-permissions.md)
- [EncryptionSigningPanel](encryption-signing.md)
