namespace Corely.IAM.Models;

public record RegisterRolesWithGroupRequest(List<Guid> RoleIds, Guid GroupId);
