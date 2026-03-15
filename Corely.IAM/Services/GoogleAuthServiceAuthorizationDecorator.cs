using Corely.Common.Extensions;
using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.Security.Providers;

namespace Corely.IAM.Services;

internal class GoogleAuthServiceAuthorizationDecorator(
    IGoogleAuthService inner,
    IAuthorizationProvider authorizationProvider
) : IGoogleAuthService
{
    private readonly IGoogleAuthService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));

    public async Task<LinkGoogleAuthResult> LinkGoogleAuthAsync(LinkGoogleAuthRequest request) =>
        _authorizationProvider.HasUserContext()
            ? await _inner.LinkGoogleAuthAsync(request)
            : new LinkGoogleAuthResult(LinkGoogleAuthResultCode.UnauthorizedError, "Unauthorized");

    public async Task<UnlinkGoogleAuthResult> UnlinkGoogleAuthAsync() =>
        _authorizationProvider.HasUserContext()
            ? await _inner.UnlinkGoogleAuthAsync()
            : new UnlinkGoogleAuthResult(
                UnlinkGoogleAuthResultCode.UnauthorizedError,
                "Unauthorized"
            );

    public async Task<AuthMethodsResult> GetAuthMethodsAsync() =>
        _authorizationProvider.HasUserContext()
            ? await _inner.GetAuthMethodsAsync()
            : new AuthMethodsResult(
                AuthMethodsResultCode.UnauthorizedError,
                "Unauthorized",
                false,
                false,
                null
            );
}
