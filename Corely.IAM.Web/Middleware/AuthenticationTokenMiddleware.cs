using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Corely.IAM.Web.Security;
using Corely.IAM.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Web.Middleware;

public class AuthenticationTokenMiddleware(
    RequestDelegate next,
    ILogger<AuthenticationTokenMiddleware> logger,
    IAuthCookieManager authCookieManager,
    IUserContextClaimsBuilder userContextClaimsBuilder
)
{
    public async Task InvokeAsync(HttpContext context, IUserContextProvider userContextProvider)
    {
        var token = context.Request.Cookies[AuthenticationConstants.AUTH_TOKEN_COOKIE];

        if (!string.IsNullOrWhiteSpace(token))
            await ValidateAndSetContextAsync(context, userContextProvider, token);

        await next(context);
    }

    private async Task ValidateAndSetContextAsync(
        HttpContext context,
        IUserContextProvider userContextProvider,
        string token
    )
    {
        try
        {
            var result = await userContextProvider.SetUserContextAsync(token);
            if (result != UserAuthTokenValidationResultCode.Success)
            {
                logger.LogDebug(
                    "Token validation failed with {ResultCode}, clearing cookies",
                    result
                );
                authCookieManager.DeleteAuthCookies(context.Response.Cookies);
                return;
            }

            var userContext = userContextProvider.GetUserContext();
            if (userContext != null)
                context.User = userContextClaimsBuilder.BuildPrincipal(userContext);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error validating authentication token, clearing cookies");
            authCookieManager.DeleteAuthCookies(context.Response.Cookies);
        }
    }
}
