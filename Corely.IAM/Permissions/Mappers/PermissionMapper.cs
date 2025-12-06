using Corely.IAM.Permissions.Entities;
using Corely.IAM.Permissions.Models;

namespace Corely.IAM.Permissions.Mappers;

internal static class PermissionMapper
{
    public static Permission ToPermission(this CreatePermissionRequest request)
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

    public static PermissionEntity ToEntity(this Permission permission)
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

    public static Permission ToModel(this PermissionEntity entity)
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
        };
    }
}
