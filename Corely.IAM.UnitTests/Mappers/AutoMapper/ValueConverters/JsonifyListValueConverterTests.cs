using System.Text.Json;
using AutoFixture;
using Corely.IAM.Mappers.AutoMapper.ValueConverters;

namespace Corely.IAM.UnitTests.Mappers.AutoMapper.ValueConverters;

public class JsonifyListValueConverterTests
{
    private class TestClass
    {
        public string? Name { get; set; }
    }

    private readonly JsonifyListValueConverter<TestClass> _converter = new();
    private readonly Fixture _fixture = new();

    [Fact]
    public void Convert_ReturnsJsonList()
    {
        var list = _fixture.CreateMany<TestClass>().ToList();
        list.Add(null);
        list.Add(new TestClass());

        var result = _converter.Convert(list, default);

        Assert.NotNull(result);
        Assert.Equal(list.Count, result.Count);
        for (int i = 0; i < list.Count; i++)
        {
            Assert.Equal(JsonSerializer.Serialize(list[i]), result[i]);
        }
    }

    [Fact]
    public void Convert_ReturnsNull_WithNullSource()
    {
        var result = _converter.Convert(null, default);

        Assert.Null(result);
    }
}
