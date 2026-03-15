using Corely.Common.Extensions;
using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.Security.Providers;

namespace Corely.IAM.GoogleAuths.Processors;

internal class GoogleAuthProcessorAuthorizationDecorator(
    IGoogleAuthProcessor inner,
    IAuthorizationProvider authorizationProvider
) : IGoogleAuthProcessor
{
    private readonly IGoogleAuthProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));

    public async Task<LinkGoogleAuthResult> LinkGoogleAuthAsync(
        Guid userId,
        string googleIdToken
    ) =>
        _authorizationProvider.IsAuthorizedForOwnUser(userId)
            ? await _inner.LinkGoogleAuthAsync(userId, googleIdToken)
            : new LinkGoogleAuthResult(LinkGoogleAuthResultCode.UnauthorizedError, "Unauthorized");

    public async Task<UnlinkGoogleAuthResult> UnlinkGoogleAuthAsync(Guid userId) =>
        _authorizationProvider.IsAuthorizedForOwnUser(userId)
            ? await _inner.UnlinkGoogleAuthAsync(userId)
            : new UnlinkGoogleAuthResult(
                UnlinkGoogleAuthResultCode.UnauthorizedError,
                "Unauthorized"
            );

    public async Task<AuthMethodsResult> GetAuthMethodsAsync(Guid userId) =>
        _authorizationProvider.IsAuthorizedForOwnUser(userId)
            ? await _inner.GetAuthMethodsAsync(userId)
            : new AuthMethodsResult(
                AuthMethodsResultCode.UnauthorizedError,
                "Unauthorized",
                false,
                false,
                null
            );

    public Task<Guid?> GetUserIdByGoogleSubjectAsync(string googleSubjectId)
    {
        // No authorization required - this is called during Google sign-in
        // before the user has an authenticated context.
        return _inner.GetUserIdByGoogleSubjectAsync(googleSubjectId);
    }
}
