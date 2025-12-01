using Corely.IAM.Roles.Models;

namespace Corely.IAM.Roles.Mappers;

internal static class RoleMapper
{
    public static Role ToRole(this CreateRoleRequest request)
    {
        return new Role { Name = request.RoleName, AccountId = request.OwnerAccountId };
    }
}
