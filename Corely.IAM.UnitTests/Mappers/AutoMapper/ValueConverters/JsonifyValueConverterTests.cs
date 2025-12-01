using System.Text.Json;
using AutoFixture;
using Corely.IAM.Mappers.AutoMapper.ValueConverters;

namespace Corely.IAM.UnitTests.Mappers.AutoMapper.ValueConverters;

public class JsonifyValueConverterTests
{
    private class TestClass
    {
        public string? Name { get; set; }
    }

    private readonly JsonifyValueConverter<TestClass> _converter = new();
    private readonly Fixture _fixture = new();

    [Fact]
    public void Convert_ReturnsJson()
    {
        var value = _fixture.Create<TestClass>();

        var result = _converter.Convert(value, default);

        Assert.NotNull(result);
        Assert.Equal(JsonSerializer.Serialize(value), result);
    }

    [Fact]
    public void Convert_ReturnsJson_WithNullSource()
    {
        TestClass? value = null;

        var result = _converter.Convert(null, default);

        Assert.NotNull(result);
        Assert.Equal(JsonSerializer.Serialize(value), result);
    }
}
