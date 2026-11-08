using Corely.IAM.Web.Services;

namespace Corely.IAM.Web.UnitTests.Helpers;

/// <summary>
/// Runs each operation on the test's own services, so a component test sees its mocks. With
/// <see cref="SignedIn"/> false it behaves as a tab whose session ended, and with <see cref="HasSession"/>
/// false as a tab with none: either way the work never runs.
/// <c>OperationScopesTests</c> covers the real scopes.
/// </summary>
internal class InlineOperationScopes(IServiceProvider services) : IOperationScopes
{
    public bool HasSession { get; set; } = true;
    public bool SignedIn { get; set; } = true;
    public int Runs { get; private set; }

    public async Task<T?> RunAsync<T>(Func<IServiceProvider, Task<T>> work)
    {
        Runs++;
        return HasSession && SignedIn ? await work(services) : default;
    }

    public async Task<bool> RunAsync(Func<IServiceProvider, Task> work)
    {
        Runs++;
        if (!HasSession || !SignedIn)
            return false;
        await work(services);
        return true;
    }
}
