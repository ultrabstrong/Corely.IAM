using AutoMapper;
using Corely.Common.Extensions;
using Corely.Security.Hashing.Factories;
using Corely.Security.Hashing.Models;

namespace Corely.IAM.Mappers.AutoMapper.ValueConverters;

internal sealed class PlainStringToHashedStringValueConverter
    : IValueConverter<string, IHashedValue>
{
    private readonly IHashProviderFactory _hashProviderFactory;

    public PlainStringToHashedStringValueConverter(IHashProviderFactory hashProviderFactory)
    {
        _hashProviderFactory = hashProviderFactory.ThrowIfNull(nameof(hashProviderFactory));
    }

    public IHashedValue Convert(string source, ResolutionContext context)
    {
        var hashProvider = _hashProviderFactory.GetDefaultProvider();
        var hashedValue = new HashedValue(hashProvider);
        hashedValue.Set(source);
        return hashedValue;
    }
}
