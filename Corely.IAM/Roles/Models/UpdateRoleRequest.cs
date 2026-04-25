namespace Corely.IAM.Roles.Models;

public record UpdateRoleRequest(Guid RoleId, Guid AccountId, string Name, string? Description);
