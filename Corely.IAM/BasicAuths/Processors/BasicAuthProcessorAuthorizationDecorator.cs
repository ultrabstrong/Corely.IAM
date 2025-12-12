using Corely.Common.Extensions;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Exceptions;
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

    public async Task<UpsertBasicAuthResult> UpsertBasicAuthAsync(UpsertBasicAuthRequest request)
    {
        AuthorizeForOwnUser(request.UserId, AuthAction.Update);
        return await _inner.UpsertBasicAuthAsync(request);
    }

    public Task<bool> VerifyBasicAuthAsync(VerifyBasicAuthRequest request)
    {
        // No authorization required - this is the authentication mechanism itself.
        // Users must be able to verify credentials before they have an authenticated context.
        return _inner.VerifyBasicAuthAsync(request);
    }

    private void AuthorizeForOwnUser(int requestUserId, AuthAction action)
    {
        var userContext =
            _userContextProvider.GetUserContext() ?? throw new UserContextNotSetException();

        if (userContext.UserId == requestUserId)
            return;

        throw new AuthorizationException("basicauth", action.ToString(), requestUserId);
    }
}
