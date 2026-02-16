using Corely.IAM.Users.Models;
using Corely.IAM.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Corely.IAM.Web.Components;

public abstract class AuthenticatedPageBase : ComponentBase
{
    [Inject]
    protected IBlazorUserContextAccessor BlazorUserContextAccessor { get; set; } = null!;

    [Inject]
    protected NavigationManager NavigationManager { get; set; } = null!;

    protected UserContext? UserContext { get; private set; }
    protected bool IsAuthenticated => UserContext?.User != null;

    protected override async Task OnInitializedAsync()
    {
        UserContext = await BlazorUserContextAccessor.GetUserContextAsync();

        if (!IsAuthenticated)
        {
            NavigationManager.NavigateTo(AppRoutes.SignIn, forceLoad: true);
            return;
        }

        await OnInitializedAuthenticatedAsync();
    }

    protected virtual Task OnInitializedAuthenticatedAsync() => Task.CompletedTask;
}
