using System.Net.Http.Headers;

namespace Corely.IAM.Web.FunctionalTests.Infrastructure;

public sealed record SentCookie(
    string Name,
    string Value,
    bool HttpOnly,
    bool Secure,
    string? SameSite,
    DateTimeOffset? Expires
)
{
    public bool IsDeletion =>
        string.IsNullOrEmpty(Value)
        || (Expires.HasValue && Expires.Value <= DateTimeOffset.UnixEpoch);
}

public sealed class CookieJar
{
    private readonly Dictionary<string, string> _cookies = [];

    public IReadOnlyDictionary<string, string> Cookies => _cookies;

    public string? this[string name] => _cookies.TryGetValue(name, out var v) ? v : null;

    public bool Contains(string name) => _cookies.ContainsKey(name);

    public string? ToHeaderValue() =>
        _cookies.Count == 0 ? null : string.Join("; ", _cookies.Select(c => $"{c.Key}={c.Value}"));

    public IReadOnlyList<SentCookie> Apply(HttpResponseHeaders headers)
    {
        var applied = new List<SentCookie>();
        if (!headers.TryGetValues("Set-Cookie", out var values))
            return applied;

        foreach (var raw in values)
        {
            var cookie = Parse(raw);
            if (cookie is null)
                continue;

            applied.Add(cookie);
            if (cookie.IsDeletion)
                _cookies.Remove(cookie.Name);
            else
                _cookies[cookie.Name] = cookie.Value;
        }

        return applied;
    }

    private static SentCookie? Parse(string raw)
    {
        var parts = raw.Split(';', StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return null;

        var nameValue = parts[0].Split('=', 2);
        if (nameValue.Length != 2)
            return null;

        var httpOnly = false;
        var secure = false;
        string? sameSite = null;
        DateTimeOffset? expires = null;

        foreach (var attribute in parts.Skip(1))
        {
            if (attribute.Equals("httponly", StringComparison.OrdinalIgnoreCase))
                httpOnly = true;
            else if (attribute.Equals("secure", StringComparison.OrdinalIgnoreCase))
                secure = true;
            else if (attribute.StartsWith("samesite=", StringComparison.OrdinalIgnoreCase))
                sameSite = attribute["samesite=".Length..];
            else if (attribute.StartsWith("expires=", StringComparison.OrdinalIgnoreCase))
            {
                if (
                    DateTimeOffset.TryParse(
                        attribute["expires=".Length..],
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.AssumeUniversal,
                        out var parsed
                    )
                )
                    expires = parsed;
            }
        }

        return new SentCookie(nameValue[0], nameValue[1], httpOnly, secure, sameSite, expires);
    }
}
