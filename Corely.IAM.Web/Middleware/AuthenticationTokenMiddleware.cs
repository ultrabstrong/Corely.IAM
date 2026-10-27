using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Corely.IAM.Web.Configuration;
using Corely.IAM.Web.Extensions;
using Corely.IAM.Web.Security;
using Corely.IAM.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Corely.IAM.Web.Middleware;

internal class AuthenticationTokenMiddleware(
    RequestDelegate next,
    ILogger<AuthenticationTokenMiddleware> logger,
    IAuthCookieManager authCookieManager,
    IUserContextClaimsBuilder userContextClaimsBuilder,
    IOptions<IAMWebOptions> options
)
{
    public async Task InvokeAsync(
        HttpContext context,
        IAuthenticationService authenticationService,
        IUserContextProvider userContextProvider
    )
    {
        if (options.Value.AcceptBearerTokens && context.Request.HasBearerToken())
        {
            await next(context);
            return;
        }

        var token = context.Request.Cookies[AuthCookieManager.AUTH_TOKEN_COOKIE];
        var refreshToken = context.Request.Cookies[AuthCookieManager.REFRESH_TOKEN_COOKIE];

        if (!string.IsNullOrWhiteSpace(token) || !string.IsNullOrWhiteSpace(refreshToken))
            await ValidateAndSetContextAsync(
                context,
                authenticationService,
                userContextProvider,
                token,
                refreshToken
            );

        await next(context);
    }

    private async Task ValidateAndSetContextAsync(
        HttpContext context,
        IAuthenticationService authenticationService,
        IUserContextProvider userContextProvider,
        string? token,
        string? refreshToken
    )
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                var result = await authenticationService.AuthenticateWithTokenAsync(token);
                if (result == UserAuthTokenValidationResultCode.Success)
                {
                    SetUserPrincipal(context, userContextProvider);
                    return;
                }

                logger.LogDebug(
                    "Token validation failed with {ResultCode}, attempting renewal",
                    result
                );
            }

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                DeleteAuthStateCookies(context.Response.Cookies);
                return;
            }

            var renewResult = await authenticationService.RenewAuthTokenAsync(new(refreshToken));
            if (
                renewResult.ResultCode == RenewAuthTokenResultCode.Success
                && !string.IsNullOrWhiteSpace(renewResult.AuthToken)
                && renewResult.AuthTokenId.HasValue
                && !string.IsNullOrWhiteSpace(renewResult.RefreshToken)
                && renewResult.RefreshExpiresUtc.HasValue
            )
            {
                authCookieManager.SetAuthCookies(
                    context.Response.Cookies,
                    renewResult.AuthToken,
                    renewResult.AuthTokenId.Value,
                    renewResult.RefreshToken,
                    renewResult.RefreshExpiresUtc.Value,
                    context.Request.IsHttps
                );
                SetUserPrincipal(context, userContextProvider);
                return;
            }

            if (renewResult.ResultCode == RenewAuthTokenResultCode.RefreshTokenAlreadyRotatedError)
            {
                logger.LogDebug(
                    "Refresh token was replaced by a concurrent renewal, keeping cookies"
                );
                return;
            }

            logger.LogDebug(
                "Token renewal failed with {ResultCode}, clearing cookies",
                renewResult.ResultCode
            );
            DeleteAuthStateCookies(context.Response.Cookies);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error validating authentication token, clearing cookies");
            DeleteAuthStateCookies(context.Response.Cookies);
        }
    }

    private void DeleteAuthStateCookies(IResponseCookies cookies)
    {
        authCookieManager.DeleteAuthCookies(cookies);
        authCookieManager.DeleteDeviceIdCookie(cookies);
    }

    private void SetUserPrincipal(HttpContext context, IUserContextProvider userContextProvider)
    {
        var userContext = userContextProvider.GetUserContext();
        if (userContext != null)
            context.User = userContextClaimsBuilder.BuildPrincipal(userContext);
    }
}
