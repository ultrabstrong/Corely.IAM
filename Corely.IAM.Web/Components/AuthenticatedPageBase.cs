using Corely.IAM.Users.Models;
using Corely.IAM.Web.Extensions;
using Corely.IAM.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.Web.Components;

public abstract class AuthenticatedPageBase : OwningComponentBase
{
    protected IBlazorUserContextAccessor BlazorUserContextAccessor =>
        ScopedServices.GetRequiredService<IBlazorUserContextAccessor>();

    [Inject]
    protected NavigationManager NavigationManager { get; set; } = null!;

    protected UserContext? UserContext { get; private set; }
    protected bool IsAuthenticated => UserContext?.User != null;
    protected Guid CurrentAccountId => UserContext?.CurrentAccount?.Id ?? Guid.Empty;

    protected override async Task OnInitializedAsync()
    {
        UserContext = await BlazorUserContextAccessor.GetUserContextAsync();

        if (!IsAuthenticated)
        {
            NavigationManager.NavigateTo(NavigationManager.SignInUri(), forceLoad: true);
            return;
        }

        await OnInitializedAuthenticatedAsync();
    }

    protected virtual Task OnInitializedAuthenticatedAsync() => Task.CompletedTask;
}
