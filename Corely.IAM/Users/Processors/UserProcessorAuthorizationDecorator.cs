using Corely.Common.Extensions;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Providers;
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

    public async Task<GetUserResult> GetUserAsync(int userId) =>
        await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Read,
            PermissionConstants.USER_RESOURCE_TYPE,
            userId
        )
            ? await _inner.GetUserAsync(userId)
            : new GetUserResult(
                GetUserResultCode.UnauthorizedError,
                $"Unauthorized to read user {userId}",
                null
            );

    public async Task<UpdateUserResult> UpdateUserAsync(User user) =>
        _authorizationProvider.IsAuthorizedForOwnUser(user.Id)
            ? await _inner.UpdateUserAsync(user)
            : new UpdateUserResult(
                UpdateUserResultCode.UnauthorizedError,
                $"Unauthorized to update user {user.Id}"
            );

    public async Task<GetAsymmetricKeyResult> GetAsymmetricSignatureVerificationKeyAsync(
        int userId
    ) =>
        await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Read,
            PermissionConstants.USER_RESOURCE_TYPE,
            userId
        )
            ? await _inner.GetAsymmetricSignatureVerificationKeyAsync(userId)
            : new GetAsymmetricKeyResult(
                GetAsymmetricKeyResultCode.UnauthorizedError,
                $"Unauthorized to read user {userId}",
                null
            );

    public async Task<AssignRolesToUserResult> AssignRolesToUserAsync(
        AssignRolesToUserRequest request
    ) =>
        request.BypassAuthorization
        || await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Update,
            PermissionConstants.USER_RESOURCE_TYPE,
            request.UserId
        )
            ? await _inner.AssignRolesToUserAsync(request)
            : new AssignRolesToUserResult(
                AssignRolesToUserResultCode.UnauthorizedError,
                $"Unauthorized to update user {request.UserId}",
                0,
                []
            );

    public async Task<RemoveRolesFromUserResult> RemoveRolesFromUserAsync(
        RemoveRolesFromUserRequest request
    ) =>
        request.BypassAuthorization
        || await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Update,
            PermissionConstants.USER_RESOURCE_TYPE,
            request.UserId
        )
            ? await _inner.RemoveRolesFromUserAsync(request)
            : new RemoveRolesFromUserResult(
                RemoveRolesFromUserResultCode.UnauthorizedError,
                $"Unauthorized to update user {request.UserId}",
                0,
                []
            );

    public async Task<DeleteUserResult> DeleteUserAsync(int userId) =>
        _authorizationProvider.IsAuthorizedForOwnUser(userId)
            ? await _inner.DeleteUserAsync(userId)
            : new DeleteUserResult(
                DeleteUserResultCode.UnauthorizedError,
                $"Unauthorized to delete user {userId}"
            );
}
