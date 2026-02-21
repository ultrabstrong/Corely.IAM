namespace Corely.IAM.Groups.Models;

internal record RemoveRolesFromGroupRequest(List<Guid> RoleIds, Guid GroupId);
