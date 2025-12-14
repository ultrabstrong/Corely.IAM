using Corely.IAM.Groups.Models;

namespace Corely.IAM.Groups.Processors;

internal interface IGroupProcessor
{
    Task<CreateGroupResult> CreateGroupAsync(CreateGroupRequest request);
    Task<AddUsersToGroupResult> AddUsersToGroupAsync(AddUsersToGroupRequest request);
    Task<RemoveUsersFromGroupResult> RemoveUsersFromGroupAsync(RemoveUsersFromGroupRequest request);
    Task<AssignRolesToGroupResult> AssignRolesToGroupAsync(AssignRolesToGroupRequest request);
    Task<RemoveRolesFromGroupResult> RemoveRolesFromGroupAsync(RemoveRolesFromGroupRequest request);
    Task<DeleteGroupResult> DeleteGroupAsync(int groupId);
}
