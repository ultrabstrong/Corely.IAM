using Corely.IAM.Users.Models;
using Corely.IAM.Web.Extensions;
using Corely.IAM.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.Web.Components;

public abstract class AuthenticatedPageBase : OperationComponentBase
{
    [Inject]
    protected NavigationManager NavigationManager { get; set; } = null!;

    protected UserContext? UserContext { get; private set; }
    protected bool IsAuthenticated => UserContext?.User != null;
    protected Guid CurrentAccountId => UserContext?.CurrentAccount?.Id ?? Guid.Empty;

    protected override async Task OnInitializedAsync()
    {
        // An expired session has already started a reload; only a tab with none goes to sign in.
        if (!HasSession)
        {
            NavigationManager.NavigateTo(NavigationManager.SignInUri(), forceLoad: true);
            return;
        }

        await InOperationAsync(async services =>
        {
            UserContext = await services
                .GetRequiredService<IBlazorUserContextAccessor>()
                .GetUserContextAsync();
            if (IsAuthenticated)
                await OnInitializedAuthenticatedAsync(services);
        });
    }

    /// <summary>
    /// Runs in the same operation that signed the page in, after <see cref="UserContext"/> is set.
    /// </summary>
    protected virtual Task OnInitializedAuthenticatedAsync(IServiceProvider services) =>
        Task.CompletedTask;
}
