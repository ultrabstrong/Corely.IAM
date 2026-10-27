using System.Security.Cryptography;
using Corely.IAM.ApiKeys.Entities;

namespace Corely.IAM.ApiKeys.Models;

internal sealed record ApiKeyCredential(string Prefix, string Secret)
{
    public const string SCHEME = "ciam";
    private const string PREFIX_ALPHABET = "abcdefghijklmnopqrstuvwxyz234567";

    public override string ToString() => $"{SCHEME}_{Prefix}_{Secret}";

    public static string NewPrefix() =>
        string.Create(
            ApiKeySecretEntity.PREFIX_LENGTH,
            PREFIX_ALPHABET,
            (span, alphabet) =>
            {
                for (var i = 0; i < span.Length; i++)
                    span[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
            }
        );

    public static bool TryParse(string? value, out ApiKeyCredential credential)
    {
        credential = new(string.Empty, string.Empty);

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var parts = value.Split('_', 3);
        if (parts.Length != 3 || parts[0] != SCHEME)
            return false;

        if (
            parts[1].Length != ApiKeySecretEntity.PREFIX_LENGTH
            || parts[1].Any(c => !PREFIX_ALPHABET.Contains(c))
        )
            return false;

        if (string.IsNullOrWhiteSpace(parts[2]))
            return false;

        credential = new(parts[1], parts[2]);
        return true;
    }
}
