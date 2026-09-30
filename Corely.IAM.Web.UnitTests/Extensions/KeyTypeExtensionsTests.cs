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

    [Fact]
    public void RotationWarning_Throws_ForUnknownKeyType() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ((KeyType)99).RotationWarning());
}
