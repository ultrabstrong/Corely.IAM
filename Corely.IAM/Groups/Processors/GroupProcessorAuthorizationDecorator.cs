using Corely.Common.Extensions;
using Corely.IAM.Groups.Models;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Providers;

namespace Corely.IAM.Groups.Processors;

internal class GroupProcessorAuthorizationDecorator(
    IGroupProcessor inner,
    IAuthorizationProvider authorizationProvider
) : IGroupProcessor
{
    private readonly IGroupProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));

    public async Task<CreateGroupResult> CreateGroupAsync(CreateGroupRequest request) =>
        await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Create,
            PermissionConstants.GROUP_RESOURCE_TYPE
        )
            ? await _inner.CreateGroupAsync(request)
            : new CreateGroupResult(
                CreateGroupResultCode.UnauthorizedError,
                "Unauthorized to create group",
                -1
            );

    public async Task<AddUsersToGroupResult> AddUsersToGroupAsync(AddUsersToGroupRequest request) =>
        await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Update,
            PermissionConstants.GROUP_RESOURCE_TYPE,
            request.GroupId
        )
            ? await _inner.AddUsersToGroupAsync(request)
            : new AddUsersToGroupResult(
                AddUsersToGroupResultCode.UnauthorizedError,
                $"Unauthorized to update group {request.GroupId}",
                0,
                []
            );

    public async Task<RemoveUsersFromGroupResult> RemoveUsersFromGroupAsync(
        RemoveUsersFromGroupRequest request
    ) =>
        await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Update,
            PermissionConstants.GROUP_RESOURCE_TYPE,
            request.GroupId
        )
            ? await _inner.RemoveUsersFromGroupAsync(request)
            : new RemoveUsersFromGroupResult(
                RemoveUsersFromGroupResultCode.UnauthorizedError,
                $"Unauthorized to update group {request.GroupId}",
                0,
                []
            );

    public async Task<AssignRolesToGroupResult> AssignRolesToGroupAsync(
        AssignRolesToGroupRequest request
    ) =>
        await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Update,
            PermissionConstants.GROUP_RESOURCE_TYPE,
            request.GroupId
        )
            ? await _inner.AssignRolesToGroupAsync(request)
            : new AssignRolesToGroupResult(
                AssignRolesToGroupResultCode.UnauthorizedError,
                $"Unauthorized to update group {request.GroupId}",
                0,
                []
            );

    public async Task<RemoveRolesFromGroupResult> RemoveRolesFromGroupAsync(
        RemoveRolesFromGroupRequest request
    ) =>
        request.BypassAuthorization
        || await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Update,
            PermissionConstants.GROUP_RESOURCE_TYPE,
            request.GroupId
        )
            ? await _inner.RemoveRolesFromGroupAsync(request)
            : new RemoveRolesFromGroupResult(
                RemoveRolesFromGroupResultCode.UnauthorizedError,
                $"Unauthorized to update group {request.GroupId}",
                0,
                []
            );

    public async Task<DeleteGroupResult> DeleteGroupAsync(int groupId) =>
        await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Delete,
            PermissionConstants.GROUP_RESOURCE_TYPE,
            groupId
        )
            ? await _inner.DeleteGroupAsync(groupId)
            : new DeleteGroupResult(
                DeleteGroupResultCode.UnauthorizedError,
                $"Unauthorized to delete group {groupId}"
            );
}
