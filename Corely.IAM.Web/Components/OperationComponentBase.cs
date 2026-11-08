using Corely.IAM.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Corely.IAM.Web.Components;

/// <summary>
/// A component that calls services. Each action runs through <see cref="InOperationAsync"/> in a
/// signed in DI scope of its own, so no read returns what an earlier action loaded.
/// </summary>
public abstract class OperationComponentBase : ComponentBase, IAsyncDisposable
{
    [Inject]
    private IOperationScopes OperationScopes { get; set; } = null!;

    /// <inheritdoc cref="IOperationScopes.HasSession"/>
    protected bool HasSession => OperationScopes.HasSession;

    /// <inheritdoc cref="IOperationScopes.RunAsync{T}"/>
    protected Task<T?> InOperationAsync<T>(Func<IServiceProvider, Task<T>> work) =>
        OperationScopes.RunAsync(work);

    /// <inheritdoc cref="IOperationScopes.RunAsync(Func{IServiceProvider, Task})"/>
    protected Task<bool> InOperationAsync(Func<IServiceProvider, Task> work) =>
        OperationScopes.RunAsync(work);

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases what a derived component holds; call the base when overriding. Blazor disposes a
    /// component through <see cref="DisposeAsync"/> only, so a derived component overrides this
    /// rather than implementing <see cref="IDisposable"/>.
    /// </summary>
    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;
}
