namespace Corely.IAM.Models;

public record RegisterRolesWithUserRequest(List<int> RoleIds, int UserId);
