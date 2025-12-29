namespace Corely.IAM.Groups.Models;

internal record AssignRolesToGroupRequest(List<Guid> RoleIds, Guid GroupId);
