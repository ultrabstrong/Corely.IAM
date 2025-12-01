using AutoMapper;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Permissions.Models;

namespace Corely.IAM.Permissions.Mappers;

internal class PermissionProfile : Profile
{
    public PermissionProfile()
    {
        CreateMap<Permission, PermissionEntity>(MemberList.Source).ReverseMap();
    }
}
