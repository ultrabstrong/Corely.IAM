using Corely.Common.Extensions;
using Corely.IAM.Security.Providers;
using Corely.Security.Encryption.Factories;

namespace Corely.IAM.IdentityProviders.Providers;

internal sealed class IdentityProviderCredentialCipher(
    ISecurityConfigurationProvider securityConfigurationProvider,
    ISymmetricEncryptionProviderFactory encryptionProviderFactory
)
{
    private readonly ISecurityConfigurationProvider _securityConfigurationProvider =
        securityConfigurationProvider.ThrowIfNull(nameof(securityConfigurationProvider));
    private readonly ISymmetricEncryptionProviderFactory _encryptionProviderFactory =
        encryptionProviderFactory.ThrowIfNull(nameof(encryptionProviderFactory));

    public string Encrypt(string plaintext) =>
        _encryptionProviderFactory
            .GetDefaultProvider()
            .Encrypt(plaintext, _securityConfigurationProvider.GetSystemSymmetricKey());

    public string Decrypt(string encrypted) =>
        _encryptionProviderFactory
            .GetProviderForDecrypting(encrypted)
            .Decrypt(encrypted, _securityConfigurationProvider.GetSystemSymmetricKey());
}
