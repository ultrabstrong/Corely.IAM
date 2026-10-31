using Corely.IAM.Security.Enums;
using Corely.IAM.Web.Extensions;

namespace Corely.IAM.Web.UnitTests.Extensions;

public class KeyTypeExtensionsTests
{
    [Theory]
    [InlineData(KeyType.SymmetricEncryption)]
    [InlineData(KeyType.AsymmetricEncryption)]
    public void RotationWarning_ExplainsReEncrypt_ForEncryptionKeys(KeyType keyType) =>
        Assert.Contains("Re-encrypt", keyType.RotationWarning());

    [Fact]
    public void RotationWarning_ExplainsOldSignatures_ForSigningKey() =>
        Assert.Contains(
            "signatures made before still verify",
            KeyType.AsymmetricSignature.RotationWarning()
        );

    [Theory]
    [InlineData(KeyType.SymmetricEncryption)]
    [InlineData(KeyType.AsymmetricEncryption)]
    [InlineData(KeyType.AsymmetricSignature)]
    public void CopyKeyWarnings_SayItIsUncommonSecretAndNotToShare_ForEveryKey(KeyType keyType)
    {
        var warnings = string.Join(" ", keyType.CopyKeyWarnings());

        Assert.Contains("usually do not need this", warnings);
        Assert.Contains("This app handles", warnings);
        Assert.Contains("This key is a secret", warnings);
        Assert.Contains("somewhere secure", warnings);
        Assert.Contains("anyone you do not trust", warnings);
        Assert.Contains("Rotating later does not cancel this copy", warnings);
        Assert.DoesNotContain("DocsToData", warnings);
    }

    [Theory]
    [InlineData(KeyType.SymmetricEncryption)]
    [InlineData(KeyType.AsymmetricEncryption)]
    public void CopyKeyWarnings_SayAnyHolderCanRead_ForEncryptionKeys(KeyType keyType) =>
        Assert.Contains(
            "Anyone holding this key can read data",
            string.Join(" ", keyType.CopyKeyWarnings())
        );

    [Fact]
    public void CopyKeyWarnings_SayAnyHolderCanSign_ForTheSigningKey() =>
        Assert.Contains(
            "Anyone holding this key can sign as this account",
            string.Join(" ", KeyType.AsymmetricSignature.CopyKeyWarnings())
        );

    [Fact]
    public void RotationWarning_Throws_ForUnknownKeyType() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ((KeyType)99).RotationWarning());
}
