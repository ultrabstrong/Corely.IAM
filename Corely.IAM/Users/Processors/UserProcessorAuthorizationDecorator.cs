using Corely.Common.Extensions;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Exceptions;
using Corely.IAM.Security.Processors;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.Users.Processors;

internal class UserProcessorAuthorizationDecorator(
    IUserProcessor inner,
    IAuthorizationProvider authorizationProvider,
    IUserContextProvider userContextProvider
) : IUserProcessor
{
    private readonly IUserProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );

    public Task<CreateUserResult> CreateUserAsync(CreateUserRequest request) =>
        _inner.CreateUserAsync(request);

    public async Task<User?> GetUserAsync(int userId)
    {
        await _authorizationProvider.AuthorizeAsync(
            PermissionConstants.USER_RESOURCE_TYPE,
            AuthAction.Read,
            userId
        );
        return await _inner.GetUserAsync(userId);
    }

    public async Task<User?> GetUserAsync(string userName)
    {
        await _authorizationProvider.AuthorizeAsync(
            PermissionConstants.USER_RESOURCE_TYPE,
            AuthAction.Read
        );
        return await _inner.GetUserAsync(userName);
    }

    public async Task UpdateUserAsync(User user)
    {
        await _authorizationProvider.AuthorizeAsync(
            PermissionConstants.USER_RESOURCE_TYPE,
            AuthAction.Update,
            user.Id
        );
        await _inner.UpdateUserAsync(user);
    }

    public async Task<string?> GetUserAuthTokenAsync(int userId)
    {
        await _authorizationProvider.AuthorizeAsync(
            PermissionConstants.USER_RESOURCE_TYPE,
            AuthAction.Read,
            userId
        );
        return await _inner.GetUserAuthTokenAsync(userId);
    }

    public async Task<bool> IsUserAuthTokenValidAsync(int userId, string authToken)
    {
        await _authorizationProvider.AuthorizeAsync(
            PermissionConstants.USER_RESOURCE_TYPE,
            AuthAction.Read,
            userId
        );
        return await _inner.IsUserAuthTokenValidAsync(userId, authToken);
    }

    public async Task<bool> RevokeUserAuthTokenAsync(int userId, string jti)
    {
        await _authorizationProvider.AuthorizeAsync(
            PermissionConstants.USER_RESOURCE_TYPE,
            AuthAction.Update,
            userId
        );
        return await _inner.RevokeUserAuthTokenAsync(userId, jti);
    }

    public async Task RevokeAllUserAuthTokensAsync(int userId)
    {
        await _authorizationProvider.AuthorizeAsync(
            PermissionConstants.USER_RESOURCE_TYPE,
            AuthAction.Update,
            userId
        );
        await _inner.RevokeAllUserAuthTokensAsync(userId);
    }

    public async Task<string?> GetAsymmetricSignatureVerificationKeyAsync(int userId)
    {
        await _authorizationProvider.AuthorizeAsync(
            PermissionConstants.USER_RESOURCE_TYPE,
            AuthAction.Read,
            userId
        );
        return await _inner.GetAsymmetricSignatureVerificationKeyAsync(userId);
    }

    public async Task<AssignRolesToUserResult> AssignRolesToUserAsync(
        AssignRolesToUserRequest request
    )
    {
        if (!request.BypassAuthorization)
            await _authorizationProvider.AuthorizeAsync(
                PermissionConstants.USER_RESOURCE_TYPE,
                AuthAction.Update,
                request.UserId
            );
        return await _inner.AssignRolesToUserAsync(request);
    }

    public async Task<RemoveRolesFromUserResult> RemoveRolesFromUserAsync(
        RemoveRolesFromUserRequest request
    )
    {
        if (!request.BypassAuthorization)
            await _authorizationProvider.AuthorizeAsync(
                PermissionConstants.USER_RESOURCE_TYPE,
                AuthAction.Update,
                request.UserId
            );
        return await _inner.RemoveRolesFromUserAsync(request);
    }

    public async Task<DeleteUserResult> DeleteUserAsync(int userId)
    {
        AuthorizeForOwnUser(userId);
        return await _inner.DeleteUserAsync(userId);
    }

    private void AuthorizeForOwnUser(int requestUserId)
    {
        var userContext =
            _userContextProvider.GetUserContext() ?? throw new UserContextNotSetException();

        if (userContext.UserId == requestUserId)
            return;

        throw new AuthorizationException(
            PermissionConstants.USER_RESOURCE_TYPE,
            AuthAction.Delete.ToString(),
            requestUserId
        );
    }
}
