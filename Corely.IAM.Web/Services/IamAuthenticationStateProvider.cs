using Microsoft.AspNetCore.Components.Authorization;

namespace Corely.IAM.Web.Services;

public class IamAuthenticationStateProvider(
    IBlazorUserContextAccessor blazorUserContextAccessor,
    IUserContextClaimsBuilder userContextClaimsBuilder
) : AuthenticationStateProvider
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var userContext = await blazorUserContextAccessor.GetUserContextAsync();
        var principal = userContextClaimsBuilder.BuildPrincipal(userContext);
        return new AuthenticationState(principal);
    }
}
