using Corely.Common.Extensions;
using Corely.Common.Filtering;
using Corely.Common.Filtering.Ordering;
using Corely.IAM.Models;
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

    public async Task<GetUserResult> GetUserAsync(Guid userId) =>
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

    public async Task<ModifyResult> UpdateUserAsync(UpdateUserRequest request) =>
        _authorizationProvider.IsAuthorizedForOwnUser(request.UserId)
            ? await _inner.UpdateUserAsync(request)
            : new ModifyResult(
                ModifyResultCode.UnauthorizedError,
                $"Unauthorized to update user {request.UserId}"
            );

    public async Task<GetAsymmetricKeyResult> GetAsymmetricSignatureVerificationKeyAsync(
        Guid userId
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
        || (
            await _authorizationProvider.IsAuthorizedAsync(
                AuthAction.Update,
                PermissionConstants.USER_RESOURCE_TYPE,
                request.UserId
            )
            && await _authorizationProvider.IsAuthorizedAsync(
                AuthAction.Read,
                PermissionConstants.ROLE_RESOURCE_TYPE,
                [.. request.RoleIds]
            )
        )
            ? await _inner.AssignRolesToUserAsync(request)
            : new AssignRolesToUserResult(
                AssignRolesToUserResultCode.UnauthorizedError,
                $"Unauthorized to update user {request.UserId} or read roles",
                0,
                []
            );

    public async Task<RemoveRolesFromUserResult> RemoveRolesFromUserAsync(
        RemoveRolesFromUserRequest request
    ) =>
        request.BypassAuthorization
        || (
            await _authorizationProvider.IsAuthorizedAsync(
                AuthAction.Update,
                PermissionConstants.USER_RESOURCE_TYPE,
                request.UserId
            )
            && await _authorizationProvider.IsAuthorizedAsync(
                AuthAction.Read,
                PermissionConstants.ROLE_RESOURCE_TYPE,
                [.. request.RoleIds]
            )
        )
            ? await _inner.RemoveRolesFromUserAsync(request)
            : new RemoveRolesFromUserResult(
                RemoveRolesFromUserResultCode.UnauthorizedError,
                $"Unauthorized to update user {request.UserId} or read roles",
                0,
                []
            );

    public async Task<DeleteUserResult> DeleteUserAsync(Guid userId) =>
        _authorizationProvider.IsAuthorizedForOwnUser(userId)
            ? await _inner.DeleteUserAsync(userId)
            : new DeleteUserResult(
                DeleteUserResultCode.UnauthorizedError,
                $"Unauthorized to delete user {userId}"
            );

    public async Task<ListResult<User>> ListUsersAsync(
        FilterBuilder<User>? filter,
        OrderBuilder<User>? order,
        int skip,
        int take
    ) =>
        await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Read,
            PermissionConstants.USER_RESOURCE_TYPE
        )
            ? await _inner.ListUsersAsync(filter, order, skip, take)
            : new ListResult<User>(
                RetrieveResultCode.UnauthorizedError,
                "Unauthorized to list users",
                null
            );

    public Task<GetResult<User>> GetUserByIdAsync(Guid userId, bool hydrate) =>
        _authorizationProvider.IsAuthorizedForOwnUser(userId)
            ? _inner.GetUserByIdAsync(userId, hydrate)
            : Task.FromResult(
                new GetResult<User>(
                    RetrieveResultCode.UnauthorizedError,
                    $"Unauthorized to read user {userId}",
                    null
                )
            );
}
