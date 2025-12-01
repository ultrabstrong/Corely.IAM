using AutoFixture;
using Corely.IAM.Mappers.AutoMapper.TypeConverters;
using Corely.IAM.UnitTests.ClassData;
using Corely.Security.Encryption.Models;
using Corely.Security.Encryption.Providers;

namespace Corely.IAM.UnitTests.Mappers.AutoMapper.TypeConverters;

public class SymmetricEncryptedValueToStringTypeConverterTests
{
    private readonly SymmetricEncryptedValueToStringTypeConverter _converter = new();
    private readonly Fixture _fixture = new();

    [Fact]
    public void Convert_ReturnsString()
    {
        var value = _fixture.Create<string>();
        var encryptedValue = new SymmetricEncryptedValue(Mock.Of<ISymmetricEncryptionProvider>())
        {
            Secret = value,
        };

        var result = _converter.Convert(encryptedValue, default, default);

        Assert.Equal(value, result);
    }

    [Fact]
    public void Convert_ReturnsNull_WithNullSecretValue()
    {
        var result = _converter.Convert(null, default, default);

        Assert.Null(result);
    }

    [Theory, ClassData(typeof(NullEmptyAndWhitespace))]
    public void Convert_ReturnsNullEmptyOrWhitespace(string value)
    {
        var encryptedValue = new SymmetricEncryptedValue(Mock.Of<ISymmetricEncryptionProvider>())
        {
            Secret = value,
        };

        var result = _converter.Convert(encryptedValue, default, default);

        Assert.Equal(value, result);
    }
}
