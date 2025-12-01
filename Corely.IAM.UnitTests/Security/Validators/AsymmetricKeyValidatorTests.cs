using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Validators;
using Corely.IAM.UnitTests.ClassData;
using Corely.Security.Encryption.Models;
using Corely.Security.Encryption.Providers;
using FluentValidation.TestHelper;

namespace Corely.IAM.UnitTests.Security.Validators;

public class AsymmetricKeyValidatorTests
{
    private readonly AsymmetricKeyValidator _validator = new();

    [Theory, ClassData(typeof(NullEmptyAndWhitespace))]
    public void AsymmetricKeyValidator_HasValidationError_WhenPublicKeyInvalid(string publicKey)
    {
        var asymmetricKey = new AsymmetricKey { PublicKey = publicKey };

        var result = _validator.TestValidate(asymmetricKey);
        result.ShouldHaveValidationErrorFor(x => x.PublicKey);
    }

    [Fact]
    public void AsymmetricKeyValidator_HasValidationError_WhenPrivateKeyIsNull()
    {
        var asymmetricKey = new AsymmetricKey { PublicKey = "public key", PrivateKey = null };

        var result = _validator.TestValidate(asymmetricKey);
        result.ShouldHaveValidationErrorFor(x => x.PrivateKey);
    }

    [Theory, ClassData(typeof(NullEmptyAndWhitespace))]
    public void AsymmetricKeyValidator_HasValidationError_WhenPrivateKeyInvalid(string privateKey)
    {
        var asymmetricKey = new AsymmetricKey
        {
            PublicKey = "public key",
            PrivateKey = new SymmetricEncryptedValue(new AesEncryptionProvider())
            {
                Secret = privateKey,
            },
        };

        var result = _validator.TestValidate(asymmetricKey);
        result.ShouldHaveValidationErrorFor(x => x.PrivateKey.Secret);
    }

    [Fact]
    public void AsymmetricKeyValidator_HasValidationError_WhenVersionIsNegative()
    {
        var asymmetricKey = new AsymmetricKey
        {
            Version = AsymmetricKeyConstants.VERSION_MIN_VALUE - 1,
            PublicKey = "public key",
            PrivateKey = new SymmetricEncryptedValue(new AesEncryptionProvider())
            {
                Secret = "private key",
            },
        };

        var result = _validator.TestValidate(asymmetricKey);
        result.ShouldHaveValidationErrorFor(x => x.Version);
    }

    [Theory, ClassData(typeof(NullEmptyAndWhitespace))]
    public void AsymmetricKeyValidator_HasValidationError_WhenProviderTypeCodeInvalid(
        string providerTypeCode
    )
    {
        var asymmetricKey = new AsymmetricKey
        {
            Version = AsymmetricKeyConstants.VERSION_MIN_VALUE,
            PublicKey = "public key",
            PrivateKey = new SymmetricEncryptedValue(new AesEncryptionProvider())
            {
                Secret = "private key",
            },
            ProviderTypeCode = providerTypeCode,
        };
        var result = _validator.TestValidate(asymmetricKey);
        result.ShouldHaveValidationErrorFor(x => x.ProviderTypeCode);
    }
}
