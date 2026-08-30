using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Corely.IAM.Web.Security;
using Corely.IAM.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Corely.IAM.Web.Middleware;

public class AuthenticationTokenMiddleware(
    RequestDelegate next,
    ILogger<AuthenticationTokenMiddleware> logger,
    IAuthCookieManager authCookieManager,
    IUserContextClaimsBuilder userContextClaimsBuilder,
    IOptions<SecurityOptions> securityOptions
)
{
    private readonly int _authSessionTtlSeconds = securityOptions.Value.AuthSessionTtlSeconds;

    public async Task InvokeAsync(
        HttpContext context,
        IAuthenticationService authenticationService,
        IUserContextProvider userContextProvider
    )
    {
        var token = context.Request.Cookies[AuthenticationConstants.AUTH_TOKEN_COOKIE];

        if (!string.IsNullOrWhiteSpace(token))
            await ValidateAndSetContextAsync(
                context,
                authenticationService,
                userContextProvider,
                token
            );

        await next(context);
    }

    private async Task ValidateAndSetContextAsync(
        HttpContext context,
        IAuthenticationService authenticationService,
        IUserContextProvider userContextProvider,
        string token
    )
    {
        try
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

            var renewResult = await authenticationService.RenewAuthTokenAsync(new(token));
            if (
                renewResult.ResultCode == RenewAuthTokenResultCode.Success
                && !string.IsNullOrWhiteSpace(renewResult.AuthToken)
                && renewResult.AuthTokenId.HasValue
            )
            {
                authCookieManager.SetAuthCookies(
                    context.Response.Cookies,
                    renewResult.AuthToken,
                    renewResult.AuthTokenId.Value,
                    context.Request.IsHttps,
                    _authSessionTtlSeconds
                );
                SetUserPrincipal(context, userContextProvider);
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
