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

    public async Task<CreateGroupResult> CreateGroupAsync(CreateGroupRequest request)
    {
        await _authorizationProvider.AuthorizeAsync(
            PermissionConstants.GROUP_RESOURCE_TYPE,
            AuthAction.Create
        );
        return await _inner.CreateGroupAsync(request);
    }

    public async Task<AddUsersToGroupResult> AddUsersToGroupAsync(AddUsersToGroupRequest request)
    {
        await _authorizationProvider.AuthorizeAsync(
            PermissionConstants.GROUP_RESOURCE_TYPE,
            AuthAction.Update,
            request.GroupId
        );
        return await _inner.AddUsersToGroupAsync(request);
    }

    public async Task<RemoveUsersFromGroupResult> RemoveUsersFromGroupAsync(
        RemoveUsersFromGroupRequest request
    )
    {
        await _authorizationProvider.AuthorizeAsync(
            PermissionConstants.GROUP_RESOURCE_TYPE,
            AuthAction.Update,
            request.GroupId
        );
        return await _inner.RemoveUsersFromGroupAsync(request);
    }

    public async Task<AssignRolesToGroupResult> AssignRolesToGroupAsync(
        AssignRolesToGroupRequest request
    )
    {
        await _authorizationProvider.AuthorizeAsync(
            PermissionConstants.GROUP_RESOURCE_TYPE,
            AuthAction.Update,
            request.GroupId
        );
        return await _inner.AssignRolesToGroupAsync(request);
    }

    public async Task<RemoveRolesFromGroupResult> RemoveRolesFromGroupAsync(
        RemoveRolesFromGroupRequest request
    )
    {
        if (!request.BypassAuthorization)
            await _authorizationProvider.AuthorizeAsync(
                PermissionConstants.GROUP_RESOURCE_TYPE,
                AuthAction.Update,
                request.GroupId
            );
        return await _inner.RemoveRolesFromGroupAsync(request);
    }

    public async Task<DeleteGroupResult> DeleteGroupAsync(int groupId)
    {
        await _authorizationProvider.AuthorizeAsync(
            PermissionConstants.GROUP_RESOURCE_TYPE,
            AuthAction.Delete,
            groupId
        );
        return await _inner.DeleteGroupAsync(groupId);
    }
}
