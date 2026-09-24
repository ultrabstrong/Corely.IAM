using Corely.Security.Encryption.Factories;
using Corely.Security.Encryption.Models;

namespace Corely.IAM.Security.Mappers;

internal static class EncryptedValueMapper
{
    extension(ISymmetricEncryptedValue? source)
    {
        public string? ToEncryptedString()
        {
            return source?.Secret;
        }
    }

    extension(string source)
    {
        public ISymmetricEncryptedValue ToEncryptedValue(
            ISymmetricEncryptionProviderFactory encryptionProviderFactory
        )
        {
            var encryptionProvider = encryptionProviderFactory.GetProviderForDecrypting(source);
            return new SymmetricEncryptedValue(encryptionProvider) { Secret = source };
        }
    }
}
