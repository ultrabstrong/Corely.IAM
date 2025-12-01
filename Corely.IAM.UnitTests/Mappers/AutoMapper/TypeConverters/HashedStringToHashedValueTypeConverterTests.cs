using AutoFixture;
using Corely.IAM.Mappers.AutoMapper.TypeConverters;
using Corely.IAM.UnitTests.ClassData;
using Corely.Security.Hashing.Factories;
using Corely.Security.Hashing.Providers;

namespace Corely.IAM.UnitTests.Mappers.AutoMapper.TypeConverters;

public class HashedStringToHashedValueTypeConverterTests
{
    private readonly HashStringToHashedValueTypeConverter _converter;
    private readonly Fixture _fixture = new();

    public HashedStringToHashedValueTypeConverterTests()
    {
        var hashProvider = Mock.Of<IHashProvider>();
        var hashProviderFactory = Mock.Of<IHashProviderFactory>(f =>
            f.GetProviderToVerify(It.IsAny<string>()) == hashProvider
        );

        _converter = new(hashProviderFactory);
    }

    [Fact]
    public void Convert_ReturnsHashedValue()
    {
        var value = _fixture.Create<string>();

        var result = _converter.Convert(value, default, default);

        Assert.Equal(value, result.Hash);
    }

    [Theory, ClassData(typeof(NullEmptyAndWhitespace))]
    public void Convert_ReturnsNullEmptyOrWhitespace(string value)
    {
        var result = _converter.Convert(value, default, default);

        Assert.Equal(value, result.Hash);
    }
}
