using System.Text.Json;
using AutoMapper;

namespace Corely.IAM.Mappers.AutoMapper.ValueConverters;

internal sealed class JsonifyListValueConverter<T> : IValueConverter<IEnumerable<T>, List<string>?>
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public JsonifyListValueConverter()
    {
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
    }

    public List<string>? Convert(IEnumerable<T> sourceMember, ResolutionContext context)
    {
        if (sourceMember == null)
        {
            return null;
        }
        return sourceMember
            .Select(s => JsonSerializer.Serialize(s, _jsonSerializerOptions))
            .ToList();
    }
}
