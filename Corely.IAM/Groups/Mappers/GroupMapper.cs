using Corely.IAM.Groups.Entities;
using Corely.IAM.Groups.Models;

namespace Corely.IAM.Groups.Mappers;

internal static class GroupMapper
{
    public static Group ToGroup(this CreateGroupRequest request)
    {
        return new Group { Name = request.GroupName, AccountId = request.OwnerAccountId };
    }

    public static GroupEntity ToEntity(this Group group)
    {
        return new GroupEntity
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            AccountId = group.AccountId,
        };
    }

    public static Group ToModel(this GroupEntity entity)
    {
        return new Group
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            AccountId = entity.AccountId,
        };
    }
}
