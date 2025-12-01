using System.Text.Json;
using AutoMapper;

namespace Corely.IAM.Mappers.AutoMapper.ValueConverters;

internal sealed class JsonifyValueConverter<T> : IValueConverter<T, string>
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public JsonifyValueConverter()
    {
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
    }

    public string Convert(T sourceMember, ResolutionContext context)
    {
        return JsonSerializer.Serialize(sourceMember, _jsonSerializerOptions);
    }
}
