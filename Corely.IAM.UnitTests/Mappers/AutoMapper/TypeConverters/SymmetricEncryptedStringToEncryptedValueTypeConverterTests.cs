using AutoFixture;
using Corely.IAM.Mappers.AutoMapper.TypeConverters;
using Corely.IAM.UnitTests.ClassData;
using Corely.Security.Encryption.Factories;
using Corely.Security.Encryption.Providers;

namespace Corely.IAM.UnitTests.Mappers.AutoMapper.TypeConverters;

public class SymmetricEncryptedStringToEncryptedValueTypeConverterTests
{
    private readonly SymmetricEncryptedStringToEncryptedValueTypeConverter _converter;
    private readonly Fixture _fixture = new();

    public SymmetricEncryptedStringToEncryptedValueTypeConverterTests()
    {
        var encryptionProvider = Mock.Of<ISymmetricEncryptionProvider>();
        var encryptionProviderFactory = Mock.Of<ISymmetricEncryptionProviderFactory>(f =>
            f.GetProviderForDecrypting(It.IsAny<string>()) == encryptionProvider
        );

        _converter = new(encryptionProviderFactory);
    }

    [Fact]
    public void Convert_ReturnsEncryptedValue()
    {
        var value = _fixture.Create<string>();

        var result = _converter.Convert(value, default, default);

        Assert.Equal(value, result.Secret);
    }

    [Theory, ClassData(typeof(NullEmptyAndWhitespace))]
    public void Convert_ReturnsNullEmptyOrWhitespace(string value)
    {
        var result = _converter.Convert(value, default, default);

        Assert.Equal(value, result.Secret);
    }
}
