using AutoMapper;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.Mappers.AutoMapper.ValueConverters;

namespace Corely.IAM.BasicAuths.Mappers;

internal class UpsertBasicAuthRequestProfile : Profile
{
    public UpsertBasicAuthRequestProfile()
    {
        CreateMap<UpsertBasicAuthRequest, BasicAuth>(MemberList.Source)
            .ForMember(
                dest => dest.Password,
                opt => opt.ConvertUsing<PlainStringToHashedStringValueConverter, string>()
            );
    }
}
