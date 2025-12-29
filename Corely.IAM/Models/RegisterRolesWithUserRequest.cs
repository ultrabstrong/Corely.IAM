namespace Corely.IAM.Models;

public record RegisterRolesWithUserRequest(List<Guid> RoleIds, Guid UserId);
