using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Corely.IAM.Web.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Web.Services;

public class BlazorUserContextAccessor(
    IUserContextProvider userContextProvider,
    IHttpContextAccessor httpContextAccessor,
    ILogger<BlazorUserContextAccessor> logger
) : IBlazorUserContextAccessor
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<UserContext?> GetUserContextAsync()
    {
        // Fast path: provider already has context from middleware
        var existingContext = userContextProvider.GetUserContext();
        if (existingContext != null)
        {
            return existingContext;
        }

        if (!await _semaphore.WaitAsync(TimeSpan.FromSeconds(5)))
        {
            return null;
        }

        try
        {
            // Double-check after acquiring lock
            existingContext = userContextProvider.GetUserContext();
            if (existingContext != null)
            {
                return existingContext;
            }

            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return null;
            }

            var token = httpContext.Request.Cookies[AuthenticationConstants.AUTH_TOKEN_COOKIE];
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var result = await userContextProvider.SetUserContextAsync(token);
            if (result == UserAuthTokenValidationResultCode.Success)
            {
                return userContextProvider.GetUserContext();
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to set user context from auth token");
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
