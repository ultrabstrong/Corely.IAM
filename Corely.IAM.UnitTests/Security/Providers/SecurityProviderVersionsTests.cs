using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.UnitTests.Security.Providers;

public class SecurityProviderVersionsTests
{
    private readonly ISecurityProvider _securityProvider =
        new ServiceFactory().GetRequiredService<ISecurityProvider>();

    private SymmetricKey Symmetric(int version, DateTime? compromisedUtc = null)
    {
        var key = _securityProvider.GetSymmetricEncryptionKeyEncryptedWithSystemKey();
        key.Version = version;
        key.CompromisedUtc = compromisedUtc;
        return key;
    }

    private AsymmetricKey Encryption(int version, DateTime? compromisedUtc = null)
    {
        var key = _securityProvider.GetAsymmetricEncryptionKeyEncryptedWithSystemKey();
        key.Version = version;
        key.CompromisedUtc = compromisedUtc;
        return key;
    }

    private AsymmetricKey Signature(int version, DateTime? compromisedUtc = null)
    {
        var key = _securityProvider.GetAsymmetricSignatureKeyEncryptedWithSystemKey();
        key.Version = version;
        key.CompromisedUtc = compromisedUtc;
        return key;
    }

    [Fact]
    public void BuildSymmetricEncryptionProvider_ListsEveryVersion_WithNoPublicKey()
    {
        var keys = new[] { Symmetric(1, DateTime.UnixEpoch), Symmetric(2) };

        var provider = _securityProvider.BuildSymmetricEncryptionProvider(keys);

        Assert.Equal(
            [
                new KeyVersionInfo(1, keys[0].ProviderName, null, true),
                new KeyVersionInfo(2, keys[1].ProviderName, null, false),
            ],
            provider.Versions
        );
        Assert.Equal([1], provider.CompromisedVersions);
    }

    [Fact]
    public void BuildAsymmetricEncryptionProvider_ListsEveryVersion_WithItsOwnPublicKey()
    {
        var keys = new[] { Encryption(1), Encryption(2, DateTime.UnixEpoch), Encryption(3) };

        var provider = _securityProvider.BuildAsymmetricEncryptionProvider(keys);

        Assert.Equal(
            keys.Select(k => new KeyVersionInfo(
                k.Version,
                k.ProviderName,
                k.PublicKey,
                k.CompromisedUtc != null
            )),
            provider.Versions
        );
        Assert.Equal([2], provider.CompromisedVersions);
    }

    [Fact]
    public void BuildAsymmetricSignatureProvider_ListsEveryVersion_WithItsOwnPublicKey()
    {
        var keys = new[] { Signature(1, DateTime.UnixEpoch), Signature(2) };

        var provider = _securityProvider.BuildAsymmetricSignatureProvider(keys);

        Assert.Equal(
            keys.Select(k => new KeyVersionInfo(
                k.Version,
                k.ProviderName,
                k.PublicKey,
                k.CompromisedUtc != null
            )),
            provider.Versions
        );
        Assert.Equal([1], provider.CompromisedVersions);
    }

    [Fact]
    public void KeyVersionInfo_HasNoPlaceForASecret() =>
        Assert.Equal(
            [
                nameof(KeyVersionInfo.IsCompromised),
                nameof(KeyVersionInfo.ProviderName),
                nameof(KeyVersionInfo.PublicKey),
                nameof(KeyVersionInfo.Version),
            ],
            typeof(KeyVersionInfo)
                .GetProperties()
                .Select(p => p.Name)
                .Where(n => n != "EqualityContract")
                .Order()
        );
}
