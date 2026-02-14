using Corely.Common.Filtering;
using Corely.Common.Filtering.Ordering;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;

namespace Corely.IAM.Groups.Processors;

internal interface IGroupProcessor
{
    Task<CreateGroupResult> CreateGroupAsync(CreateGroupRequest request);
    Task<AddUsersToGroupResult> AddUsersToGroupAsync(AddUsersToGroupRequest request);
    Task<RemoveUsersFromGroupResult> RemoveUsersFromGroupAsync(RemoveUsersFromGroupRequest request);
    Task<AssignRolesToGroupResult> AssignRolesToGroupAsync(AssignRolesToGroupRequest request);
    Task<RemoveRolesFromGroupResult> RemoveRolesFromGroupAsync(RemoveRolesFromGroupRequest request);
    Task<ModifyResult> UpdateGroupAsync(UpdateGroupRequest request);
    Task<DeleteGroupResult> DeleteGroupAsync(Guid groupId);
    Task<ListResult<Group>> ListGroupsAsync(
        FilterBuilder<Group>? filter,
        OrderBuilder<Group>? order,
        int skip,
        int take
    );
    Task<GetResult<Group>> GetGroupByIdAsync(Guid groupId, bool hydrate);
}
