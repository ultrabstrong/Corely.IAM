using Corely.IAM.Roles.Entities;
using Corely.IAM.Roles.Models;

namespace Corely.IAM.Roles.Mappers;

internal static class RoleMapper
{
    extension(CreateRoleRequest request)
    {
        public Role ToRole()
        {
            return new Role { Name = request.RoleName, AccountId = request.OwnerAccountId };
        }
    }

    extension(Role role)
    {
        public RoleEntity ToEntity()
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
    }

    extension(RoleEntity entity)
    {
        public Role ToModel()
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
}
