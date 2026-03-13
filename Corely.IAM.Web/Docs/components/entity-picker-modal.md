# EntityPickerModal

Multi-select modal for picking entities from a paginated, searchable list. Used on detail pages to add users to groups, roles to users, permissions to roles, etc.

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Title` | `string` | `"Select Items"` | Modal header |
| `SearchPlaceholder` | `string` | `"Search..."` | Input placeholder |
| `FetchItemsAsync` | `Func<int, int, Task<(List<PickerItem>, int)>>` | — | Server fetch callback `(skip, take)` → `(items, total)` |
| `ExcludeIds` | `IReadOnlyCollection<Guid>` | `[]` | IDs to exclude (already-assigned items) |
| `OnConfirm` | `EventCallback<List<Guid>>` | — | Fired with selected IDs |

## PickerItem

```csharp
public record PickerItem(Guid Id, string DisplayText, string? Description = null);
```

## Usage

```razor
<EntityPickerModal @ref="_rolePicker"
    Title="Add Roles"
    FetchItemsAsync="FetchAvailableRolesAsync"
    ExcludeIds="_existingRoleIds"
    OnConfirm="OnRolesSelected" />
```

## Behavior

- Opens via `ShowAsync()` — resets state and loads initial batch
- Server-side pagination (25 per batch) with "Load More" button
- Client-side search filtering on cached items (debounced 300ms)
- Checkbox selection with selected count in footer
- "Add Selected (N)" button enabled only when items are checked
- Scrollable list (max-height 400px)
