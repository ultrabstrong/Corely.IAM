namespace Corely.IAM.Models;

public record RegisterRoleRequest(string RoleName, Guid AccountId);
