namespace Corely.IAM.Roles.Models;

internal record CreateRoleRequest(string RoleName, int OwnerAccountId);
