using AutoMapper;
using Corely.IAM.Permissions.Models;

namespace Corely.IAM.Permissions.Mappers;

internal class CreatePermissionRequestProfile : Profile
{
    public CreatePermissionRequestProfile()
    {
        CreateMap<CreatePermissionRequest, Permission>(MemberList.Source)
            .ForMember(m => m.Name, opt => opt.MapFrom(m => m.PermissionName))
            .ForMember(m => m.AccountId, opt => opt.MapFrom(m => m.OwnerAccountId));
    }
}
