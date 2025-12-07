using Corely.Common.Extensions;
using Corely.IAM.Auth.Constants;
using Corely.IAM.Auth.Providers;
using Corely.IAM.Groups.Models;
using Corely.IAM.Permissions.Constants;

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
}
