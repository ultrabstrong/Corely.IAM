using Corely.Common.Extensions;
using Corely.IAM.Models;
using Corely.IAM.Platform.Providers;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;
using Corely.IAM.Web.Configuration;
using Corely.IAM.Web.Extensions;
using Corely.IAM.Web.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SignInResultModel = Corely.IAM.Models.SignInResult;

namespace Corely.IAM.Web.Services;

internal class PostAuthenticationFlowService(
    IAuthenticationService authenticationService,
    IUserContextProvider userContextProvider,
    IAuthCookieManager authCookieManager,
    ISecurityPolicyProvider securityPolicyProvider,
    IOptions<IAMWebOptions> iamWebOptions
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
    private readonly ISecurityPolicyProvider _securityPolicyProvider =
        securityPolicyProvider.ThrowIfNull(nameof(securityPolicyProvider));

    public async Task<IActionResult> CompleteSignInAsync(
        HttpContext httpContext,
        SignInResultModel signInResult,
        string? returnUrl = null
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

        var authSessionTtlSeconds = (
            await _securityPolicyProvider.GetPolicyAsync()
        ).AuthSessionTtlSeconds;
        _authCookieManager.SetAuthCookies(
            httpContext.Response.Cookies,
            signInResult.AuthToken,
            signInResult.AuthTokenId.Value,
            httpContext.Request.IsHttps,
            authSessionTtlSeconds
        );

        var homePath = iamWebOptions.Value.HomePath;
        var localReturnUrl = returnUrl.AsLocalUrl();
        var userContext = _userContextProvider.GetUserContext();
        if (userContext == null || userContext.AvailableAccounts.Count == 0)
        {
            return new RedirectResult(homePath);
        }

        if (userContext.AvailableAccounts.Count > 1)
        {
            return new RedirectResult(localReturnUrl ?? AppRoutes.SelectAccount);
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
            authSessionTtlSeconds
        );

        return new RedirectResult(localReturnUrl ?? homePath);
    }
}
