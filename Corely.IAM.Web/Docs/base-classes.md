# Page Base Classes

Class hierarchy for the Blazor management components in Corely.IAM.Web, and for a host's own pages.

## Hierarchy

```
ComponentBase (Blazor)
└── OperationComponentBase
    └── AuthenticatedPageBase
    └── EntityPageBase
        ├── EntityListPageBase<T>
        └── EntityDetailPageBase
```

## OperationComponentBase

For any component that calls services, page or not.

| Member | Description |
|--------|-------------|
| `InOperationAsync(services => ...)` | Runs one action in a DI scope of its own, signed in as the tab's user, and disposes it when the action ends. Returns the work's result, or `default` (`false` for the non generic form) when the scope could not be signed in |
| `HasSession` | Whether the tab arrived with a session at all |
| `DisposeAsyncCore()` | Override to release what the component holds, calling the base. Blazor disposes a component through `IAsyncDisposable` only, so a derived component never implements `IDisposable` |

### Each action runs in a scope of its own

Blazor Server keeps one DI scope per browser tab, and a component can stay on screen for minutes. A
`DbContext` kept that long returns the rows it already loaded instead of what the database holds now,
and two components sharing one fail with "A second operation was started on this context instance".
So every user visible action (the reads that render together on load, one timer tick, a button's
action) runs through `InOperationAsync` and resolves what it needs from the
provider it is handed:

```csharp
protected override Task OnInitializedAsync() =>
    InOperationAsync(async services =>
    {
        var result = await services.GetRequiredService<IRetrievalService>().GetGroupAsync(Id);
        _group = result.Item;
    });
```

A reload after an action is an operation of its own: a service may write in SQL, which the action's
scope would not see. Never keep a service in a field across actions, and never `@inject` or `[Inject]` a scoped service
that reaches the database: property injection resolves from the tab's scope and brings back both
problems. Singletons, framework services (`NavigationManager`, `IJSRuntime`, `ILogger<T>`,
`IOptions<T>`) and `IAccountDisplayState`, which is shared across the tab on purpose, stay injected.

When a scope cannot be signed in, the work does not run and the browser leaves the page: to sign in
when there is no session, or a full reload when the token expired or was revoked, which renews it from
the refresh token or signs the user out. A page left open past its token's life reloads on its next
action.

## AuthenticatedPageBase

Ensures user context is loaded. Redirects unauthenticated users to `/signin`, returning to the page afterwards.

| Member | Type | Description |
|--------|------|-------------|
| `UserContext` | `UserContext?` | Current authenticated user context |
| `IsAuthenticated` | `bool` | `true` if `UserContext?.User != null` |
| `CurrentAccountId` | `Guid` | The current account's id from the token, or `Guid.Empty` |
| `OnInitializedAuthenticatedAsync(services)` | `virtual Task` | Override point, called after successful authentication, in the same operation |

**Behavior:** `OnInitializedAsync()` runs one operation that loads `UserContext` and then calls
`OnInitializedAuthenticatedAsync(services)` with that operation's services. A tab with no session is
sent to sign in with `forceLoad: true`.

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
| `LoadCoreAsync(services)` | Abstract: load page data from the operation's services |
| `ReloadAsync()` | Runs `LoadCoreAsync` in an operation of its own, with loading state and error handling. After an action it reads what the action wrote, even a write a service made in SQL, which a read in the action's own scope would miss |
| `FailLoad(resultCode, resourceName)` | Marks the load failed and says why: no access, or couldn't be loaded. Call it from `LoadCoreAsync` when a retrieval doesn't succeed |
| `ExecuteSafeAsync(services => ...)` | Runs a user's action in an operation of its own with try-catch and loading state; an action that changes data then calls `ReloadAsync()` |
| `SetResultMessage(success, msg, failMsg)` | Sets alert state from operation result |
| `TryParseGuid(input, out result)` | Safe GUID parse with error alert on failure |
| `ShowConfirmation(modal, id, msg)` | Opens a `ConfirmModal` for a specific entity |

**Behavior:** Sealed `OnInitializedAuthenticatedAsync(services)` loads the page in the operation that signed it in.

## EntityListPageBase\<T\>

Pagination, search, and sort for entity list pages. A derived page that disposes its own resources
overrides `DisposeAsyncCore()` and calls the base.

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
    protected override async Task LoadCoreAsync(IServiceProvider services)
    {
        var result = await services.GetRequiredService<IMyService>().ListAsync(_skip, _take, _searchText);
        _items = result.Items;
        _totalCount = result.TotalCount;
    }
}
```
