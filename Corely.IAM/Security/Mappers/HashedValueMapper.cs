using Corely.Security.Hashing.Factories;
using Corely.Security.Hashing.Models;

namespace Corely.IAM.Security.Mappers;

internal static class HashedValueMapper
{
    public static string? ToHashString(this IHashedValue? source)
    {
        return source?.Hash;
    }

    public static IHashedValue ToHashedValue(
        this string source,
        IHashProviderFactory hashProviderFactory
    )
    {
        var hashProvider = hashProviderFactory.GetProviderToVerify(source);
        return new HashedValue(hashProvider) { Hash = source };
    }

    public static IHashedValue ToHashedValueFromPlainText(
        this string source,
        IHashProviderFactory hashProviderFactory
    )
    {
        var hashProvider = hashProviderFactory.GetDefaultProvider();
        var hashedValue = new HashedValue(hashProvider);
        hashedValue.Set(source);
        return hashedValue;
    }
}
