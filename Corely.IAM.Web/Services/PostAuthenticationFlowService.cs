using Corely.Common.Extensions;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;
using Corely.IAM.Web.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SignInResultModel = Corely.IAM.Models.SignInResult;

namespace Corely.IAM.Web.Services;

public class PostAuthenticationFlowService(
    IAuthenticationService authenticationService,
    IUserContextProvider userContextProvider,
    IAuthCookieManager authCookieManager,
    IOptions<Corely.IAM.Security.Models.SecurityOptions> securityOptions
) : IPostAuthenticationFlowService
{
    private readonly IAuthenticationService _authenticationService =
        authenticationService.ThrowIfNull(nameof(authenticationService));
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );
    private readonly IAuthCookieManager _authCookieManager = authCookieManager.ThrowIfNull(
        nameof(authCookieManager)
    );
    private readonly int _authSessionTtlSeconds = securityOptions
        .ThrowIfNull(nameof(securityOptions))
        .Value.AuthSessionTtlSeconds;

    public async Task<IActionResult> CompleteSignInAsync(
        HttpContext httpContext,
        SignInResultModel signInResult,
        int _unusedAuthCookieTtlSeconds
    )
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(signInResult);

        if (
            signInResult.ResultCode != SignInResultCode.Success
            || string.IsNullOrWhiteSpace(signInResult.AuthToken)
            || !signInResult.AuthTokenId.HasValue
        )
        {
            throw new InvalidOperationException(
                "Post-authentication flow requires a successful sign-in result with auth cookies."
            );
        }

        _authCookieManager.SetAuthCookies(
            httpContext.Response.Cookies,
            signInResult.AuthToken,
            signInResult.AuthTokenId.Value,
            httpContext.Request.IsHttps,
            _authSessionTtlSeconds
        );

        var userContext = _userContextProvider.GetUserContext();
        if (userContext == null || userContext.AvailableAccounts.Count == 0)
        {
            return new RedirectResult(AppRoutes.Dashboard);
        }

        if (userContext.AvailableAccounts.Count > 1)
        {
            return new RedirectResult(AppRoutes.SelectAccount);
        }

        var switchResult = await _authenticationService.SwitchAccountAsync(
            new SwitchAccountRequest(userContext.AvailableAccounts[0].Id)
        );
        if (switchResult.ResultCode != SignInResultCode.Success)
        {
            return new RedirectResult(AppRoutes.SelectAccount);
        }

        _authCookieManager.SetAuthCookies(
            httpContext.Response.Cookies,
            switchResult.AuthToken!,
            switchResult.AuthTokenId!.Value,
            httpContext.Request.IsHttps,
            _authSessionTtlSeconds
        );

        return new RedirectResult(AppRoutes.Dashboard);
    }
}
