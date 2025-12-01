using AutoMapper;
using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.BasicAuths.Models;

namespace Corely.IAM.BasicAuths.Mappers;

internal sealed class BasicAuthProfile : Profile
{
    public BasicAuthProfile()
    {
        CreateMap<BasicAuth, BasicAuthEntity>(MemberList.Source).ReverseMap();
    }
}
