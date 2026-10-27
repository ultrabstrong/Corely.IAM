using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Corely.IAM.Web.Configuration;
using Corely.IAM.Web.Extensions;
using Corely.IAM.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Corely.IAM.Web.Middleware;

internal class BearerTokenMiddleware(
    RequestDelegate next,
    ILogger<BearerTokenMiddleware> logger,
    IOptions<IAMWebOptions> options,
    IUserContextClaimsBuilder userContextClaimsBuilder
)
{
    public const string INVALID_TOKEN_ITEM = "Corely.IAM.Web.InvalidBearerToken";

    public async Task InvokeAsync(
        HttpContext context,
        IAuthenticationService authenticationService,
        IUserContextProvider userContextProvider
    )
    {
        if (options.Value.AcceptBearerTokens && context.Request.HasBearerToken())
        {
            var result = await ValidateAsync(authenticationService, context.Request.BearerToken());

            if (result == UserAuthTokenValidationResultCode.Success)
            {
                context.User = userContextClaimsBuilder.BuildPrincipal(
                    userContextProvider.GetUserContext()
                );
            }
            else
            {
                logger.LogDebug("Bearer token validation failed with {ResultCode}", result);
                context.Items[INVALID_TOKEN_ITEM] = true;
            }
        }

        await next(context);
    }

    private async Task<UserAuthTokenValidationResultCode> ValidateAsync(
        IAuthenticationService authenticationService,
        string? token
    )
    {
        if (token is null)
            return UserAuthTokenValidationResultCode.InvalidTokenFormat;

        try
        {
            return await authenticationService.AuthenticateWithTokenAsync(token);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error validating bearer token");
            return UserAuthTokenValidationResultCode.TokenValidationFailed;
        }
    }
}
