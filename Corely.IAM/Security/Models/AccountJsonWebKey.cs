using System.Text.Json.Serialization;

namespace Corely.IAM.Security.Models;

public record AccountJsonWebKey
{
    public const string USE_SIGNATURE = "sig";
    public const string USE_ENCRYPTION = "enc";
    public const string KEY_TYPE_RSA = "RSA";
    public const string KEY_TYPE_ELLIPTIC_CURVE = "EC";

    [JsonPropertyName("kty")]
    public required string KeyType { get; init; }

    [JsonPropertyName("use")]
    public required string Use { get; init; }

    [JsonPropertyName("kid")]
    public required string KeyId { get; init; }

    [JsonPropertyName("alg")]
    public required string Algorithm { get; init; }

    [JsonPropertyName("n")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Modulus { get; init; }

    [JsonPropertyName("e")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Exponent { get; init; }

    [JsonPropertyName("crv")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Curve { get; init; }

    [JsonPropertyName("x")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? X { get; init; }

    [JsonPropertyName("y")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Y { get; init; }

    public static string KeyIdFor(Guid accountId, string use, int version) =>
        $"{accountId:N}-{use}-{version}";
}
