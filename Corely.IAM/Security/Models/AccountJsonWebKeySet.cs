using System.Text.Json.Serialization;

namespace Corely.IAM.Security.Models;

public record AccountJsonWebKeySet(
    [property: JsonPropertyName("keys")] IReadOnlyList<AccountJsonWebKey> Keys
);
