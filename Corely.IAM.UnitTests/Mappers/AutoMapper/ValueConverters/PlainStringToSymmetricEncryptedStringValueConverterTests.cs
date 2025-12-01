using AutoFixture;
using Corely.IAM.Mappers.AutoMapper.ValueConverters;
using Corely.IAM.UnitTests.ClassData;
using Corely.Security.Encryption.Factories;
using Corely.Security.Encryption.Models;

namespace Corely.IAM.UnitTests.Mappers.AutoMapper.ValueConverters;

public class PlainStringToSymmetricEncryptedStringValueConverterTests
{
    private readonly Fixture _fixture = new();
    private readonly ISecurityConfigurationProvider _securityConfigurationProviderMock;
    private readonly PlainStringToSymmetricEncryptedStringValueConverter _valueConverter;

    public PlainStringToSymmetricEncryptedStringValueConverterTests()
    {
        var serviceFactory = new ServiceFactory();
        _valueConverter = new(
            serviceFactory.GetRequiredService<ISecurityConfigurationProvider>(),
            serviceFactory.GetRequiredService<ISymmetricEncryptionProviderFactory>()
        );
        _securityConfigurationProviderMock =
            serviceFactory.GetRequiredService<ISecurityConfigurationProvider>();
    }

    [Fact]
    public void Convert_ReturnsSymmetricEncryptedValue()
    {
        var plainString = _fixture.Create<string>();

        var result = _valueConverter.Convert(plainString, null);

        Assert.NotNull(result);
        Assert.IsType<SymmetricEncryptedValue>(result);
        Assert.Equal(
            plainString,
            result.GetDecrypted(_securityConfigurationProviderMock.GetSystemSymmetricKey())
        );
    }

    [Theory, ClassData(typeof(EmptyAndWhitespace))]
    public void Convert_ReturnsSymmetricEncryptedValue_WithEmptyWhitespace(string value)
    {
        var result = _valueConverter.Convert(value, default);

        Assert.NotNull(result.Secret);
        Assert.Equal(
            value,
            result.GetDecrypted(_securityConfigurationProviderMock.GetSystemSymmetricKey())
        );
    }
}
