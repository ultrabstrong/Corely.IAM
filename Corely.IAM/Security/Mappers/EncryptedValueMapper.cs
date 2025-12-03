using Corely.Security.Encryption.Factories;
using Corely.Security.Encryption.Models;

namespace Corely.IAM.Security.Mappers;

internal static class EncryptedValueMapper
{
    public static string? ToEncryptedString(this ISymmetricEncryptedValue? source)
    {
        return source?.Secret;
    }

    public static ISymmetricEncryptedValue ToEncryptedValue(
        this string source,
        ISymmetricEncryptionProviderFactory encryptionProviderFactory
    )
    {
        var encryptionProvider = encryptionProviderFactory.GetProviderForDecrypting(source);
        return new SymmetricEncryptedValue(encryptionProvider) { Secret = source };
    }
}
