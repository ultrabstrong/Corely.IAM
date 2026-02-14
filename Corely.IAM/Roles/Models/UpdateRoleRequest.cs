namespace Corely.IAM.Roles.Models;

public record UpdateRoleRequest(Guid RoleId, string Name, string? Description);
