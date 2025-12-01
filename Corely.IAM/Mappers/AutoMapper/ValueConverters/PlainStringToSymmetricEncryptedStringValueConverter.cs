using AutoMapper;
using Corely.Common.Extensions;
using Corely.Security.Encryption.Factories;
using Corely.Security.Encryption.Models;
using Corely.Security.Encryption.Providers;

namespace Corely.IAM.Mappers.AutoMapper.ValueConverters;

internal sealed class PlainStringToSymmetricEncryptedStringValueConverter
    : IValueConverter<string, ISymmetricEncryptedValue>
{
    private readonly ISecurityConfigurationProvider _securityConfigurationProvider;
    private readonly ISymmetricEncryptionProvider _symmetricEncryptionProvider;

    public PlainStringToSymmetricEncryptedStringValueConverter(
        ISecurityConfigurationProvider securityConfigurationProvider,
        ISymmetricEncryptionProviderFactory symmetricEncryptionProviderFactory
    )
    {
        _securityConfigurationProvider = securityConfigurationProvider.ThrowIfNull(
            nameof(securityConfigurationProvider)
        );
        _symmetricEncryptionProvider = symmetricEncryptionProviderFactory
            .ThrowIfNull(nameof(symmetricEncryptionProviderFactory))
            .GetDefaultProvider();
    }

    public ISymmetricEncryptedValue Convert(string sourceMember, ResolutionContext context)
    {
        var systemKeyStoreProvider = _securityConfigurationProvider.GetSystemSymmetricKey();
        var secret = _symmetricEncryptionProvider.Encrypt(sourceMember, systemKeyStoreProvider);
        var symmetricEncryptedValue = new SymmetricEncryptedValue(_symmetricEncryptionProvider)
        {
            Secret = secret,
        };
        return symmetricEncryptedValue;
    }
}
