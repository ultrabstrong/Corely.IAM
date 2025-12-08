using Corely.Common.Extensions;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.Security.Exceptions;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.BasicAuths.Processors;

internal class BasicAuthProcessorAuthorizationDecorator(
    IBasicAuthProcessor inner,
    IUserContextProvider userContextProvider
) : IBasicAuthProcessor
{
    private readonly IBasicAuthProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );

    public async Task<UpsertBasicAuthResult> UpsertBasicAuthAsync(UpsertBasicAuthRequest request)
    {
        AuthorizeForOwnUser(request.UserId, "Update");
        return await _inner.UpsertBasicAuthAsync(request);
    }

    public async Task<bool> VerifyBasicAuthAsync(VerifyBasicAuthRequest request)
    {
        AuthorizeForOwnUser(request.UserId, "Read");
        return await _inner.VerifyBasicAuthAsync(request);
    }

    private void AuthorizeForOwnUser(int requestUserId, string action)
    {
        var userContext =
            _userContextProvider.GetUserContext() ?? throw new UserContextNotSetException();

        // Allow if user is operating on their own credentials
        if (userContext.UserId == requestUserId)
            return;

        // TODO: Future enhancement - allow admins to manage other users' passwords
        throw new AuthorizationException("basicauth", action, requestUserId);
    }
}
