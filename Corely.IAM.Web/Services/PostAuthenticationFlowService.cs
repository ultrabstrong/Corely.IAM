using Corely.Common.Extensions;
using Corely.IAM.Models;
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
            || string.IsNullOrWhiteSpace(signInResult.RefreshToken)
            || !signInResult.RefreshExpiresUtc.HasValue
        )
        {
            throw new InvalidOperationException(
                "Post-authentication flow requires a successful sign-in result with auth cookies."
            );
        }

        _authCookieManager.SetAuthCookies(
            httpContext.Response.Cookies,
            signInResult.AuthToken!,
            signInResult.AuthTokenId!.Value,
            signInResult.RefreshToken!,
            signInResult.RefreshExpiresUtc!.Value,
            httpContext.Request.IsHttps
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
            switchResult.RefreshToken!,
            switchResult.RefreshExpiresUtc!.Value,
            httpContext.Request.IsHttps
        );

        return new RedirectResult(localReturnUrl ?? homePath);
    }
}
