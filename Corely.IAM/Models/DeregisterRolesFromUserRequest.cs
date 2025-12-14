namespace Corely.IAM.Models;

public record DeregisterRolesFromUserRequest(List<int> RoleIds, int UserId);
