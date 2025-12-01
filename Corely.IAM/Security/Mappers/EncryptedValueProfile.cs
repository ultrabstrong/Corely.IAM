using AutoMapper;
using Corely.IAM.Mappers.AutoMapper.TypeConverters;
using Corely.Security.Encryption.Models;

namespace Corely.IAM.Security.Mappers;

internal class EncryptedValueProfile : Profile
{
    public EncryptedValueProfile()
    {
        CreateMap<ISymmetricEncryptedValue, string?>()
            .ConvertUsing<SymmetricEncryptedValueToStringTypeConverter>();
        CreateMap<string, ISymmetricEncryptedValue>()
            .ConvertUsing<SymmetricEncryptedStringToEncryptedValueTypeConverter>();
    }
}
