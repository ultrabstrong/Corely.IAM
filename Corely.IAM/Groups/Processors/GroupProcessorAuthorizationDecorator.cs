using Corely.Common.Extensions;
using Corely.IAM.ApiKeys.Models;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Models;

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
        _authorizationProvider.HasAccountContext(request.OwnerAccountId)
        && await _authorizationProvider.IsAuthorizedAsync(AuthAction.Create, Group.RESOURCE_TYPE)
            ? await _inner.CreateGroupAsync(request)
            : new CreateGroupResult(
                CreateGroupResultCode.UnauthorizedError,
                "Unauthorized to create group",
                Guid.Empty
            );

    public async Task<AddUsersToGroupResult> AddUsersToGroupAsync(AddUsersToGroupRequest request)
    {
        if (
            !_authorizationProvider.HasAccountContext(request.AccountId.Id)
            || !await _authorizationProvider.IsAuthorizedAsync(
                AuthAction.Update,
                Group.RESOURCE_TYPE,
                request.GroupId
            )
            || !await _authorizationProvider.IsAuthorizedAsync(
                AuthAction.Read,
                User.RESOURCE_TYPE,
                [.. request.UserIds]
            )
        )
        {
            return new AddUsersToGroupResult(
                AddUsersToGroupResultCode.UnauthorizedError,
                $"Unauthorized to update group {request.GroupId} or read users",
                0,
                []
            );
        }

        if (!await _authorizationProvider.CanGrantGroupAsync(request.GroupId))
        {
            return new AddUsersToGroupResult(
                AddUsersToGroupResultCode.UnauthorizedError,
                "Cannot grant permissions you do not hold",
                0,
                []
            );
        }

        return await _inner.AddUsersToGroupAsync(request);
    }

    public async Task<RemoveUsersFromGroupResult> RemoveUsersFromGroupAsync(
        RemoveUsersFromGroupRequest request
    ) =>
        _authorizationProvider.HasAccountContext(request.AccountId.Id)
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Update,
            Group.RESOURCE_TYPE,
            request.GroupId
        )
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Read,
            User.RESOURCE_TYPE,
            [.. request.UserIds]
        )
            ? await _inner.RemoveUsersFromGroupAsync(request)
            : new RemoveUsersFromGroupResult(
                RemoveUsersFromGroupResultCode.UnauthorizedError,
                $"Unauthorized to update group {request.GroupId} or read users",
                0,
                []
            );

    public async Task<AddApiKeysToGroupResult> AddApiKeysToGroupAsync(
        AddApiKeysToGroupRequest request
    )
    {
        if (
            !_authorizationProvider.HasAccountContext(request.AccountId.Id)
            || !await _authorizationProvider.IsAuthorizedAsync(
                AuthAction.Update,
                Group.RESOURCE_TYPE,
                request.GroupId
            )
            || !await _authorizationProvider.IsAuthorizedAsync(
                AuthAction.Read,
                ApiKey.RESOURCE_TYPE,
                [.. request.ApiKeyIds]
            )
        )
        {
            return new AddApiKeysToGroupResult(
                AddApiKeysToGroupResultCode.UnauthorizedError,
                $"Unauthorized to update group {request.GroupId} or read API keys",
                0,
                []
            );
        }

        if (!await _authorizationProvider.CanGrantGroupAsync(request.GroupId))
        {
            return new AddApiKeysToGroupResult(
                AddApiKeysToGroupResultCode.UnauthorizedError,
                "Cannot grant permissions you do not hold",
                0,
                []
            );
        }

        return await _inner.AddApiKeysToGroupAsync(request);
    }

    public async Task<RemoveApiKeysFromGroupResult> RemoveApiKeysFromGroupAsync(
        RemoveApiKeysFromGroupRequest request
    ) =>
        _authorizationProvider.HasAccountContext(request.AccountId.Id)
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Update,
            Group.RESOURCE_TYPE,
            request.GroupId
        )
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Read,
            ApiKey.RESOURCE_TYPE,
            [.. request.ApiKeyIds]
        )
            ? await _inner.RemoveApiKeysFromGroupAsync(request)
            : new RemoveApiKeysFromGroupResult(
                RemoveApiKeysFromGroupResultCode.UnauthorizedError,
                $"Unauthorized to update group {request.GroupId} or read API keys",
                0,
                []
            );

    public async Task<AssignRolesToGroupResult> AssignRolesToGroupAsync(
        AssignRolesToGroupRequest request
    )
    {
        if (
            !_authorizationProvider.HasAccountContext(request.AccountId.Id)
            || !await _authorizationProvider.IsAuthorizedAsync(
                AuthAction.Update,
                Group.RESOURCE_TYPE,
                request.GroupId
            )
            || !await _authorizationProvider.IsAuthorizedAsync(
                AuthAction.Read,
                Role.RESOURCE_TYPE,
                [.. request.RoleIds]
            )
        )
        {
            return new AssignRolesToGroupResult(
                AssignRolesToGroupResultCode.UnauthorizedError,
                $"Unauthorized to update group {request.GroupId} or read roles",
                0,
                []
            );
        }

        if (!await _authorizationProvider.CanGrantRolesAsync(request.RoleIds))
        {
            return new AssignRolesToGroupResult(
                AssignRolesToGroupResultCode.UnauthorizedError,
                "Cannot grant permissions you do not hold",
                0,
                []
            );
        }

        return await _inner.AssignRolesToGroupAsync(request);
    }

    public async Task<RemoveRolesFromGroupResult> RemoveRolesFromGroupAsync(
        RemoveRolesFromGroupRequest request
    ) =>
        _authorizationProvider.HasAccountContext(request.AccountId.Id)
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Update,
            Group.RESOURCE_TYPE,
            request.GroupId
        )
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Read,
            Role.RESOURCE_TYPE,
            [.. request.RoleIds]
        )
            ? await _inner.RemoveRolesFromGroupAsync(request)
            : new RemoveRolesFromGroupResult(
                RemoveRolesFromGroupResultCode.UnauthorizedError,
                $"Unauthorized to update group {request.GroupId} or read roles",
                0,
                []
            );

    public async Task<ModifyResult> UpdateGroupAsync(UpdateGroupRequest request) =>
        _authorizationProvider.HasAccountContext(request.AccountId)
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Update,
            Group.RESOURCE_TYPE,
            request.GroupId
        )
            ? await _inner.UpdateGroupAsync(request)
            : new ModifyResult(
                ModifyResultCode.UnauthorizedError,
                $"Unauthorized to update group {request.GroupId}"
            );

    public async Task<DeleteGroupResult> DeleteGroupAsync(Guid groupId, AccountScope accountId) =>
        _authorizationProvider.HasAccountContext(accountId.Id)
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Delete,
            Group.RESOURCE_TYPE,
            groupId
        )
            ? await _inner.DeleteGroupAsync(groupId, accountId)
            : new DeleteGroupResult(
                DeleteGroupResultCode.UnauthorizedError,
                $"Unauthorized to delete group {groupId}"
            );

    public async Task<ListResult<Group>> ListGroupsAsync(
        ListGroupsRequest request,
        IReadOnlySet<Guid>? authorizedResourceIds = null
    )
    {
        if (
            !_authorizationProvider.HasAccountContext(request.AccountId)
            || !await _authorizationProvider.IsAuthorizedAsync(AuthAction.Read, Group.RESOURCE_TYPE)
        )
        {
            return new ListResult<Group>(
                RetrieveResultCode.UnauthorizedError,
                "Unauthorized to list groups",
                null
            );
        }

        // Never the caller's scope: it could only widen it.
        return await _inner.ListGroupsAsync(
            request,
            await _authorizationProvider.GetAuthorizedResourceIdsAsync(
                AuthAction.Read,
                Group.RESOURCE_TYPE
            )
        );
    }

    public async Task<GetResult<Group>> GetGroupByIdAsync(
        Guid groupId,
        bool hydrate,
        AccountScope accountId
    ) =>
        _authorizationProvider.HasAccountContext(accountId.Id)
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Read,
            Group.RESOURCE_TYPE,
            groupId
        )
            ? await _inner.GetGroupByIdAsync(groupId, hydrate, accountId)
            : new GetResult<Group>(
                RetrieveResultCode.UnauthorizedError,
                $"Unauthorized to read group {groupId}",
                null
            );
}
