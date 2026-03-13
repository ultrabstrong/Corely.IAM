# FormModal

Generic modal wrapper for create forms with confirm/cancel buttons.

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Title` | `string` | `"Create"` | Modal header |
| `ConfirmText` | `string` | `"Create"` | Confirm button label |
| `ChildContent` | `RenderFragment?` | — | Form HTML in modal body |
| `OnConfirm` | `EventCallback` | — | Fired on confirm click |

## Usage

```razor
<FormModal @ref="_createModal" Title="Create Group" OnConfirm="CreateGroupAsync">
    <div class="mb-3">
        <label class="form-label">Name</label>
        <input @bind="_newGroupName" class="form-control" required />
    </div>
</FormModal>
```

Open with `_createModal.Show()`. Closes automatically after `OnConfirm` completes.

## Behavior

- Opens via `Show()`, closes via `Hide()`
- Buttons disabled during `OnConfirm` execution
- Always closes in `finally` block (even on error)
- Backdrop click triggers cancel (skipped if loading)
