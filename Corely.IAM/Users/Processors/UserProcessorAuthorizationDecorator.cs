using Corely.Common.Extensions;
using Corely.IAM.Auth.Constants;
using Corely.IAM.Auth.Providers;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Users.Models;

namespace Corely.IAM.Users.Processors;

internal class UserProcessorAuthorizationDecorator(
    IUserProcessor inner,
    IAuthorizationProvider authorizationProvider
) : IUserProcessor
{
    private readonly IUserProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));

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
}
