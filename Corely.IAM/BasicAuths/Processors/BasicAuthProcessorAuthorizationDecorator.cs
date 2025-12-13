using Corely.Common.Extensions;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.Enums;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.BasicAuths.Processors;

internal class BasicAuthProcessorAuthorizationDecorator(
    IBasicAuthProcessor inner,
    IIamUserContextProvider userContextProvider
) : IBasicAuthProcessor
{
    private readonly IBasicAuthProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IIamUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );

    public async Task<UpsertBasicAuthResult> UpsertBasicAuthAsync(UpsertBasicAuthRequest request) =>
        IsAuthorizedForOwnUser(request.UserId)
            ? await _inner.UpsertBasicAuthAsync(request)
            : new UpsertBasicAuthResult(
                UpsertBasicAuthResultCode.UnauthorizedError,
                $"Unauthorized to update basic auth for user {request.UserId}",
                -1,
                default
            );

    public Task<VerifyBasicAuthResult> VerifyBasicAuthAsync(VerifyBasicAuthRequest request)
    {
        // No authorization required - this is the authentication mechanism itself.
        // Users must be able to verify credentials before they have an authenticated context.
        return _inner.VerifyBasicAuthAsync(request);
    }

    private bool IsAuthorizedForOwnUser(int requestUserId)
    {
        var userContext = _userContextProvider.GetUserContext();
        return userContext?.UserId == requestUserId;
    }
}
