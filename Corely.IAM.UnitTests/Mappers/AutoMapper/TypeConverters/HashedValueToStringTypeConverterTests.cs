using AutoFixture;
using Corely.IAM.Mappers.AutoMapper.TypeConverters;
using Corely.IAM.UnitTests.ClassData;
using Corely.Security.Hashing.Models;
using Corely.Security.Hashing.Providers;

namespace Corely.IAM.UnitTests.Mappers.AutoMapper.TypeConverters;

public class HashedValueToStringTypeConverterTests
{
    private readonly HashedValueToStringTypeConverter _converter = new();
    private readonly Fixture _fixture = new();

    [Fact]
    public void Convert_ReturnsString()
    {
        var value = _fixture.Create<string>();
        var hashedValue = new HashedValue(Mock.Of<IHashProvider>()) { Hash = value };

        var result = _converter.Convert(hashedValue, default, default);

        Assert.Equal(value, result);
    }

    [Fact]
    public void Convert_ReturnsNull_WithNullHashValue()
    {
        var result = _converter.Convert(null, default, default);

        Assert.Null(result);
    }

    [Theory, ClassData(typeof(NullEmptyAndWhitespace))]
    public void Convert_ReturnsNullEmptyOrWhitespace(string value)
    {
        var hashedValue = new HashedValue(Mock.Of<IHashProvider>()) { Hash = value };

        var result = _converter.Convert(hashedValue, default, default);

        Assert.Equal(value, result);
    }
}
