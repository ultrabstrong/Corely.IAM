# Migrating to Corely.IAM.Web 11.0

Corely.IAM.Web 11.0 runs every action of a Blazor component in a DI scope of its own. In 10.0 a
component owned one scope for as long as it was on screen, so a page refreshing on a timer, or reloading
after a change made elsewhere, kept showing the rows its `DbContext` first loaded. Corely.IAM itself
does not change.

## What breaks

| Before | After |
|--------|-------|
| Components inherit `OwningComponentBase` and read `ScopedServices` | Components inherit `OperationComponentBase` and run each action through `InOperationAsync(services => ...)` |
| `AuthenticatedPageBase.BlazorUserContextAccessor` | Gone; `UserContext` is loaded in the page's first operation |
| `OnInitializedAuthenticatedAsync()` | `OnInitializedAuthenticatedAsync(IServiceProvider services)`, in the operation that signed the page in |
| `EntityPageBase.LoadCoreAsync()` | `LoadCoreAsync(IServiceProvider services)` |
| `EntityPageBase.ExecuteSafeAsync(Func<Task>)` | `ExecuteSafeAsync(Func<IServiceProvider, Task>)`; reload inside it with `ReloadAsync(services)` |
| Disposal came from `OwningComponentBase` | `OperationComponentBase` implements `IAsyncDisposable` with the same `DisposeAsyncCore()` override point. An override of `Dispose(bool)` moves to `DisposeAsyncCore()`: Blazor calls only `DisposeAsync` on a component that has both, so an `IDisposable` added beside it is never called |

A page left open past its auth token's life now reloads on its next action, which renews the session
or signs the user out, and a revoked session ends on the next action rather than the next page.

## What to do

1. For each component of your own that calls services, change `OwningComponentBase` to
   `OperationComponentBase` (or keep a page base class) and delete its `ScopedServices` properties.
2. Wrap each action in `InOperationAsync`, resolving services from the provider it hands you:

   ```csharp
   protected override async Task LoadCoreAsync(IServiceProvider services)
   {
       var result = await services.GetRequiredService<IMyService>().ListAsync(_skip, _take);
       _items = result.Items;
   }

   private Task SaveAsync() =>
       ExecuteSafeAsync(async services =>
       {
           await services.GetRequiredService<IMyService>().SaveAsync(_model);
           await ReloadAsync(services);
       });
   ```

3. Drop the `GetUserContextAsync()` call a component made to sign its own scope in: every operation is
   signed in already.
4. A component that cannot take a base class injects `IOperationScopes` and calls `RunAsync` the same
   way. Never keep a service in a field across actions.

[Your Own Blazor Pages](Corely.IAM.Web/Docs/setup.md#your-own-blazor-pages) states the rules.
