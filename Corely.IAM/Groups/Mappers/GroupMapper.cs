using Corely.IAM.Groups.Entities;
using Corely.IAM.Groups.Models;

namespace Corely.IAM.Groups.Mappers;

internal static class GroupMapper
{
    extension(CreateGroupRequest request)
    {
        public Group ToGroup()
        {
            return new Group { Name = request.GroupName, AccountId = request.OwnerAccountId };
        }
    }

    extension(Group group)
    {
        public GroupEntity ToEntity()
        {
            return new GroupEntity
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description,
                AccountId = group.AccountId,
            };
        }
    }

    extension(GroupEntity entity)
    {
        public Group ToModel()
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
}
