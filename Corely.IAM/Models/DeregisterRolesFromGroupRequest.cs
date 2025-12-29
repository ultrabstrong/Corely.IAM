namespace Corely.IAM.Models;

public record DeregisterRolesFromGroupRequest(List<Guid> RoleIds, Guid GroupId);
