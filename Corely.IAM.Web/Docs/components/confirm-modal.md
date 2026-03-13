# ConfirmModal

Confirmation dialog for destructive or important actions.

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Title` | `string` | `"Confirm"` | Modal header |
| `Message` | `string` | `"Are you sure?"` | Body text (red if `Type == Danger`) |
| `ConfirmText` | `string` | `"Confirm"` | Confirm button label |
| `Type` | `ConfirmModalType` | `Danger` | Button style: `Danger`, `Warning`, `Primary` |
| `OnConfirm` | `EventCallback` | — | Fired on confirm |
| `OnCancel` | `EventCallback` | — | Fired on cancel or backdrop click |

## Usage

```razor
<ConfirmModal @ref="_deleteModal"
    Title="Delete Group"
    Message="@_confirmMessage"
    ConfirmText="Delete"
    OnConfirm="DeleteGroupAsync" />
```

Open with `_deleteModal.Show()`. Typically used with `EntityPageBase.ShowConfirmation()`:

```csharp
ShowConfirmation(_deleteModal, groupId, $"Delete group '{group.Name}'?");
```

## Behavior

- Buttons disabled during `OnConfirm` execution
- Closes automatically after confirm or cancel
- Backdrop click triggers cancel (disabled while loading)
