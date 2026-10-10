# Page Base Classes

Class hierarchy for all Blazor management pages in Corely.IAM.Web.

## Hierarchy

```
OwningComponentBase (Blazor)
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
| `OnInitializedAuthenticatedAsync()` | `virtual Task` | Override point, called after successful authentication |

**Behavior:** Sealed `OnInitializedAsync()` calls `BlazorUserContextAccessor.GetUserContextAsync()`. If not authenticated, redirects to sign-in with `forceLoad: true`.

### Services come from the page's own scope

Blazor Server keeps one DI scope per browser tab, so every component in the tab would otherwise share
one `DbContext`, and two components loading at once fail with "A second operation was started on this
context instance". Every page therefore owns a scope, created with the page and disposed with it, and
takes its services from `ScopedServices`:

```csharp
private IRetrievalService RetrievalService => ScopedServices.GetRequiredService<IRetrievalService>();
```

`OnInitializedAsync()` signs that scope in, so services resolved from it see the user. Do not `@inject`
or `[Inject]` a scoped service that reaches the database: property injection always resolves from the
tab's scope and brings the shared context back. Singletons, framework services (`NavigationManager`,
`IJSRuntime`, `ILogger<T>`, `IOptions<T>`) and `IAccountDisplayState`, which is shared across the tab on
purpose, stay injected.

## EntityPageBase

Centralized error handling, loading state, and confirmation dialog support.

| Member | Type | Description |
|--------|------|-------------|
| `_message` | `string?` | Alert message text |
| `_messageType` | `AlertType` | Alert severity |
| `_loading` | `bool` | Page-level loading state |
| `_loadFailed` | `bool` | The last load failed; a list page shows no spinner and no table |
| `_confirmItemId` | `Guid` | ID pending confirmation |
| `_confirmMessage` | `string` | Confirmation dialog text |

| Method | Description |
|--------|-------------|
| `LoadCoreAsync()` | Abstract: load page data |
| `ReloadAsync()` | Wraps `LoadCoreAsync()` with loading state and error handling |
| `FailLoad(resultCode, resourceName)` | Marks the load failed and says why: no access, or couldn't be loaded. Call it from `LoadCoreAsync()` when a retrieval doesn't succeed |
| `ExecuteSafeAsync(action)` | Wraps any async action with try-catch and loading state |
| `SetResultMessage(success, msg, failMsg)` | Sets alert state from operation result |
| `TryParseGuid(input, out result)` | Safe GUID parse with error alert on failure |
| `ShowConfirmation(modal, id, msg)` | Opens a `ConfirmModal` for a specific entity |

**Behavior:** Sealed `OnInitializedAuthenticatedAsync()` automatically calls `ReloadAsync()` on page load.

## EntityListPageBase\<T\>

Pagination, search, and sort for entity list pages. A derived page that disposes its own resources
overrides `DisposeAsyncCore()` and calls the base, which disposes the scope.

| Member | Type | Default | Description |
|--------|------|---------|-------------|
| `_items` | `List<T>?` | none | Current page of items |
| `_skip` | `int` | `0` | Pagination offset |
| `_take` | `int` | `25` | Page size |
| `_totalCount` | `int` | none | Total items from server |
| `_searchText` | `string` | `""` | Current search filter |
| `_sortColumn` | `string?` | none | Active sort column |
| `_sortDirection` | `SortDirection?` | none | Sort order |

| Method | Description |
|--------|-------------|
| `OnPageChangedAsync(newSkip)` | Updates offset and reloads |
| `OnSearchChangedAsync()` | Debounced (300ms) search that resets offset and reloads |
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
    private IMyService MyService => ScopedServices.GetRequiredService<IMyService>();

    protected override async Task LoadCoreAsync()
    {
        var result = await MyService.ListAsync(_skip, _take, _searchText);
        _items = result.Items;
        _totalCount = result.TotalCount;
    }
}
```
