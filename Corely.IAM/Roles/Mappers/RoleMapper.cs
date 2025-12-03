using Corely.IAM.Roles.Entities;
using Corely.IAM.Roles.Models;

namespace Corely.IAM.Roles.Mappers;

internal static class RoleMapper
{
    public static Role ToRole(this CreateRoleRequest request)
    {
        return new Role { Name = request.RoleName, AccountId = request.OwnerAccountId };
    }

    public static RoleEntity ToEntity(this Role role)
    {
        return new RoleEntity
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystemDefined = role.IsSystemDefined,
            AccountId = role.AccountId,
        };
    }

    public static Role ToModel(this RoleEntity entity)
    {
        return new Role
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsSystemDefined = entity.IsSystemDefined,
            AccountId = entity.AccountId,
        };
    }
}
