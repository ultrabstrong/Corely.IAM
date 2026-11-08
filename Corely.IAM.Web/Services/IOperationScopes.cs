namespace Corely.IAM.Web.Services;

/// <summary>
/// Runs one user visible action of a Blazor component in a DI scope of its own, signed in as the
/// tab's user and disposed when the action ends, so every read sees the database as it is now.
/// </summary>
public interface IOperationScopes
{
    /// <summary>
    /// Whether the tab arrived with a session. Without one, no operation runs; a component that needs
    /// a user sends the browser to sign in, and one that does not, such as a permission gate on a
    /// public page, shows its anonymous content.
    /// </summary>
    bool HasSession { get; }

    /// <summary>
    /// Runs <paramref name="work"/> in a new signed in scope and returns its result. When the scope
    /// cannot be signed in, the work does not run and <c>default</c> is returned; for a session that
    /// expired or was revoked, the browser is also reloaded, which renews it or signs the user out.
    /// </summary>
    Task<T?> RunAsync<T>(Func<IServiceProvider, Task<T>> work);

    /// <summary>
    /// Runs <paramref name="work"/> in a new signed in scope. Returns <c>false</c>, without running
    /// it, when the scope cannot be signed in.
    /// </summary>
    Task<bool> RunAsync(Func<IServiceProvider, Task> work);
}
