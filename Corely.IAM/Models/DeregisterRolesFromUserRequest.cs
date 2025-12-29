namespace Corely.IAM.Models;

public record DeregisterRolesFromUserRequest(List<Guid> RoleIds, Guid UserId);
