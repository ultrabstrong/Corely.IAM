namespace Corely.IAM.Models;

public record RegisterRolesWithGroupRequest(List<int> RoleIds, int GroupId);
