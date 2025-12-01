using AutoMapper;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Roles.Models;

namespace Corely.IAM.Roles.Mappers;

internal class RoleProfile : Profile
{
    public RoleProfile()
    {
        CreateMap<Role, RoleEntity>(MemberList.Source).ReverseMap();
    }
}
