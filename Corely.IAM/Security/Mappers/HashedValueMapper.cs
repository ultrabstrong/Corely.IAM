using Corely.Security.Hashing.Factories;
using Corely.Security.Hashing.Models;

namespace Corely.IAM.Security.Mappers;

internal static class HashedValueMapper
{
    extension(IHashedValue? source)
    {
        public string? ToHashString()
        {
            return source?.Hash;
        }
    }

    extension(string source)
    {
        public IHashedValue ToHashedValue(IHashProviderFactory hashProviderFactory)
        {
            var hashProvider = hashProviderFactory.GetProviderToVerify(source);
            return new HashedValue(hashProvider) { Hash = source };
        }

        public IHashedValue ToHashedValueFromPlainText(IHashProviderFactory hashProviderFactory)
        {
            var hashProvider = hashProviderFactory.GetDefaultProvider();
            var hashedValue = new HashedValue(hashProvider);
            hashedValue.Set(source);
            return hashedValue;
        }

        public IHashedValue ToHashedValueFromPlainText(
            IHashProviderFactory hashProviderFactory,
            string providerCode
        )
        {
            var hashProvider = hashProviderFactory.GetProvider(providerCode);
            var hashedValue = new HashedValue(hashProvider);
            hashedValue.Set(source);
            return hashedValue;
        }
    }
}
