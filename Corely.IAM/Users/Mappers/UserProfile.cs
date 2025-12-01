using AutoMapper;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;

namespace Corely.IAM.Users.Mappers;

internal sealed class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserEntity>(MemberList.Source).ReverseMap();
    }
}
