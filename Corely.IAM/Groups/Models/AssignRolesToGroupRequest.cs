namespace Corely.IAM.Groups.Models;

internal record AssignRolesToGroupRequest(List<int> RoleIds, int GroupId);
