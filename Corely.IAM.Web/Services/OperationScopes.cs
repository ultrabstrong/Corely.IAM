using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Corely.IAM.Web.Security;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.Web.Services;

internal class OperationScopes(
    IServiceScopeFactory scopeFactory,
    IHttpContextAccessor httpContextAccessor,
    NavigationManager navigationManager
) : IOperationScopes
{
    public bool HasSession => !string.IsNullOrWhiteSpace(ReadToken());

    private string? _token;
    private bool _tokenRead;

    public async Task<T?> RunAsync<T>(Func<IServiceProvider, Task<T>> work)
    {
        ArgumentNullException.ThrowIfNull(work);

        await using var scope = scopeFactory.CreateAsyncScope();
        if (!await SignInAsync(scope.ServiceProvider))
            return default;

        return await work(scope.ServiceProvider);
    }

    public async Task<bool> RunAsync(Func<IServiceProvider, Task> work)
    {
        ArgumentNullException.ThrowIfNull(work);

        return await RunAsync(async services =>
        {
            await work(services);
            return true;
        });
    }

    private async Task<bool> SignInAsync(IServiceProvider services)
    {
        if (!HasSession)
            return false;
        var token = ReadToken()!;

        var result = await services
            .GetRequiredService<IAuthenticationService>()
            .AuthenticateWithTokenAsync(token);
        if (result == UserAuthTokenValidationResultCode.Success)
            return true;

        // The token expired or was revoked while the tab stayed open. A full reload goes through
        // the authentication middleware, which renews it from the refresh token or signs the user out.
        navigationManager.NavigateTo(navigationManager.Uri, forceLoad: true);
        return false;
    }

    // A circuit's HttpContext is fixed when it connects, so the token is read once and kept.
    private string? ReadToken()
    {
        if (!_tokenRead)
        {
            var httpContext = httpContextAccessor.HttpContext;
            _token =
                httpContext?.Items[AuthCookieManager.RENEWED_AUTH_TOKEN_ITEM] as string
                ?? httpContext?.Request.Cookies[AuthCookieManager.AUTH_TOKEN_COOKIE];
            _tokenRead = true;
        }
        return _token;
    }
}
