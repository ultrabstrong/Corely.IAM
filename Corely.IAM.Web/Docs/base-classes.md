# Page Base Classes

Class hierarchy for all Blazor management pages in Corely.IAM.Web.

## Hierarchy

```
ComponentBase (Blazor)
└── AuthenticatedPageBase
    └── EntityPageBase
        ├── EntityListPageBase<T>
        └── EntityDetailPageBase
```

## AuthenticatedPageBase

Ensures user context is loaded. Redirects unauthenticated users to `/signin`.

| Member | Type | Description |
|--------|------|-------------|
| `UserContext` | `UserContext?` | Current authenticated user context |
| `IsAuthenticated` | `bool` | `true` if `UserContext?.User != null` |
| `OnInitializedAuthenticatedAsync()` | `virtual Task` | Override point — called after successful authentication |

**Behavior:** Sealed `OnInitializedAsync()` calls `BlazorUserContextAccessor.GetUserContextAsync()`. If not authenticated, redirects to sign-in with `forceLoad: true`.

## EntityPageBase

Centralized error handling, loading state, and confirmation dialog support.

| Member | Type | Description |
|--------|------|-------------|
| `_message` | `string?` | Alert message text |
| `_messageType` | `AlertType` | Alert severity |
| `_loading` | `bool` | Page-level loading state |
| `_confirmItemId` | `Guid` | ID pending confirmation |
| `_confirmMessage` | `string` | Confirmation dialog text |

| Method | Description |
|--------|-------------|
| `LoadCoreAsync()` | Abstract — load page data |
| `ReloadAsync()` | Wraps `LoadCoreAsync()` with loading state and error handling |
| `ExecuteSafeAsync(action)` | Wraps any async action with try-catch and loading state |
| `SetResultMessage(success, msg, failMsg)` | Sets alert state from operation result |
| `TryParseGuid(input, out result)` | Safe GUID parse with error alert on failure |
| `ShowConfirmation(modal, id, msg)` | Opens a `ConfirmModal` for a specific entity |

**Behavior:** Sealed `OnInitializedAuthenticatedAsync()` automatically calls `ReloadAsync()` on page load.

## EntityListPageBase\<T\>

Pagination, search, and sort for entity list pages. Implements `IAsyncDisposable`.

| Member | Type | Default | Description |
|--------|------|---------|-------------|
| `_items` | `List<T>?` | — | Current page of items |
| `_skip` | `int` | `0` | Pagination offset |
| `_take` | `int` | `25` | Page size |
| `_totalCount` | `int` | — | Total items from server |
| `_searchText` | `string` | `""` | Current search filter |
| `_sortColumn` | `string?` | — | Active sort column |
| `_sortDirection` | `SortDirection?` | — | Sort order |

| Method | Description |
|--------|-------------|
| `OnPageChangedAsync(newSkip)` | Updates offset and reloads |
| `OnSearchChangedAsync()` | Debounced (300ms) search — resets offset and reloads |
| `CycleSortAsync(column)` | Cycles through ascending → descending → none |
| `GetSortIcon(column)` | Returns Bootstrap icon class for sort state |
| `GetSortClass(column)` | Returns CSS class for sort state |

## EntityDetailPageBase

Route parameter binding for single-entity detail pages.

| Member | Type | Description |
|--------|------|-------------|
| `Id` | `Guid` | Route parameter (`[Parameter]`) |

## Usage

Create a custom list page:

```csharp
@page "/my-items"
@inherits EntityListPageBase<MyItem>

@code {
    [Inject] private IMyService MyService { get; set; } = null!;

    protected override async Task LoadCoreAsync()
    {
        var result = await MyService.ListAsync(_skip, _take, _searchText);
        _items = result.Items;
        _totalCount = result.TotalCount;
    }
}
```
