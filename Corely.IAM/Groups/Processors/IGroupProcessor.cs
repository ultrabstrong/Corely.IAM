using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Security.Models;

namespace Corely.IAM.Groups.Processors;

internal interface IGroupProcessor
{
    Task<CreateGroupResult> CreateGroupAsync(CreateGroupRequest request);
    Task<AddUsersToGroupResult> AddUsersToGroupAsync(AddUsersToGroupRequest request);
    Task<RemoveUsersFromGroupResult> RemoveUsersFromGroupAsync(RemoveUsersFromGroupRequest request);
    Task<AddApiKeysToGroupResult> AddApiKeysToGroupAsync(AddApiKeysToGroupRequest request);
    Task<RemoveApiKeysFromGroupResult> RemoveApiKeysFromGroupAsync(
        RemoveApiKeysFromGroupRequest request
    );
    Task<AssignRolesToGroupResult> AssignRolesToGroupAsync(AssignRolesToGroupRequest request);
    Task<RemoveRolesFromGroupResult> RemoveRolesFromGroupAsync(RemoveRolesFromGroupRequest request);
    Task<ModifyResult> UpdateGroupAsync(UpdateGroupRequest request);
    Task<DeleteGroupResult> DeleteGroupAsync(Guid groupId, AccountScope accountId);
    Task<ListResult<Group>> ListGroupsAsync(
        ListGroupsRequest request,
        IReadOnlySet<Guid>? authorizedResourceIds = null
    );
    Task<GetResult<Group>> GetGroupByIdAsync(Guid groupId, bool hydrate, AccountScope accountId);
}
