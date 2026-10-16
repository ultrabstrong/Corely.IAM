using Corely.IAM.Permissions.Entities;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Security.Enums;

namespace Corely.IAM.Permissions.Mappers;

internal static class PermissionMapper
{
    extension(ResourceTypeInfo type)
    {
        public PermissionEntity ToOwnerDefaultEntity(Guid accountId) =>
            new()
            {
                Id = Guid.CreateVersion7(),
                AccountId = accountId,
                ResourceType = type.Name,
                ResourceId = Guid.Empty,
                Create = type.OwnerActions.Contains(AuthAction.Create),
                Read = type.OwnerActions.Contains(AuthAction.Read),
                Update = type.OwnerActions.Contains(AuthAction.Update),
                Delete = type.OwnerActions.Contains(AuthAction.Delete),
                Execute = type.OwnerActions.Contains(AuthAction.Execute),
                Description = type.OwnerDefaultDescription(),
                IsSystemDefined = true,
            };
    }

    extension(CreatePermissionRequest request)
    {
        public Permission ToPermission()
        {
            return new Permission
            {
                AccountId = request.OwnerAccountId,
                ResourceType = request.ResourceType,
                ResourceId = request.ResourceId,
                Create = request.Create,
                Read = request.Read,
                Update = request.Update,
                Delete = request.Delete,
                Execute = request.Execute,
                Description = request.Description,
            };
        }
    }

    extension(Permission permission)
    {
        public PermissionEntity ToEntity()
        {
            return new PermissionEntity
            {
                Id = permission.Id,
                Description = permission.Description,
                AccountId = permission.AccountId,
                ResourceType = permission.ResourceType,
                ResourceId = permission.ResourceId,
                Create = permission.Create,
                Read = permission.Read,
                Update = permission.Update,
                Delete = permission.Delete,
                Execute = permission.Execute,
            };
        }
    }

    extension(PermissionEntity entity)
    {
        public Permission ToModel()
        {
            return new Permission
            {
                Id = entity.Id,
                Description = entity.Description,
                AccountId = entity.AccountId,
                ResourceType = entity.ResourceType,
                ResourceId = entity.ResourceId,
                Create = entity.Create,
                Read = entity.Read,
                Update = entity.Update,
                Delete = entity.Delete,
                Execute = entity.Execute,
                IsSystemDefined = entity.IsSystemDefined,
            };
        }

        public bool Allows(AuthAction action) =>
            action switch
            {
                AuthAction.Create => entity.Create,
                AuthAction.Read => entity.Read,
                AuthAction.Update => entity.Update,
                AuthAction.Delete => entity.Delete,
                AuthAction.Execute => entity.Execute,
                _ => false,
            };

        public bool IsCoveredBy(IEnumerable<PermissionEntity> held)
        {
            var covering = held.Where(h =>
                    h.ResourceType == entity.ResourceType
                    && (h.ResourceId == Guid.Empty || h.ResourceId == entity.ResourceId)
                )
                .ToList();

            return Enum.GetValues<AuthAction>()
                .Where(entity.Allows)
                .All(action => covering.Any(h => h.Allows(action)));
        }
    }
}
