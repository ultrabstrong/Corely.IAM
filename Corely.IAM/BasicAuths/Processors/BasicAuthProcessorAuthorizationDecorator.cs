using Corely.Common.Extensions;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.Security.Providers;

namespace Corely.IAM.BasicAuths.Processors;

internal class BasicAuthProcessorAuthorizationDecorator(
    IBasicAuthProcessor inner,
    IAuthorizationProvider authorizationProvider
) : IBasicAuthProcessor
{
    private readonly IBasicAuthProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));

    public Task<CreateBasicAuthResult> CreateBasicAuthAsync(CreateBasicAuthRequest request)
    {
        // No authorization required - this is called during user registration
        // before the user has an authenticated context.
        return _inner.CreateBasicAuthAsync(request);
    }

    public async Task<UpdateBasicAuthResult> UpdateBasicAuthAsync(UpdateBasicAuthRequest request) =>
        _authorizationProvider.IsAuthorizedForOwnUser(request.UserId)
            ? await _inner.UpdateBasicAuthAsync(request)
            : new UpdateBasicAuthResult(
                UpdateBasicAuthResultCode.UnauthorizedError,
                $"Unauthorized to update basic auth for user {request.UserId}"
            );

    public Task<VerifyBasicAuthResult> VerifyBasicAuthAsync(VerifyBasicAuthRequest request)
    {
        // No authorization required - this is the authentication mechanism itself.
        // Users must be able to verify credentials before they have an authenticated context.
        return _inner.VerifyBasicAuthAsync(request);
    }
}
